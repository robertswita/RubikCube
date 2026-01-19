# Operatory Algorytmu Genetycznego dla Kostki Rubika

Ten dokument opisuje operatory mutacji i krzyżowania zaimplementowane w solverze kostki Rubika opartym na algorytmie genetycznym.

## Operatory Mutacji

Operatory mutacji wprowadzają losowe zmiany w chromosomach (sekwencjach ruchów), co pozwala na eksplorację przestrzeni rozwiązań i unikanie lokalnych minimów.

### Zaimplementowane

#### SingleGeneMutation (Mutacja pojedynczego genu)
Zamienia dokładnie jeden losowy gen (ruch) na inny losowy, prawidłowy ruch z puli dostępnych ruchów (`FreeMoves`).

**Wpływ na rozwiązywanie:** Wprowadza minimalne, kontrolowane zmiany. Odpowiednik oryginalnego zachowania mutacji w klasycznym GA. Bezpieczna i przewidywalna mutacja.

#### RandomMutation (Mutacja losowa)
Zamienia określoną liczbę genów na nowe losowe wartości. Dla chromosomów Rubika wykorzystuje `ValidMoves`, dla innych typów używa metody `Randomize`.

**Wpływ na rozwiązywanie:** Podobna do SingleGeneMutation, ale z konfigurowalną liczbą mutowanych genów. Większa liczba genów = większa eksploracja, ale potencjalnie destrukcyjne zmiany.

#### SwapMutation (Mutacja zamiany)
Zamienia miejscami dwa losowo wybrane geny w chromosomie.

**Wpływ na rozwiązywanie:** Zmienia kolejność ruchów bez wprowadzania nowych. Może odkryć, że inna kolejność tych samych ruchów prowadzi do lepszego wyniku. Zachowuje "materiał genetyczny".

#### InversionMutation (Mutacja inwersji)
Odwraca kolejność genów w losowo wybranym podsegmencie chromosomu.

**Wpływ na rozwiązywanie:** Radykalna zmiana kolejności w segmencie. Może przypadkowo utworzyć użyteczne wzorce, gdy sekwencja ruchów wykonana "od tyłu" daje interesujący efekt.

#### ScrambleMutation (Mutacja tasowania)
Losowo tasuje geny w wybranym podsegmencie chromosomu (algorytm Fisher-Yates).

**Wpływ na rozwiązywanie:** Podobna do inwersji, ale bardziej losowa. Całkowicie reorganizuje fragment rozwiązania, co może pomóc wydostać się z lokalnego minimum.

#### ConjugationMutation (Mutacja koniugacji)
Implementuje wzorzec koniugacji ABA' z teorii grup. Wybiera losowy punkt w pierwszej połowie genomu i tworzy lustrzane odbicie ruchów z odwróconymi kątami w drugiej połowie.

**Wpływ na rozwiązywanie:** Wykorzystuje właściwości algebraiczne kostki Rubika. Koniugacje są fundamentalnym narzędziem w ręcznym rozwiązywaniu kostki - pozwalają "przenieść" efekt algorytmu w inne miejsce kostki.

#### CommutatorMutation (Mutacja komutatora)
Implementuje wzorzec komutatora ABA'B' z teorii grup. Komutator to sekwencja, która wpływa tylko na niewielką liczbę elementów kostki.

**Wpływ na rozwiązywanie:** Komutatory są kluczowe w zaawansowanym rozwiązywaniu kostki. Pozwalają na precyzyjne manipulowanie małą liczbą kostek (cubies) bez wpływania na resztę. Wzorzec ABA'B' oznacza: wykonaj A, wykonaj B, cofnij A, cofnij B.

#### NeighborMutation (Mutacja sąsiedztwa)
Zmienia ruch na "podobny" - ten sam axis/face ale inny kąt (np. R→R' lub R→R2).

**Wpływ na rozwiązywanie:** Subtelna mutacja, która zachowuje ogólną strukturę rozwiązania. Zamiast całkowicie losowego ruchu, próbuje wariantów tego samego ruchu. Może szybciej znaleźć optymalne rozwiązanie, gdy struktura jest poprawna, ale kąty są złe.

#### SimplifyMutation (Mutacja upraszczająca)
Wykrywa i upraszcza redundantne wzorce.

**Podstawowe uproszczenia (wszystkie wymiary):**
- R R → R2 (dwa obroty 90° = jeden 180°)
- R R R → R' (trzy obroty 90° = jeden -90°)
- R R' → usuń oba (wzajemne anulowanie)

**Rozszerzone uproszczenia dla 4D+ (wzorce ortogonalne):**
W 4D+ ruchy na ortogonalnych płaszczyznach komutują (można je wykonać w dowolnej kolejności):
- (0,1) ⊥ (2,3) — XY ⊥ ZW
- (0,2) ⊥ (1,3) — XZ ⊥ YW
- (0,3) ⊥ (1,2) — XW ⊥ YZ

Nowe możliwości upraszczania:
1. **Wykrywanie par przez ortogonalne:** Wzorzec A, B, A gdzie B ⊥ A może być uproszczony do B, A²
2. **Reordering ortogonalnych ruchów:** Zamiana kolejności ruchów na ortogonalnych płaszczyznach może stworzyć okazje do uproszczenia
3. **Okno wyszukiwania:** Zamiast sprawdzać tylko 2 kolejne ruchy, sprawdza okno do 4 ruchów

**Wpływ na rozwiązywanie:** Optymalizuje długość rozwiązania bez zmiany efektu końcowego. Dla 3D wykrywa bezpośrednie duplikaty. Dla 4D+ dodatkowo wykorzystuje właściwości komutujących ruchów na ortogonalnych płaszczyznach do znajdowania ukrytych możliwości uproszczenia.

#### InverseSequenceMutation (Mutacja odwrotnej sekwencji)
Zastępuje segment jego odwrotnością - odwraca kolejność i invertuje kąt każdego ruchu.

**Wpływ na rozwiązywanie:** Tworzy sekwencję, która "cofa" oryginalny segment. Przydatne gdy część rozwiązania jest zbędna lub prowadzi w złym kierunku.

#### InsertMutation (Mutacja wstawiająca)
Wstawia "neutralną" parę ruchów (np. R R') w losowym miejscu.

**Wpływ na rozwiązywanie:** Pozwala na eksplorację dłuższych rozwiązań bez niszczenia istniejącego postępu. Neutralna para nie zmienia stanu kostki, ale może zostać zmodyfikowana przez późniejsze mutacje w użyteczną sekwencję.

#### ShiftMutation (Mutacja przesunięcia / rotacji cyklicznej)
Wykonuje cykliczne przesunięcie genów w chromosomie. Może przesuwać cały chromosom lub tylko wybrany segment.

**Przykłady:**
- Przesunięcie w prawo: `abcdef → fabcde`
- Przesunięcie w lewo: `abcdef → bcdefa`
- Przesunięcie segmentu: `abCDEfgh → abECDfgh`

**Wpływ na rozwiązywanie:** Jest to wysoce destrukcyjna mutacja, ponieważ ruchy kostki Rubika nie są przemienne (R U ≠ U R). Przesunięta sekwencja zastosowana do tego samego stanu początkowego da zupełnie inny wynik. Jednak może być użyteczna do:
- Ucieczki z lokalnych minimów (podobnie jak ScrambleMutation)
- Eksploracji przestrzeni rozwiązań gdy chromosom zawiera "martwe" sekcje
- Testowania alternatywnych kolejności ruchów

**Uwaga:** Ten operator jest powszechnie stosowany w problemach permutacyjnych (np. TSP), gdzie punkt startowy nie ma znaczenia. W przypadku kostki Rubika jest mniej naturalny, ale może służyć jako narzędzie eksploracyjne.

**Implementacja:** Używa efektywnego algorytmu odwracania (reversal algorithm) o złożoności O(n) do wykonania przesunięcia w miejscu.

#### AdaptiveMutation (Mutacja adaptacyjna)
Dostosowuje intensywność mutacji na podstawie fitness chromosomu, balansując eksplorację i eksploatację.

**Zasada działania:**
- Słaby fitness (wysoka wartość) → agresywna mutacja (więcej genów, większe zmiany)
- Dobry fitness (niska wartość) → łagodna mutacja (mniej genów, mniejsze zmiany)

**Intensywność obliczana jako:**
```
intensity = (fitness - minFitness) / (maxFitness - minFitness)
genesToMutate = minGenes + intensity * (maxGenes - minGenes)
```

**Strategie w zależności od intensywności:**
- Wysoka (>70%): Mutacja tasująca (ScrambleMutation) - agresywna restrukturyzacja
- Średnia (30-70%): Losowa wymiana genów (RandomMutation)
- Niska (<30%): Mutacja sąsiedztwa (NeighborMutation) - drobne zmiany kątów

**Wpływ na rozwiązywanie:** Pozwala algorytmowi automatycznie dostosować strategię:
- Gdy rozwiązanie jest dalekie od optimum, eksploruje szeroko
- Gdy rozwiązanie jest bliskie optimum, dostraja precyzyjnie

Jest to operator "meta" łączący zalety wielu prostszych operatorów, wybierając odpowiedni w zależności od jakości chromosomu.

#### DisplacementMutation (Mutacja przemieszczenia)
Usuwa segment z jednej pozycji i wstawia go w innym miejscu chromosomu.

**Algorytm:**
1. Wybierz losowy segment [start, end)
2. Wybierz losowy punkt wstawienia poza segmentem
3. Usuń segment i wstaw go w nowej pozycji

**Przykład:**
```
Oryginał:     [A B C D E F G H]
Segment:      [C D E] (pozycje 2-4)
Wstaw na:     pozycję 6
Wynik:        [A B F G C D E H]
```

**Wpływ na rozwiązywanie:** Zachowuje cały materiał genetyczny, ale zmienia jego układ. Dla kostki Rubika może odkryć, że ta sama sekwencja ruchów wykonana w innym miejscu rozwiązania daje lepsze wyniki.

#### TranslocationMutation (Mutacja translokacji)
Zamienia miejscami dwa nienachodzące się segmenty.

**Algorytm:**
1. Wybierz pierwszy segment [start1, end1)
2. Wybierz drugi segment [start2, end2) nieprzecinający się z pierwszym
3. Zamień oba segmenty miejscami

**Przykład:**
```
Oryginał:  [A B C D E F G H I J]
Segment1:  [B C] (pozycje 1-2)
Segment2:  [F G H] (pozycje 5-7)
Wynik:     [A F G H D E B C I J]
```

**Wpływ na rozwiązywanie:** Zamienia dwa "pod-algorytmy" w rozwiązaniu. Może odkryć, że inna kolejność bloków ruchów prowadzi do lepszego wyniku. Przydatne gdy rozwiązanie zawiera kilka niezależnych sekwencji.

#### CreepMutation (Mutacja pełzająca)
Wprowadza małe, przyrostowe zmiany w genach.

**Dla kostki Rubika:**
- Zmienia kąty o ±1 (90° ↔ 180° ↔ -90°)
- Zmienia warstwę (slice) o ±1 (dla większych kostek)
- Zmienia płaszczyznę na sąsiednią

**Dla optymalizacji ciągłej:**
- Dodaje małe losowe wartości w zakresie [-creepRange, +creepRange]

**Wpływ na rozwiązywanie:** Idealna do precyzyjnego dostrajania rozwiązań bliskich optimum. Zamiast dużych skoków, wprowadza subtelne modyfikacje. Inspirowana strategiami ewolucyjnymi (ES), gdzie małe mutacje kumulują się przez pokolenia.

#### GaussianMutation (Mutacja gaussowska)
Dodaje szum o rozkładzie normalnym (Gaussa) do genów.

**Formuła:** noise ~ N(0, σ²)

Parametr σ (sigma) kontroluje siłę mutacji:
- Małe σ: Lokalne przeszukiwanie o drobnej granularności
- Duże σ: Bardziej eksploracyjne mutacje

**Dla kostki Rubika (dyskretne ruchy):**
- Mały szum (|noise| < 1): Tylko zmiana kąta
- Średni szum (1 < |noise| < 2): Zmiana kąta + warstwy
- Duży szum (|noise| > 2): Całkowita wymiana genu

**Wpływ na rozwiązywanie:** Rozkład Gaussa zapewnia, że większość mutacji jest małych, ale czasem zdarzają się większe skoki. To naturalnie balansuje eksploatację (małe zmiany) z eksploracją (duże zmiany). Powszechnie stosowana w strategiach ewolucyjnych (ES) i CMA-ES.

**Implementacja:** Wykorzystuje transformację Box-Mullera do generowania liczb z rozkładu normalnego.

#### HyperplaneMutation (Mutacja hyperplanarowa)
Transformuje ruchy między różnymi hiperpłaszczyznami (3D "komórkami") w kostkach 4D+.

**Struktura N-wymiarowej kostki:**
- Kostka 3D: 6 ścian (komórki 2D)
- Kostka 4D: 8 komórek (kostki 3D jako "ściany")
- Kostka 5D: 10 komórek (hiperkostki 4D)

**Algorytm:**
1. Wybierz losowy gen (ruch)
2. Przesuń oś ruchu o losowe przesunięcie: `newAxis = (axis + offset) % N`
3. Znajdź odpowiadającą płaszczyznę rotacji dla nowej osi
4. Zachowaj warstwę (slice) i kąt rotacji

**Przykład (kostka 4D):**
```
Oryginalny ruch: Axis=0, Slice=1, Plane=0 (rotacja XY na warstwie 1 osi X)
Po mutacji z offset=2: Axis=2, Slice=1, Plane=? (odpowiadająca płaszczyzna)
```

**Zachowywane właściwości:**
- Pozycja warstwy (ta sama względna pozycja w nowej hiperpłaszczyźnie)
- Kąt rotacji
- Strukturalna relacja między osią a płaszczyzną (gdzie to możliwe)

**Wpływ na rozwiązywanie:** Dla kostki 3D działa jak "rotacja układu współrzędnych" ruchu. Dla 4D+ eksploruje symetrie między różnymi hiperpłaszczyznami, co może pomóc w znajdowaniu rozwiązań działających w wyższych wymiarach. Szczególnie użyteczna gdy algorytm utknął w lokalnym minimum specyficznym dla jednej hiperpłaszczyzny.

#### OrthogonalConjugationMutation (Koniugacja ortogonalna)
Tworzy komutatory (wzorzec ABA'B') używając ruchów na ortogonalnych płaszczyznach.

**Ortogonalność płaszczyzn:**
Dwie płaszczyzny rotacji są ortogonalne, jeśli nie współdzielą żadnej wspólnej osi:
- 3D (3 płaszczyzny): Brak par ortogonalnych (każda para dzieli jedną oś)
- 4D (6 płaszczyzn): 3 pary ortogonalne:
  - (0,1) ⊥ (2,3) - XY ortogonalna do ZW
  - (0,2) ⊥ (1,3) - XZ ortogonalna do YW
  - (0,3) ⊥ (1,2) - XW ortogonalna do YZ
- 5D+: Więcej par ortogonalnych

**Algorytm:**
1. Wybierz losowy ruch A z chromosomu
2. Znajdź płaszczyznę ortogonalną do płaszczyzny A (lub najbardziej odległą dla 3D)
3. Wygeneruj ruch B na ortogonalnej płaszczyźnie
4. Wstaw wzorzec komutatora: A, B, A', B' na pozycji

**Przykład (kostka 4D):**
```
Ruch A: Plane=0 (XY)
Ortogonalna płaszczyzna: Plane=5 (ZW)
Ruch B: losowy ruch na płaszczyźnie ZW
Wynik: [A_XY, B_ZW, A'_XY, B'_ZW]
```

**Specjalne właściwości ortogonalnych komutatorów:**
- Wpływają na mniejszą liczbę elementów niż dowolne komutatory
- Ruchy lepiej "komutują" - mniejsza interferencja między A i B
- Tworzą bardziej "geometrycznie czyste" transformacje

**Wpływ na rozwiązywanie:** Dla 4D+ kostek, ortogonalne komutatory są szczególnie efektywne do precyzyjnego przestawiania małej liczby elementów bez zakłócania reszty kostki. Dla 3D używa płaszczyzn o minimalnym nakładaniu się jako przybliżenia.

#### PatternMutation (Mutacja wzorcowa)
Wstawia znane algorytmy speedcubingowe do chromosomu.

**Dla kostek 3D:**
- Sexy move: R U R' U' (bardzo powszechny trigger)
- Inverse sexy: U R U' R'
- Sledgehammer: R' F R F'
- Hedgeslammer: F R' F' R
- Sune: R U R' U R U2 R'
- Anti-Sune: R U2 R' U' R U' R'
- Double sexy: (R U R' U')2
- T-perm trigger: R U R' F'
- Warianty leworęczne

**Dla kostek 4D+:**
- Uogólnione komutatory: A B A' B' dla różnych kombinacji płaszczyzn
- Podwójne komutatory: (A B A' B')2
- Wzorce koniugacji: A B A', A2 B A2

**Implementacja:**
- Cache wzorców budowany przy pierwszym użyciu dla aktualnego wymiaru
- Losowy wzorzec wstawiany na losowej pozycji w chromosomie
- Wzorce dostosowane do rozmiaru kostki (slice = lastSlice dla ruchów zewnętrznych)

**Wpływ na rozwiązywanie:** Przyspiesza konwergencję GA poprzez wprowadzanie sprawdzonych sekwencji. Te algorytmy są wynikiem dziesięcioleci optymalizacji przez speedcuberów i reprezentują efektywne manipulacje kostki. GA może budować na tych fundamentach zamiast odkrywać je od nowa.

#### BlockBuildingMutation (Mutacja budowania bloków)
Wstawia sekwencje budowania bloków z popularnych metod rozwiązywania.

**Dla kostek 3D - CFOP/Roux:**

*F2L (First Two Layers) - wstawianie par:*
- R U R' (wstawienie pary z góry)
- R U' R' (alternatywny kąt)
- R U2 R' (obrót 180°)
- F' U F (wstawienie frontowe)
- U R U' R' (setup + wstawienie)

*Roux - budowanie bloków:*
- M U M' (ruchy środkowej warstwy)
- M' U M (odwrotność)

*Cross - budowanie krzyża:*
- F R (proste wstawienie krawędzi)
- R' D' R (sprowadzenie krawędzi)
- D R' D' R (setup krzyża)

*Elementy warstwa-po-warstwie:*
- R' D' R D (skręcenie narożnika)
- L D L' D' (wariant lewy)
- R2 U2, F2 R2 (szybkie korekty)

**Dla kostek 4D+:**
- Uogólnione bloki A B A' dla różnych płaszczyzn
- Komutatory A B A' B' między płaszczyznami
- Wzorce A2 B2 do korekty warstw
- Koordynacja wewnętrznych/zewnętrznych warstw dla większych kostek

**Wpływ na rozwiązywanie:** Wykorzystuje wiedzę domenową z metod CFOP i Roux. Te "building blocks" to sprawdzone sposoby efektywnego rozwiązywania fragmentów kostki. Dla GA, wprowadzenie tych wzorców może znacząco przyspieszyć znalezienie częściowych rozwiązań, które potem mogą być łączone i optymalizowane.

#### LocalSearchMutation (Mutacja z lokalnym przeszukiwaniem)
Wykonuje hill-climbing w małym sąsiedztwie, próbując wielu małych modyfikacji i zachowując najlepszą.

**Parametry:**
- neighborhoodSize: liczba kandydackich modyfikacji na iterację (domyślnie 5)
- maxIterations: maksymalna liczba iteracji hill-climbingu (domyślnie 3)

**Typy modyfikacji (losowo wybierane):**
1. **Zmiana kąta:** Zmienia kąt losowego ruchu na inny (90°→180°, 180°→-90°, itd.)
2. **Uproszczenie sąsiednich:** Jeśli dwa sąsiednie ruchy są na tym samym axis/plane/slice, łączy je
3. **Zamiana na sąsiada:** Zmienia warstwę o ±1 lub kąt o 1
4. **Usunięcie par anulujących:** Szuka par typu R R' i zastępuje losowymi ruchami
5. **Zamiana z ValidMoves:** Zastępuje gen losowym prawidłowym ruchem

**Ocena kandydatów:**
- Z ustawionym baseCube: rzeczywista ocena fitness na kopii kostki
- Bez baseCube: heurystyki (kary za pary anulujące, nagrody za różnorodność)

**Algorytm:**
```
dla i = 1 do maxIterations:
    kandydaci = generuj neighborhoodSize modyfikacji
    najlepszy = oceń kandydatów
    jeśli najlepszy.score < obecny.score:
        zastosuj najlepszy
    w przeciwnym razie:
        przerwij (brak poprawy)
```

**Wpływ na rozwiązywanie:** Łączy zalety GA (globalna eksploracja) z lokalnym przeszukiwaniem (precyzyjna optymalizacja). Każda mutacja nie tylko wprowadza zmianę, ale aktywnie szuka najlepszej zmiany w okolicy. Szczególnie skuteczna w końcowych fazach ewolucji, gdy rozwiązanie jest blisko optimum.

---

## Operatory Krzyżowania

Operatory krzyżowania łączą materiał genetyczny z dwóch rodziców, tworząc potomstwo z cechami obu.

### Zaimplementowane

#### SinglePointCrossover (Krzyżowanie jednopunktowe)
Wybiera losowy punkt podziału i tworzy dzieci poprzez wymianę segmentów:
- Dziecko 1: geny rodzica1[0..punkt) + geny rodzica2[punkt..koniec)
- Dziecko 2: geny rodzica2[0..punkt) + geny rodzica1[punkt..koniec)

**Wpływ na rozwiązywanie:** Klasyczne krzyżowanie. Łączy "początek" jednego rozwiązania z "końcem" innego. Działa dobrze gdy dobre cechy są zgrupowane w ciągłych segmentach.

#### TwoPointCrossover (Krzyżowanie dwupunktowe)
Wybiera dwa punkty i wymienia segment między nimi:
- Dziecko 1: rodzic1[0..p1) + rodzic2[p1..p2) + rodzic1[p2..koniec)
- Dziecko 2: rodzic2[0..p1) + rodzic1[p1..p2) + rodzic2[p2..koniec)

**Wpływ na rozwiązywanie:** Pozwala na wymianę "środkowej" części rozwiązania. Może zachować zarówno początek jak i koniec dobrego rozwiązania, wymieniając tylko problematyczny środek.

#### UniformCrossover (Krzyżowanie równomierne)
Każdy gen jest niezależnie wybierany z jednego lub drugiego rodzica z zadanym prawdopodobieństwem (domyślnie 50%).

**Wpływ na rozwiązywanie:** Maksymalne mieszanie materiału genetycznego. Nie zachowuje ciągłych bloków, ale pozwala na bardzo szczegółową rekombinację. Przydatne gdy dobre cechy są rozproszone po całym chromosomie.

#### SegmentPreservingCrossover (Krzyżowanie zachowujące segmenty)
Identyfikuje "dobre" podsekwencje (na podstawie lokalnej poprawy fitness) i zachowuje je podczas krzyżowania.

**Wpływ na rozwiązywanie:** Inteligentne krzyżowanie, które rozpoznaje wartościowe fragmenty rozwiązania i chroni je przed zniszczeniem. Wymaga dodatkowej analizy fitness, ale może znacząco przyspieszyć konwergencję, zachowując odkryte "budulce" dobrego rozwiązania.

#### OrderCrossover / OX (Krzyżowanie z zachowaniem kolejności)
Operator zaprojektowany do zachowania względnej kolejności genów z rodziców.

**Algorytm:**
1. Wybierz dwa losowe punkty krzyżowania
2. Skopiuj segment między punktami z Rodzica1 do Dziecka1
3. Wypełnij pozostałe pozycje genami z Rodzica2 w kolejności, zaczynając od pozycji za drugim punktem i zawijając

**Przykład:**
```
P1: [A B | C D E | F G H]  (segment oznaczony |)
P2: [H G F E D C B A]

Dziecko1: [G F | C D E | B A]  (segment z P1, reszta z P2 w kolejności)
```

**Wpływ na rozwiązywanie:** OX zachowuje "bloki budulcowe" z jednego rodzica (segment) jednocześnie inkorporując względną kolejność ruchów z drugiego rodzica. Dla kostki Rubika, gdzie kolejność ruchów jest kluczowa, może odkryć efektywne kombinacje sekwencji.

**Uwaga:** OX został pierwotnie zaprojektowany dla problemów permutacyjnych (TSP), gdzie każdy element występuje dokładnie raz. Dla kostki Rubika (gdzie geny mogą się powtarzać) operator został zaadaptowany do zachowania ducha algorytmu.

#### PMXCrossover / PMX (Krzyżowanie z częściowym odwzorowaniem)
Partially Mapped Crossover - operator utrzymujący relacje pozycyjne między genami.

**Algorytm:**
1. Wybierz dwa punkty krzyżowania
2. Skopiuj segment z P1 do C1 na te same pozycje
3. Utwórz mapowania dla segmentu (gen_P1 ↔ gen_P2)
4. Dla każdej pozycji poza segmentem:
   - Jeśli gen z P2 nie występuje w segmencie, użyj go bezpośrednio
   - Jeśli występuje, podążaj za łańcuchem mapowań aż znajdziesz gen spoza segmentu

**Przykład (przypadek permutacyjny):**
```
P1: [1 2 | 3 4 5 | 6 7 8]
P2: [4 7 | 2 5 1 | 8 3 6]

Segment: pozycje 2-4
Mapowania z segmentu: 3↔2, 4↔5, 5↔1

Dziecko1 segment: [_ _ | 3 4 5 | _ _ _]
Pozycja 0: P2[0]=4, 4 jest w segmencie, mapuj 4→5→1, użyj 1
Pozycja 1: P2[1]=7, nie w segmencie, użyj 7
...
```

**Wpływ na rozwiązywanie:** PMX zachowuje absolutne pozycje genów lepiej niż OX. Dla kostki Rubika może być użyteczny gdy określone ruchy na określonych pozycjach chromosomu mają szczególne znaczenie. Mapowanie zapewnia bardziej "płynne" przejście między materiałem genetycznym rodziców.

**Uwaga:** Implementacja obsługuje cykle w mapowaniach (które mogą wystąpić gdy geny się powtarzają) poprzez detekcję i przerwanie nieskończonych pętli.

#### CycleCrossover / CX (Krzyżowanie cyklowe)
Cycle Crossover - operator identyfikujący cykle między rodzicami i naprzemiennie dziedziczący z nich.

**Algorytm:**
1. Znajdź wszystkie cykle pozycji między P1 i P2:
   - Zacznij od pozycji 0, weź gen z P1
   - Znajdź ten gen w P2, przejdź do tej pozycji
   - Powtarzaj aż wrócisz do początku
2. Dla kolejnych cykli naprzemiennie przypisuj:
   - Cykl nieparzysty: C1 dostaje geny z P1, C2 z P2
   - Cykl parzysty: C1 dostaje geny z P2, C2 z P1

**Przykład:**
```
Pozycja: [0  1  2  3  4  5  6  7]
P1:      [1  2  3  4  5  6  7  8]
P2:      [8  4  6  1  2  3  5  7]

Cykl zaczynając od pozycji 0:
- P1[0]=1, znajdź 1 w P2 → pozycja 3
- P1[3]=4, znajdź 4 w P2 → pozycja 1
- P1[1]=2, znajdź 2 w P2 → pozycja 4
- P1[4]=5, znajdź 5 w P2 → pozycja 6
- P1[6]=7, znajdź 7 w P2 → pozycja 7
- P1[7]=8, znajdź 8 w P2 → pozycja 0 (powrót)
Pozycje w cyklu: {0, 1, 3, 4, 6, 7}

C1: P1 na pozycjach cyklu, P2 gdzie indziej
[1  2  6  4  5  3  7  8]
```

**Wpływ na rozwiązywanie:** CX zachowuje ścisłe relacje pozycja-wartość w obrębie cykli. Dla kostki Rubika, dzieci dziedziczą spójne "bloki" od każdego rodzica bez rozbijania powiązań pozycyjnych.

#### EdgeRecombinationCrossover / ERX (Krzyżowanie z rekombinacją krawędzi)
Edge Recombination - operator zachowujący relacje sąsiedztwa między genami.

**Algorytm:**
1. Zbuduj tablicę krawędzi: dla każdego genu wypisz jego sąsiadów z obu rodziców
2. Zacznij od genu z najmniejszą liczbą krawędzi
3. Powtarzaj aż dziecko będzie kompletne:
   a. Usuń bieżący gen ze wszystkich list sąsiadów
   b. Jeśli bieżący gen ma sąsiadów, wybierz tego z najmniejszą liczbą pozostałych krawędzi
   c. W przeciwnym razie wybierz losowy nieodwiedzony gen

**Przykład:**
```
P1: [A B C D E]
P2: [B D A C E]

Tablica krawędzi:
A: {B, D, C} (B z P1, D i C z P2)
B: {A, C, D} (A i C z P1, D z P2)
C: {B, D, A, E} (B i D z P1, A i E z P2)
D: {C, E, B, A} (C i E z P1, B i A z P2)
E: {D, C} (D z P1, C z P2)

Budowanie dziecka od A:
- Wybierz A, sąsiedzi to {B, D, C}
- B ma 2 krawędzie, D ma 3, C ma 3 → wybierz B
- ...
```

**Wpływ na rozwiązywanie:** ERX zachowuje lokalne sąsiedztwa ruchów z obu rodziców. Dla kostki Rubika jest szczególnie przydatny, ponieważ sąsiednie ruchy często tworzą znaczące wzorce (ruchy setupowe, triggery). Dzieci zachowują te lokalne struktury.

---

## Operatory Selekcji

Operatory selekcji wybierają osobniki do reprodukcji na podstawie ich fitness.

### Zaimplementowane

#### Rank (Selekcja rankingowa)
Wybiera najlepszych N osobników według fitness.

#### Tournament (Selekcja turniejowa)
Losowo wybiera grupę osobników i zwycięzca (najlepszy fitness) przechodzi do puli rodziców.

#### Roulette (Selekcja ruletkowa)
Prawdopodobieństwo wyboru proporcjonalne do fitness.

#### RouletteRank (Selekcja ruletkowa z rangą)
Prawdopodobieństwo wyboru proporcjonalne do pozycji w rankingu, nie do wartości fitness.

#### Unique (Selekcja unikalna)
Wybiera osobniki o unikalnych wartościach fitness, promując różnorodność.

#### SUS (Stochastic Universal Sampling - Stochastyczne Próbkowanie Uniwersalne)
Ulepszenie selekcji ruletkowej. Zamiast N losowych "rzutów ruletką", używa pojedynczej losowej wartości startowej i N równomiernie rozmieszczonych wskaźników.

**Zalety nad klasyczną ruletką:**
- Zero biasu: oczekiwana liczba kopii równa się rzeczywistej liczbie wybranych
- Minimalna wariancja: różnica między oczekiwaną a rzeczywistą liczbą jest zminimalizowana
- Lepsza konserwacja różnorodności populacji

**Wpływ na rozwiązywanie:** Daje słabszym osobnikom lepszą szansę na selekcję w porównaniu do standardowej ruletki, co pomaga w utrzymaniu różnorodności genetycznej i unikaniu przedwczesnej konwergencji.

#### Boltzmann (Selekcja Boltzmanna)
Prawdopodobieństwo wyboru oparte na rozkładzie Boltzmanna: P(i) = exp(-fitness[i] / T) / Σexp(-fitness[j] / T)

Parametr T (temperatura) kontroluje presję selekcyjną:
- Wysoka T (50-100): Prawie równomierna selekcja, promuje eksplorację
- Niska T (1-10): Silne faworyzowanie najlepszych osobników, promuje eksploatację

**Wpływ na rozwiązywanie:** Szczególnie użyteczna dla:
- Unikania przedwczesnej konwergencji we wczesnych generacjach (wysoka T)
- Precyzyjnego strojenia rozwiązań w późniejszych generacjach (niska T)
- Utrzymania różnorodności populacji przy jednoczesnym faworyzowaniu lepszych osobników

**Inspiracja:** Mechanizm zaczerpnięty z symulowanego wyżarzania (simulated annealing), gdzie temperatura kontroluje prawdopodobieństwo akceptacji gorszych rozwiązań.

#### Truncation (Selekcja obcinająca)
Tylko górne k% populacji jest uprawnione do selekcji. Spośród tej elitarnej puli wybór jest równomierny.

**Parametr:** truncationRate (0.0 - 1.0)
- 0.5 (50%): Umiarkowana presja, zbalansowana eksploracja/eksploatacja
- 0.25 (25%): Silna presja, szybsza konwergencja ale ryzyko przedwczesnej zbieżności
- 0.1 (10%): Bardzo silna presja, używana w strategiach ewolucyjnych (μ,λ)

**Wpływ na rozwiązywanie:** Tworzy silną presję selekcyjną poprzez całkowite wykluczenie dolnej części populacji z reprodukcji. Proste do zrozumienia i implementacji, deterministyczne obcinanie z losowym wyborem z elity.

#### LinearRanking (Liniowa selekcja rankingowa)
Prawdopodobieństwo selekcji jest liniowo proporcjonalne do rangi osobnika.

**Formuła:** P(i) = (2 - s)/N + 2*(rank - 1)*(s - 1)/(N*(N - 1))

gdzie s to parametr presji selekcyjnej (1.0 - 2.0):
- s = 1.0: Selekcja równomierna (wszystkie równe prawdopodobieństwo)
- s = 2.0: Maksymalna presja liniowa (najlepszy ma 2x średniego prawdopodobieństwa, najgorszy 0)
- s = 1.5: Umiarkowana presja (zalecana wartość domyślna)

**Przykład (N=5, s=1.5):**
```
Rangi (najlepszy do najgorszego): 5, 4, 3, 2, 1
Prawdopodobieństwa: 0.30, 0.25, 0.20, 0.15, 0.10
```

**Zalety nad ruletką fitness-proporcjonalną:**
- Unika dominacji przez super-dopasowanych osobników
- Działa dobrze gdy wartości fitness mają dużą wariancję
- Utrzymuje stałą presję selekcyjną niezależnie od skalowania fitness

#### ExponentialRanking (Wykładnicza selekcja rankingowa)
Prawdopodobieństwo selekcji maleje wykładniczo z rangą.

**Formuła:** P(i) = base^rank / Σ(base^rank)

**Parametr:** base (0.0 - 1.0)
- Wartość bliska 1.0 (np. 0.99): Łagodny spadek, więcej równomierna selekcja
- Wartość bliższa 0.0 (np. 0.9): Stromy spadek, silna presja na czołowych osobników

**Przykład (N=5, base=0.9):**
```
Rangi (najlepszy do najgorszego): 1, 2, 3, 4, 5
Surowe wartości: 0.9^1, 0.9^2, 0.9^3, 0.9^4, 0.9^5 = 0.9, 0.81, 0.729, 0.656, 0.590
Prawdopodobieństwa (znormalizowane): 0.244, 0.220, 0.198, 0.178, 0.160
```

**Wpływ na rozwiązywanie:** Silniejsze różnicowanie między czołowymi osobnikami niż liniowe rankowanie. Dolni osobnicy nadal mają niezerowe (ale bardzo małe) prawdopodobieństwo. Dobre do precyzyjnego strojenia gdy populacja się zbiega.

---

## Teoria grup i kostka Rubika

Kostka Rubika jest doskonałym przykładem grupy w sensie matematycznym. Zbiór wszystkich możliwych stanów kostki z operacją składania ruchów tworzy grupę Rubika.

### Kluczowe pojęcia:

**Koniugacja (ABA'):** Wykonaj A, wykonaj B, cofnij A. Efekt: "przenosi" działanie B w miejsce określone przez A.

**Komutator (ABA'B'):** Wykonaj A, wykonaj B, cofnij A, cofnij B. Efekt: wpływa tylko na elementy, które są różnie traktowane przez A i B.

Te konstrukcje są wykorzystywane w zaawansowanych metodach rozwiązywania kostki (np. CFOP, Roux) i są inspiracją dla operatorów domenowych w naszym GA.
