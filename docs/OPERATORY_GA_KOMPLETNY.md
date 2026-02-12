# Kompletna Dokumentacja Operatorów Algorytmu Genetycznego dla Kostki Rubika

Ten dokument stanowi wyczerpujące omówienie wszystkich operatorów algorytmu genetycznego zaimplementowanych w solverze kostki Rubika. Opisane operatory zostały zaprojektowane z myślą o rozwiązywaniu pełnej superkostki Rubika N×N×N w wymiarach od 3D do wyższych. Dokument łączy wiedzę teoretyczną z teorii grup z praktycznymi aspektami implementacji, prezentując zarówno klasyczne operatory GA, jak i nowatorskie rozwiązania domenowe opracowane specjalnie dla tego problemu.

---

## Spis treści

1. [Wprowadzenie teoretyczne](#wprowadzenie-teoretyczne)
2. [Kodowanie ruchów w N wymiarach](#kodowanie-ruchów-w-n-wymiarach)
3. [Operatory mutacji](#operatory-mutacji)
   - [Operatory generyczne](#operatory-generyczne)
   - [Operatory domenowe](#operatory-domenowe-nowatorskie)
   - [Operatory specyficzne dla 4D+](#operatory-specyficzne-dla-4d)
4. [Operatory krzyżowania](#operatory-krzyżowania)
5. [Operatory selekcji](#operatory-selekcji)
6. [Katalog algorytmów speedcubingowych](#katalog-algorytmów-speedcubingowych)
7. [Podsumowanie i rekomendacje](#podsumowanie-i-rekomendacje)

---

## Wprowadzenie teoretyczne

### Kostka Rubika jako grupa matematyczna

Kostka Rubika stanowi doskonały przykład grupy w sensie matematycznym. Zbiór wszystkich możliwych stanów kostki wraz z operacją składania ruchów tworzy tak zwaną grupę Rubika, która dla standardowej kostki 3×3×3 liczy około 43 kwintylionów (4.3×10¹⁹) elementów. Ta algebraiczna struktura pozwala na wykorzystanie pojęć z teorii grup przy projektowaniu operatorów algorytmu genetycznego.

Dwa fundamentalne pojęcia z teorii grup znajdują szczególne zastosowanie w rozwiązywaniu kostki Rubika:

**Koniugacja** jest operacją zapisywaną jako ABA', gdzie A i A' są wzajemnymi odwrotnościami. Intuicyjnie, koniugacja "przenosi" działanie sekwencji B w miejsce określone przez sekwencję A. Można to wyobrazić sobie jako przygotowanie sceny (A), wykonanie głównego działania (B), a następnie cofnięcie przygotowania (A'). Rezultat jest taki, jakby sekwencja B została wykonana w innej lokalizacji na kostce.

**Komutator** jest bardziej zaawansowaną konstrukcją zapisywaną jako ABA'B'. Komutator mierzy "nieprzemienność" dwóch sekwencji A i B. Jeśli sekwencje te komutują (można je wykonać w dowolnej kolejności z tym samym efektem), komutator nie zmienia stanu kostki. Jeśli nie komutują, komutator wpływa tylko na te elementy kostki, które są różnie traktowane przez A i B. Ta właściwość jest niezwykle użyteczna, ponieważ pozwala na precyzyjne manipulowanie niewielką liczbą elementów bez zakłócania reszty kostki.

### Algorytm genetyczny dla kostki Rubika

W prezentowanym podejściu chromosom reprezentuje sekwencję ruchów (genów), które po zastosowaniu do pewnego stanu początkowego kostki prowadzą do stanu końcowego. Celem algorytmu jest znalezienie sekwencji, która minimalizuje funkcję oceny (fitness), gdzie fitness równy zero oznacza kostkę rozwiązaną.

Operatory algorytmu genetycznego można podzielić na trzy główne kategorie: operatory mutacji wprowadzające losowe zmiany w chromosomach, operatory krzyżowania łączące materiał genetyczny z dwóch rodziców oraz operatory selekcji wybierające osobniki do reprodukcji na podstawie ich przystosowania.

---

## Kodowanie ruchów w N wymiarach

### Struktura reprezentacji ruchu

Każdy ruch w kostce Rubika jest kodowany za pomocą czterech komponentów w klasie `TMove` (`RubikCube/TMove.cs`):

```csharp
public int Axis;   // Oś prostopadła do płaszczyzny rotacji (0 do N-1)
public int Slice;  // Warstwa wzdłuż osi (0 do Size-1)
public int Plane;  // Płaszczyzna rotacji (0 do C(N,2)-1)
public int Angle;  // Kąt obrotu: 0=90°, 1=180°, 2=-90°
```

To kodowanie jest w pełni uogólnione dla dowolnej liczby wymiarów. W standardowej kostce 3D mamy 3 osie (X, Y, Z), 3 warstwy na każdej osi (dla kostki 3×3×3) oraz 3 płaszczyzny rotacji odpowiadające trzem ścianom. Przejście do wyższych wymiarów zwiększa zarówno liczbę osi, jak i liczbę płaszczyzn rotacji.

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

### Ortogonalność płaszczyzn

Szczególnie istotne dla wymiarów 4D i wyższych jest pojęcie ortogonalności płaszczyzn. Dwie płaszczyzny rotacji są ortogonalne, jeśli nie współdzielą żadnej wspólnej osi:

- **3D (3 płaszczyzny):** Brak par ortogonalnych — każda para dzieli jedną oś
- **4D (6 płaszczyzn):** 3 pary ortogonalne:
  - (0,1) ⊥ (2,3) — XY ortogonalna do ZW
  - (0,2) ⊥ (1,3) — XZ ortogonalna do YW
  - (0,3) ⊥ (1,2) — XW ortogonalna do YZ
- **5D+:** Więcej par ortogonalnych, skaluje się automatycznie

Ta właściwość ma fundamentalne znaczenie dla operatorów mutacji, ponieważ ruchy na ortogonalnych płaszczyznach komutują (można je wykonać w dowolnej kolejności z tym samym rezultatem), co otwiera nowe możliwości upraszczania i optymalizacji sekwencji ruchów.

---

## Operatory mutacji

Operatory mutacji wprowadzają losowe zmiany w chromosomach, co pozwala na eksplorację przestrzeni rozwiązań i unikanie lokalnych minimów. Prezentowane operatory można podzielić na trzy kategorie: generyczne (działające na dowolnych chromosomach), domenowe (wykorzystujące specyfikę kostki Rubika) oraz specyficzne dla wymiarów 4D i wyższych.

### Operatory generyczne

Operatory generyczne traktują geny jako abstrakcyjne wartości, nie analizując ich znaczenia domenowego. Działają one poprawnie dla dowolnej reprezentacji chromosomu i są niezależne od liczby wymiarów.

#### SwapMutation (Mutacja zamiany)

**Plik:** `GA/Operators/Mutation/SwapMutation.cs`

Operator SwapMutation realizuje jedną z najprostszych możliwych mutacji: wybiera dwa losowe geny w chromosomie i zamienia je miejscami. Mimo pozornej prostoty, ta mutacja może mieć znaczące efekty w przypadku kostki Rubika, gdzie kolejność ruchów ma kluczowe znaczenie.

Rozważmy przykład chromosomu reprezentującego sekwencję sześciu ruchów: [R, U, F', L2, B, D]. Po mutacji zamiany pozycji 1 i 4 otrzymujemy: [R, B, F', L2, U, D]. Pomimo że zestaw ruchów pozostał identyczny, całkowicie inna kolejność ich wykonania prowadzi do zupełnie innego stanu końcowego kostki.

**Przykład:**
```
Przed: [R, U, F', L2, B]
Po:    [R, B, F', L2, U]  // zamieniono pozycje 1 i 4
```

Operator ten jest szczególnie użyteczny, gdy algorytm odkrył już wartościowy zestaw ruchów, ale ich kolejność wymaga optymalizacji. Zachowując cały "materiał genetyczny", pozwala eksplorować różne permutacje bez wprowadzania nowych elementów.

**Dlaczego działa dla N wymiarów:** Operator nie dekoduje ruchów — traktuje geny jako abstrakcyjne wartości. Zmiana kolejności ruchów jest operacją niezależną od wymiarowości.

#### InversionMutation (Mutacja inwersji)

**Plik:** `GA/Operators/Mutation/InversionMutation.cs`

InversionMutation odwraca kolejność genów w losowo wybranym podsegmencie chromosomu. Jest to bardziej radykalna operacja niż zwykła zamiana, ponieważ zmienia wzajemne relacje wielu sąsiadujących genów jednocześnie.

**Przykład:**
```
Przed: [R, U, F', L2, B, D]
Po:    [R, L2, F', U, B, D]  // odwrócono segment [1..3]
```

W kontekście kostki Rubika inwersja może przypadkowo utworzyć użyteczne wzorce. Sekwencja ruchów wykonana "od tyłu" często ma interesujące właściwości, choć nie jest równoważna prostej odwrotności (która wymagałaby również zmiany kierunku każdego ruchu).

**Dlaczego działa dla N wymiarów:** Odwracanie sekwencji jest operacją czysto pozycyjną. Nie ma znaczenia, czy geny reprezentują ruchy 3D czy 7D.

#### ScrambleMutation (Mutacja tasowania)

**Plik:** `GA/Operators/Mutation/ScrambleMutation.cs`

ScrambleMutation losowo tasuje geny w wybranym podsegmencie chromosomu, wykorzystując algorytm Fisher-Yates zapewniający równomierny rozkład wszystkich możliwych permutacji. Jest to najbardziej destrukcyjna z operacji permutacyjnych, ponieważ całkowicie reorganizuje strukturę fragmentu chromosomu.

Ta mutacja jest szczególnie przydatna do wydostania się z lokalnych minimów, gdy algorytm utknął na nieperspektywicznym rozwiązaniu. Całkowita reorganizacja fragmentu może odkryć zupełnie nowe ścieżki w przestrzeni poszukiwań, choć kosztem potencjalnej utraty częściowo wypracowanych dobrych sekwencji.

**Dlaczego działa dla N wymiarów:** Tasowanie to permutacja pozycji, niezależna od zawartości genów.

#### ShiftMutation (Mutacja przesunięcia cyklicznego)

**Plik:** `GA/Operators/Mutation/ShiftMutation.cs`

ShiftMutation wykonuje cykliczne przesunięcie (rotację) genów w chromosomie. Może operować na całym chromosomie lub tylko na wybranym segmencie, przesuwając elementy w prawo lub w lewo o określoną liczbę pozycji.

**Przykłady:**
```
Przesunięcie w prawo: abcdef → fabcde
Przesunięcie w lewo:  abcdef → bcdefa
Przesunięcie segmentu: abCDEfgh → abECDfgh
```

Ten operator, choć popularny w problemach permutacyjnych takich jak problem komiwojażera (gdzie punkt startowy nie ma znaczenia), jest dość destrukcyjny w przypadku kostki Rubika, ponieważ ruchy nie są przemienne. Niemniej może służyć jako narzędzie eksploracyjne do ucieczki z lokalnych minimów.

**Implementacja:** Wykorzystuje efektywny algorytm odwracania (reversal algorithm) o złożoności O(n), który wykonuje przesunięcie w miejscu bez alokacji dodatkowej pamięci.

**Dlaczego działa dla N wymiarów:** Operator wykonuje czysto pozycyjną operację — przesuwa geny cyklicznie bez analizowania ich zawartości.

#### DisplacementMutation (Mutacja przemieszczenia)

**Plik:** `GA/Operators/Mutation/DisplacementMutation.cs`

DisplacementMutation usuwa segment z jednej pozycji chromosomu i wstawia go w innym miejscu, zachowując cały materiał genetyczny, ale zmieniając jego układ.

**Przykład:**
```
Oryginał:     [A B C D E F G H]
Segment:      [C D E] (pozycje 2-4)
Wstaw na:     pozycję 6
Wynik:        [A B F G C D E H]
```

Dla kostki Rubika ta mutacja może odkryć, że ta sama sekwencja ruchów wykonana w innym miejscu rozwiązania daje lepsze wyniki. Jest to łagodniejsza operacja niż pełne tasowanie, ponieważ zachowuje wewnętrzną strukturę przenoszonego segmentu.

**Dlaczego działa dla N wymiarów:** Operator wykonuje czysto pozycyjną manipulację — usuwa i wstawia segmenty bez analizowania zawartości genów.

#### TranslocationMutation (Mutacja translokacji)

**Plik:** `GA/Operators/Mutation/TranslocationMutation.cs`

TranslocationMutation zamienia miejscami dwa nienachodzące się segmenty chromosomu. W przeciwieństwie do DisplacementMutation, gdzie jeden segment jest przenoszony w inne miejsce, tutaj dwa segmenty wymieniają swoje pozycje.

**Przykład:**
```
Oryginał:  [A B C D E F G H I J]
Segment1:  [B C] (pozycje 1-2)
Segment2:  [F G H] (pozycje 5-7)
Wynik:     [A F G H D E B C I J]
```

Ta mutacja jest użyteczna, gdy rozwiązanie składa się z kilku niezależnych "pod-algorytmów" i warto zbadać, czy inna kolejność tych bloków prowadzi do lepszego wyniku.

**Dlaczego działa dla N wymiarów:** Podobnie jak DisplacementMutation, operuje wyłącznie na pozycjach genów.

---

### Operatory domenowe (nowatorskie)

Poniższe operatory zostały zaprojektowane specjalnie dla problemu kostki Rubika, wykorzystując właściwości algebraiczne grupy Rubika oraz wiedzę domenową z zakresu speedcubingu. Stanowią one nowatorski wkład w literaturę dotyczącą algorytmów genetycznych dla łamigłówek kombinatorycznych.

#### CreepMutation (Mutacja pełzająca) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/CreepMutation.cs`

CreepMutation wprowadza małe, przyrostowe zmiany w genach, inspirowana strategiami ewolucyjnymi (Evolution Strategies), gdzie małe mutacje kumulują się przez pokolenia, prowadząc do precyzyjnej optymalizacji.

Dla kostki Rubika operator ten modyfikuje pojedyncze komponenty ruchu o minimalne wartości:
- Zmiana kąta o ±1 (na przykład z 90° na 180° lub z 180° na -90°)
- Zmiana warstwy (slice) o ±1 (dla większych kostek)
- Zmiana płaszczyzny na sąsiednią w przestrzeni płaszczyzn

Pełzająca natura tej mutacji czyni ją idealną do precyzyjnego dostrajania rozwiązań, które są już blisko optimum. Zamiast wprowadzać duże, potencjalnie destrukcyjne zmiany, stopniowo modyfikuje chromosom, pozwalając algorytmowi na subtelne korekty trajektorii poszukiwań.

**Dlaczego działa dla N wymiarów:** Operator modyfikuje komponenty ruchu (Angle, Slice, Plane) które mają taką samą semantykę we wszystkich wymiarach. Zmiana płaszczyzny na "sąsiednią" działa poprawnie niezależnie od liczby płaszczyzn C(N,2).

#### GaussianMutation (Mutacja gaussowska) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/GaussianMutation.cs`

GaussianMutation dodaje do genów szum o rozkładzie normalnym (Gaussa), wykorzystując transformację Box-Mullera do generowania liczb losowych z tego rozkładu. Parametr σ (sigma) kontroluje siłę mutacji: małe wartości σ prowadzą do lokalnego przeszukiwania o drobnej granularności, podczas gdy duże wartości pozwalają na bardziej eksploracyjne mutacje.

**Formuła:** noise ~ N(0, σ²)

Rozkład Gaussa ma szczególną właściwość: większość próbek skupia się blisko średniej, ale z niezerowym prawdopodobieństwem występują też wartości bardziej oddalone. To naturalnie balansuje eksploatację (małe zmiany blisko obecnego rozwiązania) z eksploracją (sporadyczne większe skoki).

**Dla kostki Rubika (dyskretne ruchy):**
- Mały szum (|noise| < 1): Tylko zmiana kąta ruchu
- Średni szum (1 < |noise| < 2): Zmiana kąta oraz warstwy
- Duży szum (|noise| > 2): Całkowita wymiana genu na losowy ruch z puli ValidMoves

**Dlaczego działa dla N wymiarów:** Dla dyskretnych ruchów Rubika, operator używa wielkości szumu jako proxy intensywności mutacji, a następnie deleguje do operacji modyfikujących Angle/Slice, które są N-wymiarowo agnostyczne. Dla dużego szumu używa ValidMoves, które są automatycznie generowane dla aktualnego wymiaru.

#### NeighborMutation (Mutacja sąsiedztwa) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/NeighborMutation.cs`

NeighborMutation zmienia ruch na "podobny" — ten sam axis/face, ale inny kąt. Na przykład ruch R (obrót prawej ściany o 90°) może zostać zmieniony na R' (obrót o -90°) lub R2 (obrót o 180°).

Jest to subtelna mutacja zachowująca ogólną strukturę rozwiązania. Zakłada, że chromosom ma już prawidłową sekwencję ruchów pod względem osi i warstw, a jedynie kąty wymagają korekty. W wielu przypadkach dobry algorytm ma poprawną strukturę, ale niewłaściwe kierunki obrotów, i ten operator pozwala na efektywne dostrojenie tej konkretnej cechy.

**Formuła inwersji kąta:** `nowy_kąt = 2 - stary_kąt`

Ta formuła poprawnie mapuje:
- 0 (90°) → 2 (-90°)
- 1 (180°) → 1 (180°) — obrót o 180° jest własną odwrotnością
- 2 (-90°) → 0 (90°)

**Dlaczego działa dla N wymiarów:** Operator modyfikuje wyłącznie komponent `Angle`, który ma identyczną semantykę (0, 1, 2 = 90°, 180°, -90°) we wszystkich wymiarach.

#### AdaptiveMutation (Mutacja adaptacyjna) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/AdaptiveMutation.cs`

AdaptiveMutation jest meta-operatorem, który dostosowuje intensywność i typ mutacji na podstawie fitness chromosomu. Realizuje dynamiczne balansowanie między eksploracją a eksploatacją bez konieczności ręcznego strojenia parametrów.

**Zasada działania:**
- Słaby fitness (wysoka wartość) → agresywna mutacja (więcej genów, większe zmiany)
- Dobry fitness (niska wartość) → łagodna mutacja (mniej genów, mniejsze zmiany)

**Intensywność obliczana jako:**
```
intensity = (fitness - minFitness) / (maxFitness - minFitness)
genesToMutate = minGenes + intensity * (maxGenes - minGenes)
```

**Strategie w zależności od intensywności:**
- Wysoka (>70%): Mutacja tasująca (ScrambleMutation) — agresywna restrukturyzacja
- Średnia (30-70%): Losowa wymiana genów (RandomMutation)
- Niska (<30%): Mutacja sąsiedztwa (NeighborMutation) — drobne zmiany kątów

To podejście automatyzuje proces, który w tradycyjnych GA wymaga ręcznego harmonogramu mutacji lub kosztownego strojenia hiperparametrów.

**Dlaczego działa dla N wymiarów:** Operator deleguje do innych operatorów (Scramble, Random, Neighbor), które wszystkie są kompatybilne z N wymiarami. Logika wyboru strategii opiera się wyłącznie na wartości fitness, która jest skalarna niezależnie od wymiarowości.

#### ConjugationMutation (Mutacja koniugacji) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/ConjugationMutation.cs`

ConjugationMutation implementuje wzorzec koniugacji ABA' z teorii grup, będący fundamentalnym narzędziem w ręcznym rozwiązywaniu kostki Rubika. Koniugacja pozwala "przenieść" efekt algorytmu B w inne miejsce kostki za pomocą sekwencji przygotowującej A.

Operator wybiera losowy punkt w pierwszej połowie genomu i tworzy lustrzane odbicie ruchów z odwróconymi kątami w drugiej połowie. Rezultatem jest sekwencja, która wykonuje pewne działanie (B) w kontekście określonym przez ruchy przygotowujące (A).

**Optymalizacja dla 4D+:**
W wymiarach >= 4 operator zapewnia, że centralny ruch "B" jest na ortogonalnej płaszczyźnie względem otaczających ruchów "A":
- Buduje cache ortogonalnych płaszczyzn z `TAffine.Planes`
- Sprawdza, czy centralny ruch jest już optymalny
- Jeśli nie, generuje nowy ruch B na ortogonalnej płaszczyźnie używając `ValidMoves`
- Dla 3D zachowuje poprzednie zachowanie (brak płaszczyzn ortogonalnych)

**Kompatybilność:**
- 3D: ✅ Zachowanie bez zmian
- 4D: ✅ Preferuje ortogonalne płaszczyzny dla ruchu B
- 5D+: ✅ Więcej opcji ortogonalnych

**Geometryczna interpretacja:** Koniugacja "przenosi" efekt sekwencji B w miejsce określone przez A. To działa tak samo w 3D, 4D czy 7D — zmienia się tylko przestrzeń, w której operujemy.

#### CommutatorMutation (Mutacja komutatora) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/CommutatorMutation.cs`

CommutatorMutation implementuje wzorzec komutatora ABA'B' z teorii grup. Komutator jest sekwencją, która wpływa tylko na niewielką liczbę elementów kostki — te, które są różnie traktowane przez A i B. Jest to kluczowe narzędzie w zaawansowanym rozwiązywaniu kostki, pozwalające na precyzyjne manipulowanie wybranymi elementami bez zakłócania pozostałych.

Wzorzec ABA'B' można rozumieć następująco: wykonaj A (przygotowanie), wykonaj B (główne działanie), cofnij A (przywróć kontekst), cofnij B (anuluj poboczne efekty). Tylko te elementy, które były różnie dotknięte przez A i B, pozostają zmienione.

**Optymalizacja dla 4D+:**
W wymiarach >= 4 operator preferuje ruchy na ortogonalnych płaszczyznach dla ruchu B:
- Buduje cache ortogonalnych płaszczyzn z `TAffine.Planes`
- Najpierw przeszukuje pobliskie ruchy w chromosomie (okno 8 pozycji)
- Jeśli nie znajdzie, generuje nowy ruch B na ortogonalnej płaszczyźnie
- Dla 3D zachowuje poprzednie zachowanie (brak płaszczyzn ortogonalnych)

```csharp
// For 4D+, try to find a move B on an orthogonal plane
if (n >= 4 && TryFindOrthogonalMove(chromosome, startIdx, moveA.Plane, rng, out moveB))
{
    chromosome.Genes[startIdx + 1] = moveB.Encode();
}
// Create A' (inverse of A)
moveAInverse.Angle = 2 - moveA.Angle;
// Apply: A B A' B'
chromosome.Genes[startIdx + 2] = moveAInverse.Encode();
chromosome.Genes[startIdx + 3] = moveBInverse.Encode();
```

**Kompatybilność:**
- 3D: ✅ Zachowanie bez zmian (brak par ortogonalnych)
- 4D: ✅ Preferuje ortogonalne pary
- 5D+: ✅ Więcej opcji ortogonalnych

#### SimplifyMutation (Mutacja upraszczająca) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/SimplifyMutation.cs`

SimplifyMutation wykrywa i upraszcza redundantne wzorce w chromosomie, optymalizując długość rozwiązania bez zmiany jego efektu końcowego.

**Podstawowe uproszczenia (wszystkie wymiary):**
```csharp
if (move1.Axis == move2.Axis && move1.Plane == move2.Plane && move1.Slice == move2.Slice)
{
    // Połącz kąty: R R → R2, R R R → R', R R' → usuń
    CombineMoves(chromosome, startIdx, move1, move2, rng);
}
```

- R R → R2 (dwa obroty 90° w tym samym kierunku = jeden obrót 180°)
- R R R → R' (trzy obroty 90° = jeden obrót -90°)
- R R' → usuń oba (wzajemne anulowanie ruchów przeciwnych)

**Rozszerzona logika dla 4D+ (N >= 4):**
W 4D+ ruchy na ortogonalnych płaszczyznach komutują (można je wykonać w dowolnej kolejności):
- (0,1) ⊥ (2,3) — XY ⊥ ZW
- (0,2) ⊥ (1,3) — XZ ⊥ YW
- (0,3) ⊥ (1,2) — XW ⊥ YZ

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

**Kompatybilność:**
- 3D: ✅ Podstawowa logika (bez zmian funkcjonalności)
- 4D: ✅ Pełne wykorzystanie 3 par ortogonalnych
- 5D+: ✅ Automatyczne wykrywanie większej liczby par ortogonalnych

#### InverseSequenceMutation (Mutacja odwrotnej sekwencji) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/InverseSequenceMutation.cs`

InverseSequenceMutation zastępuje segment chromosomu jego matematyczną odwrotnością. Odwrotność sekwencji ruchów to odwrócona kolejność z odwróconym kątem każdego ruchu.

Dla segmentu [R, U, F'] odwrotnością jest [F, U', R']. Wykonanie oryginalnej sekwencji, a następnie jej odwrotności, przywraca kostkę do stanu początkowego — jest to fundamentalna właściwość odwrotności w teorii grup.

Ta mutacja jest przydatna, gdy część rozwiązania jest zbędna lub prowadzi w złym kierunku. Zastąpienie jej odwrotnością efektywnie "cofa" ten fragment działania, pozwalając algorytmowi eksplorować alternatywne ścieżki.

**Dlaczego działa dla N wymiarów:** Odwrotność sekwencji ruchów to uniwersalne pojęcie — wykonanie sekwencji, a następnie jej odwrotności, zawsze przywraca stan początkowy, niezależnie od wymiarowości.

#### InsertMutation (Mutacja wstawiająca) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/InsertMutation.cs`

InsertMutation wstawia "neutralną" parę ruchów (ruch i jego odwrotność, np. R R') w losowym miejscu chromosomu. Para neutralna nie zmienia stanu kostki, ponieważ drugi ruch anuluje pierwszy.

Może się wydawać, że wstawianie neutralnych par jest bezużyteczne, jednak ta mutacja pełni ważną rolę eksploracyjną. Neutralna para, raz wstawiona, może zostać zmodyfikowana przez późniejsze mutacje w użyteczną sekwencję. Umożliwia to eksplorację dłuższych rozwiązań bez niszczenia istniejącego postępu.

**Dlaczego działa dla N wymiarów:** Para neutralna (np. R R') anuluje się w dowolnym wymiarze. To wynika z definicji grupy — każdy element ma odwrotność.

#### SingleGeneMutation (Mutacja pojedynczego genu)

**Plik:** `GA/Operators/Mutation/SingleGeneMutation.cs`

SingleGeneMutation zamienia dokładnie jeden losowy gen na inny losowy, prawidłowy ruch z puli dostępnych ruchów (`FreeMoves`). Jest to odpowiednik klasycznej mutacji punktowej w standardowych GA, przystosowany do specyfiki kostki Rubika.

```csharp
if (chromosome is IRubikChromosome rubikChromosome)
{
    chromosome.Genes[idx] = rubikChromosome.FreeMoves[rng.Next(rubikChromosome.FreeMoves.Count)];
}
```

**Dlaczego działa dla N wymiarów:** Lista `FreeMoves` jest generowana na podstawie aktualnej konfiguracji wymiarowej. Gdy `TAffine.N = 4`, lista zawiera wszystkie prawidłowe ruchy 4D.

#### RandomMutation (Mutacja losowa)

**Plik:** `GA/Operators/Mutation/RandomMutation.cs`

RandomMutation zamienia określoną liczbę genów na nowe losowe wartości z puli `ValidMoves`. W odróżnieniu od SingleGeneMutation, która modyfikuje zawsze jeden gen, RandomMutation ma konfigurowalny parametr określający liczbę mutowanych pozycji.

Większa liczba mutowanych genów zwiększa eksplorację kosztem potencjalnie destrukcyjnych zmian. Operator ten jest szczególnie użyteczny w połączeniu z AdaptiveMutation, która dynamicznie dobiera intensywność mutacji.

**Dlaczego działa dla N wymiarów:** Analogicznie do `SingleGeneMutation` — używa listy ruchów świadomej wymiarowości.

#### PatternMutation (Mutacja wzorcowa) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/PatternMutation.cs`

PatternMutation wstawia znane algorytmy speedcubingowe do chromosomu, wykorzystując dziesięciolecia optymalizacji dokonane przez społeczność speedcuberów. Te algorytmy reprezentują wysoce efektywne manipulacje kostki, które GA mógłby odkrywać przez bardzo długi czas samodzielnie.

**Dla kostek 3D zaimplementowano ponad 315 algorytmów** — szczegółowy katalog znajduje się w sekcji [Katalog algorytmów speedcubingowych](#katalog-algorytmów-speedcubingowych).

**Dla kostek 4D+:**
- Uogólnione komutatory: A B A' B' dla różnych kombinacji płaszczyzn
- Podwójne komutatory: (A B A' B')²
- Wzorce koniugacji: A B A', A² B A²

**Implementacja:**
- Cache wzorców budowany przy pierwszym użyciu dla aktualnego wymiaru
- Losowy wzorzec wstawiany na losowej pozycji w chromosomie
- Wzorce dostosowane do rozmiaru kostki (slice = lastSlice dla ruchów zewnętrznych)

**Dlaczego działa dla N wymiarów:** Cache wzorców budowany dynamicznie dla `TAffine.N`. Dla N > 3 generuje parametryczne wzorce oparte na komutatorach z `TAffine.Planes`.

#### BlockBuildingMutation (Mutacja budowania bloków) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/BlockBuildingMutation.cs`

BlockBuildingMutation wstawia sekwencje budowania bloków z trzech popularnych metod rozwiązywania: CFOP, Roux i ZZ. Łącznie operator zawiera około 248 algorytmów dla kostek 3D — szczegółowy katalog znajduje się w sekcji [Katalog algorytmów speedcubingowych](#katalog-algorytmów-speedcubingowych).

**Dla kostek 4D+:**
- Uogólnione bloki A B A' dla różnych płaszczyzn
- Komutatory A B A' B' między płaszczyznami
- Wzorce A² B² do korekty warstw
- Koordynacja wewnętrznych/zewnętrznych warstw dla większych kostek

**Dlaczego działa dla N wymiarów:** Płaszczyzny i osie wybierane parametrycznie z `TAffine.Planes`, rozmiar kostki uwzględniany dla warstw wewnętrznych.

#### LocalSearchMutation (Mutacja z lokalnym przeszukiwaniem) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/LocalSearchMutation.cs`

LocalSearchMutation jest hybrydowym operatorem łączącym zalety algorytmu genetycznego (globalna eksploracja) z lokalnym przeszukiwaniem (precyzyjna optymalizacja). Wykonuje hill-climbing w małym sąsiedztwie, próbując wielu modyfikacji i zachowując najlepszą.

**Parametry:**
- neighborhoodSize: liczba kandydackich modyfikacji na iterację (domyślnie 8)
- maxIterations: maksymalna liczba iteracji hill-climbingu (domyślnie 4)

**11 typów modyfikacji:**

| Typ | Nazwa | Opis |
|-----|-------|------|
| 0 | Zmiana kąta | Losowa zmiana kąta ruchu |
| 1 | Uproszczenie sąsiednich | R R → R2, R R' → usuń |
| 2 | Zamiana na sąsiada | Zmiana slice o ±1 lub kąta o 1 |
| 3 | Usunięcie par anulujących | Wykrycie i zamiana R R' |
| 4 | Zamiana z ValidMoves | Zastąpienie losowym prawidłowym ruchem |
| 5 | **Wstawianie ruchu** | Wstawienie nowego ruchu, przesunięcie reszty |
| 6 | **Usuwanie ruchu** | Usunięcie ruchu, przesunięcie i uzupełnienie |
| 7 | **Zamiana nie-sąsiednich** | Swap dwóch ruchów oddalonych o ≥2 pozycje |
| 8 | **Modyfikacja wzorcowa** | Wykrywanie i upraszczanie znanych wzorców |
| 9 | **Optymalizacja gradientowa** | Próba wszystkich 3 kątów dla pozycji |
| 10 | **Multi-pozycyjna gradient** | Optymalizacja kątów dla 2-3 pozycji |

**Wzorce do wykrywania:**

| Wzorzec | Zamiana | Opis |
|---------|---------|------|
| X X | X2 | Dwa takie same 90° → jeden 180° |
| X X' | (usuń) | Para anulująca |
| X Y X' | Y (zmodyfikowany) | Nadmiarowy koniugat |
| (A B A' B')² | A B A' B' | Podwójny komutator |

**Heurystyki oceny (bez baseCube):**
- Kara +10 za pary anulujące (R R')
- Kara +5 za pary do uproszczenia (R R → R2)
- Kara +3 za nadmiarowe koniugaty (X Y X')
- Kara +2 za długie sekwencje tej samej płaszczyzny (>3)
- Bonus -0.5 za każdą użytą płaszczyznę/oś (różnorodność)
- Bonus -2 za wykryte komutatory (A B A' B')
- Bonus -0.2 za ruchy 180° (często efektywne)

**Algorytm:**
```
dla i = 1 do maxIterations:
    kandydaci = generuj neighborhoodSize modyfikacji (11 typów)
    najlepszy = oceń kandydatów
    jeśli najlepszy.score < obecny.score:
        zastosuj najlepszy
    w przeciwnym razie:
        przerwij (brak poprawy)
```

**Dlaczego działa dla N wymiarów:** Wszystkie operacje używają `TMove.Decode/Encode` które są N-agnostyczne. Heurystyki oparte na właściwościach ruchów (plane, axis) działają dla dowolnego N. `ValidMoves` automatycznie zawiera wszystkie ruchy dla aktualnego wymiaru.

---

### Operatory specyficzne dla 4D+

Poniższe operatory zostały zaprojektowane specjalnie dla kostek o wymiarach 4D i wyższych, wykorzystując właściwości geometryczne przestrzeni wielowymiarowych.

#### HyperplaneMutation (Mutacja hiperplanarowa) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/HyperplaneMutation.cs`

HyperplaneMutation transformuje ruchy między różnymi hiperpłaszczyznami (3D "komórkami") w kostkach 4D+.

**Struktura N-wymiarowej kostki:**
- Kostka 3D: 6 ścian (komórki 2D)
- Kostka 4D: 8 komórek (kostki 3D jako "ściany")
- Kostka 5D: 10 komórek (hiperkostki 4D)

**Algorytm:**
1. Wybierz losowy gen (ruch) do mutacji
2. Przesuń oś ruchu o losowe przesunięcie: `newAxis = (axis + offset) % N`
3. Znajdź odpowiadającą płaszczyznę rotacji zachowującą relację z nową osią
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

**Kompatybilność:**
- 3D: Działa jako "rotacja układu współrzędnych" ruchu (mniej użyteczne, ale poprawne)
- 4D: Pełne wykorzystanie — eksploruje symetrie między 8 komórkami 3D
- 5D+: Skaluje się automatycznie do wyższych wymiarów

**Dlaczego działa dla N wymiarów:** Operator używa `TAffine.N` do określenia zakresu osi i `TAffine.Planes` do znajdowania odpowiadających płaszczyzn. Mechanizm przesunięcia osi jest w pełni parametryczny względem N.

#### OrthogonalConjugationMutation (Koniugacja ortogonalna) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Mutation/OrthogonalConjugationMutation.cs`

OrthogonalConjugationMutation tworzy komutatory (wzorzec ABA'B') używając ruchów na ortogonalnych płaszczyznach, co jest możliwe tylko w wymiarach 4D i wyższych.

**Ortogonalność płaszczyzn:**
Dwie płaszczyzny rotacji są ortogonalne, jeśli nie współdzielą żadnej wspólnej osi:
- 3D (3 płaszczyzny): Brak par ortogonalnych — używa płaszczyzn o minimalnym nakładaniu
- 4D (6 płaszczyzn): 3 pary ortogonalne:
  - (0,1) ⊥ (2,3) — XY ortogonalna do ZW
  - (0,2) ⊥ (1,3) — XZ ortogonalna do YW
  - (0,3) ⊥ (1,2) — XW ortogonalna do YZ
- 5D+: Więcej par ortogonalnych

**Algorytm:**
1. Wybierz losowy ruch A z chromosomu
2. Znajdź ortogonalną płaszczyznę dla ruchu B (lub najbardziej odległą dla 3D)
3. Wygeneruj ruch B na ortogonalnej płaszczyźnie
4. Wstaw komutator: A, B, A', B'

**Przykład (kostka 4D):**
```
Ruch A: Plane=0 (XY)
Ortogonalna płaszczyzna: Plane=5 (ZW)
Ruch B: losowy ruch na płaszczyźnie ZW
Wynik: [A_XY, B_ZW, A'_XY, B'_ZW]
```

**Specjalne właściwości ortogonalnych komutatorów:**
- Wpływają na mniejszą liczbę elementów niż dowolne komutatory
- Ruchy lepiej "komutują" — mniejsza interferencja między A i B
- Tworzą bardziej "geometrycznie czyste" transformacje

**Kompatybilność:**
- 3D: Działa z fallback (najbardziej odległe płaszczyzny) — mniej efektywne, ale poprawne
- 4D: Pełne wykorzystanie ortogonalnych par
- 5D+: Więcej opcji ortogonalnych, większa elastyczność

**Dlaczego działa dla N wymiarów:** Operator buduje cache par ortogonalnych używając `TAffine.Planes`. Dla N >= 4 znajduje prawdziwe pary ortogonalne; dla N = 3 używa fallback wybierając płaszczyzny o minimalnym nakładaniu.

---

## Operatory krzyżowania

Operatory krzyżowania łączą materiał genetyczny z dwóch rodziców, tworząc potomstwo z cechami obu. Wszystkie poniższe operatory są w pełni kompatybilne z dowolną liczbą wymiarów, ponieważ operują na pozycjach genów, nie analizując ich znaczenia domenowego.

#### SinglePointCrossover (Krzyżowanie jednopunktowe)

**Plik:** `GA/Operators/Crossover/SinglePointCrossover.cs`

SinglePointCrossover jest klasycznym operatorem krzyżowania, który wybiera losowy punkt podziału i tworzy dzieci poprzez wymianę segmentów:
- Dziecko 1: geny rodzica1[0..punkt) + geny rodzica2[punkt..koniec)
- Dziecko 2: geny rodzica2[0..punkt) + geny rodzica1[punkt..koniec)

Operator działa dobrze, gdy dobre cechy są zgrupowane w ciągłych segmentach. W kontekście kostki Rubika może połączyć dobrą sekwencję początkową z jednego rozwiązania z dobrą sekwencją końcową z innego.

#### TwoPointCrossover (Krzyżowanie dwupunktowe)

**Plik:** `GA/Operators/Crossover/TwoPointCrossover.cs`

TwoPointCrossover wybiera dwa punkty i wymienia segment między nimi:
- Dziecko 1: rodzic1[0..p1) + rodzic2[p1..p2) + rodzic1[p2..koniec)
- Dziecko 2: rodzic2[0..p1) + rodzic1[p1..p2) + rodzic2[p2..koniec)

Jest to użyteczne, gdy problematyczna część rozwiązania znajduje się pośrodku chromosomu, pozwalając zachować zarówno początek jak i koniec dobrego rozwiązania.

#### UniformCrossover (Krzyżowanie równomierne)

**Plik:** `GA/Operators/Crossover/UniformCrossover.cs`

UniformCrossover niezależnie wybiera każdy gen z jednego lub drugiego rodzica z zadanym prawdopodobieństwem (domyślnie 50%). Realizuje maksymalne mieszanie materiału genetycznego, nie zachowując ciągłych bloków.

Jest przydatne, gdy dobre cechy są rozproszone po całym chromosomie, a nie skupione w ciągłych segmentach.

#### SegmentPreservingCrossover (Krzyżowanie zachowujące segmenty) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Crossover/SegmentPreservingCrossover.cs`

SegmentPreservingCrossover jest inteligentnym operatorem, który identyfikuje "dobre" podsekwencje na podstawie lokalnej poprawy fitness i chroni je podczas krzyżowania. Wymaga dodatkowej analizy fitness, ale może znacząco przyspieszyć konwergencję, zachowując odkryte "budulce" dobrego rozwiązania.

#### OrderCrossover / OX (Krzyżowanie z zachowaniem kolejności)

**Plik:** `GA/Operators/Crossover/OrderCrossover.cs`

OrderCrossover został zaprojektowany do zachowania względnej kolejności genów z rodziców.

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

Dla kostki Rubika, gdzie kolejność ruchów jest kluczowa, OX może odkryć efektywne kombinacje sekwencji, zachowując spójne bloki z jednego rodzica przy jednoczesnym inkorporowaniu względnej kolejności z drugiego.

**Dlaczego działa dla N wymiarów:** OX operuje na pozycjach genów w chromosomie, nie analizując ich semantyki.

#### PMXCrossover / PMX (Krzyżowanie z częściowym odwzorowaniem)

**Plik:** `GA/Operators/Crossover/PMXCrossover.cs`

PMX (Partially Mapped Crossover) utrzymuje relacje pozycyjne między genami poprzez tworzenie łańcuchów mapowań.

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

**Dlaczego działa dla N wymiarów:** PMX traktuje geny jako abstrakcyjne wartości i buduje mapowania pozycyjne.

#### CycleCrossover / CX (Krzyżowanie cyklowe)

**Plik:** `GA/Operators/Crossover/CycleCrossover.cs`

CycleCrossover identyfikuje cykle pozycji między rodzicami i naprzemiennie dziedziczy z nich.

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

**Dlaczego działa dla N wymiarów:** CX operuje na abstrakcyjnych wartościach genów i pozycjach w chromosomie.

#### EdgeRecombinationCrossover / ERX (Krzyżowanie z rekombinacją krawędzi)

**Plik:** `GA/Operators/Crossover/EdgeRecombinationCrossover.cs`

ERX zachowuje relacje sąsiedztwa między genami z obu rodziców.

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

Dla kostki Rubika ERX jest szczególnie przydatny, ponieważ sąsiednie ruchy często tworzą znaczące wzorce (ruchy setupowe, triggery).

**Dlaczego działa dla N wymiarów:** ERX analizuje sąsiedztwo pozycji w chromosomie, nie semantykę genów.

---

## Operatory selekcji

Operatory selekcji wybierają osobniki do reprodukcji na podstawie ich przystosowania (fitness). Wszystkie są niezależne od reprezentacji chromosomu i liczby wymiarów, ponieważ operują wyłącznie na skalarnych wartościach fitness.

#### TournamentSelection (Selekcja turniejowa)

**Plik:** `GA/Operators/Selection/TournamentSelection.cs`

Selekcja turniejowa losowo wybiera grupę osobników (rozmiar turnieju jest parametrem) i zwycięzca z najlepszym fitness przechodzi do puli rodziców. Wielkość turnieju kontroluje presję selekcyjną: większy turniej = silniejsza presja.

#### RankSelection (Selekcja rankingowa)

**Plik:** `GA/Operators/Selection/RankSelection.cs`

Selekcja rankingowa wybiera najlepszych N osobników według fitness, ignorując różnice między wartościami fitness w wybranej grupie.

#### RouletteSelection (Selekcja ruletkowa)

**Plik:** `GA/Operators/Selection/RouletteSelection.cs`

W selekcji ruletkowej prawdopodobieństwo wyboru jest proporcjonalne do fitness. Osobniki o lepszym przystosowaniu mają większą szansę na selekcję, ale nawet słabe osobniki mają niezerowe prawdopodobieństwo.

#### RouletteRankSelection (Selekcja ruletkowa z rangą)

**Plik:** `GA/Operators/Selection/RouletteRankSelection.cs`

Wariant ruletki, gdzie prawdopodobieństwo jest proporcjonalne do pozycji w rankingu, nie do wartości fitness. Eliminuje to problemy ze skalowaniem, gdy wartości fitness mają dużą wariancję.

#### UniqueSelection (Selekcja unikalna)

**Plik:** `GA/Operators/Selection/UniqueSelection.cs`

Selekcja unikalna wybiera osobniki o unikalnych wartościach fitness, aktywnie promując różnorodność w populacji i zapobiegając dominacji klonów.

#### SUSSelection (Stochastyczne próbkowanie uniwersalne) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Selection/SUSSelection.cs`

SUS (Stochastic Universal Sampling) jest ulepszeniem selekcji ruletkowej. Zamiast N niezależnych "rzutów ruletką", używa pojedynczej losowej wartości startowej i N równomiernie rozmieszczonych wskaźników.

**Zalety nad klasyczną ruletką:**
- Zero biasu: oczekiwana liczba kopii równa się rzeczywistej
- Minimalna wariancja: różnica między oczekiwaną a rzeczywistą liczbą jest minimalna
- Lepsza konserwacja różnorodności populacji

SUS daje słabszym osobnikom lepszą szansę na selekcję w porównaniu do standardowej ruletki, co pomaga unikać przedwczesnej konwergencji.

#### BoltzmannSelection (Selekcja Boltzmanna) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Selection/BoltzmannSelection.cs`

Selekcja Boltzmanna opiera prawdopodobieństwo wyboru na rozkładzie Boltzmanna:

P(i) = exp(-fitness[i] / T) / Σexp(-fitness[j] / T)

**Wpływ temperatury T:**
- Wysoka T (50-100): Prawie równomierna selekcja, silna eksploracja
- Niska T (1-10): Silne faworyzowanie najlepszych, silna eksploatacja

**Inspiracja:** Mechanizm zaczerpnięty z symulowanego wyżarzania (simulated annealing), gdzie temperatura kontroluje prawdopodobieństwo akceptacji gorszych rozwiązań.

#### TruncationSelection (Selekcja obcinająca) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Selection/TruncationSelection.cs`

Selekcja obcinająca dopuszcza do reprodukcji tylko górne k% populacji. Spośród tej elitarnej puli wybór jest równomierny losowy.

**Parametr truncationRate (0.0 - 1.0):**
- 0.5 (50%): Umiarkowana presja, zbalansowana eksploracja/eksploatacja
- 0.25 (25%): Silna presja, szybsza konwergencja ale ryzyko przedwczesnej zbieżności
- 0.1 (10%): Bardzo silna presja, używana w strategiach ewolucyjnych (μ,λ)

#### LinearRankingSelection (Liniowa selekcja rankingowa) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Selection/LinearRankingSelection.cs`

Prawdopodobieństwo selekcji jest liniowo proporcjonalne do rangi osobnika.

**Formuła:** P(i) = (2 - s)/N + 2*(rank - 1)*(s - 1)/(N*(N - 1))

**Parametr s (presja selekcyjna, 1.0 - 2.0):**
- s = 1.0: Selekcja równomierna (wszystkie równe prawdopodobieństwo)
- s = 2.0: Maksymalna presja liniowa (najlepszy ma 2× średniego prawdopodobieństwa, najgorszy 0)
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

#### ExponentialRankingSelection (Wykładnicza selekcja rankingowa) — *Nowatorski operator, Dr. inż. Mateusz Kosikowski*

**Plik:** `GA/Operators/Selection/ExponentialRankingSelection.cs`

Prawdopodobieństwo selekcji maleje wykładniczo z rangą.

**Formuła:** P(i) = base^rank / Σ(base^rank)

**Parametr base (0.0 - 1.0):**
- Wartość bliska 1.0 (np. 0.99): Łagodny spadek, więcej równomierna selekcja
- Wartość bliższa 0.0 (np. 0.9): Stromy spadek, silna presja na czołowych osobników

**Przykład (N=5, base=0.9):**
```
Rangi (najlepszy do najgorszego): 1, 2, 3, 4, 5
Surowe wartości: 0.9^1, 0.9^2, 0.9^3, 0.9^4, 0.9^5 = 0.9, 0.81, 0.729, 0.656, 0.590
Prawdopodobieństwa (znormalizowane): 0.244, 0.220, 0.198, 0.178, 0.160
```

Daje silniejsze różnicowanie między czołowymi osobnikami niż liniowe rankowanie.

---

## Katalog algorytmów speedcubingowych

Poniżej znajduje się kompletny katalog algorytmów speedcubingowych używanych przez operatory PatternMutation i BlockBuildingMutation.

### PLL (Permutation of Last Layer) — 21 algorytmów

#### Edge-only PLLs (4)

| Nazwa | Algorytm | Ruchy | Efekt |
|-------|----------|-------|-------|
| **Ua** | R U' R U R U R U' R' U' R2 | 11 | Cykl 3 krawędzi (zgodnie) |
| **Ub** | R2 U R U R' U' R' U' R' U R' | 11 | Cykl 3 krawędzi (przeciwnie) |
| **H** | R2 U2 R U2 R2 U2 R2 U2 R U2 R2 | 11 | Zamiana przeciwległych par |
| **Z** | R' U' R U' R U R U' R' U R U R2 U' R' | 15 | Zamiana sąsiednich par |

#### Corner-only PLLs (3)

| Nazwa | Algorytm | Ruchy | Efekt |
|-------|----------|-------|-------|
| **Aa** | R' F R' B2 R F' R' B2 R2 | 9 | Cykl 3 narożników (zgodnie) |
| **Ab** | R2 B2 R F R' B2 R F' R | 9 | Cykl 3 narożników (przeciwnie) |
| **E** | R B' R' F R B R' F' R B R' F R B' R' F' | 16 | Zamiana przekątnych narożników |

#### Adjacent Corner Swap PLLs (6)

| Nazwa | Algorytm | Ruchy | Efekt |
|-------|----------|-------|-------|
| **T** | R U R' U' R' F R2 U' R' U' R U R' F' | 14 | Zamiana sąsiednich narożników + krawędzi |
| **F** | R' U' F' R U R' U' R' F R2 U' R' U' R U R' U R | 18 | Zamiana sąsiednich narożników + krawędzi |
| **Ja** | R' U L' U2 R U' R' U2 R L | 10 | Zamiana sąsiednich + cykl krawędzi |
| **Jb** | R U R' F' R U R' U' R' F R2 U' R' | 13 | Zamiana sąsiednich + cykl krawędzi |
| **Ra** | R U' R' U' R U R D R' U' R D' R' U2 R' | 15 | Zamiana sąsiednich + cykl krawędzi |
| **Rb** | R' U2 R U2 R' F R U R' U' R' F' R2 | 13 | Zamiana sąsiednich + cykl krawędzi |

#### Diagonal Corner Swap PLLs (4)

| Nazwa | Algorytm | Ruchy | Efekt |
|-------|----------|-------|-------|
| **Y** | F R U' R' U' R U R' F' R U R' U' R' F R F' | 17 | Zamiana przekątnych narożników + krawędzi |
| **V** | R' U R' U' R D' R' D R' U D' R2 U' R2 D R2 | 16 | Zamiana przekątnych narożników + krawędzi |
| **Na** | R U R' U R U R' F' R U R' U' R' F R2 U' R' U2 R U' R' | 21 | Zamiana przekątnych narożników |
| **Nb** | R' U R U' R' F' U' F R U R' F R' F' R U' R | 17 | Zamiana przekątnych narożników |

#### G-perms (4)

| Nazwa | Algorytm | Ruchy | Efekt |
|-------|----------|-------|-------|
| **Ga** | R2 U R' U R' U' R U' R2 U' D R' U R D' | 15 | Cykl narożników + cykl krawędzi |
| **Gb** | R' U' R U D' R2 U R' U R U' R U' R2 D | 15 | Cykl narożników + cykl krawędzi |
| **Gc** | R2 U' R U' R U R' U R2 U D' R U' R' D | 15 | Cykl narożników + cykl krawędzi |
| **Gd** | R U R' U' D R2 U' R U' R' U R' U R2 D' | 15 | Cykl narożników + cykl krawędzi |

### OLL (Orientation of Last Layer) — 57 algorytmów

#### All Edges Oriented (Krzyż na górze) — 7 przypadków

| OLL# | Nazwa | Algorytm | Ruchy |
|------|-------|----------|-------|
| 21 | H/Double Sune | R U R' U R U' R' U R U2 R' | 11 |
| 22 | Pi | R U2 R2 U' R2 U' R2 U2 R | 9 |
| 23 | Headlights | R2 D R' U2 R D' R' U2 R' | 9 |
| 24 | Chameleon | F R' F' R U R U' R' | 8 |
| 25 | Bowtie | F' R U R' U' R' F R | 8 |
| 26 | Antisune | R U2 R' U' R U' R' | 7 |
| 27 | Sune | R U R' U R U2 R' | 7 |

#### T-shapes — 2 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 33 | R U R' U' R' F R F' | 8 |
| 45 | F R U R' U' F' | 6 |

#### Squares — 2 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 5 | R' U2 R U R' U R | 7 |
| 6 | R U2 R' U' R U' R' | 7 |

#### C-shapes — 2 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 34 | R U R2 U' R' F R U R U' F' | 11 |
| 46 | R' U' R' F R F' U R | 8 |

#### W-shapes — 2 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 36 | L' U' L U' L' U L U L F' L' F | 12 |
| 38 | R U R' U R U' R' U' R' F R F' | 12 |

#### Corners Oriented — 2 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 28 | R U R' U' M' U R U' R' | 9 |
| 57 | R U R' U' M' U R U' R' U' M | 11 |

#### P-shapes — 4 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 31 | R' U' F U R U' R' F' R | 9 |
| 32 | R U B' U' R' U R B R' | 9 |
| 43 | F' U' L' U L F | 6 |
| 44 | F U R U' R' F' | 6 |

#### I-shapes (Line) — 4 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 51 | F U R U' R' U R U' R' F' | 10 |
| 52 | R U R' U R U' B U' B' R' | 10 |
| 55 | R' F R U R U' R2 F' R2 U' R' U R U R' | 15 |
| 56 | F R U R' U' R F' R U R' U' R' F R F' | 15 |

#### Fish shapes — 4 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 9 | R U R' U' R' F R2 U R' U' F' | 11 |
| 10 | R U R' U R' F R F' R U2 R' | 11 |
| 35 | R U2 R2 F R F' R U2 R' | 9 |
| 37 | F R U' R' U' R U R' F' | 9 |

#### Knight Move shapes — 4 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 13 | F U R U' R2 F' R U R U' R' | 11 |
| 14 | R' F R U R' F' R F U' F' | 10 |
| 15 | R' F' R L' U' L U R' F R | 10 |
| 16 | R U R' L U L' U' R U' R' | 10 |

#### Awkward shapes — 4 przypadki

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 29 | R U R' U' R U' R' F' U' F R U R' | 13 |
| 30 | F U R U2 R' U' R U2 R' U' F' | 11 |
| 41 | R U R' U R U2 R' F R U R' U' F' | 13 |
| 42 | R' U' R U' R' U2 R F R U R' U' F' | 13 |

#### L-shapes — 6 przypadków

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 47 | F' L' U' L U L' U' L U F | 10 |
| 48 | F R U R' U' R U R' U' F' | 10 |
| 49 | R B' R2 F R2 B R2 F' R | 9 |
| 50 | R B' R B R2 U2 F R' F' R | 10 |
| 53 | F R U R' U' F' R U R' U' R' F R F' | 14 |
| 54 | R U R' U' R' F R F' R U R' U' R' F R F' | 16 |

#### Lightning Bolt shapes — 6 przypadków

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 7 | F R U R' U' F' U F R U R' U' F' | 13 |
| 8 | R' U' R U' R' U2 R | 7 |
| 11 | F' L' U' L U F U' F' L' U' L U F | 13 |
| 12 | F R U R' U' F' U F R U R' U' F' | 13 |
| 39 | L F' L' U' L U F U' L' | 9 |
| 40 | R' F R U R' U' F' U R | 9 |

#### Dot cases — 8 przypadków

| OLL# | Algorytm | Ruchy |
|------|----------|-------|
| 1 | R U2 R2 F R F' U2 R' F R F' | 11 |
| 2 | F R U R' U' F' U2 F' L' U' L U F | 13 |
| 3 | F' L' U' L U F U' F' L' U' L U F | 13 |
| 4 | F' L' U' L U F U F' L' U' L U F | 13 |
| 17 | R U R' U R' F R F' U2 R' F R F' | 13 |
| 18 | R U2 R2 F R F' U2 M' U R U' R' | 12 |
| 19 | R' U2 F R U R' U' F2 U2 F R | 11 |
| 20 | R U R' U R U' R' U R U2 R' U' R U R' U' R U' R' | 19 |

### COLL (Corners of Last Layer) — 42 algorytmy

#### COLL H — 4 przypadki

| Nazwa | Algorytm | Ruchy |
|-------|----------|-------|
| H1 | R U R' U R U' R' U R U2 R' | 11 |
| H2 | R U2 R' U' R U R' U' R U' R' | 11 |
| H3 | F R U R' U' R U R' U' R U R' U' F' | 14 |
| H4 | R U R' U R U L' U R' U' L | 11 |

#### COLL Pi — 6 przypadków

| Nazwa | Algorytm | Ruchy |
|-------|----------|-------|
| Pi1 | R U2 R' U' R U R' U2 R' F R F' | 12 |
| Pi2 | F R' F' R U2 R U' R' U R U2 R' | 12 |
| Pi3 | R' U' R' F R F' R U' R' U2 R | 11 |
| Pi4 | R U2 R' U' R U R' U' R U R' U' R U' R' | 15 |
| Pi5 | R' F' R U R' U' R' F R2 U' R' U2 R | 13 |
| Pi6 | R U R' U' R' F R2 U R' U' R U R' U' F' | 15 |

#### COLL U (Sune) — 6 przypadków

| Nazwa | Algorytm | Ruchy |
|-------|----------|-------|
| U1 | R U R' U R U2 R' | 7 |
| U2 | R U R' U R U' R' U R U2 R' | 11 |
| U3 | R2 D R' U2 R D' R' U2 R' | 9 |
| U4 | R2 D' R U2 R' D R U2 R | 9 |
| U5 | F R U' R' U R U R' U R U' R' F' | 13 |
| U6 | R' U' R U' R' U R U' R' U2 R | 11 |

#### COLL T — 6 przypadków

| Nazwa | Algorytm | Ruchy |
|-------|----------|-------|
| T1 | R U R' U' R' F R F' | 8 |
| T2 | L' U' L U L F' L' F | 8 |
| T3 | R U2 R' U' R U' R2 U2 R U R' U R | 13 |
| T4 | R U R D R' U R D' R2 | 9 |
| T5 | R' U R U2 R' L' U R U' L | 10 |
| T6 | L' U' L U2 L R U' L' U R' | 10 |

#### COLL L — 6 przypadków

| Nazwa | Algorytm | Ruchy |
|-------|----------|-------|
| L1 | F R U' R' U' R U R' F' | 9 |
| L2 | F' L' U L U L' U' L F | 9 |
| L3 | R' U' R U R' F' R U R' U' R' F R2 | 13 |
| L4 | R U R' U' R U' R' F' U' F R U R' | 13 |
| L5 | F R' F' R U2 R U2 R' | 8 |
| L6 | F' L F L' U2 L' U2 L | 8 |

#### COLL AS (Anti-Sune) — 6 przypadków

| Nazwa | Algorytm | Ruchy |
|-------|----------|-------|
| AS1 | R U2 R' U' R U' R' | 7 |
| AS2 | R' U' R U' R' U R U' R' U2 R | 11 |
| AS3 | L' U R U' L U R' | 7 |
| AS4 | R U' L' U R' U' L | 7 |
| AS5 | F' R U R' U' R' F R U R U' R' | 12 |
| AS6 | R U R' U R U2 R' U' R U R' U R U2 R' | 15 |

#### COLL S (Sune-like) — 6 przypadków

| Nazwa | Algorytm | Ruchy |
|-------|----------|-------|
| S1 | R U R' U' R' F R F' R U R' U R U2 R' | 15 |
| S2 | L' U' L U L F' L' F L' U' L U' L' U2 L | 15 |
| S3 | R U R' U R U2 R' U R U R' U R U2 R' | 15 |
| S4 | R U R' U R U R' U R U2 R' | 11 |
| S5 | F R' F' R U R U' R' | 8 |
| S6 | F' L F L' U' L' U L | 8 |

### F2L (First Two Layers) — 41 algorytmów

#### Podstawowe przypadki (4)

| F2L# | Algorytm | Ruchy | Opis |
|------|----------|-------|------|
| 1 | R U R' | 3 | Para gotowa, biały na prawej |
| 2 | U' F' U F | 4 | Para gotowa, biały na górze |
| 3 | F' U' F | 3 | Para gotowa, biały z przodu |
| 4 | R U' R' | 3 | Para gotowa, wstawienie od tyłu |

#### Narożnik w slocie, krawędź na górze (6)

| F2L# | Algorytm | Ruchy |
|------|----------|-------|
| 5 | U' R U' R' U R U R' | 8 |
| 6 | U' R U R' U R U R' | 8 |
| 7 | U' R U2 R' U R U R' | 8 |
| 8 | U F' U F U' F' U' F | 8 |
| 9 | U F' U' F U' F' U' F | 8 |
| 10 | U F' U2 F U' F' U' F | 8 |

#### Krawędź w slocie, narożnik na górze (6)

| F2L# | Algorytm | Ruchy |
|------|----------|-------|
| 11 | R U R' U' R U R' U' R U R' | 11 |
| 12 | R U' R' U R U' R' U R U' R' | 11 |
| 13 | R U R' U' R U R' | 7 |
| 14 | R U' R' U R U2 R' U R U' R' | 11 |
| 15 | U' R U R' U R U R' | 8 |
| 16 | U' R U' R' U R U R' | 8 |

#### Narożnik białym do góry (6)

| F2L# | Algorytm | Ruchy |
|------|----------|-------|
| 17 | R U2 R' U' R U R' | 7 |
| 18 | F' U2 F U F' U' F | 7 |
| 19 | U R U2 R' U R U' R' | 8 |
| 20 | U' F' U2 F U' F' U F | 8 |
| 21 | F' U F U' F' U F | 7 |
| 22 | R U' R' U R U' R' | 7 |

#### Narożnik białym na bok (6)

| F2L# | Algorytm | Ruchy |
|------|----------|-------|
| 23 | U' R U' R' U2 R U' R' | 8 |
| 24 | U F' U F U2 F' U F | 8 |
| 25 | F' U' F U F' U' F | 7 |
| 26 | R U R' U' R U R' | 7 |
| 27 | R U' R' U R U' R' | 7 |
| 28 | F' U F U' F' U F | 7 |

#### Kolory pasują (6)

| F2L# | Algorytm | Ruchy |
|------|----------|-------|
| 29 | R U' R' U2 R U R' | 7 |
| 30 | F' U F U2 F' U' F | 7 |
| 31 | U2 R U R' U R U' R' | 8 |
| 32 | U2 F' U' F U' F' U F | 8 |
| 33 | U R U' R' U' R U R' | 8 |
| 34 | U' F' U F U F' U' F | 8 |

#### Kolory przeciwne (6)

| F2L# | Algorytm | Ruchy |
|------|----------|-------|
| 35 | U' R U R' U2 R U' R' | 8 |
| 36 | U F' U' F U2 F' U F | 8 |
| 37 | R U R' U2 R U' R' | 7 |
| 38 | F' U' F U2 F' U F | 7 |
| 39 | U' R U' R' U R U R' | 8 |
| 40 | U F' U F U' F' U' F | 8 |

#### Przypadek specjalny (1)

| F2L# | Algorytm | Ruchy |
|------|----------|-------|
| 41 | R U' R' U R U2 R' U R U' R' | 11 |

### Triggery podstawowe — 8 algorytmów

| Nazwa | Algorytm | Ruchy | Użycie |
|-------|----------|-------|--------|
| **Sexy move** | R U R' U' | 4 | Najpowszechniejszy trigger |
| **Inverse sexy** | U R U' R' | 4 | Odwrotność sexy |
| **Sledgehammer** | R' F R F' | 4 | Trigger z F |
| **Hedgeslammer** | F R' F' R | 4 | Odwrotność sledgehammer |
| **Left sexy** | L' U' L U | 4 | Leworęczna wersja |
| **Double sexy** | (R U R' U')2 | 8 | Podwójny sexy |
| **Corner twist** | R' D' R D | 4 | Skręcenie narożnika |
| **Double corner** | (R' D' R D)2 | 8 | Podwójne skręcenie |

### Metoda Roux — 90 algorytmów

*(First Block 18 + Second Block 20 + CMLL 42 + LSE 10)*

Szczegółowe algorytmy dostępne w pliku źródłowym `GA/Operators/Mutation/BlockBuildingMutation.cs`.

### Metoda ZZ — 98 algorytmów

*(EOLine 24 + Left Block 18 + Right Block 18 + ZZF2L 30 + ZZLL 8)*

Szczegółowe algorytmy dostępne w pliku źródłowym `GA/Operators/Mutation/BlockBuildingMutation.cs`.

---

## Podsumowanie i rekomendacje

### Tabela kompatybilności z wymiarami

| Operator | 3D | 4D | 5D+ | Typ |
|----------|:--:|:--:|:---:|-----|
| SwapMutation | ✅ | ✅ | ✅ | Generyczny |
| InversionMutation | ✅ | ✅ | ✅ | Generyczny |
| ScrambleMutation | ✅ | ✅ | ✅ | Generyczny |
| ShiftMutation | ✅ | ✅ | ✅ | Generyczny |
| DisplacementMutation | ✅ | ✅ | ✅ | Generyczny |
| TranslocationMutation | ✅ | ✅ | ✅ | Generyczny |
| CreepMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| GaussianMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| NeighborMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| AdaptiveMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| ConjugationMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| CommutatorMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| SimplifyMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| InverseSequenceMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| InsertMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| SingleGeneMutation | ✅ | ✅ | ✅ | Domenowy |
| RandomMutation | ✅ | ✅ | ✅ | Domenowy |
| PatternMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| BlockBuildingMutation | ✅ | ✅ | ✅ | Domenowy (nowatorski) |
| LocalSearchMutation | ✅ | ✅ | ✅ | Hybrydowy (nowatorski) |
| HyperplaneMutation | ⚠️ | ✅ | ✅ | 4D+ (nowatorski) |
| OrthogonalConjugationMutation | ⚠️ | ✅ | ✅ | 4D+ (nowatorski) |
| SinglePointCrossover | ✅ | ✅ | ✅ | Generyczny |
| TwoPointCrossover | ✅ | ✅ | ✅ | Generyczny |
| UniformCrossover | ✅ | ✅ | ✅ | Generyczny |
| SegmentPreservingCrossover | ✅ | ✅ | ✅ | Nowatorski |
| OrderCrossover (OX) | ✅ | ✅ | ✅ | Generyczny |
| PMXCrossover | ✅ | ✅ | ✅ | Generyczny |
| CycleCrossover (CX) | ✅ | ✅ | ✅ | Generyczny |
| EdgeRecombinationCrossover (ERX) | ✅ | ✅ | ✅ | Generyczny |
| TournamentSelection | ✅ | ✅ | ✅ | Generyczny |
| RankSelection | ✅ | ✅ | ✅ | Generyczny |
| RouletteSelection | ✅ | ✅ | ✅ | Generyczny |
| RouletteRankSelection | ✅ | ✅ | ✅ | Generyczny |
| UniqueSelection | ✅ | ✅ | ✅ | Generyczny |
| SUSSelection | ✅ | ✅ | ✅ | Nowatorski |
| BoltzmannSelection | ✅ | ✅ | ✅ | Nowatorski |
| TruncationSelection | ✅ | ✅ | ✅ | Nowatorski |
| LinearRankingSelection | ✅ | ✅ | ✅ | Nowatorski |
| ExponentialRankingSelection | ✅ | ✅ | ✅ | Nowatorski |

**Legenda:**
- ✅ W pełni funkcjonalny
- ⚠️ Działa, ale mniej efektywny (brak ortogonalnych płaszczyzn w 3D)

### Rekomendacje implementacyjne

**Faza 1: Eksploracja**
We wczesnych generacjach zaleca się stosowanie operatorów o wysokiej eksploracji: ScrambleMutation, RandomMutation, AdaptiveMutation (z wysoką intensywnością), oraz selekcji z niską presją (SUS, Boltzmann z wysoką temperaturą).

**Faza 2: Eksploatacja**
W późniejszych generacjach, gdy populacja zbiega do dobrych rozwiązań, należy przejść na operatory precyzyjne: NeighborMutation, CreepMutation, LocalSearchMutation, SimplifyMutation, oraz selekcję z wysoką presją (Truncation, LinearRanking).

**Faza 3: Domenowa optymalizacja**
Na każdym etapie warto stosować operatory domenowe: PatternMutation i BlockBuildingMutation wstawiają sprawdzone sekwencje speedcubingowe, ConjugationMutation i CommutatorMutation wykorzystują algebraiczną strukturę grupy Rubika.

**Dla wymiarów 4D+:**
Dodatkowo należy włączyć HyperplaneMutation i OrthogonalConjugationMutation, które eksploatują właściwości geometryczne przestrzeni wielowymiarowych. SimplifyMutation automatycznie wykorzystuje ortogonalność płaszczyzn do znajdowania ukrytych możliwości uproszczenia.

### Krytyczna luka: Podwójne rotacje

Obecna struktura `TMove` koduje **proste rotacje** — obrót w jednej płaszczyźnie 2D. Jednak w 4D i wyższych wymiarach istnieją **rotacje podwójne** (rotacje Clifforda), gdzie obracamy jednocześnie w dwóch ortogonalnych płaszczyznach.

**Przykład w 4D:**
- **Prosta rotacja:** Obrót o 90° w płaszczyźnie XY
- **Podwójna rotacja:** Obrót o 90° w płaszczyźnie XY ORAZ jednocześnie o 90° w płaszczyźnie ZW

Podwójna rotacja **nie może** być rozłożona na sekwencję prostych rotacji — jest to fundamentalnie inna operacja. Implementacja operatora DoubleRotationMutation wymaga rozszerzenia struktury TMove.

---

## Odniesienia do kodu

- `RubikCube/TMove.cs` — Struktura kodowania ruchów
- `RubikCube/TAffine.cs` — Definicja wymiarów i płaszczyzn (linia 21: generowanie płaszczyzn)
- `GA/Operators/Mutation/` — Katalog z operatorami mutacji
- `GA/Operators/Crossover/` — Katalog z operatorami krzyżowania
- `GA/Operators/Selection/` — Katalog z operatorami selekcji

---

## Bibliografia

1. Goldberg, D.E. (1989). *Genetic Algorithms in Search, Optimization, and Machine Learning*. Addison-Wesley.
2. Singmaster, D. (1981). *Notes on Rubik's Magic Cube*. Penguin Books.
3. Korf, R.E. (1997). "Finding Optimal Solutions to Rubik's Cube Using Pattern Databases". *Proceedings of AAAI-97*.
4. Rokicki, T., Kociemba, H., Davidson, M., & Dethridge, J. (2014). "The diameter of the Rubik's Cube group is twenty". *SIAM Review*.

---

*Dokument stworzony dla projektu RubikCube — Solver kostki Rubika oparty na algorytmie genetycznym*

