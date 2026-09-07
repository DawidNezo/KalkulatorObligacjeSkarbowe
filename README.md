# Kalkulator polskich obligacji skarbowych

Liczy, ile zarobisz na detalicznych obligacjach skarbowych, jeśli wyjdziesz z nich
w dowolnym miesiącu. Dla każdej emisji dostajesz tabelę: wiersz na każdy miesiąc,
kwoty brutto i netto, opłata za wcześniejszy wykup i roczna stopa zwrotu.

Obsługuje wszystkie osiem rodzajów: OTS, ROR, DOR, TOS, COI, ROS, EDO i ROD.

---

## Spis treści

- [Szybki start](#szybki-start)
- [Co dostajesz](#co-dostajesz)
- [Jak czytać tabelę](#jak-czytać-tabelę)
- [Porównanie wszystkich obligacji](#porównanie-wszystkich-obligacji)
- [Obsługiwane obligacje](#obsługiwane-obligacje)
- [Podatek](#podatek)
- [Zanim uwierzysz liczbom](#zanim-uwierzysz-liczbom)
- [Własne dane](#własne-dane)
- [Dokumentacja techniczna](#dokumentacja-techniczna)
- [Licencja i pochodzenie danych](#licencja-i-pochodzenie-danych)

---

## Szybki start

```bash
./bonds
```

To wszystko. Skrypt buduje projekt, jeśli trzeba, i wypisuje tabelę dla wszystkich
emisji, przy założeniu zakupu dzisiaj i jednej obligacji.

Częstsze warianty:

```bash
./bonds --units 1000                    # pozycja 1000 obligacji
./bonds --issue EDO0836                 # tylko jedna emisja
./bonds --purchase 2026-09-10           # konkretna data zakupu
./bonds matrix                          # porównanie wszystkich emisji w jednej tabeli
./bonds --format csv --out out/tabela.csv   # do arkusza
./bonds --help                          # pełna lista opcji
```

Data zakupu wybiera emisję. Ministerstwo emituje nowy zestaw obligacji co miesiąc
i sprzedaje go przez jeden miesiąc kalendarzowy, więc `data/issues/` ma podkatalog
na miesiąc — `data/issues/2026-09/` to emisje z listów wrześniowych. Kalkulator
sam bierze ten zgodny z datą zakupu, a przy dacie z miesiąca, którego nie ma,
wypisuje listę miesięcy, które są.

Żeby dostać wszystko naraz — tabele we trzech formatach, trzy macierze porównań
oraz tabelę i przebieg naliczania dla każdej emisji osobno — uruchom `./bonds-all`.
Bez argumentów przelicza każdy miesiąc z `data/issues/` i zapisuje wyniki
do `out/<RRRR-MM>/`; `./bonds-all --month 2026-09` robi tylko jeden miesiąc,
a `./bonds-all --help` pokazuje, jak zawęzić zestaw albo podać własną pozycję.

Potrzebujesz .NET 10. Wynik idzie na standardowe wyjście, więc `./bonds > plik.md`
działa — komunikaty buildu lecą osobno i nie zaśmiecają pliku.

---

## Co dostajesz

Fragment tabeli dla obligacji rocznej ROR, pozycja 100 sztuk (nominał 10 000 zł),
zakup 17 sierpnia 2026:

| M | Data wyjścia | Sposób | Kupony brutto | Odsetki w wykupie | Opłata | Wypłata brutto | Zysk netto | IRR netto |
|---|---|---|--:|--:|--:|--:|--:|--:|
| 1 | 2026-09-17 | wykup przedterminowy | 0,00 | 33,00 | 33,00 | 10000,00 | 0,00 | 0,00% |
| 10 | 2027-06-17 | wykup przedterminowy | 281,00 | 31,00 | 50,00 | 10262,00 | 208,61 | 2,54% |
| 11 | 2027-07-17 | wykup przedterminowy | 312,00 | 31,00 | 50,00 | 10293,00 | 233,72 | 2,59% |
| 12 | 2027-08-17 | **wykup** | 343,00 | 31,00 | 0,00 | 10374,00 | 302,94 | 3,07% |
| 13 | 2027-09-17 | nie istnieje | — | — | — | — | — | — |

Widać z tego trzy rzeczy: wyjście w 1. miesiącu nic nie daje, bo opłata zjada całe
odsetki; wyjście w 11. miesiącu kosztuje 50 zł opłaty; w 12. miesiącu obligacja
kończy się sama i opłaty nie ma.

Liczby zależą od danych rynkowych w repozytorium: pierwszy okres ROR ma stopę
4,00% z listu emisyjnego, a kolejne — stopę referencyjną NBP, dziś 3,75%
(decyzja RPP z 5 marca 2026 r.). Po każdej zmianie stopy tabela wyjdzie inaczej.

Prawdziwe wyjście ma jeszcze kolumny ze stopą okresu, podatkiem i zyskiem
procentowym — opis poniżej.

---

## Jak czytać tabelę

| Kolumna | Co pokazuje |
|---|---|
| `M` | miesiąc wyjścia, licząc od zakupu |
| `Data wyjścia` | ta sama data co `M`, w kalendarzu |
| `Sposób` | `wykup przedterminowy` — wychodzisz przed czasem, może być opłata. `wykup` — obligacja kończy się sama, bez opłaty. `nie istnieje` — ten miesiąc jest już po terminie |
| `Stopa okresu` | oprocentowanie okresu, w którym wypada wyjście. Gwiazdka `*` znaczy, że to prognoza, a nie ogłoszona wartość |
| `Kupony brutto` | odsetki, które już dostałeś na konto przed tą datą (ROR, DOR i COI wypłacają je w trakcie) |
| `Odsetki w wykupie` | odsetki zawarte w samej wypłacie z wykupu. Przy obligacjach z kapitalizacją to całość odsetek za cały okres trzymania |
| `Opłata` | potrącenie za wcześniejszy wykup |
| `Wypłata brutto` | wszystko, co dostałeś: kupony plus wypłata z wykupu, po opłacie, przed podatkiem |
| `Podatek` | podatek Belki pobrany ze wszystkich wypłat, tak jak robi to agent emisji — [opis niżej](#podatek) |
| `Zysk netto` | wypłaty po podatku minus to, co wpłaciłeś |
| `Zysk %` | zysk netto podzielony przez kwotę, którą wpłaciłeś |
| `IRR netto` | roczna stopa zwrotu. **Tym porównuj obligacje między sobą** |

Dlaczego IRR, a nie zwykły zysk procentowy: ROR wypłaca kupony co miesiąc, EDO nie
wypłaca nic przez dziesięć lat. Ta sama „ilość odsetek” jest warta więcej, kiedy
wraca do ciebie wcześniej. IRR to uwzględnia, zwykły procent nie.

---

## Porównanie wszystkich obligacji

```bash
./bonds matrix
```

Jedna tabela: wiersz na miesiąc, kolumna na emisję, w środku roczna stopa zwrotu.

| M | Data wyjścia | COI0830 | DOR0828 | EDO0836 | OTS1126 | ROD0838 | ROR0827 | ROS0832 | TOS0829 |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|
| 10 | 2027-06-17 | 1,90% | 2,42% | 1,42% | — | 1,61% | 2,52% | 2,09% | 2,59% |
| 11 | 2027-07-17 | 2,08% | 2,49% | 1,67% | — | 1,87% | 2,57% | 2,28% | 2,68% |
| 12 | 2027-08-17 | 2,22% | 2,55% | 1,90% | — | 2,10% | 3,05% | 2,43% | 2,75% |

Kreska oznacza, że ta obligacja już nie istnieje w tym miesiącu — OTS jest
trzymiesięczna, więc kończy się dużo wcześniej.

Zamiast stopy zwrotu można pokazać kwotę zysku albo procent:

```bash
./bonds matrix --metric profit
./bonds matrix --metric rate
```

---

## Obsługiwane obligacje

| Kod | Rodzaj | Okres | Oprocentowanie | Odsetki | Opłata za wcześniejszy wykup |
|---|---|---|---|---|---|
| `OTS1126` | trzymiesięczna, stała | 3 mies. | 2,00% | przy wykupie | brak, ale **przepadają wszystkie odsetki** |
| `ROR0827` | roczna, zmienna | 12 mies. | 4,00%, dalej stopa NBP | co miesiąc | 0,50 zł |
| `DOR0828` | dwuletnia, zmienna | 24 mies. | 4,15%, dalej NBP + 0,15% | co miesiąc | 0,70 zł |
| `TOS0829` | trzyletnia, stała | 3 lata | 4,40% | przy wykupie, kapitalizacja roczna | 1,00 zł |
| `COI0830` | czteroletnia, indeksowana | 4 lata | 4,75%, dalej CPI + 1,50% | co rok | 2,00 zł |
| `ROS0832` | rodzinna, sześcioletnia | 6 lat | 5,00%, dalej CPI + 2,00% | przy wykupie, kapitalizacja roczna | 2,00 zł |
| `EDO0836` | emerytalna, dziesięcioletnia | 10 lat | 5,35%, dalej CPI + 2,00% | przy wykupie, kapitalizacja roczna | 3,00 zł |
| `ROD0838` | rodzinna, dwunastoletnia | 12 lat | 5,60%, dalej CPI + 2,50% | przy wykupie, kapitalizacja roczna | 3,00 zł |

Kwoty na jedną obligację o nominale 100 zł. ROS i ROD kupisz tylko z uprawnieniem
do świadczenia 800+; kalkulator tego nie sprawdza.

Kody w tabeli to emisje sierpniowe. W repozytorium są dwa miesiące:

| Miesiąc sprzedaży | Listy emisyjne | Emisje |
|---|---|---|
| sierpień 2026 | nr 73–80/2026 z 21 lipca 2026 r. | `OTS1126`, `ROR0827`, `DOR0828`, `TOS0829`, `COI0830`, `ROS0832`, `EDO0836`, `ROD0838` |
| wrzesień 2026 | nr 82–89/2026 z 20 sierpnia 2026 r. | `OTS1226`, `ROR0927`, `DOR0928`, `TOS0929`, `COI0930`, `ROS0932`, `EDO0936`, `ROD0938` |

Oba miesiące mają identyczne stopy pierwszego okresu i marże — te z tabeli wyżej.
Różnią się tylko okresem sprzedaży i terminem wykupu, ale to trzeba sprawdzać
w każdym liście osobno, bo Ministerstwo ustala stopę pierwszego okresu na każdą
emisję od nowa.

Parametry nie są w kodzie — są w plikach `data/issues/<RRRR-MM>/`, po jednym na
emisję, a PDF-y listów leżą w `zrodla/listy_emisyjne/<RRRR-MM>/`. Nowy miesiąc
dodaje się bez programowania.

> **Uwaga na ROR, DOR i COI.** Przy tych trzech wypłata z wcześniejszego wykupu
> może wyjść **poniżej 100 zł za obligację**. Ochrona kapitału obowiązuje tylko
> w pierwszym okresie odsetkowym, a potem opłata jest potrącana w pełnej wysokości.
> Wyjście z DOR w drugim miesiącu daje 99,63 zł (100 zł + 0,33 zł odsetek przy
> stopie NBP 3,75% + 0,15% marży − 0,70 zł opłaty). Tak jest w liście emisyjnym
> i tabela to pokazuje jako stratę.

---

## Podatek

19% od odsetek (podatek Belki, art. 30a ust. 1 pkt 2 ustawy o PIT). Opłata za
wcześniejszy wykup pomniejsza podstawę.

Kalkulator liczy podatek tak, jak pobiera go agent emisji:

- **od każdej wypłaty osobno** — kupon co miesiąc to co miesiąc jedno potrącenie,
  wykup to osobne potrącenie;
- **od całej pozycji** — podstawą jest wypłata za wszystkie obligacje razem, nie
  za jedną sztukę;
- **zaokrąglany w górę do grosza.** To reguła art. 63 §1a Ordynacji podatkowej,
  która obejmuje właśnie odsetki z papierów wartościowych. Popularna reguła
  „do pełnych złotych" (art. 63 §1) do tego podatku **nie ma zastosowania**.

Przykład: przy jednej obligacji ROR miesięczny kupon to 0,33 zł, 19% z tego to
0,0627 zł, a po zaokrągleniu w górę — 0,07 zł podatku. Dlatego kwoty netto przy
małej pozycji są odrobinę gorsze w przeliczeniu na sztukę niż przy dużej; jeśli
chcesz realistyczne kwoty, podaj `--units` odpowiadające temu, co faktycznie
kupujesz.

Obligacje z kapitalizacją (TOS, ROS, EDO, ROD) płacą podatek raz, przy wykupie, od
całości odsetek. Obligacje kuponowe (ROR, DOR, COI) płacą przy każdej wypłacie.
To pierwsza grupa ma z tego przewagę i widać ją w kolumnie IRR.

---

## Zanim uwierzysz liczbom

**Prawie wszystkie stopy poza pierwszym okresem są prognozą, nie danymi.** To
najważniejsze ograniczenie i nie da się go usunąć: obligacje indeksowane płacą
według inflacji, której jeszcze nie ogłoszono, a ROR i DOR według stopy NBP, o
której RPP jeszcze nie zdecydowała. Kalkulator w miejsce nieznanej wartości
wstawia założenie ze scenariusza i oznacza taki wiersz gwiazdką `*`.

Przy obligacji dwunastoletniej ROD z założeń pochodzi jedenaście okresów
z dwunastu. To projekcja, nie prognoza — pokazuje, ile wyjdzie *jeśli* inflacja
utrzyma się na zadanym poziomie.

Dane, które są znane, są prawdziwe. `data/market/nbp-reference.json` i
`data/market/cpi-yoy.json` są generowane przez `./bonds-dane` z plików źródłowych
w `zrodla/nbp/`: archiwum stóp NBP (`stopy_procentowe_archiwum.xml`) i arkusza NBP
z inflacją bazową (`bazowa.xlsx`, kolumna CPI liczona na danych GUS). Dzięki temu
najbliższe okresy ROR i DOR pokazują rzeczywistą stopę referencyjną, nie
założenie. **Tych dwóch plików nie edytuj ręcznie** — podmień plik źródłowy
i uruchom skrypt jeszcze raz.

```bash
./bonds-dane --dry-run     # co by się zmieniło
./bonds-dane               # przelicz serie
```

Skrypt nie łączy się z siecią: świeże pliki źródłowe pobiera człowiek
z [NBP](https://nbp.pl/podstawowe-stopy-procentowe-archiwum/) i
[NBP / inflacja bazowa](https://nbp.pl/statystyka-i-sprawozdawczosc/inflacja-bazowa/),
więc każdy wynik da się odtworzyć z tego, co leży w repozytorium.

Pozostałe ograniczenia:

- **Wyjście liczone jest na datę miesięczną.** W rzeczywistości od złożenia
  dyspozycji do wykupu mija pięć dni roboczych. Różnica to zwykle kilka groszy.
- **Wyjście dokładnie w dniu wypłaty kuponu wychodzi trochę za korzystnie.**
  Prawdziwe przepisy zabraniają składać dyspozycję w tym jednym dniu.
- **Okno przedterminowego wykupu (7 dni od zakupu, 20 dni przed końcem) jest
  sprawdzane na datę wypłaty, a nie na datę złożenia dyspozycji.** Przy siatce
  miesięcznej nie zmienia to żadnego wiersza.
- **Nie ma reinwestycji kuponów.** IRR mówi, jaka to stopa zwrotu, a nie ile
  będziesz mieć, jeśli wypłacone odsetki kupisz z powrotem.
- **Limit zakupu ROS i ROD nie jest sprawdzany.**

---

## Własne dane

### Inne założenia inflacyjne

W `data/scenarios/` leżą gotowe scenariusze:

```bash
./bonds --scenario data/scenarios/wysoka-inflacja.json
```

Własny scenariusz to trzy linie:

```json
{
  "name": "CPI 2%, stopa NBP 3%",
  "assumedCpiPercent": 2.00,
  "assumedNbpReferencePercent": 3.00
}
```

Te wartości są używane wszędzie tam, gdzie nie ma jeszcze ogłoszonych danych.

### Nowa emisja

Kodu nie trzeba ruszać. Zakładasz katalog `data/issues/<RRRR-MM>/` na miesiąc
sprzedaży, wrzucasz do niego plik na emisję (nazwa dowolna, byle `.json`)
i przepisujesz parametry z listu emisyjnego. Listy Ministerstwa Finansów mają
zawsze ten sam układ, więc każde pole da się wskazać palcem w dokumencie. Poniżej
plik dla wymyślonej emisji EDO z października 2026 r., a pod nim tabela: co
wpisać i gdzie w liście to znaleźć.

```json
{
  "code": "EDO1036",
  "kind": "Edo",
  "source": "List emisyjny nr 96/2026 z 21 września 2026 r. (EDO1036), ust. 14-19 i 26, zał. 1-3",
  "nominalZloty": 100.00,
  "purchasePriceZloty": 100.00,
  "periodsPerYear": 1,
  "periodCount": 10,
  "capitalizes": true,
  "roundCapitalizedBase": false,
  "principalFloor": "AllPeriods",
  "sale": { "from": "2026-10-01", "to": "2026-10-31" },
  "rate": { "type": "inflation", "firstPeriodRatePercent": 5.35, "marginPercent": 2.00 },
  "fee": { "type": "accruedCapped", "amountZloty": 3.00 },
  "earlyRedemption": { "minDaysAfterPurchase": 7, "minDaysBeforeMaturity": 20 }
}
```

Procenty zapisuje się tak jak w liście: `5.35` znaczy 5,35%. Kwoty w złotych,
daty jako `RRRR-MM-DD`. Wszystkie pola są wymagane (dwa wyjątki:
`rate.marginPercent` przy oprocentowaniu stałym i `fee.amountZloty` przy
`forfeitAllInterest`) — brakujące albo błędnie nazwane pole kończy się błędem
z nazwą pliku, nigdy cichym domyślnym ustawieniem.

| Pole | Co wpisać | Gdzie to jest w liście |
|---|---|---|
| `code` | skrócona nazwa emisji, np. `EDO1036` | nagłówek listu |
| `kind` | rodzina: `Ots`, `Ror`, `Dor`, `Tos`, `Coi`, `Ros`, `Edo`, `Rod` | z nazwy emisji. To tylko etykieta w raporcie — o obliczeniach decydują pozostałe pola |
| `source` | z jakiego dokumentu pochodzą te liczby | wpisz sam: numer listu, data, ustępy i załączniki. **Wymagane** — inaczej za rok nikt nie sprawdzi, skąd wzięła się dana liczba |
| `nominalZloty` | zwykle `100.00` | ust. „Nominał jednej obligacji wynosi…” |
| `purchasePriceZloty` | musi być równe nominałowi | ust. „Cena sprzedaży jest równa wartości nominalnej” |
| `periodsPerYear` | ile okresów odsetkowych mieści się w roku: `12` przy miesięcznych, `4` przy trzymiesięcznych, `1` przy rocznych | ust. o naliczaniu odsetek mówi, jakie są okresy: „w pierwszym **rocznym** okresie odsetkowym”, „**miesięcznym**”, „**trzymiesięcznym**” |
| `periodCount` | liczba okresów odsetkowych przez całe życie obligacji: 12 dla rocznej ROR, 10 dla dziesięcioletniej EDO | policz wzory `WP1…WPk` w załączniku o przedterminowym wykupie albo stopy `r1…rk` w załączniku o oprocentowaniu |
| `capitalizes` | `true`, gdy odsetki dopisują się do kapitału; `false`, gdy są wypłacane | ust. o naliczaniu odsetek. `true`, gdy pisze „od wartości nominalnej **powiększonej o odsetki naliczone** na koniec każdego poprzedniego okresu”. `false`, gdy odsetki są „wypłacane” po każdym okresie |
| `roundCapitalizedBase` | `false` prawie zawsze; `true` tylko wtedy, gdy załącznik zaokrągla podstawę kapitalizacji po każdym roku (tak robi TOS) | załącznik o przedterminowym wykupie. `true`, gdy wzór używa osobnego symbolu na podstawę i opisuje go „wartość nominalna powiększona o naliczone odsetki … **zaokrąglona do dwóch miejsc po przecinku**” (TOS). `false`, gdy wzór wypisuje iloczyn `(1+r₁)·(1+r₂)·…` bez zaokrągleń pośrednich (EDO, ROS, ROD) |
| `principalFloor` | `AllPeriods`, `FirstPeriodOnly` albo `None` | załącznik o przedterminowym wykupie, klauzula pod wzorem. „dla WPk < 100 WPk = 100, k = 1…10” → `AllPeriods`. „WP = 100 **w pierwszym okresie odsetkowym**” → `FirstPeriodOnly`. Brak klauzuli → `None` |
| `sale.from`, `sale.to` | pierwszy i ostatni dzień sprzedaży | ust. „Obligacje są oferowane do sprzedaży w dniach od … do …” |
| `rate.type` | `fixed` — stałe na całe życie. `nbpReference` — stopa referencyjna NBP plus marża. `inflation` — inflacja CPI plus marża | ust. o stopie od drugiego okresu i wskazany w nim załącznik |
| `rate.firstPeriodRatePercent` | stopa pierwszego okresu | ust. „W pierwszym okresie odsetkowym stopa procentowa wynosi …% w skali roku” |
| `rate.marginPercent` | marża dodawana do wskaźnika w kolejnych okresach; `0.00`, jeśli marży nie ma | załącznik ze wzorem na stopę, składnik `m`. Przy `fixed` pole jest nieużywane i można je pominąć — pliki w repozytorium wpisują tam `0.00` |
| `fee.type` | `accruedCapped` — potrącenie zawsze ograniczone narosłymi odsetkami. `twoTier` — ograniczone tylko w pierwszym okresie, potem pełna kwota. `forfeitAllInterest` — brak kwoty, ale przepadają wszystkie odsetki | ust. o skutkach przedterminowego wykupu. Jeden punkt „pomniejszana o kwotę narosłych odsetek, ale nie wyższą niż X zł” → `accruedCapped`. Dwa punkty, gdzie drugi mówi „począwszy od drugiego okresu odsetkowego … pomniejszana o kwotę X zł” → `twoTier` |
| `fee.amountZloty` | kwota potrącenia za jedną obligację | ta sama kwota `X zł` z punktu wyżej (przy `forfeitAllInterest` pomiń) |
| `earlyRedemption.minDaysAfterPurchase` | ile dni po zakupie wolno najwcześniej zażądać wykupu, zwykle `7` | ust. o prawie wezwania do przedterminowego wykupu: „nie wcześniej niż po upływie **siedmiu** dni kalendarzowych od dnia sprzedaży” |
| `earlyRedemption.minDaysBeforeMaturity` | ile dni przed wykupem wolno zażądać najpóźniej, zwykle `20` | ten sam ustęp: „nie później niż **dwadzieścia** dni kalendarzowych przed dniem wykupu” |

Cztery pola bywają mylące, bo żadne nie jest w liście nazwane wprost:
`capitalizes` i `fee.type` wynikają ze sformułowań w ustępach,
`roundCapitalizedBase` i `principalFloor` — z drobnego druku pod wzorami
w załącznikach. Jeśli nie masz pewności, porównaj swój list z plikiem emisji tej
samej rodziny z poprzedniego miesiąca: każdy z nich ma w polu `source` numery
ustępów i załączników, od których warto zacząć czytanie. Numerów **nie kopiuj** —
przesuwają się między miesiącami, więc odczytaj je z własnego listu.

Cena zamiany (np. 99,90 zł) nie jest obsługiwana: kalkulator modeluje wyłącznie
zakup za gotówkę po cenie równej nominałowi, bo nie liczy podatku od dyskonta.
Plik z niższą ceną zostanie odrzucony przy wczytywaniu — lepiej odmówić, niż
pokazać zawyżony zysk netto.

**Sprawdź plik po dodaniu.** Ta komenda wypisuje każdy okres osobno: jego daty,
stopę, kupon i wartość obligacji na koniec okresu.

```bash
./bonds explain --issue EDO1036 --purchase 2026-10-01
```

Sprawdzaj pierwszy wiersz, bo tylko on opiera się wyłącznie na danych z listu —
dalsze okresy zależą już od prognozy (kolumna `Źródło stopy` to pokazuje).
Kolumna `Wartość na koniec` powinna wyjść równa `100 zł + 100 zł × stopa ÷ liczba
okresów w roku`: dla 5,35% i okresów rocznych to 105,35 zł, dla 4,00% i okresów
miesięcznych — 100,33 zł.

Kolumna `Kupon` od razu weryfikuje pole `capitalizes`. Przy `true` pokazuje 0,00,
bo odsetki zostają w obligacji i widać je w `Wartość na koniec`. Przy `false`
pokazuje wypłacaną kwotę. Jeśli widzisz tam zero, a Twoja obligacja ma wypłacać
odsetki co miesiąc, to `capitalizes` jest ustawione odwrotnie.

Listy krótkoterminowe podają czasem gotowy wynik do porównania: list OTS wylicza
w treści 0,50 zł odsetek i 100,50 zł należności, list TOS — 113,79 zł na dzień
wykupu. Listy ROR, DOR, COI, ROS, EDO i ROD takich przykładów nie zawierają,
więc tam zostaje rachunek na kartce.

> Data zakupu musi mieścić się w okresie sprzedaży emisji. Repozytorium opisuje
> sierpień i wrzesień 2026 r., więc uruchomienie z datą po tym okresie kończy się
> błędem, który wymienia miesiące, dla których dane są. Wtedy dodaj katalog
> nowego miesiąca albo podaj `--purchase` z jednego z tych miesięcy.

Po dodaniu miesiąca uruchom testy: `ShippedIssueMonthsTests` sprawdza, że każdy
katalog ma osiem rodzin obligacji, że okno sprzedaży w plikach zgadza się z nazwą
katalogu i że żaden kod nie powtarza się w dwóch miesiącach. To wyłapuje plik
wrzucony do złego katalogu.

### Comiesięczna rutyna

Cały przebieg — nowe listy, świeże dane rynkowe, przeliczone raporty — jest
opisany jako skill dla Claude Code w
[`.claude/skills/nowy-miesiac/`](.claude/skills/nowy-miesiac/SKILL.md).

Ręcznie najbardziej przydaje się `./bonds-dane letter`, które wypisuje ustępy listu
emisyjnego po numerach i wskazuje, z którego ustępu bierze się które pole:

```bash
./bonds-dane letter zrodla/listy_emisyjne/2026-09/file_409911.pdf --fields
./bonds-dane letter zrodla/listy_emisyjne/2026-09/file_409911.pdf --paragraph 26
./bonds-dane letter zrodla/listy_emisyjne/2026-09/file_409911.pdf --annexes
```

Numeracja ustępów przesuwa się między rodzinami obligacji i między miesiącami —
skutki przedterminowego wykupu są w liście ROR w ust. 28, a w liście COI
w ust. 26 — więc pola `source` nie da się skopiować z poprzedniego miesiąca.
Ta komenda wymaga `pdftotext` z paczki poppler (`brew install poppler`); reszta
kalkulatora nie.

---

## Dokumentacja techniczna

Dokumentacja dla programistów jest po angielsku, tak jak kod; po polsku jest
wszystko, co czyta użytkownik kalkulatora — ten README, komunikaty i raporty.

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — jak liczy: wzór, harmonogram
  okresów, zaokrąglenia, struktura kodu, testy
- [docs/CONVENTIONS.md](docs/CONVENTIONS.md) — standardy kodu i bramki buildu

---

## Licencja i pochodzenie danych

Kod, dokumentacja, parametry emisji w `data/issues/`, scenariusze i raporty
w `out/` są na licencji [Apache 2.0](LICENSE).

Granica licencji jest granicą katalogu: wszystko, co obce, leży w `zrodla/`
i licencja tego nie obejmuje. Są tam listy emisyjne i komunikaty Ministra
Finansów z [obligacjeskarbowe.pl](https://www.obligacjeskarbowe.pl/) (dokumenty
urzędowe) oraz pliki danych ze stron [NBP](https://nbp.pl/). Pochodzenie
każdego pliku — z linkiem do oryginału — opisuje
[zrodla/README.md](zrodla/README.md). Leżą w repozytorium po to, żeby każdy
wynik dało się odtworzyć i sprawdzić u źródła.
