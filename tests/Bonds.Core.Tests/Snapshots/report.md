# Polskie obligacje skarbowe detaliczne — wyjście w każdym miesiącu

- Data zakupu: 2026-08-17
- Pozycja: 100 obligacji
- Podatek: 19,00% od odsetek, zaokrąglany w górę do grosza (art. 63 §1a Ordynacji podatkowej); opłata za przedterminowy wykup pomniejsza podstawę
- Scenariusz: Snapshot: CPI 3,50%, NBP 4,00%
- Założona inflacja CPI: 3,50%; założona stopa referencyjna NBP: 4,00%

## OTS1126 — Ots, 3 miesięcy

- Źródło: test fixture for OTS1126
- Wykup: 2026-11-17
- Oprocentowanie: stała 2,00%
- Kapitalizacja: brak, odsetki wypłacane
- Opłata za przedterminowy wykup: bez opłaty, przepadają wszystkie odsetki
- Wszystkie stopy z danych opublikowanych

| M | Data wyjścia | Sposób | Stopa okresu | Kupony brutto | Odsetki w wykupie | Opłata | Wypłata brutto | Podatek | Zysk netto | Zysk % | IRR netto |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 2026-09-17 | wykup przedterminowy | 2,00% | 0,00 | 17,00 | 17,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 2 | 2026-10-17 | wykup przedterminowy | 2,00% | 0,00 | 33,00 | 33,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 3 | 2026-11-17 | wykup | 2,00% | 0,00 | 50,00 | 0,00 | 10050,00 | 9,50 | 40,50 | 0,41% | 1,62% |
| 4 | 2026-12-17 | nie istnieje | — | — | — | — | — | — | — | — | — |

## ROR0827 — Ror, 12 miesięcy

- Źródło: test fixture for ROR0827
- Wykup: 2027-08-17
- Oprocentowanie: okres 1: 4,00%, dalej stopa referencyjna NBP + 0,00%
- Kapitalizacja: brak, odsetki wypłacane
- Opłata za przedterminowy wykup: 0,50 zł (w 1. okresie nie więcej niż narosłe odsetki)
- Prognoza od okresu 2 (2026-09-17)

| M | Data wyjścia | Sposób | Stopa okresu | Kupony brutto | Odsetki w wykupie | Opłata | Wypłata brutto | Podatek | Zysk netto | Zysk % | IRR netto |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 2026-09-17 | wykup przedterminowy | 4,00% | 0,00 | 33,00 | 33,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 2 | 2026-10-17 | wykup przedterminowy | 4,00%* | 33,00 | 33,00 | 50,00 | 10016,00 | 6,27 | 9,73 | 0,10% | 0,58% |
| 3 | 2026-11-17 | wykup przedterminowy | 4,00%* | 66,00 | 33,00 | 50,00 | 10049,00 | 12,54 | 36,46 | 0,36% | 1,46% |
| 4 | 2026-12-17 | wykup przedterminowy | 4,00%* | 99,00 | 33,00 | 50,00 | 10082,00 | 18,81 | 63,19 | 0,63% | 1,91% |
| 5 | 2027-01-17 | wykup przedterminowy | 4,00%* | 132,00 | 33,00 | 50,00 | 10115,00 | 25,08 | 89,92 | 0,90% | 2,17% |
| 6 | 2027-02-17 | wykup przedterminowy | 4,00%* | 165,00 | 33,00 | 50,00 | 10148,00 | 31,35 | 116,65 | 1,17% | 2,34% |
| 7 | 2027-03-17 | wykup przedterminowy | 4,00%* | 198,00 | 33,00 | 50,00 | 10181,00 | 37,62 | 143,38 | 1,43% | 2,50% |
| 8 | 2027-04-17 | wykup przedterminowy | 4,00%* | 231,00 | 33,00 | 50,00 | 10214,00 | 43,89 | 170,11 | 1,70% | 2,59% |
| 9 | 2027-05-17 | wykup przedterminowy | 4,00%* | 264,00 | 33,00 | 50,00 | 10247,00 | 50,16 | 196,84 | 1,97% | 2,67% |
| 10 | 2027-06-17 | wykup przedterminowy | 4,00%* | 297,00 | 33,00 | 50,00 | 10280,00 | 56,43 | 223,57 | 2,24% | 2,72% |
| 11 | 2027-07-17 | wykup przedterminowy | 4,00%* | 330,00 | 33,00 | 50,00 | 10313,00 | 62,70 | 250,30 | 2,50% | 2,78% |
| 12 | 2027-08-17 | wykup | 4,00%* | 363,00 | 33,00 | 0,00 | 10396,00 | 75,24 | 320,76 | 3,21% | 3,26% |
| 13 | 2027-09-17 | nie istnieje | — | — | — | — | — | — | — | — | — |

## TOS0829 — Tos, 36 miesięcy

- Źródło: test fixture for TOS0829
- Wykup: 2029-08-17
- Oprocentowanie: stała 4,40%
- Kapitalizacja: roczna
- Opłata za przedterminowy wykup: 1,00 zł, nie więcej niż narosłe odsetki
- Wszystkie stopy z danych opublikowanych

| M | Data wyjścia | Sposób | Stopa okresu | Kupony brutto | Odsetki w wykupie | Opłata | Wypłata brutto | Podatek | Zysk netto | Zysk % | IRR netto |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 2026-09-17 | wykup przedterminowy | 4,40% | 0,00 | 37,00 | 37,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 2 | 2026-10-17 | wykup przedterminowy | 4,40% | 0,00 | 74,00 | 74,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 3 | 2026-11-17 | wykup przedterminowy | 4,40% | 0,00 | 111,00 | 100,00 | 10011,00 | 2,09 | 8,91 | 0,09% | 0,35% |
| 4 | 2026-12-17 | wykup przedterminowy | 4,40% | 0,00 | 147,00 | 100,00 | 10047,00 | 8,93 | 38,07 | 0,38% | 1,14% |
| 5 | 2027-01-17 | wykup przedterminowy | 4,40% | 0,00 | 184,00 | 100,00 | 10084,00 | 15,96 | 68,04 | 0,68% | 1,63% |
| 6 | 2027-02-17 | wykup przedterminowy | 4,40% | 0,00 | 222,00 | 100,00 | 10122,00 | 23,18 | 98,82 | 0,99% | 1,97% |
| 7 | 2027-03-17 | wykup przedterminowy | 4,40% | 0,00 | 256,00 | 100,00 | 10156,00 | 29,64 | 126,36 | 1,26% | 2,19% |
| 8 | 2027-04-17 | wykup przedterminowy | 4,40% | 0,00 | 293,00 | 100,00 | 10193,00 | 36,67 | 156,33 | 1,56% | 2,36% |
| 9 | 2027-05-17 | wykup przedterminowy | 4,40% | 0,00 | 329,00 | 100,00 | 10229,00 | 43,51 | 185,49 | 1,85% | 2,49% |
| 10 | 2027-06-17 | wykup przedterminowy | 4,40% | 0,00 | 366,00 | 100,00 | 10266,00 | 50,54 | 215,46 | 2,15% | 2,59% |
| 11 | 2027-07-17 | wykup przedterminowy | 4,40% | 0,00 | 403,00 | 100,00 | 10303,00 | 57,57 | 245,43 | 2,45% | 2,69% |
| 12 | 2027-08-17 | wykup przedterminowy | 4,40% | 0,00 | 440,00 | 100,00 | 10340,00 | 64,60 | 275,40 | 2,75% | 2,75% |
| 13 | 2027-09-17 | wykup przedterminowy | 4,40% | 0,00 | 479,00 | 100,00 | 10379,00 | 72,01 | 306,99 | 3,07% | 2,83% |
| 14 | 2027-10-17 | wykup przedterminowy | 4,40% | 0,00 | 517,00 | 100,00 | 10417,00 | 79,23 | 337,77 | 3,38% | 2,89% |
| 15 | 2027-11-17 | wykup przedterminowy | 4,40% | 0,00 | 555,00 | 100,00 | 10455,00 | 86,45 | 368,55 | 3,69% | 2,93% |
| 16 | 2027-12-17 | wykup przedterminowy | 4,40% | 0,00 | 593,00 | 100,00 | 10493,00 | 93,67 | 399,33 | 3,99% | 2,98% |
| 17 | 2028-01-17 | wykup przedterminowy | 4,40% | 0,00 | 632,00 | 100,00 | 10532,00 | 101,08 | 430,92 | 4,31% | 3,02% |
| 18 | 2028-02-17 | wykup przedterminowy | 4,40% | 0,00 | 671,00 | 100,00 | 10571,00 | 108,49 | 462,51 | 4,63% | 3,05% |
| 19 | 2028-03-17 | wykup przedterminowy | 4,40% | 0,00 | 707,00 | 100,00 | 10607,00 | 115,33 | 491,67 | 4,92% | 3,08% |
| 20 | 2028-04-17 | wykup przedterminowy | 4,40% | 0,00 | 746,00 | 100,00 | 10646,00 | 122,74 | 523,26 | 5,23% | 3,10% |
| 21 | 2028-05-17 | wykup przedterminowy | 4,40% | 0,00 | 784,00 | 100,00 | 10684,00 | 129,96 | 554,04 | 5,54% | 3,13% |
| 22 | 2028-06-17 | wykup przedterminowy | 4,40% | 0,00 | 823,00 | 100,00 | 10723,00 | 137,37 | 585,63 | 5,86% | 3,15% |
| 23 | 2028-07-17 | wykup przedterminowy | 4,40% | 0,00 | 860,00 | 100,00 | 10760,00 | 144,40 | 615,60 | 6,16% | 3,16% |
| 24 | 2028-08-17 | wykup przedterminowy | 4,40% | 0,00 | 899,00 | 100,00 | 10799,00 | 151,81 | 647,19 | 6,47% | 3,18% |
| 25 | 2028-09-17 | wykup przedterminowy | 4,40% | 0,00 | 940,00 | 100,00 | 10840,00 | 159,60 | 680,40 | 6,80% | 3,20% |
| 26 | 2028-10-17 | wykup przedterminowy | 4,40% | 0,00 | 979,00 | 100,00 | 10879,00 | 167,01 | 711,99 | 7,12% | 3,22% |
| 27 | 2028-11-17 | wykup przedterminowy | 4,40% | 0,00 | 1020,00 | 100,00 | 10920,00 | 174,80 | 745,20 | 7,45% | 3,24% |
| 28 | 2028-12-17 | wykup przedterminowy | 4,40% | 0,00 | 1059,00 | 100,00 | 10959,00 | 182,21 | 776,79 | 7,77% | 3,25% |
| 29 | 2029-01-17 | wykup przedterminowy | 4,40% | 0,00 | 1100,00 | 100,00 | 11000,00 | 190,00 | 810,00 | 8,10% | 3,27% |
| 30 | 2029-02-17 | wykup przedterminowy | 4,40% | 0,00 | 1141,00 | 100,00 | 11041,00 | 197,79 | 843,21 | 8,43% | 3,28% |
| 31 | 2029-03-17 | wykup przedterminowy | 4,40% | 0,00 | 1178,00 | 100,00 | 11078,00 | 204,82 | 873,18 | 8,73% | 3,29% |
| 32 | 2029-04-17 | wykup przedterminowy | 4,40% | 0,00 | 1218,00 | 100,00 | 11118,00 | 212,42 | 905,58 | 9,06% | 3,30% |
| 33 | 2029-05-17 | wykup przedterminowy | 4,40% | 0,00 | 1258,00 | 100,00 | 11158,00 | 220,02 | 937,98 | 9,38% | 3,31% |
| 34 | 2029-06-17 | wykup przedterminowy | 4,40% | 0,00 | 1298,00 | 100,00 | 11198,00 | 227,62 | 970,38 | 9,70% | 3,32% |
| 35 | 2029-07-17 | wykup przedterminowy | 4,40% | 0,00 | 1338,00 | 100,00 | 11238,00 | 235,22 | 1002,78 | 10,03% | 3,33% |
| 36 | 2029-08-17 | wykup | 4,40% | 0,00 | 1379,00 | 0,00 | 11379,00 | 262,01 | 1116,99 | 11,17% | 3,59% |
| 37 | 2029-09-17 | nie istnieje | — | — | — | — | — | — | — | — | — |

## COI0830 — Coi, 48 miesięcy

- Źródło: test fixture for COI0830
- Wykup: 2030-08-17
- Oprocentowanie: okres 1: 4,75%, dalej CPI r/r + 1,50%
- Kapitalizacja: brak, odsetki wypłacane
- Opłata za przedterminowy wykup: 2,00 zł (w 1. okresie nie więcej niż narosłe odsetki)
- Prognoza od okresu 2 (2027-08-17)

| M | Data wyjścia | Sposób | Stopa okresu | Kupony brutto | Odsetki w wykupie | Opłata | Wypłata brutto | Podatek | Zysk netto | Zysk % | IRR netto |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 2026-09-17 | wykup przedterminowy | 4,75% | 0,00 | 40,00 | 40,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 2 | 2026-10-17 | wykup przedterminowy | 4,75% | 0,00 | 79,00 | 79,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 3 | 2026-11-17 | wykup przedterminowy | 4,75% | 0,00 | 120,00 | 120,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 4 | 2026-12-17 | wykup przedterminowy | 4,75% | 0,00 | 159,00 | 159,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 5 | 2027-01-17 | wykup przedterminowy | 4,75% | 0,00 | 199,00 | 199,00 | 10000,00 | 0,00 | 0,00 | 0,00% | 0,00% |
| 6 | 2027-02-17 | wykup przedterminowy | 4,75% | 0,00 | 239,00 | 200,00 | 10039,00 | 7,41 | 31,59 | 0,32% | 0,63% |
| 7 | 2027-03-17 | wykup przedterminowy | 4,75% | 0,00 | 276,00 | 200,00 | 10076,00 | 14,44 | 61,56 | 0,62% | 1,06% |
| 8 | 2027-04-17 | wykup przedterminowy | 4,75% | 0,00 | 316,00 | 200,00 | 10116,00 | 22,04 | 93,96 | 0,94% | 1,41% |
| 9 | 2027-05-17 | wykup przedterminowy | 4,75% | 0,00 | 355,00 | 200,00 | 10155,00 | 29,45 | 125,55 | 1,26% | 1,68% |
| 10 | 2027-06-17 | wykup przedterminowy | 4,75% | 0,00 | 396,00 | 200,00 | 10196,00 | 37,24 | 158,76 | 1,59% | 1,91% |
| 11 | 2027-07-17 | wykup przedterminowy | 4,75% | 0,00 | 435,00 | 200,00 | 10235,00 | 44,65 | 190,35 | 1,90% | 2,08% |
| 12 | 2027-08-17 | wykup przedterminowy | 4,75% | 0,00 | 475,00 | 200,00 | 10275,00 | 52,25 | 222,75 | 2,23% | 2,23% |
| 13 | 2027-09-17 | wykup przedterminowy | 5,00%* | 475,00 | 42,00 | 200,00 | 10317,00 | 90,25 | 226,75 | 2,27% | 2,09% |
| 14 | 2027-10-17 | wykup przedterminowy | 5,00%* | 475,00 | 83,00 | 200,00 | 10358,00 | 90,25 | 267,75 | 2,68% | 2,30% |
| 15 | 2027-11-17 | wykup przedterminowy | 5,00%* | 475,00 | 126,00 | 200,00 | 10401,00 | 90,25 | 310,75 | 3,11% | 2,49% |
| 16 | 2027-12-17 | wykup przedterminowy | 5,00%* | 475,00 | 167,00 | 200,00 | 10442,00 | 90,25 | 351,75 | 3,52% | 2,65% |
| 17 | 2028-01-17 | wykup przedterminowy | 5,00%* | 475,00 | 209,00 | 200,00 | 10484,00 | 91,96 | 392,04 | 3,92% | 2,78% |
| 18 | 2028-02-17 | wykup przedterminowy | 5,00%* | 475,00 | 251,00 | 200,00 | 10526,00 | 99,94 | 426,06 | 4,26% | 2,85% |
| 19 | 2028-03-17 | wykup przedterminowy | 5,00%* | 475,00 | 291,00 | 200,00 | 10566,00 | 107,54 | 458,46 | 4,58% | 2,91% |
| 20 | 2028-04-17 | wykup przedterminowy | 5,00%* | 475,00 | 333,00 | 200,00 | 10608,00 | 115,52 | 492,48 | 4,92% | 2,97% |
| 21 | 2028-05-17 | wykup przedterminowy | 5,00%* | 475,00 | 374,00 | 200,00 | 10649,00 | 123,31 | 525,69 | 5,26% | 3,02% |
| 22 | 2028-06-17 | wykup przedterminowy | 5,00%* | 475,00 | 417,00 | 200,00 | 10692,00 | 131,48 | 560,52 | 5,61% | 3,07% |
| 23 | 2028-07-17 | wykup przedterminowy | 5,00%* | 475,00 | 458,00 | 200,00 | 10733,00 | 139,27 | 593,73 | 5,94% | 3,11% |
| 24 | 2028-08-17 | wykup przedterminowy | 5,00%* | 475,00 | 500,00 | 200,00 | 10775,00 | 147,25 | 627,75 | 6,28% | 3,15% |
| 25 | 2028-09-17 | wykup przedterminowy | 5,00%* | 975,00 | 42,00 | 200,00 | 10817,00 | 185,25 | 631,75 | 6,32% | 3,04% |
| 26 | 2028-10-17 | wykup przedterminowy | 5,00%* | 975,00 | 84,00 | 200,00 | 10859,00 | 185,25 | 673,75 | 6,74% | 3,12% |
| 27 | 2028-11-17 | wykup przedterminowy | 5,00%* | 975,00 | 126,00 | 200,00 | 10901,00 | 185,25 | 715,75 | 7,16% | 3,19% |
| 28 | 2028-12-17 | wykup przedterminowy | 5,00%* | 975,00 | 167,00 | 200,00 | 10942,00 | 185,25 | 756,75 | 7,57% | 3,26% |
| 29 | 2029-01-17 | wykup przedterminowy | 5,00%* | 975,00 | 210,00 | 200,00 | 10985,00 | 187,15 | 797,85 | 7,98% | 3,31% |
| 30 | 2029-02-17 | wykup przedterminowy | 5,00%* | 975,00 | 252,00 | 200,00 | 11027,00 | 195,13 | 831,87 | 8,32% | 3,34% |
| 31 | 2029-03-17 | wykup przedterminowy | 5,00%* | 975,00 | 290,00 | 200,00 | 11065,00 | 202,35 | 862,65 | 8,63% | 3,36% |
| 32 | 2029-04-17 | wykup przedterminowy | 5,00%* | 975,00 | 333,00 | 200,00 | 11108,00 | 210,52 | 897,48 | 8,97% | 3,38% |
| 33 | 2029-05-17 | wykup przedterminowy | 5,00%* | 975,00 | 374,00 | 200,00 | 11149,00 | 218,31 | 930,69 | 9,31% | 3,40% |
| 34 | 2029-06-17 | wykup przedterminowy | 5,00%* | 975,00 | 416,00 | 200,00 | 11191,00 | 226,29 | 964,71 | 9,65% | 3,42% |
| 35 | 2029-07-17 | wykup przedterminowy | 5,00%* | 975,00 | 458,00 | 200,00 | 11233,00 | 234,27 | 998,73 | 9,99% | 3,44% |
| 36 | 2029-08-17 | wykup przedterminowy | 5,00%* | 975,00 | 500,00 | 200,00 | 11275,00 | 242,25 | 1032,75 | 10,33% | 3,46% |
| 37 | 2029-09-17 | wykup przedterminowy | 5,00%* | 1475,00 | 42,00 | 200,00 | 11317,00 | 280,25 | 1036,75 | 10,37% | 3,38% |
| 38 | 2029-10-17 | wykup przedterminowy | 5,00%* | 1475,00 | 84,00 | 200,00 | 11359,00 | 280,25 | 1078,75 | 10,79% | 3,42% |
| 39 | 2029-11-17 | wykup przedterminowy | 5,00%* | 1475,00 | 126,00 | 200,00 | 11401,00 | 280,25 | 1120,75 | 11,21% | 3,47% |
| 40 | 2029-12-17 | wykup przedterminowy | 5,00%* | 1475,00 | 167,00 | 200,00 | 11442,00 | 280,25 | 1161,75 | 11,62% | 3,50% |
| 41 | 2030-01-17 | wykup przedterminowy | 5,00%* | 1475,00 | 210,00 | 200,00 | 11485,00 | 282,15 | 1202,85 | 12,03% | 3,54% |
| 42 | 2030-02-17 | wykup przedterminowy | 5,00%* | 1475,00 | 252,00 | 200,00 | 11527,00 | 290,13 | 1236,87 | 12,37% | 3,55% |
| 43 | 2030-03-17 | wykup przedterminowy | 5,00%* | 1475,00 | 290,00 | 200,00 | 11565,00 | 297,35 | 1267,65 | 12,68% | 3,56% |
| 44 | 2030-04-17 | wykup przedterminowy | 5,00%* | 1475,00 | 333,00 | 200,00 | 11608,00 | 305,52 | 1302,48 | 13,02% | 3,57% |
| 45 | 2030-05-17 | wykup przedterminowy | 5,00%* | 1475,00 | 374,00 | 200,00 | 11649,00 | 313,31 | 1335,69 | 13,36% | 3,58% |
| 46 | 2030-06-17 | wykup przedterminowy | 5,00%* | 1475,00 | 416,00 | 200,00 | 11691,00 | 321,29 | 1369,71 | 13,70% | 3,59% |
| 47 | 2030-07-17 | wykup przedterminowy | 5,00%* | 1475,00 | 458,00 | 200,00 | 11733,00 | 329,27 | 1403,73 | 14,04% | 3,60% |
| 48 | 2030-08-17 | wykup | 5,00%* | 1475,00 | 500,00 | 0,00 | 11975,00 | 375,25 | 1599,75 | 16,00% | 3,99% |
| 49 | 2030-09-17 | nie istnieje | — | — | — | — | — | — | — | — | — |

## Legenda

- **Kupony brutto** — odsetki wypłacone przed datą wyjścia (ROR, DOR i COI).
- **Odsetki w wykupie** — odsetki zawarte w wypłacie z wykupu, przed opłatą; dla obligacji kapitalizujących to całość odsetek.
- **Wypłata brutto** — kupony plus wypłata z wykupu po opłacie, przed podatkiem.
- **Podatek** — pobierany z każdej wypłaty dla całej pozycji i zaokrąglany w górę do grosza (art. 63 §1a Ordynacji podatkowej).
- **IRR netto** — roczna wewnętrzna stopa zwrotu z przepływów netto; jedyna porównywalna miara między obligacjami kuponowymi i kapitalizującymi.
- `*` przy stopie — wartość z prognozy, nie z danych opublikowanych.
- **niedostępny** w kolumnie Sposób — dnia wyjścia nie obejmuje okno przedterminowego wykupu z listu emisyjnego.

