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

### Planowane

#### CommutatorMutation (Mutacja komutatora)
Implementuje wzorzec komutatora ABA'B' z teorii grup. Komutator to sekwencja, która wpływa tylko na niewielką liczbę elementów kostki.

**Wpływ na rozwiązywanie:** Komutatory są kluczowe w zaawansowanym rozwiązywaniu kostki. Pozwalają na precyzyjne manipulowanie małą liczbą kostek (cubies) bez wpływania na resztę. Wzorzec ABA'B' oznacza: wykonaj A, wykonaj B, cofnij A, cofnij B.

#### NeighborMutation (Mutacja sąsiedztwa)
Zmienia ruch na "podobny" - ten sam axis/face ale inny kąt (np. R→R' lub R→R2).

**Wpływ na rozwiązywanie:** Subtelna mutacja, która zachowuje ogólną strukturę rozwiązania. Zamiast całkowicie losowego ruchu, próbuje wariantów tego samego ruchu. Może szybciej znaleźć optymalne rozwiązanie, gdy struktura jest poprawna, ale kąty są złe.

#### SimplifyMutation (Mutacja upraszczająca)
Wykrywa i upraszcza redundantne wzorce:
- R R → R2 (dwa obroty 90° = jeden 180°)
- R R R → R' (trzy obroty 90° = jeden -90°)
- R R' → usuń oba (wzajemne anulowanie)

**Wpływ na rozwiązywanie:** Optymalizuje długość rozwiązania bez zmiany efektu końcowego. Krótsze rozwiązania są preferowane, a ta mutacja aktywnie je skraca. Może być stosowana jako krok post-processingu.

#### InverseSequenceMutation (Mutacja odwrotnej sekwencji)
Zastępuje segment jego odwrotnością - odwraca kolejność i invertuje kąt każdego ruchu.

**Wpływ na rozwiązywanie:** Tworzy sekwencję, która "cofa" oryginalny segment. Przydatne gdy część rozwiązania jest zbędna lub prowadzi w złym kierunku.

#### InsertMutation (Mutacja wstawiająca)
Wstawia "neutralną" parę ruchów (np. R R') w losowym miejscu.

**Wpływ na rozwiązywanie:** Pozwala na eksplorację dłuższych rozwiązań bez niszczenia istniejącego postępu. Neutralna para nie zmienia stanu kostki, ale może zostać zmodyfikowana przez późniejsze mutacje w użyteczną sekwencję.

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

### Planowane

#### SegmentPreservingCrossover (Krzyżowanie zachowujące segmenty)
Identyfikuje "dobre" podsekwencje (na podstawie lokalnej poprawy fitness) i zachowuje je podczas krzyżowania.

**Wpływ na rozwiązywanie:** Inteligentne krzyżowanie, które rozpoznaje wartościowe fragmenty rozwiązania i chroni je przed zniszczeniem. Wymaga dodatkowej analizy fitness, ale może znacząco przyspieszyć konwergencję, zachowując odkryte "budulce" dobrego rozwiązania.

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

---

## Teoria grup i kostka Rubika

Kostka Rubika jest doskonałym przykładem grupy w sensie matematycznym. Zbiór wszystkich możliwych stanów kostki z operacją składania ruchów tworzy grupę Rubika.

### Kluczowe pojęcia:

**Koniugacja (ABA'):** Wykonaj A, wykonaj B, cofnij A. Efekt: "przenosi" działanie B w miejsce określone przez A.

**Komutator (ABA'B'):** Wykonaj A, wykonaj B, cofnij A, cofnij B. Efekt: wpływa tylko na elementy, które są różnie traktowane przez A i B.

Te konstrukcje są wykorzystywane w zaawansowanych metodach rozwiązywania kostki (np. CFOP, Roux) i są inspiracją dla operatorów domenowych w naszym GA.
