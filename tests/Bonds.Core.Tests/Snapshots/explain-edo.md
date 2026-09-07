# EDO0836 — przebieg naliczania

- Źródło: test fixture for EDO0836
- Zakup: 2026-08-17, wykup: 2036-08-17
- Nominał: 100,00 zł, cena zakupu: 100,00 zł
- Okresów odsetkowych: 10, po 12 mies. (F = 1)
- Oprocentowanie: okres 1: 5,35%, dalej CPI r/r + 2,00%
- Kapitalizacja: roczna, zaokrąglanie bazy: nie
- Podłoga 100 zł: AllPeriods
- Opłata: 3,00 zł, nie więcej niż narosłe odsetki
- Scenariusz: Snapshot: CPI 3,50%, NBP 4,00%

| Okres | Początek | Koniec | ACT | Stopa | Źródło stopy | Kupon | Wartość na koniec |
|---|---|---|---|---|---|---|---|
| 1 | 2026-08-17 | 2027-08-17 | 365 | 5,35% | dane | 0,00 | 105,35 |
| 2 | 2027-08-17 | 2028-08-17 | 366 | 5,50% | prognoza | 0,00 | 111,14 |
| 3 | 2028-08-17 | 2029-08-17 | 365 | 5,50% | prognoza | 0,00 | 117,26 |
| 4 | 2029-08-17 | 2030-08-17 | 365 | 5,50% | prognoza | 0,00 | 123,71 |
| 5 | 2030-08-17 | 2031-08-17 | 365 | 5,50% | prognoza | 0,00 | 130,51 |
| 6 | 2031-08-17 | 2032-08-17 | 366 | 5,50% | prognoza | 0,00 | 137,69 |
| 7 | 2032-08-17 | 2033-08-17 | 365 | 5,50% | prognoza | 0,00 | 145,26 |
| 8 | 2033-08-17 | 2034-08-17 | 365 | 5,50% | prognoza | 0,00 | 153,25 |
| 9 | 2034-08-17 | 2035-08-17 | 365 | 5,50% | prognoza | 0,00 | 161,68 |
| 10 | 2035-08-17 | 2036-08-17 | 366 | 5,50% | prognoza | 0,00 | 170,57 |

Wartość w dniu wykupu: **170,57 zł** na jedną obligację, przed podatkiem.

