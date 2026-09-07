# Source documents

Everything in this directory comes from third parties and is **not covered by
the Apache 2.0 license of this repository** (the `LICENSE` file in the root).
The rest of the repository — the code, the documentation, the issue parameters
in `data/issues/`, the scenarios and the reports in `out/` — is the project's
own work and the license does cover it.

The files live here so that every number of the calculator can be reproduced
and checked against its source, without a network.

## Legal status

- The issue letters and announcements of the Minister of Finance are **official
  documents and materials**. Under article 4(2) of the Polish copyright act
  (ustawa o prawie autorskim i prawach pokrewnych) they are not subject to
  copyright.
- The NBP files are published statistical data (interest rates, core inflation)
  made publicly available on the NBP websites.
- Trademarks and names remain the property of their owners.

## `listy_emisyjne/` — issue letters of the Minister of Finance

Downloaded from [obligacjeskarbowe.pl](https://www.obligacjeskarbowe.pl/), file
names as the site served them. The directory name is the sale month.

### 2026-08 (letters no. 73–80/2026 of 21 July 2026)

| File | Letter | Issue | Downloaded from |
|---|---|---|---|
| `file_409736.pdf` | no. 73/2026 | OTS1126 | [media_files/91bfd7cb…](https://www.obligacjeskarbowe.pl/media_files/91bfd7cb-d1b0-463e-83d3-30a223f0b8a0.pdf) |
| `file_409938.pdf` | no. 74/2026 | ROR0827 | [media_files/0d1fc5e4…](https://www.obligacjeskarbowe.pl/media_files/0d1fc5e4-04ad-429e-b946-c67d3cacd00c.pdf) |
| `file_409923.pdf` | no. 75/2026 | DOR0828 | [media_files/eaac2bdc…](https://www.obligacjeskarbowe.pl/media_files/eaac2bdc-34d5-4754-9866-673cb3508c65.pdf) |
| `file_409755.pdf` | no. 76/2026 | TOS0829 | [media_files/0f23ffd0…](https://www.obligacjeskarbowe.pl/media_files/0f23ffd0-a3d6-467d-b3cc-c3ffcc190ecd.pdf) |
| `file_409748.pdf` | no. 77/2026 | COI0830 | [media_files/3003cc7d…](https://www.obligacjeskarbowe.pl/media_files/3003cc7d-bb13-4008-8479-3793d6426fb4.pdf) |
| `file_409750.pdf` | no. 78/2026 | EDO0836 | [media_files/e1c62af6…](https://www.obligacjeskarbowe.pl/media_files/e1c62af6-021b-492a-b8cb-15336a05a72c.pdf) |
| `file_409754.pdf` | no. 79/2026 | ROS0832 | [media_files/d866d60c…](https://www.obligacjeskarbowe.pl/media_files/d866d60c-2d69-4d0e-91e5-0405e9f1361e.pdf) |
| `file_409751.pdf` | no. 80/2026 | ROD0838 | [media_files/6604510b…](https://www.obligacjeskarbowe.pl/media_files/6604510b-a341-4020-b781-d0ed60e774bb.pdf) |

### 2026-09 (letters no. 82–89/2026 of 20 August 2026)

| File | Letter | Issue | Downloaded from |
|---|---|---|---|
| `file_409917.pdf` | no. 82/2026 | OTS1226 | [media_files/778f24eb…](https://www.obligacjeskarbowe.pl/media_files/778f24eb-e13d-4d64-9b59-93a64c3ae9d2.pdf) |
| `file_409975.pdf` | no. 83/2026 | ROR0927 | [media_files/ec193ea6…](https://www.obligacjeskarbowe.pl/media_files/ec193ea6-fcd0-497a-a328-a7d9b2cc9d06.pdf) |
| `file_409914.pdf` | no. 84/2026 | DOR0928 | [media_files/af0e7a4e…](https://www.obligacjeskarbowe.pl/media_files/af0e7a4e-d836-465a-b7e9-8f26f727ff33.pdf) |
| `file_409973.pdf` | no. 85/2026 | TOS0929 | [media_files/68f35a7e…](https://www.obligacjeskarbowe.pl/media_files/68f35a7e-781c-4fab-82af-3f87b2ef7294.pdf) |
| `file_409911.pdf` | no. 86/2026 | COI0930 | [media_files/21db12fd…](https://www.obligacjeskarbowe.pl/media_files/21db12fd-35ec-4cbd-b3f4-28a9a7ae2c7e.pdf) |
| `file_409915.pdf` | no. 87/2026 | EDO0936 | [media_files/9dc0862c…](https://www.obligacjeskarbowe.pl/media_files/9dc0862c-b074-481a-9e90-f28b14969fb9.pdf) |
| `file_409974.pdf` | no. 88/2026 | ROS0932 | [media_files/84817c16…](https://www.obligacjeskarbowe.pl/media_files/84817c16-3d45-497a-b933-c6c894ea29dc.pdf) |
| `file_409976.pdf` | no. 89/2026 | ROD0938 | [media_files/1fe0f15f…](https://www.obligacjeskarbowe.pl/media_files/1fe0f15f-cbd2-4035-9359-edcdb54623d8.pdf) |

Next to the PDFs sit excerpts from the site's announcements about the
September offer (`komunikat.txt`) and about the exchange purchase
(`zamiana.txt`) — official materials as well.

## `nbp/` — data of the National Bank of Poland

Read only by `./bonds-dane`, which rewrites them into the series in
`data/market/`. A person downloads fresh versions and replaces the files here:

| File | Content | Downloaded from |
|---|---|---|
| `stopy_procentowe_archiwum.xml` | archive of the Monetary Policy Council decisions on the base rates | [nbp.pl/podstawowe-stopy-procentowe-archiwum](https://nbp.pl/podstawowe-stopy-procentowe-archiwum/) |
| `bazowa.xlsx` | core inflation workbook with the CPI y/y column (data of Statistics Poland) | [nbp.pl/statystyka-i-sprawozdawczosc/inflacja-bazowa](https://nbp.pl/statystyka-i-sprawozdawczosc/inflacja-bazowa/) |
