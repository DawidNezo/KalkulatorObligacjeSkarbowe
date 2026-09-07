# Code standards

Internal documentation. The user-facing description is in the
[README](../README.md), the calculation mechanism in
[ARCHITECTURE](ARCHITECTURE.md).

- [Principles](#principles)
- [Build gates](#build-gates)
- [Deviations from the analysis rules](#deviations-from-the-analysis-rules)

---

## Principles

SOLID, KISS, DRY, YAGNI, Clean Code and TDD, written down as the concrete
decisions of this repository.

**Simplicity.** One calculation engine instead of eight calculators. Zero
dependencies in production code. The command line parser is written by hand:
a package would add nothing here and could break on an update.

**One language, one set of gates.** The whole repository is C# on .NET 10 plus
three shell scripts that only build and run. `Bonds.Sources` reads `.xlsx`,
`.xml` and PDFs without a single package: `ZipArchive` and `XDocument` are in
the base library, and a PDF is unpacked by `pdftotext` called as a process.

This decision replaced an earlier, worse one: the same converters first lived
in Python, "to keep OOXML parsing out of production code". The justification
was false — Python parsed OOXML just the same, only in a second language. The
real cost was different: `data/market/*.json` feeds **every number in every
report**, and the files were produced by code with no tests, no analyzers and
no locale guards, in a repository that holds `CA1305` as an error. The port
surfaced a bug that the Python version lacked only by accident: `pdftotext`
emits the form-feed character without a line break, so the first paragraph of
every page was lost.

**Stay with the patterns that already exist.** A new rate or fee rule is a new
record deriving from `RateRule` or `FeeRule`, exactly like the three existing
ones. A new output format implements `IReportWriter`. Do not introduce a second
way of doing the same thing.

**Design patterns only where they solve a real problem.** Strategy for
`RateRule`, `FeeRule` and `IReportWriter`, because the issue letters really do
define interchangeable rules. Facade in `BondCalculator`. There are no
factories, builders or observers, because there is no problem for them.

**Composition over inheritance.** `BondIssue` is composed of `RateRule`,
`FeeRule`, `PrincipalFloor` and `EarlyRedemptionWindow`. There is no
`EdoBond : Bond`. Hierarchies exist only where they model a sum of variants.

**Explicit validation and error handling.** Every domain constructor checks its
invariants. `JsonDataStore` raises `DataLoadException` with the file name and
the name of the missing field; unknown fields, duplicates and gaps in a series
are data errors too. The CLI maps exceptions to exit codes: `0` success, `1`
usage error (including a `--out` write failure), `2` data error. Messages that
a user can see are in Polish and carry no `(Parameter '...')` suffix.

**No magic values.** `Money.GroszePerZloty`, `BondIssue.MonthsPerYear`,
`TaxCalculator.StandardRate`, `NbpReferenceLinkedRate.BusinessDaysBeforeMonth`,
`InflationLinkedRate.PublicationMonthsBeforePeriod`. Issue parameters are not
in the code at all — they live in the JSON files.

**No speculative optimisation.** Rates are resolved once per `BondPlan`,
because the table asks for them hundreds of times. Beyond that, no caches. IRR
is computed by bisection, not Newton's method: slower, but it always converges.

**Do not invent APIs or requirements.** Every formula carries a comment that
points at the paragraph or annex of the issue letter. The `source` field of an
issue file is required.

**Two languages, one boundary.** Everything the calculator's user reads is
Polish: the README, the CLI help and messages, the reports. Everything a
developer reads is English: identifiers, comments (plain sentences in the
spirit of ASD-STE100: affirmative, one thought per sentence), the documents in
`docs/`, the skill files, the notices, the comments in the shell scripts. One
comment crosses the line: the header of `bonds-all` is printed verbatim by its
`usage()` as the `--help` output, so it is user-facing and Polish. Quotations
from the issue letters stay in Polish — they are evidence, not prose.

**Mind records and `with`.** A `with` expression does not run the record's
constructor body. A value derived from other fields must be a computed
property, not a field initialised in the constructor — otherwise it goes stale
after `with`. Example: `BondIssue.PeriodMonths`.

---

## Build gates

Set in [`Directory.Build.props`](../Directory.Build.props) and
[`.editorconfig`](../.editorconfig), binding for every project. A build with
any warning does not pass.

| Setting | Effect |
|---|---|
| `TreatWarningsAsErrors` | every compiler warning stops the build |
| `WarningLevel 9999` | enables all compiler warning levels |
| `Nullable enable` | possible-`null` warnings are errors |
| `EnableNETAnalyzers`, `AnalysisLevel latest-recommended` | .NET analyzers, the `CA*` rules |
| `CodeAnalysisTreatWarningsAsErrors` | `CA*` rules stop the build too |
| `EnforceCodeStyleInBuild` | `IDE*` rules from `.editorconfig` checked in the build, not only in the IDE |
| `GenerateDocumentationFile` | required for `IDE0005` to run in the build |
| `Deterministic`, `ContinuousIntegrationBuild` | the same input produces an identical output file; `ContinuousIntegrationBuild` turns on only with `CI=true` |
| `NuGetAuditMode all` | security audit of transitive dependencies as well |

`CA1305`, locale-dependent formatting, is an error on purpose: the whole output
of the calculator must look identical on every machine. Every interpolated
`AppendLine` in the writers passes the culture explicitly, and all numbers and
dates go through `ReportFormat`, the only place that knows `pl-PL`.

---

## Deviations from the analysis rules

| Rule | Decision | Reason |
|---|---|---|
| `CS1591` | silenced | demands an XML comment on every public member. Comments in this repository explain the issue letters and go where they add something |
| `CA1716` | disabled | the parameter name `date` collides with the `Date` keyword of VB. This library is not consumed from VB |
| `CA1822` | silenced on `JsonDataStore` and `RedemptionCalculator` | the methods are stateless, but the classes are injected as collaborators. A static method cannot be substituted in a test or swapped for another implementation. The suppression is an attribute on the type, with the reason in code |
| `NU1901`–`NU1904` | warning, not error | a new security advisory must not break a build that has no network |
