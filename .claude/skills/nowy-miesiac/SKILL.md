---
name: nowy-miesiac
description: Adds a new monthly treasury bond offer to the calculator and refreshes the market data. Use when a YYYY-MM directory with Ministry of Finance issue letters appears in zrodla/listy_emisyjne/, when stopy_procentowe_archiwum.xml or bazowa.xlsx is replaced in zrodla/nbp/, or when the reports in out/ need recomputing for a new month. Triggers (Polish): "nowa emisja", "nowe listy emisyjne", "nowy miesiąc", "odśwież dane rynkowe", "przelicz raporty".
---

# New monthly bond offer

Every month the Ministry of Finance issues eight bonds: OTS, ROR, DOR, TOS,
COI, ROS, EDO and ROD. Each issue is on sale for one calendar month, so one
month is one directory in three places:

```
zrodla/listy_emisyjne/2026-09/   issue letter PDFs, as a person downloaded them
data/issues/2026-09/             parameters copied from those letters, one file per issue
out/2026-09/                     reports computed for a purchase in that month
```

This skill's job is to bring those three directories into agreement.

## Step 1. Establish what changed

```bash
ls zrodla/listy_emisyjne/          # months that have letters
ls data/issues/                    # months that have parameters
ls -la zrodla/nbp/                 # dates of the NBP and Statistics Poland source files
```

A month present in `zrodla/listy_emisyjne/` and absent from `data/issues/`
needs steps 2–4. A replaced `.xml` or `.xlsx` file needs step 2.

Record every new third-party file (PDFs, replaced NBP files) in the manifest
[`zrodla/README.md`](../../../zrodla/README.md): the file, the letter number,
the issue and the address the person downloaded it from.

## Step 2. Refresh the market data

```bash
./bonds-dane --dry-run     # what would change
./bonds-dane               # write data/market/nbp-reference.json and cpi-yoy.json
```

The script does not care about the network: it reads only
`zrodla/nbp/stopy_procentowe_archiwum.xml` and `zrodla/nbp/bazowa.xlsx`. If
those files are stale, ask the human for fresh ones — `./bonds-dane --help`
prints the NBP page links. **Do not edit the files in `data/market/` by
hand**: they are generated and the next run of the script overwrites the
changes.

Check in the script's output that the last CPI reading and the last MPC
decision match what you expect.

## Step 3. Copy the parameters from the issue letters

For every PDF in the new directory:

```bash
./bonds-dane letter zrodla/listy_emisyjne/2026-09/file_409911.pdf --fields
```

This prints the letter number, the short issue name and the paragraphs that
decide each field. One paragraph in full: `--paragraph 26`. The annexes with
the formulas:

```bash
./bonds-dane letter zrodla/listy_emisyjne/2026-09/file_409911.pdf --annexes
```

The full "field → where it sits in the letter" table is in
[README, section "Nowa emisja"](../../../README.md#nowa-emisja). Read it
instead of reconstructing the mapping from memory.

Name the files like the earlier months: `NN-kod.json`, numbered by bond
length — `01-ots…`, `02-ror…`, `03-dor…`, `04-tos…`, `05-coi…`, `06-ros…`,
`07-edo…`, `08-rod…`.

### What not to copy from the previous month

Parameters are often identical month after month, but **four things change
independently of each other** and must be read from this specific letter:

| Field | Why |
|---|---|
| `source` | the letter number and its date are new, and **the paragraph numbering shifts between families and between months**. The effects of early redemption sit in paragraph 28 of the ROR letter and in paragraph 26 of the COI letter. Take the numbers from the output of `./bonds-dane letter --fields`, not from last month's file |
| `sale.from`, `sale.to` | the first and last day of the sale month; February and 30-day months end differently |
| `rate.firstPeriodRatePercent` | the first-period rate is set administratively for every issue separately and really does change |
| `rate.marginPercent` | the margin changes less often, but it changes |

The remaining fields (`periodsPerYear`, `periodCount`, `capitalizes`,
`roundCapitalizedBase`, `principalFloor`, `fee.type`) follow from how the bond
family is constructed and are stable in practice — confirm them in the letter
anyway, because a mistake in any of them silently corrupts every amount. The
four most confusing ones (`capitalizes`, `fee.type`, `roundCapitalizedBase`,
`principalFloor`) each have a dedicated paragraph in the README with the exact
wording of the clauses.

Do **not** enter the exchange price (99.90 zloty): the calculator models only
a cash purchase at a price equal to the nominal and rejects a file with a
lower price at load time.

## Step 4. Check and compute

```bash
dotnet test Bonds.sln --configuration Release --verbosity quiet --nologo
```

`ShippedIssueMonthsTests` guards the directory layout: every month has eight
families, the sale window in the files matches the directory name, and no
issue code repeats across two months. This catches a file dropped into the
wrong directory and an overlooked sale date.

Then the accrual walkthrough of every new issue:

```bash
./bonds explain --issue COI0930 --purchase 2026-09-01
```

Read the **first row of the table**: only it rests purely on the letter's
data. `Wartość na koniec` should equal
`100 zł + 100 zł × rate ÷ periods per year`. The `Kupon` column verifies
`capitalizes`: with `true` it shows 0,00, with `false` the payout amount.

The short-term letters state a ready-made result to compare against: the OTS
letter computes 0,50 zł of interest and a 100,50 zł claim, the TOS letter —
113,79 zł on the redemption day. Check those numbers if the letter contains
them.

Finally the reports:

```bash
./bonds-all                       # every month in data/issues/
./bonds-all --month 2026-09       # only the new month
```

## What to report to the human

- every parameter that differs from the previous month — above all the
  first-period rate and the margin;
- every field that could not be read from the letter unambiguously, with the
  number of the paragraph you searched;
- how many interest periods are a projection rather than data. For the
  twelve-year ROD bond that is usually eleven of twelve, and the table marks
  them with an asterisk.

Do not guess a missing value and do not insert a default: `JsonDataStore`
deliberately fails with the file name and the field name, so that a bad file
never turns into a bad number.
