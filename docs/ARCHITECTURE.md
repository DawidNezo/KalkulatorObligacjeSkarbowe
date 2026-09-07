# How it calculates

Internal documentation. The user-facing description is in the
[README](../README.md).

- [One formula for eight bonds](#one-formula-for-eight-bonds)
- [Schedule of interest periods](#schedule-of-interest-periods)
- [Interest rates](#interest-rates)
- [Rounding](#rounding)
- [Fee and principal protection](#fee-and-principal-protection)
- [Tax](#tax)
- [Amounts and numeric types](#amounts-and-numeric-types)
- [Code structure](#code-structure)
- [Issue file format](#issue-file-format)
- [Tests](#tests)

Basis: issue letters no. 73–80/2026 of the Minister of Finance and Economy of
21 July 2026, in the `zrodla/listy_emisyjne/` directory.

---

## One formula for eight bonds

Every issue letter defines the interest amount of a period and the amount paid
on early redemption. After substituting the eight parameter sets, one
expression remains, implemented in
[`RedemptionCalculator.Settle`](../src/Bonds.Core/Engine/RedemptionCalculator.cs):

```
WP = N · Π(1 + r_j,  j < k) · (1 + r_k · a / (ACT · F)) − b     [floor: WP ≥ 100]
O  = N · r · a / (ACT · F)                                      [full period ⇒ N·r/F]
```

| Symbol | Meaning |
|---|---|
| `N` | nominal value, 100 zloty |
| `F` | interest periods per year: 12, 4 or 1 |
| `k` | index of the period that settles the exit day |
| `a` | days from the period start inclusive to the exit day exclusive |
| `ACT` | actual number of days in the period, last day exclusive |
| `Π` | capitalisation product; equal to 1 for coupon-paying bonds |
| `b` | early redemption fee |

The eight issues differ only in flag values:

| Issue | F | Periods | Capitalises | Fee rule | 100 zł floor | Base rounding | Rate rule |
|---|--:|--:|:-:|---|---|:-:|---|
| OTS1126 | 4 | 1 | no | `forfeitAllInterest` | `None` | — | `fixed` |
| ROR0827 | 12 | 12 | no | `twoTier` | `FirstPeriodOnly` | — | `nbpReference` |
| DOR0828 | 12 | 24 | no | `twoTier` | `FirstPeriodOnly` | — | `nbpReference` |
| TOS0829 | 1 | 3 | yes | `accruedCapped` | `AllPeriods` | yes | `fixed` |
| COI0830 | 1 | 4 | no | `twoTier` | `FirstPeriodOnly` | — | `inflation` |
| ROS0832 | 1 | 6 | yes | `accruedCapped` | `AllPeriods` | no | `inflation` |
| EDO0836 | 1 | 10 | yes | `accruedCapped` | `AllPeriods` | no | `inflation` |
| ROD0838 | 1 | 12 | yes | `accruedCapped` | `AllPeriods` | no | `inflation` |

OTS fits this formula as `F = 4` with a single period:
`100 · 2% / 4 = 0.50 zloty`, exactly the amount in paragraph 14 of letter
no. 73/2026.

Coupons and redemption values use no divisor of 365. The annexes give
`a/(ACT·F)`, and a full period means `a = ACT`, so a coupon is `N·r/F`. The
monthly ROR coupon is `r/12`, not `r · days/365` — the two formulas differ for
February.

---

## Schedule of interest periods

Boundaries are counted from the purchase date, never from the previous
boundary. The day-of-month of the purchase date is the anchor, and a too-short
month clips only that one boundary. For a purchase on 31 August 2026 (annex 3
of the ROR letter):

| Period | Boundaries | What happens |
|---|---|---|
| 1 | `31.08 → 30.09` | September has 30 days, boundary clipped |
| 2 | `30.09 → 31.10` | the anchor (31) returns |
| 6 | `31.01 → 28.02` | February has 28 days, boundary clipped |
| 7 | `28.02 → 31.03` | the anchor returns |

`purchase.AddMonths(k)` produces this result. `AddMonths(1)` in a loop over
consecutive boundaries loses the anchor after February and derails the schedule
for purchases on days 29–31 of a month. Implementation:
[`SchedulePlanner`](../src/Bonds.Core/Engine/SchedulePlanner.cs).

The exit day is settled by the period for which `Start < day ≤ End`. The
purchase day settles no period.

---

## Interest rates

The first-period rate is stated in the letter. The following ones come from a
rule:

| Rule | Formula |
|---|---|
| `fixed` | constant for the whole life of the bond |
| `nbpReference` | NBP reference rate + margin |
| `inflation` | 12-month CPI index + margin |

In both variable rules `r = i + m`, and for `i < 0` the value `i = 0` is
taken. The floor sits on the **index**, not on the rate: under deflation the
rate falls to the margin, not to zero. Writing `max(0, i + m)` would give the
same result for positive inflation and a wrong one for deflation.

Two ways of reading the index, both easy to get wrong by one step:

- **CPI** — the index announced by Statistics Poland in the month *preceding*
  the first month of the interest period. The `cpi-yoy.json` file is keyed by
  the month of **announcement**, not the month the index concerns.
- **NBP** — the reference rate as of the **10th business day** before the first
  day of the calendar month in which the period starts. Hence
  [`PolishBusinessDayCalendar`](../src/Bonds.Core/Calendar/PolishBusinessDayCalendar.cs)
  with the public holidays and movable Easter computed with the Gauss–Meeus
  algorithm. Christmas Eve is a holiday only from 2025 on (Dz.U. 2024
  poz. 1965) and the calendar must tell the years apart — an unconditional
  holiday would shift every historical December fixing by one day.

The market series carry a `knownThrough` field. A query for a later date
returns the value assumed by the scenario and is marked as a projection, which
the report shows with an asterisk.

---

## Rounding

The issue letters are not uniform here, and that is the sole reason the
`roundCapitalizedBase` flag exists:

| Issue | Capitalisation base `N_{k−1}` |
|---|---|
| TOS, annex 2 | rounded to 2 places, computed from the closed form `100·(1+r)^{k−1}` |
| EDO, ROS, ROD, annexes 2 and 3 | no intermediate rounding; the whole product in full precision, rounded once |

Redemption at maturity always uses the full product with no intermediate
rounding, per annex 1 of the TOS letter and annex 2 of the EDO, ROS and ROD
letters.

Amounts round half away from zero (`MidpointRounding.AwayFromZero`), like the
two-place amounts in the letters.

---

## Fee and principal protection

Three fee rules:

| Rule | Behaviour |
|---|---|
| `twoTier` | period 1: `min(accrued interest, fee)`. From period 2: the full fee |
| `accruedCapped` | always `min(accrued interest, fee)` |
| `forfeitAllInterest` | no fee, but all interest is forfeited (paragraph 23 point 3 of the OTS letter), redemption at nominal |

The 100 zloty floor has two scopes. `AllPeriods` holds in every period,
`FirstPeriodOnly` only in the first.

`FirstPeriodOnly` together with `twoTier` means that from the second period the
payment can fall below nominal: for DOR an exit in the second month pays
`100 + period-2 coupon − 0.70`, at today's NBP rate of 3.75% + 0.15% margin
`100 + 0.33 − 0.70 = 99.63 zloty`. **This agrees with the ROR, DOR and COI
letters.** Do not fix it by extending the floor to all periods.

The early redemption window (7 days after purchase, 20 days before maturity)
is a deliberate simplification: the letters apply these limits to the day the
**instruction is placed**, and the calculator checks them on the payment day.
On a monthly grid the distance from the boundaries always exceeds both limits,
so no row changes because of this; a daily grid would have to model the
instruction and the five business days to payment.

---

## Tax

The issue letters say nothing about tax. The rate follows from article 30a(1)(2)
of the Polish PIT act, and the rounding from article 63 **§1a** of the Tax
Ordinance (Ordynacja podatkowa): the base and the tax on interest from
securities round **up to full grosze**. The popular "to full zloty" rule of
article 63 §1 does not cover this tax — applying it here is a common mistake;
§1a was added in 2012 precisely to close the one-day-deposit loophole.

```
base = max(0, gross interest − fee)              [for the whole position]
tax  = ceil_to_grosz(19% · base)                 [per payment, separately]
```

The fee reduces the base. A base of zero or less gives zero tax; a loss on one
bond generates no refund. Collection happens per payment on the whole position
at once — the way the issuing agent does it — so the tax on a position is not a
multiple of the tax on one unit: `ceil(0.19 · 2.31) = 0.44 zloty`, not
`7 · 0.07 = 0.49 zloty`.

The moment of collection follows from the bond type, not from a separate
parameter. Coupon bonds pay on every payout, capitalising bonds once, at
redemption, on the whole interest. The tax deferral of the second group falls
out of the model by itself.

The purchase price must equal the nominal. A lower price (exchange purchase,
99.90 zloty) means a discount, which is also taxed at redemption — that tax is
not modelled, so the validation in
[`BondIssue.Validated`](../src/Bonds.Core/Domain/BondIssue.cs) rejects such
input instead of showing an inflated net profit.

---

## Amounts and numeric types

Amounts are `long` grosze in the [`Money`](../src/Bonds.Core/Money.cs) type, so
binary floating point error has no way to occur. Multiplying `Money` by a rate
returns `decimal`, not `Money` — the caller must decide explicitly where to
round.

The only place with `double` is
[IRR](../src/Bonds.Core/Engine/InternalRateOfReturn.cs), where the discount
exponent is not an integer. The result there is a comparison measure, never an
amount. IRR is computed by bisection, not Newton's method: slower, but it
always converges or explicitly returns no result. The discount year has a
conventional 365 days (the XIRR convention, leap years ignored), and the rate
is searched up to 1000% a year — beyond that the result is an explicit absence,
a dash in the report.

Number formatting has one place, `ReportFormat`, with an explicit `pl-PL`
culture. Rule `CA1305` is raised to a build error so that no output depends on
the locale of the machine.

---

## Code structure

The tree lists the files that carry decisions; small helper types (result
records, exceptions, JSON documents) are left out.

```
src/Bonds.Core/
├─ Money.cs                    amounts as a whole number of grosze
├─ YearMonth.cs                calendar month, the key of the CPI series
├─ CommandLine/                mechanics shared by the two hand-written CLI parsers
├─ Domain/                     issue parameters and rules
│  ├─ BondIssue.cs             everything from the issue letter, nothing from code
│  ├─ BondKind.cs              issue family; a label only, drives no logic
│  ├─ RateRule.cs              fixed | nbpReference | inflation
│  ├─ FeeRule.cs               twoTier | accruedCapped | forfeitAllInterest
│  ├─ InterestPeriod.cs        period boundaries, a and ACT
│  ├─ PrincipalFloor.cs        scope of principal protection
│  └─ EarlyRedemptionWindow.cs the 7 and 20 calendar day window
├─ Calendar/                   Polish business days
├─ Market/                     NBP and CPI series, projection past the data
├─ Engine/                     the mechanism
│  ├─ SchedulePlanner.cs       schedule anchored on the purchase day
│  ├─ BondPlan.cs              issue, purchase date and rates resolved once
│  ├─ RedemptionCalculator.cs  the formulas of the annexes
│  ├─ TaxCalculator.cs         19%, up to the grosz (article 63 §1a)
│  ├─ ExitEvaluator.cs         the month-by-month table
│  └─ InternalRateOfReturn.cs  IRR by bisection
├─ Data/                       JSON loading with explicit validation
│  ├─ JsonDataStore.cs         reading and validating issue files, series and the scenario
│  ├─ MonthlyIssueDirectory.cs picks the month directory by purchase date
│  └─ DataRootLocator.cs       finds the data/ directory walking up the tree
├─ Reporting/                  Markdown, CSV, JSON, matrix, explain
└─ Application/                BondCalculator, the facade for the CLI

src/Bonds.Cli/                 CliApplication, argument parser, Program.cs
                               → the `bonds` binary, reads data/ and writes reports

src/Bonds.Sources/             readers of the NBP, Statistics Poland and MF source documents
├─ Market/
│  ├─ NbpReferenceArchive.cs   XML of the NBP rate archive
│  ├─ OpenXmlWorkbook.cs       number cells of an .xlsx sheet, without a package
│  ├─ CpiWorkbook.cs           the CPI column of the NBP core inflation workbook
│  └─ MarketSeriesWriter.cs    renders data/market/*.json
├─ Letters/
│  ├─ PdfText.cs               PDF to text through pdftotext
│  ├─ IssueLetter.cs           letter text into numbered paragraphs
│  └─ IssueFieldHints.cs       which paragraph decides which issue file field
└─ SourcesApplication.cs       → the `bonds-dane` binary, writes into data/
```

Dependencies point one way: `Cli → Application → Engine → Domain → Money`. The
`Domain` layer knows nothing of JSON, files or formatting.

`Bonds.Sources` stands beside, not beneath: from `Bonds.Core` it takes only
`YearMonth`, `DataRootLocator` and the shared parser mechanics of
`CommandLine/`, and in tests also `JsonDataStore`, with which it reads back
what it wrote. The calculation engine does not know `.xlsx` or PDFs exist,
because no computation needs them. The direction is opposite to the data flow:
`Bonds.Sources` **produces input files** and `Bonds.Cli` reads them. The two
executables never see each other.

Zero packages in production code holds here too. An `.xlsx` file is a zip of
XML, and `ZipArchive` and `XDocument` are in the base library. The only
external dependency is the `pdftotext` program of poppler, called as a process
and only by the `bonds-dane letter` command; the calculator works without it.

Rates are resolved once, when a `BondPlan` is built. The exit table queries
that plan instead of recomputing from scratch for every month.

Diagnostics come from the `explain` command, which prints every period, its
rate, the source of the rate and the bond value at the end of the period.

---

## Issue file format

What every field means and where in the issue letter its value comes from is
described in [README, section "Nowa emisja"](../README.md#nowa-emisja). Here
stays only what concerns the loading itself.

Issue files live in `data/issues/<YYYY-MM>/`, one directory per sale month. An
issue is on sale for one calendar month, and all issues loaded together must be
purchasable on the same day — `BondIssue.EnsureCanBuyOn` rejects a date outside
the sale window. The split into directories thus follows from how the issues
are constructed, not from repository housekeeping.

[`MonthlyIssueDirectory`](../src/Bonds.Core/Data/MonthlyIssueDirectory.cs)
picks the directory by purchase date. The rule is single and without
exceptions: a directory that contains `*.json` files is taken as it is, and a
directory that contains `YYYY-MM` subdirectories is a set of months, of which
the month of the purchase date is taken. An explicit path to a single month
(`--issues data/issues/2026-09`) and a flat directory of own files thus keep
working, and the error "no issue directory for YYYY-MM" lists the months that
do exist.

The market series in `data/market/` are **generated** by `./bonds-dane` from
the NBP and Statistics Poland source files in `zrodla/nbp/`; a manual edit is
lost on the next run of the script. The loader treats their format like any
other: a gap in a monthly series or a point past `knownThrough` is a data
error.

The file schema is mirrored by the classes in
[`Documents.cs`](../src/Bonds.Core/Data/Documents.cs) — one type per JSON file
and nothing more. The mapping to domain objects, with all the validation, is
done by [`JsonDataStore`](../src/Bonds.Core/Data/JsonDataStore.cs).

Every document field is nullable, and being required is asserted explicitly by
`Required(...)` while constructing `BondIssue`. There are no non-nullable types
with a default value: a `bool` defaulting to `false` would pass validation
silently and change the mechanics of an issue — exactly the bug the
`roundCapitalizedBase` field once had.

Percent values are written the way the letters and Statistics Poland releases
write them: `5.35` means 5.35%. The conversion to a fraction lives in one
place, `PercentDivisor`.

The loader rejects with `DataLoadException` — always naming the file — each of
these cases: a missing field (two exceptions: `rate.marginPercent` with
`type: fixed` and `fee.amountZloty` with `type: forfeitAllInterest`), a field
the schema does not know (a typo in a name), an unknown `type` value, an issue
code repeated in two files, a duplicated key in a market series, a gap in a
monthly series before `knownThrough`, a series point past `knownThrough`, and
a file that fails domain validation (e.g. a purchase price different from the
nominal).

---

## Tests

```bash
dotnet test
```

370 tests in three projects (`Bonds.Core.Tests` for the engine,
`Bonds.Cli.Tests` for the command line, `Bonds.Sources.Tests` for the source
document readers) and five layers.

**Golden tests from the issue letters.** Numbers straight from the documents,
checked to the grosz. The schedule of annex 3 of the ROR letter for purchase
days 29, 30 and 31 — the only ones where month clipping changes the result —
plus the boundary on 29 February of a leap year. OTS: 0.50 zloty of interest
and a 100.50 zloty redemption. TOS: `100·(1.044)³` and the bases `N₁` and `N₂`
of annex 2. DOR: a 0.35 zloty coupon and a 99.65 zloty payment in the second
month (the test holds the rate at 4.15% in period 2 as well, so the number
differs from a report computed on the current NBP rate). EDO, ROS and ROD: the
product with no intermediate rounding. Monthly coupon `r/12`, yearly coupon
`N·r`.

**Invariants on a parameter grid.** Six purchase days times four index levels
times eight issues. Principal protection wherever it applies. The fee never
above accrued interest. Interest never negative. The value of a capitalising
bond never falls over time. The value at the end of the last period equals the
redemption value. Deflation does not take the nominal. CPI at 20% for twelve
years does not overflow an amount.

Note: the "value never falls" invariant covers capitalising bonds only. A
coupon bond returns to nominal at every period boundary.

**Unit tests.** `Money` rounding (halves away from zero and the ceiling of
article 63 §1a), tax on a non-positive base and on a whole-position payment,
the rate and fee rules, the calendar with Christmas Eve from 2025 and the
December fixing, IRR positive, zero, negative and with no solution, redemption
windows, period boundaries, `BondIssue` validation, JSON loading and its
errors, month directory selection, locating the `data` directory. In
`Bonds.Cli.Tests`: the command line parser and the whole `CliApplication` run —
exit codes 0/1/2, `--help`, `--out` writing including its failures, the BOM in
CSV. In `Bonds.Sources.Tests` the same for `SourcesApplication`, including
`--dry-run`, which writes nothing, and a re-run that does not touch an
unchanged file.

**Data layout in the repository.** `ShippedIssueMonthsTests` checks every month
directory: eight bond families, a sale window equal to the whole month named by
the directory, a `source` field pointing at an issue letter, and no code
repeated across two months. Without this, a file dropped into the wrong
directory would surface only as a refusal to price a purchase.

**Source documents.** `Bonds.Sources.Tests` guards two rules that are invisible
in the output, so a mistake in them would report itself nowhere: the shift of
the CPI key from the month a reading concerns to the month of its announcement,
and the subtraction of the base 100 from the Statistics Poland index. Each of
these tests reads the rendered text back with `JsonDataStore`, the only reader
that counts. A separate test renders the series under `CurrentCulture` set to
`pl-PL`, `de-DE`, `en-US` and `ar-SA`: a locale with a decimal comma would
break the JSON, and a non-Gregorian calendar would write a different year.

The issue letter reader is tested on the text of the real COI0930 letter
(`Fixtures/list-coi0930.txt`), so the tests need neither a PDF nor `pdftotext`.
They check that the paragraphs number from 1 without a break and that every
issue file field points at the paragraph that decides it — including `fee.type`
at paragraph 26, where the bug in the August files came out. A separate test
guards the form-feed character: `pdftotext` inserts it without a line break, so
the first paragraph of every page is lost, and all the following ones with it,
because the number sequence snaps.

**Snapshots.** The Markdown, CSV, JSON, matrix and `explain` output, compared
against the files in `tests/Bonds.Core.Tests/Snapshots/`. The per-issue
snapshot covers four issues (one per combination of payout and fee, 103 rows),
the IRR matrix all eight, the profit matrix the same four. At that number of
rows a silent change of one number is invisible to a person, and the snapshot
catches it.

A snapshot is updated by deleting the file and re-running the tests. On a
difference the test writes `*.received` next to the expected file.
