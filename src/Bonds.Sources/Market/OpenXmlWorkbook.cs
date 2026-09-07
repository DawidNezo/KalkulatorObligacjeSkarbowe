using System.Collections.Immutable;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace Bonds.Sources.Market;

/// <summary>
/// Reads the number cells of one sheet of an .xlsx file.
/// <para>
/// An .xlsx file is a zip of XML parts, and both the zip reader and the XML reader
/// are in the base library. Thus the whole format needs no package. This class
/// reads number cells only: a cell with a "t" attribute other than "n" holds a
/// string, and no column that this tool reads holds one. Skipping strings also
/// means that sharedStrings.xml never has to be read.
/// </para>
/// </summary>
public sealed class OpenXmlWorkbook : IDisposable
{
    private static readonly XNamespace Sheet =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static readonly XNamespace OfficeRelationship =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly XNamespace PackageRelationship =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    private readonly ZipArchive archive;
    private readonly string path;

    private OpenXmlWorkbook(ZipArchive archive, string path)
    {
        this.archive = archive;
        this.path = path;
    }

    public static OpenXmlWorkbook Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new SourceException(path, "plik nie istnieje");
        }

        try
        {
            return new OpenXmlWorkbook(ZipFile.OpenRead(path), path);
        }
        catch (InvalidDataException error)
        {
            throw new SourceException(path, "plik nie jest arkuszem .xlsx", error);
        }
    }

    public void Dispose() => archive.Dispose();

    /// <summary>
    /// The rows of one sheet, each as a map from column letter to the raw cell
    /// value. A row with no number cell comes back empty and not missing, so that
    /// the caller keeps the row numbers of the sheet.
    /// </summary>
    public ImmutableArray<ImmutableDictionary<string, string>> NumberRowsOf(string sheetName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sheetName);

        var sheet = Part(SheetPartOf(sheetName));
        var rows = ImmutableArray.CreateBuilder<ImmutableDictionary<string, string>>();
        foreach (var row in sheet.Descendants(Sheet + "row"))
        {
            var cells = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
            foreach (var cell in row.Elements(Sheet + "c"))
            {
                // A "t" attribute other than "n" marks a cell that is not a plain
                // number. Excel leaves the attribute out for numbers, but the
                // format allows an explicit t="n" and another producer may write it.
                var type = (string?)cell.Attribute("t");
                if (type is not null && type != "n")
                {
                    continue;
                }

                var value = cell.Element(Sheet + "v")?.Value;
                var reference = (string?)cell.Attribute("r");
                if (value is null || reference is null)
                {
                    continue;
                }

                cells[ColumnOf(reference)] = value;
            }

            rows.Add(cells.ToImmutable());
        }

        return rows.ToImmutable();
    }

    /// <summary>
    /// The part that holds one sheet. The name of a sheet points at a relationship
    /// identifier, and the relationship points at the part, thus the lookup takes
    /// two steps.
    /// </summary>
    private string SheetPartOf(string sheetName)
    {
        var workbook = Part("xl/workbook.xml");
        var sheets = workbook.Descendants(Sheet + "sheet").ToList();
        var wanted = sheets.SingleOrDefault(s => (string?)s.Attribute("name") == sheetName)
            ?? throw new SourceException(path,
                $"nie ma arkusza '{sheetName}'; są: "
                + string.Join(", ", sheets.Select(s => (string?)s.Attribute("name") ?? "?")));

        var id = (string?)wanted.Attribute(OfficeRelationship + "id")
            ?? throw new SourceException(path, $"arkusz '{sheetName}' nie ma identyfikatora relacji");

        var target = Part("xl/_rels/workbook.xml.rels")
            .Descendants(PackageRelationship + "Relationship")
            .Where(r => (string?)r.Attribute("Id") == id)
            .Select(r => (string?)r.Attribute("Target"))
            .FirstOrDefault()
            ?? throw new SourceException(path, $"relacja '{id}' nie wskazuje na żaden plik");

        // A relationship target is normally relative to xl/, but the format also
        // allows a path absolute from the package root, such as "/xl/worksheets/...".
        return target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
    }

    private XDocument Part(string entryName)
    {
        var entry = archive.GetEntry(entryName)
            ?? throw new SourceException(path, $"w arkuszu nie ma części '{entryName}'");

        try
        {
            using var stream = entry.Open();
            return XDocument.Load(stream);
        }
        catch (XmlException error)
        {
            throw new SourceException(path, $"'{entryName}' nie jest poprawnym XML ({error.Message})", error);
        }
    }

    /// <summary>The letters of a cell reference such as "B12".</summary>
    private static string ColumnOf(string reference) =>
        new([.. reference.TakeWhile(char.IsAsciiLetterUpper)]);
}
