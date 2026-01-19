# Operatory Genetyczne dla N-wymiarowych Kostek Rubika

Ten dokument analizuje kompatybilność operatorów algorytmu genetycznego z kostkami Rubika o wymiarach 4D, 5D i wyższych.

## Spis treści

1. [Kodowanie ruchów w N wymiarach](#kodowanie-ruchów-w-n-wymiarach)
2. [Operatory w pełni kompatybilne z N wymiarami](#operatory-w-pełni-kompatybilne-z-n-wymiarami)
3. [Operatory wymagające optymalizacji dla 4D+](#operatory-wymagające-optymalizacji-dla-4d)
4. [Brakujące operatory specyficzne dla 4D+](#brakujące-operatory-specyficzne-dla-4d)
5. [Krytyczna luka: Podwójne rotacje](#krytyczna-luka-podwójne-rotacje)
6. [Rekomendacje implementacyjne](#rekomendacje-implementacyjne)

---

## Kodowanie ruchów w N wymiarach

### Struktura TMove

Każdy ruch w kostce jest zakodowany za pomocą czterech komponentów w klasie `TMove` (`RubikCube/TMove.cs`):

```csharp
public int Axis;   // Oś prostopadła do płaszczyzny rotacji (0 do N-1)
public int Slice;  // Warstwa wzdłuż osi (0 do Size-1)
public int Plane;  // Płaszczyzna rotacji (0 do C(N,2)-1)
public int Angle;  // Kąt obrotu: 0=90°, 1=180°, 2=-90°
```

### Automatyczne generowanie płaszczyzn

Klasa `TAffine` (`RubikCube/TAffine.cs`) automatycznie generuje wszystkie możliwe płaszczyzny rotacji jako kombinacje dwóch osi:

```csharp
Planes = new int[n * (n - 1) / 2][];  // C(N,2) kombinacji
for (int colIdx = 1; colIdx < n; colIdx++)
    for (int rowIdx = 0; rowIdx < colIdx; rowIdx++)
        Planes[idx] = new int[] { rowIdx, colIdx };
```

### Liczba płaszczyzn dla różnych wymiarów

| Wymiar | Liczba płaszczyzn | Przykładowe płaszczyzny |
|--------|-------------------|-------------------------|
| 3D | 3 | (0,1), (0,2), (1,2) |
| 4D | 6 | (0,1), (0,2), (0,3), (1,2), (1,3), (2,3) |
| 5D | 10 | (0,1), (0,2), (0,3), (0,4), (1,2), (1,3), (1,4), (2,3), (2,4), (3,4) |
| 6D | 15 | C(6,2) = 15 kombinacji |
| ND | N(N-1)/2 | Wszystkie pary (i,j) gdzie i < j |

### Interpretacja geometryczna

W **3D** mamy trzy płaszczyzny rotacji odpowiadające trzem "ścianom" kostki:
- Płaszczyzna (0,1) = rotacja wokół osi Z (ruchy typu U, D)
- Płaszczyzna (0,2) = rotacja wokół osi Y (ruchy typu F, B)
- Płaszczyzna (1,2) = rotacja wokół osi X (ruchy typu R, L)

W **4D** mamy sześć płaszczyzn rotacji. Każda "komórka" (3D hyperściana) 4D kostki może być obracana w jednej z tych płaszczyzn. Dodatkowo pojawiają się ruchy "wewnętrzne" które nie mają odpowiednika w 3D.

W **5D i wyżej** liczba płaszczyzn rośnie szybko, a złożoność interakcji między ruchami znacząco się zwiększa.

---

## Operatory w pełni kompatybilne z N wymiarami

### Operatory generyczne (nie interpretują ruchów)

Te operatory działają wyłącznie na pozycjach genów w chromosomie, nie analizując znaczenia poszczególnych ruchów:

#### SwapMutation (Mutacja zamiany)
**Plik:** `GA/Operators/Mutation/SwapMutation.cs`

**Działanie:** Zamienia miejscami dwa losowo wybrane geny.

**Dlaczego działa dla N wymiarów:** Operator nie dekoduje ruchów - traktuje geny jako abstrakcyjne wartości. Zmiana kolejności ruchów jest operacją niezależną od wymiarowości.

**Przykład:**
```
Przed: [R, U, F', L2, B]
Po:    [R, B, F', L2, U]  // zamieniono pozycje 1 i 4
```

#### InversionMutation (Mutacja inwersji)
**Plik:** `GA/Operators/Mutation/InversionMutation.cs`

**Działanie:** Odwraca kolejność genów w losowo wybranym podsegmencie.

**Dlaczego działa dla N wymiarów:** Odwracanie sekwencji jest operacją czysto pozycyjną. Nie ma znaczenia, czy geny reprezentują ruchy 3D czy 7D.

**Przykład:**
```
Przed: [R, U, F', L2, B, D]
Po:    [R, L2, F', U, B, D]  // odwrócono segment [1..3]
```

#### ScrambleMutation (Mutacja tasowania)
**Plik:** `GA/Operators/Mutation/ScrambleMutation.cs`

**Działanie:** Losowo tasuje geny w wybranym podsegmencie używając algorytmu Fisher-Yates.

**Dlaczego działa dla N wymiarów:** Tasowanie to permutacja pozycji, niezależna od zawartości genów.

#### ShiftMutation (Mutacja przesunięcia / rotacji cyklicznej)
**Plik:** `GA/Operators/Mutation/ShiftMutation.cs`

**Działanie:** Wykonuje cykliczne przesunięcie (rotację) genów w chromosomie. Może przesuwać cały chromosom lub tylko wybrany segment.

**Przykłady:**
```
Przesunięcie w prawo: abcdef → fabcde
Przesunięcie w lewo:  abcdef → bcdefa
Przesunięcie segmentu: abCDEfgh → abECDfgh
```

**Dlaczego działa dla N wymiarów:** Operator wykonuje czysto pozycyjną operację - przesuwa geny cyklicznie bez analizowania ich zawartości. Działa identycznie niezależnie od tego, czy geny reprezentują ruchy 3D, 4D czy dowolnego wyższego wymiaru.

**Uwaga:** Jest to wysoce destrukcyjna mutacja (zmiana kolejności ruchów w kostce Rubika daje zupełnie inny wynik), ale może być użyteczna do ucieczki z lokalnych minimów. Operator jest powszechnie stosowany w problemach permutacyjnych (TSP), gdzie kolejność cykliczna nie ma znaczenia - w kostce Rubika jego zastosowanie jest bardziej eksploracyjne.

**Implementacja:** Używa efektywnego algorytmu odwracania (reversal algorithm) o złożoności O(n).

#### AdaptiveMutation (Mutacja adaptacyjna)
**Plik:** `GA/Operators/Mutation/AdaptiveMutation.cs`

**Działanie:** Meta-operator, który dostosowuje intensywność mutacji na podstawie fitness chromosomu. Łączy trzy strategie:
- Wysoka intensywność (słaby fitness): ScrambleMutation
- Średnia intensywność: RandomMutation
- Niska intensywność (dobry fitness): NeighborMutation

**Dlaczego działa dla N wymiarów:** Operator deleguje do innych operatorów (Scramble, Random, Neighbor), które wszystkie są kompatybilne z N wymiarami. Logika wyboru strategii opiera się wyłącznie na wartości fitness, która jest skalarna niezależnie od wymiarowości.

#### DisplacementMutation (Mutacja przemieszczenia)
**Plik:** `GA/Operators/Mutation/DisplacementMutation.cs`

**Działanie:** Usuwa segment z chromosomu i wstawia go w innej pozycji. Zachowuje cały materiał genetyczny, ale zmienia jego układ.

**Dlaczego działa dla N wymiarów:** Operator wykonuje czysto pozycyjną manipulację - usuwa i wstawia segmenty bez analizowania zawartości genów. Działa identycznie niezależnie od tego, czy geny reprezentują ruchy 3D czy N-wymiarowe.

#### TranslocationMutation (Mutacja translokacji)
**Plik:** `GA/Operators/Mutation/TranslocationMutation.cs`

**Działanie:** Zamienia miejscami dwa nienachodzące się segmenty chromosomu.

**Dlaczego działa dla N wymiarów:** Podobnie jak DisplacementMutation, operuje wyłącznie na pozycjach genów. Zamiana segmentów jest operacją niezależną od semantyki genów.

#### CreepMutation (Mutacja pełzająca)
**Plik:** `GA/Operators/Mutation/CreepMutation.cs`

**Działanie:** Wprowadza małe, przyrostowe zmiany. Dla kostki Rubika zmienia kąty o ±1, warstwy o ±1, lub płaszczyzny na sąsiednie.

**Dlaczego działa dla N wymiarów:** Operator modyfikuje komponenty ruchu (Angle, Slice, Plane) które mają taką samą semantykę we wszystkich wymiarach. Zmiana płaszczyzny na "sąsiednią" działa poprawnie niezależnie od liczby płaszczyzn C(N,2).

#### GaussianMutation (Mutacja gaussowska)
**Plik:** `GA/Operators/Mutation/GaussianMutation.cs`

**Działanie:** Dodaje szum o rozkładzie normalnym. Wielkość szumu determinuje intensywność mutacji - od drobnych zmian kąta po całkowitą wymianę genu.

**Dlaczego działa dla N wymiarów:** Dla dyskretnych ruchów Rubika, operator używa wielkości szumu jako proxy intensywności mutacji, a następnie deleguje do operacji modyfikujących Angle/Slice, które są N-wymiarowo agnostyczne. Dla dużego szumu używa ValidMoves, które są automatycznie generowane dla aktualnego wymiaru.

#### Wszystkie operatory krzyżowania

**Pliki:**
- `GA/Operators/Crossover/SinglePointCrossover.cs`
- `GA/Operators/Crossover/TwoPointCrossover.cs`
- `GA/Operators/Crossover/UniformCrossover.cs`
- `GA/Operators/Crossover/SegmentPreservingCrossover.cs`
- `GA/Operators/Crossover/OrderCrossover.cs`
- `GA/Operators/Crossover/PMXCrossover.cs`
- `GA/Operators/Crossover/CycleCrossover.cs`
- `GA/Operators/Crossover/EdgeRecombinationCrossover.cs`

**Dlaczego działają dla N wymiarów:** Operatory krzyżowania łączą segmenty chromosomów rodziców na podstawie pozycji, nie analizując znaczenia poszczególnych genów. `SegmentPreservingCrossover` używa wartości fitness do identyfikacji "dobrych" segmentów, co również jest niezależne od wymiarowości.

##### OrderCrossover (OX)
**Plik:** `GA/Operators/Crossover/OrderCrossover.cs`

Operator zachowujący względną kolejność genów. Kopiuje segment z jednego rodzica i wypełnia pozostałe pozycje genami z drugiego rodzica w kolejności, zawijając od końca segmentu.

**Dlaczego działa dla N wymiarów:** OX operuje na pozycjach genów w chromosomie, nie analizując ich semantyki. Segment i wypełnienie są czysto pozycyjne, więc operator działa identycznie dla kostki 3D, 4D czy wyższych wymiarów.

##### PMXCrossover (Partially Mapped Crossover)
**Plik:** `GA/Operators/Crossover/PMXCrossover.cs`

Operator utrzymujący relacje pozycyjne poprzez mapowanie. Kopiuje segment i używa łańcucha mapowań do wypełnienia pozostałych pozycji, zachowując bardziej "płynne" przejście między materiałem genetycznym.

**Dlaczego działa dla N wymiarów:** PMX traktuje geny jako abstrakcyjne wartości i buduje mapowania pozycyjne. Mechanizm mapowania jest niezależny od tego, czy geny reprezentują ruchy 3D czy N-wymiarowe.

##### CycleCrossover (CX)
**Plik:** `GA/Operators/Crossover/CycleCrossover.cs`

Operator identyfikujący cykle pozycji między rodzicami. Cykl to zbiór pozycji, gdzie wartości z P1 i P2 tworzą zamkniętą pętlę przy śledzeniu: P1[i] → znajdź w P2 → użyj tej pozycji. Dzieci dziedziczą kompletne cykle naprzemiennie od każdego rodzica.

**Dlaczego działa dla N wymiarów:** CX operuje na abstrakcyjnych wartościach genów i pozycjach w chromosomie. Budowanie cykli polega na porównywaniu wartości i śledzeniu pozycji - mechanizm całkowicie niezależny od semantyki ruchów czy wymiarowości kostki.

##### EdgeRecombinationCrossover (ERX)
**Plik:** `GA/Operators/Crossover/EdgeRecombinationCrossover.cs`

Operator zachowujący relacje sąsiedztwa (krawędzie) z obu rodziców. Buduje tablicę sąsiadów dla każdego genu, następnie konstruuje dziecko wybierając kolejno sąsiadów z najmniejszą liczbą pozostałych krawędzi (zachłanna heurystyka minimalizująca utratę krawędzi).

**Dlaczego działa dla N wymiarów:** ERX analizuje sąsiedztwo pozycji w chromosomie (gen[i] sąsiaduje z gen[i-1] i gen[i+1]), nie semantykę genów. Tablica krawędzi i heurystyka wyboru są oparte na strukturze chromosomu, nie na znaczeniu poszczególnych ruchów.

#### Wszystkie operatory selekcji

**Pliki:**
- `GA/Operators/Selection/TournamentSelection.cs`
- `GA/Operators/Selection/RankSelection.cs`
- `GA/Operators/Selection/RouletteSelection.cs`
- `GA/Operators/Selection/RouletteRankSelection.cs`
- `GA/Operators/Selection/UniqueSelection.cs`
- `GA/Operators/Selection/SUSSelection.cs`
- `GA/Operators/Selection/BoltzmannSelection.cs`
- `GA/Operators/Selection/TruncationSelection.cs`
- `GA/Operators/Selection/LinearRankingSelection.cs`
- `GA/Operators/Selection/ExponentialRankingSelection.cs`

**Dlaczego działają dla N wymiarów:** Selekcja opiera się wyłącznie na wartościach fitness, które są obliczane przez ewaluator niezależnie od operatorów.

##### SUS (Stochastic Universal Sampling)
**Plik:** `GA/Operators/Selection/SUSSelection.cs`

Ulepszenie selekcji ruletkowej - zamiast N losowych "rzutów ruletką", używa pojedynczej losowej wartości startowej i N równomiernie rozmieszczonych wskaźników. Daje zerowy bias i minimalną wariancję, lepiej zachowując różnorodność populacji.

**Dlaczego działa dla N wymiarów:** Operator operuje wyłącznie na wartościach fitness, które są skalarne niezależnie od wymiarowości kostki.

##### Boltzmann Selection
**Plik:** `GA/Operators/Selection/BoltzmannSelection.cs`

Selekcja oparta na rozkładzie Boltzmanna: P(i) = exp(-fitness[i] / T) / Σexp(-fitness[j] / T). Parametr temperatury T kontroluje presję selekcyjną - wysoka T daje prawie równomierną selekcję (eksploracja), niska T silnie faworyzuje lepsze osobniki (eksploatacja).

**Dlaczego działa dla N wymiarów:** Analogicznie do SUS - operuje na skalarnych wartościach fitness, niezależnych od wymiarowości problemu.

##### Truncation Selection
**Plik:** `GA/Operators/Selection/TruncationSelection.cs`

Tylko górne k% populacji jest uprawnione do selekcji (parametr truncationRate). Spośród tej elitarnej puli wybór jest równomierny losowy. Tworzy silną presję selekcyjną poprzez całkowite wykluczenie dolnej części populacji.

**Dlaczego działa dla N wymiarów:** Operuje na posortowanej populacji (według fitness) i indeksach. Nie analizuje genów ani ich znaczenia - jedynie fitness, który jest skalarny niezależnie od wymiarowości.

##### Linear Ranking Selection
**Plik:** `GA/Operators/Selection/LinearRankingSelection.cs`

Prawdopodobieństwo selekcji jest liniowo proporcjonalne do rangi: P(i) = (2 - s)/N + 2*(rank - 1)*(s - 1)/(N*(N - 1)), gdzie s to parametr presji (1.0-2.0). Utrzymuje stałą presję selekcyjną niezależnie od rozkładu wartości fitness.

**Dlaczego działa dla N wymiarów:** Opiera się wyłącznie na randze (pozycji w posortowanej populacji), nie na wartościach genów. Rangi są przypisywane na podstawie fitness, który jest niezależny od wymiarowości.

##### Exponential Ranking Selection
**Plik:** `GA/Operators/Selection/ExponentialRankingSelection.cs`

Prawdopodobieństwo selekcji maleje wykładniczo z rangą: P(i) = base^rank / Σ(base^rank). Daje silniejsze różnicowanie między czołowymi osobnikami niż liniowe rankowanie, przy zachowaniu niezerowego prawdopodobieństwa dla słabszych.

**Dlaczego działa dla N wymiarów:** Analogicznie do Linear Ranking - operuje na rangach opartych na fitness, nie na zawartości chromosomów. Wykładniczy spadek jest funkcją pozycji, nie semantyki genów.

---

### Operatory używające ValidMoves (świadome wymiarowości)

#### SingleGeneMutation (Mutacja pojedynczego genu)
**Plik:** `GA/Operators/Mutation/SingleGeneMutation.cs`

**Działanie:** Zamienia jeden losowy gen na losowy ruch z puli `FreeMoves`.

**Dlaczego działa dla N wymiarów:** Lista `FreeMoves` jest generowana na podstawie aktualnej konfiguracji wymiarowej. Gdy `TAffine.N = 4`, lista zawiera wszystkie prawidłowe ruchy 4D.

**Kod:**
```csharp
if (chromosome is IRubikChromosome rubikChromosome)
{
    chromosome.Genes[idx] = rubikChromosome.FreeMoves[rng.Next(rubikChromosome.FreeMoves.Count)];
}
```

#### RandomMutation (Mutacja losowa)
**Plik:** `GA/Operators/Mutation/RandomMutation.cs`

**Działanie:** Zamienia określoną liczbę genów na nowe losowe wartości z `ValidMoves`.

**Dlaczego działa dla N wymiarów:** Analogicznie do `SingleGeneMutation` - używa listy ruchów świadomej wymiarowości.

---

### Operatory domenowe (manipulacja kątami)

Te operatory dekodują ruchy używając `TMove.Decode/Encode`, ale operują głównie na komponencie `Angle`, który ma taką samą semantykę we wszystkich wymiarach.

#### Formuła inwersji kąta

Wszystkie poniższe operatory używają formuły:
```csharp
move.Angle = 2 - move.Angle;
```

Ta formuła odwraca kierunek obrotu:
- 0 (90°) → 2 (-90°)
- 1 (180°) → 1 (180°) // obrót o 180° jest własną odwrotnością
- 2 (-90°) → 0 (90°)

**Matematyczne uzasadnienie:** W dowolnym wymiarze, odwrotność rotacji o kąt θ to rotacja o kąt -θ. Ponieważ używamy trzech dyskretnych wartości (ćwierćobroty), formuła `2 - angle` poprawnie oblicza odwrotność.

#### ConjugationMutation (Mutacja koniugacji)
**Plik:** `GA/Operators/Mutation/ConjugationMutation.cs`

**Działanie:** Implementuje wzorzec koniugacji ABA' z teorii grup.

**Dlaczego działa dla N wymiarów:** Koniugacja jest fundamentalnym pojęciem teorii grup, które działa niezależnie od reprezentacji grupy. Grupa Rubika w dowolnym wymiarze pozostaje grupą, a koniugacje zachowują swoje właściwości algebraiczne.

**Geometryczna interpretacja:** Koniugacja "przenosi" efekt sekwencji B w miejsce określone przez A. To działa tak samo w 3D, 4D czy 7D - zmienia się tylko przestrzeń, w której operujemy.

#### CommutatorMutation (Mutacja komutatora)
**Plik:** `GA/Operators/Mutation/CommutatorMutation.cs`

**Działanie:** Implementuje wzorzec komutatora ABA'B'.

**Dlaczego działa dla N wymiarów:** Komutatory mierzą "nieprzemienność" dwóch operacji. W kostce Rubika (dowolnego wymiaru) komutator wpływa tylko na elementy, które są różnie traktowane przez A i B. Ta właściwość jest fundamentalna i niezależna od wymiarowości.

**Kod:**
```csharp
// Create A' (inverse of A)
moveAInverse.Angle = 2 - moveA.Angle;
// Create B' (inverse of B)
moveBInverse.Angle = 2 - moveB.Angle;
// Apply: A B A' B'
chromosome.Genes[startIdx + 2] = moveAInverse.Encode();
chromosome.Genes[startIdx + 3] = moveBInverse.Encode();
```

#### NeighborMutation (Mutacja sąsiedztwa)
**Plik:** `GA/Operators/Mutation/NeighborMutation.cs`

**Działanie:** Zmienia kąt ruchu na "sąsiedni" (np. R→R' lub R→R2).

**Dlaczego działa dla N wymiarów:** Operator modyfikuje wyłącznie komponent `Angle`, który ma identyczną semantykę (0, 1, 2 = 90°, 180°, -90°) we wszystkich wymiarach.

#### InverseSequenceMutation (Mutacja odwrotnej sekwencji)
**Plik:** `GA/Operators/Mutation/InverseSequenceMutation.cs`

**Działanie:** Zastępuje segment jego odwrotnością (odwrócona kolejność + odwrócone kąty).

**Dlaczego działa dla N wymiarów:** Odwrotność sekwencji ruchów to uniwersalne pojęcie - wykonanie sekwencji, a następnie jej odwrotności, zawsze przywraca stan początkowy, niezależnie od wymiarowości.

#### InsertMutation (Mutacja wstawiająca)
**Plik:** `GA/Operators/Mutation/InsertMutation.cs`

**Działanie:** Wstawia parę neutralną (ruch + jego odwrotność).

**Dlaczego działa dla N wymiarów:** Para neutralna (np. R R') anuluje się w dowolnym wymiarze. To wynika z definicji grupy - każdy element ma odwrotność.

---

## Zoptymalizowane operatory dla 4D+

### SimplifyMutation - rozszerzona o wzorce ortogonalne ✅ ZOPTYMALIZOWANA

**Plik:** `GA/Operators/Mutation/SimplifyMutation.cs`

**Podstawowa logika (wszystkie wymiary):**
```csharp
if (move1.Axis == move2.Axis && move1.Plane == move2.Plane && move1.Slice == move2.Slice)
{
    // Połącz kąty: R R → R2, R R R → R', R R' → usuń
    CombineMoves(chromosome, startIdx, move1, move2, rng);
}
```

**Rozszerzona logika dla 4D+ (N >= 4):**
```csharp
// Cache par ortogonalnych płaszczyzn
// 4D: (0,1)⊥(2,3), (0,2)⊥(1,3), (0,3)⊥(1,2)

// 1. Wykrywanie par przez ortogonalne ruchy
// Wzorzec: A, B, A gdzie B ⊥ A → można uprościć A i A
for (offset = 2; offset < windowSize; offset++)
{
    if (MovesMatch(move1, move_at_offset) && AllBetweenOrthogonal(move1))
    {
        CombineDistantMoves(chromosome, idx1, idx2, move1, move2);
    }
}

// 2. Reordering ortogonalnych ruchów (30% szans)
// Zamiana kolejności może stworzyć okazje do uproszczenia
if (AreOrthogonal(moveA.Plane, moveB.Plane))
{
    SwapMoves(chromosome, startIdx, startIdx + 1);
}
```

**Dlaczego działa dla N wymiarów:**
- Dla N >= 4: Automatycznie buduje cache par ortogonalnych z `TAffine.Planes`
- Dla N = 3: Brak par ortogonalnych, używa tylko podstawowej logiki
- Wykrywanie ortogonalności jest w pełni parametryczne względem N

**Kompatybilność:**
- 3D: ✅ Podstawowa logika (bez zmian funkcjonalności)
- 4D: ✅ Pełne wykorzystanie 3 par ortogonalnych
- 5D+: ✅ Automatyczne wykrywanie większej liczby par ortogonalnych

---

## Operatory wymagające optymalizacji dla 4D+

### ConjugationMutation i CommutatorMutation - algebraicznie poprawne, ale nie zoptymalizowane

**Problem:** Obecne implementacje wybierają losowe kolejne ruchy do utworzenia koniugacji/komutatora. W 3D to działa dobrze, ale w 4D+ nie wszystkie kombinacje są równie efektywne.

**W 4D najbardziej użyteczne są:**
- Koniugacje używające ruchów na **ortogonalnych płaszczyznach** (wpływają na mniejszą liczbę elementów)
- Komutatory z ruchami na **sąsiednich hyperściankach** (precyzyjniejsze manipulacje)

**Proponowane ulepszenie dla 4D+:**
```csharp
// Preferuj ruchy na ortogonalnych płaszczyznach dla efektywniejszych komutatorów
public class OrthogonalCommutatorMutation<T> : IMutationOperator<T>
{
    public void Mutate(T chromosome, Random rng)
    {
        // Znajdź pary ruchów na ortogonalnych płaszczyznach
        // Utwórz komutator z tych par
    }
}
```

---

## Zaimplementowane operatory specyficzne dla 4D+

### HyperplaneMutation (Mutacja hyperplanarowa) ✅ ZAIMPLEMENTOWANY
**Plik:** `GA/Operators/Mutation/HyperplaneMutation.cs`

**Opis:** Transformuje ruchy między różnymi 3D "komórkami" w 4D+ kostce poprzez przesunięcie osi.

**Uzasadnienie:** W 4D kostka składa się z 8 komórek 3D (analogicznie jak 3D kostka ma 6 ścian 2D). Ruchy w różnych komórkach mogą mieć podobne efekty lokalne, ale różne efekty globalne.

**Algorytm:**
1. Wybierz losowy gen (ruch) do mutacji
2. Przesuń oś ruchu o losowe przesunięcie (1 do N-1): `newAxis = (axis + offset) % N`
3. Znajdź odpowiadającą płaszczyznę rotacji zachowującą relację z nową osią
4. Zachowaj warstwę (slice) i kąt rotacji

**Dlaczego działa dla N wymiarów:** Operator używa `TAffine.N` do określenia zakresu osi i `TAffine.Planes` do znajdowania odpowiadających płaszczyzn. Mechanizm przesunięcia osi jest w pełni parametryczny względem N.

**Kompatybilność:**
- 3D: Działa jako "rotacja układu współrzędnych" ruchu (mniej użyteczne, ale poprawne)
- 4D: Pełne wykorzystanie - eksploruje symetrie między 8 komórkami 3D
- 5D+: Skaluje się automatycznie do wyższych wymiarów

### OrthogonalConjugationMutation (Koniugacja ortogonalna) ✅ ZAIMPLEMENTOWANY
**Plik:** `GA/Operators/Mutation/OrthogonalConjugationMutation.cs`

**Opis:** Tworzy komutatory (wzorzec ABA'B') używając ruchów na ortogonalnych płaszczyznach.

**Ortogonalność płaszczyzn:**
Dwie płaszczyzny rotacji są ortogonalne, jeśli nie współdzielą żadnej wspólnej osi:
- 3D (3 płaszczyzny): Brak par ortogonalnych - używa płaszczyzn o minimalnym nakładaniu
- 4D (6 płaszczyzn): 3 pary ortogonalne: (0,1)⊥(2,3), (0,2)⊥(1,3), (0,3)⊥(1,2)
- 5D+: Więcej par ortogonalnych, skaluje się automatycznie

**Algorytm:**
1. Wybierz losowy ruch A z chromosomu
2. Znajdź ortogonalną płaszczyznę dla ruchu B (lub najbardziej odległą dla 3D)
3. Wygeneruj ruch B na ortogonalnej płaszczyźnie
4. Wstaw komutator: A, B, A', B'

**Dlaczego działa dla N wymiarów:** Operator buduje cache par ortogonalnych używając `TAffine.Planes`. Dla N >= 4 znajduje prawdziwe pary ortogonalne; dla N = 3 używa fallback wybierając płaszczyzny o minimalnym nakładaniu.

**Kompatybilność:**
- 3D: Działa z fallback (najbardziej odległe płaszczyzny) - mniej efektywne, ale poprawne
- 4D: Pełne wykorzystanie ortogonalnych par
- 5D+: Więcej opcji ortogonalnych, większa elastyczność

---

## Brakujące operatory specyficzne dla 4D+

### CellRotationMutation (Mutacja rotacji komórki)

**Opis:** W 4D, wykonuje rotację całej 3D komórki jako jednostki.

**Uzasadnienie:** To odpowiednik rotacji całej ściany w 3D, ale przeniesiony o wymiar wyżej.

### AxisPermutationMutation (Mutacja permutacji osi)

**Opis:** Permutuje osie w ruchu, tworząc "lustrzany" ruch w innym wymiarze.

**Uzasadnienie:** Wykorzystuje symetrie N-wymiarowej kostki do eksploracji rozwiązań.

**Pseudokod:**
```csharp
public void Mutate(T chromosome, Random rng)
{
    var move = TMove.Decode((int)chromosome.Genes[idx]);

    // Permutuj osie według losowej permutacji
    int[] perm = GenerateRandomPermutation(TAffine.N, rng);

    // Przelicz płaszczyznę na nowe osie
    var oldAxes = TAffine.Planes[move.Plane];
    var newAxes = new int[] { perm[oldAxes[0]], perm[oldAxes[1]] };
    move.Plane = FindPlaneIndex(newAxes);
    move.Axis = perm[move.Axis];

    chromosome.Genes[idx] = move.Encode();
}
```

---

## Krytyczna luka: Podwójne rotacje

### Problem

Obecna struktura `TMove` koduje **proste rotacje** - obrót w jednej płaszczyźnie 2D. Jednak w 4D i wyższych wymiarach istnieją **rotacje podwójne** (rotacje Clifforda), gdzie obracamy jednocześnie w dwóch ortogonalnych płaszczyznach.

### Przykład w 4D

W 4D możemy wykonać:
- **Prosta rotacja:** Obrót o 90° w płaszczyźnie (0,1)
- **Podwójna rotacja:** Obrót o 90° w płaszczyźnie (0,1) ORAZ jednocześnie o 90° w płaszczyźnie (2,3)

Podwójna rotacja **nie może** być rozłożona na sekwencję prostych rotacji wykonanych jedna po drugiej - jest to fundamentalnie inna operacja.

### Dlaczego to ważne

1. **Kompletność przestrzeni ruchów:** Bez podwójnych rotacji, algorytm genetyczny eksploruje tylko podzbiór możliwych stanów 4D kostki.

2. **Optymalne rozwiązania:** Niektóre konfiguracje 4D kostki można rozwiązać krócej używając podwójnych rotacji.

3. **Fizyczna interpretacja:** W symulatorach 4D kostek (np. Magic Cube 4D), podwójne rotacje są standardowymi ruchami.

### Obecne ograniczenie w TMove

```csharp
public class TMove
{
    public int Axis;   // Jedna oś
    public int Slice;  // Jedna warstwa
    public int Plane;  // Jedna płaszczyzna
    public int Angle;  // Jeden kąt
}
```

### Proponowane rozszerzenie

```csharp
public class TMove
{
    // Dla prostych rotacji (kompatybilność wsteczna)
    public int Axis;
    public int Slice;
    public int Plane;
    public int Angle;

    // Dla podwójnych rotacji (4D+)
    public int? SecondPlane;  // Druga płaszczyzna (ortogonalna do pierwszej)
    public int? SecondAngle;  // Kąt w drugiej płaszczyźnie

    public bool IsDoubleRotation => SecondPlane.HasValue;

    // Walidacja: płaszczyzny muszą być ortogonalne
    public bool IsValid
    {
        get
        {
            if (!IsDoubleRotation) return ValidateSimpleRotation();
            return ValidateDoubleRotation();
        }
    }

    private bool ValidateDoubleRotation()
    {
        // Płaszczyzny muszą być ortogonalne (nie dzielić żadnej osi)
        var axes1 = TAffine.Planes[Plane];
        var axes2 = TAffine.Planes[SecondPlane.Value];
        return !axes1.Intersect(axes2).Any();
    }
}
```

### Wpływ na operatory

Po rozszerzeniu `TMove`, następujące operatory wymagałyby aktualizacji:

| Operator | Wymagane zmiany |
|----------|-----------------|
| ConjugationMutation | Obsługa inwersji podwójnych rotacji |
| CommutatorMutation | Komutatory z podwójnymi rotacjami |
| NeighborMutation | Mutacja może zmieniać między prostą a podwójną rotacją |
| SimplifyMutation | Nowe reguły upraszczania dla podwójnych rotacji |
| InsertMutation | Neutralne pary z podwójnymi rotacjami |
| InverseSequenceMutation | Inwersja podwójnych rotacji |

---

## Rekomendacje implementacyjne

### Faza 1: Weryfikacja obecnej funkcjonalności (bez zmian kodu)

1. **Test z 4D:** Uruchom solver z `TAffine.N = 4` i zweryfikuj, że:
   - `ValidMoves` zawiera wszystkie 6 płaszczyzn
   - Ewaluator fitness działa poprawnie
   - Wszystkie operatory wykonują się bez błędów

2. **Analiza efektywności:** Porównaj czas/jakość rozwiązań dla 3D vs 4D z obecnymi operatorami.

### Faza 2: Optymalizacja operatorów domenowych

1. **SimplifyMutation:** Dodaj wykrywanie komutujących ruchów na ortogonalnych płaszczyznach.

2. **ConjugationMutation/CommutatorMutation:** Dodaj warianty preferujące ortogonalne płaszczyzny.

3. **Nowe operatory:** Zaimplementuj `HyperplaneMutation` i `AxisPermutationMutation`.

### Faza 3: Wsparcie dla podwójnych rotacji

1. **Rozszerz TMove:** Dodaj pola dla drugiej płaszczyzny i kąta.

2. **Zaktualizuj kodowanie:** Rozszerz `SizeMatrix` o dodatkowe wymiary dla podwójnych rotacji.

3. **Zaktualizuj operatory:** Dostosuj wszystkie operatory do obsługi nowego formatu.

4. **Dodaj DoubleRotationMutation:** Nowy operator specyficzny dla podwójnych rotacji.

### Faza 4: Testowanie i walidacja

1. **Testy jednostkowe:** Dla każdego operatora z różnymi wartościami N.

2. **Testy integracyjne:** Pełne uruchomienia solvera dla 3D, 4D, 5D.

3. **Benchmarki:** Porównanie efektywności przed i po optymalizacjach.

---

## Tabela podsumowująca

| Operator | 3D | 4D | 5D+ | Status | Uwagi |
|----------|:--:|:--:|:---:|--------|-------|
| SwapMutation | ✅ | ✅ | ✅ | Gotowy | Generyczny |
| InversionMutation | ✅ | ✅ | ✅ | Gotowy | Generyczny |
| ScrambleMutation | ✅ | ✅ | ✅ | Gotowy | Generyczny |
| ShiftMutation | ✅ | ✅ | ✅ | Gotowy | Generyczny, rotacja cykliczna |
| AdaptiveMutation | ✅ | ✅ | ✅ | Gotowy | Meta-operator, adaptuje intensywność |
| DisplacementMutation | ✅ | ✅ | ✅ | Gotowy | Przemieszczenie segmentu |
| TranslocationMutation | ✅ | ✅ | ✅ | Gotowy | Zamiana dwóch segmentów |
| CreepMutation | ✅ | ✅ | ✅ | Gotowy | Małe przyrostowe zmiany |
| GaussianMutation | ✅ | ✅ | ✅ | Gotowy | Szum gaussowski |
| SingleGeneMutation | ✅ | ✅ | ✅ | Gotowy | Używa ValidMoves |
| RandomMutation | ✅ | ✅ | ✅ | Gotowy | Używa ValidMoves |
| ConjugationMutation | ✅ | ✅ | ✅ | Do optymalizacji | Działa, ale nie optymalnie dla 4D+ |
| CommutatorMutation | ✅ | ✅ | ✅ | Do optymalizacji | Działa, ale nie optymalnie dla 4D+ |
| NeighborMutation | ✅ | ✅ | ✅ | Gotowy | Tylko modyfikuje Angle |
| SimplifyMutation | ✅ | ✅ | ✅ | Gotowy | Wykrywa ortogonalne wzorce |
| InverseSequenceMutation | ✅ | ✅ | ✅ | Gotowy | Generyczna inwersja |
| InsertMutation | ✅ | ✅ | ✅ | Gotowy | Pary neutralne |
| Wszystkie Crossover | ✅ | ✅ | ✅ | Gotowy | Pozycyjne |
| OrderCrossover (OX) | ✅ | ✅ | ✅ | Gotowy | Zachowanie kolejności |
| PMXCrossover | ✅ | ✅ | ✅ | Gotowy | Mapowanie pozycyjne |
| CycleCrossover (CX) | ✅ | ✅ | ✅ | Gotowy | Dziedziczenie cyklowe |
| EdgeRecombination (ERX) | ✅ | ✅ | ✅ | Gotowy | Zachowanie sąsiedztwa |
| Wszystkie Selection | ✅ | ✅ | ✅ | Gotowy | Oparte na fitness |
| SUSSelection | ✅ | ✅ | ✅ | Gotowy | Stochastyczne próbkowanie |
| BoltzmannSelection | ✅ | ✅ | ✅ | Gotowy | Selekcja temperaturowa |
| TruncationSelection | ✅ | ✅ | ✅ | Gotowy | Top k% populacji |
| LinearRankingSelection | ✅ | ✅ | ✅ | Gotowy | Liniowe prawdopodobieństwo |
| ExponentialRankingSelection | ✅ | ✅ | ✅ | Gotowy | Wykładnicze prawdopodobieństwo |
| HyperplaneMutation | ✅ | ✅ | ✅ | Gotowy | Przesunięcie osi/hiperpłaszczyzny |
| OrthogonalConjugation | ⚠️ | ✅ | ✅ | Gotowy | Ortogonalne komutatory (fallback dla 3D) |
| DoubleRotationMutation | - | ❌ | ❌ | Brak | Wymaga rozszerzenia TMove |

**Legenda:**
- ✅ W pełni funkcjonalny
- ⚠️ Działa, ale nie optymalnie
- ❌ Nie zaimplementowany
- `-` Nie dotyczy tego wymiaru

---

## Odniesienia do kodu

- `RubikCube/TMove.cs` - Struktura kodowania ruchów
- `RubikCube/TAffine.cs` - Definicja wymiarów i płaszczyzn (linia 21: generowanie płaszczyzn)
- `GA/Operators/Mutation/` - Katalog z operatorami mutacji
- `GA/Operators/Crossover/` - Katalog z operatorami krzyżowania
- `GA/Operators/Selection/` - Katalog z operatorami selekcji
