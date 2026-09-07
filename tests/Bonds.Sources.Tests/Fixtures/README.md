# Test material

`list-coi0930.txt` is the `pdftotext -layout` output of
`zrodla/listy_emisyjne/2026-09/file_409911.pdf`, which is issue letter
no. 86/2026 (COI0930). It sits here as text so that the letter-reader tests
need neither the PDF nor the `pdftotext` program.

This letter is here on purpose: it is where it came out that the effects of
early redemption sit in paragraph 26 and not in paragraph 28, as the August
issue files claimed. To recreate it after the letter changes:

```bash
pdftotext -layout zrodla/listy_emisyjne/2026-09/file_409911.pdf \
    tests/Bonds.Sources.Tests/Fixtures/list-coi0930.txt
```
