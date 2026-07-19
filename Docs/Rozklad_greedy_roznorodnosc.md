# Różnorodność rozkładów greedy — mieszana redukcja i makroruchy

Robocze odkrycia (Robert Świta ↔ Claude), lipiec 2026. Materiał do artykułu o układaniu kostki N-wymiarowej.
Narzędzie: klasa `RubikCube/TGreedyDiversity.cs` + pozycje w menu **File** (*Greedy Diversity*, *Orientation Cluster*,
*Verify Manoeuvres*).

---

## 1. Kontekst i pytanie

Host-owy greedy (`TRubikCube.GetSolveSeq`) układa pojedynczy kubik: w każdym kroku wybiera ćwierćobrót `(plane, angle)`,
który obniża `RotationCount` o 1, aż orientacja = identyczność. Reject-free (na każdej najkrótszej ścieżce istnieje
ruch-progres), losowe wybory pokrywają całe drzewo najkrótszych rozkładów. Te sekwencje seedują GA na GPU.

`RotationCount` = liczba **niezerowych obrotów Givensa** w rozkładzie orientacji (QR macierzy `M`). Kluczowa obserwacja:
**to, czy ruch jest „progresem", zależy od tego, JAK liczymy `RotationCount`** — czyli od polityki rozkładu QR. Zmieniając
tę politykę, zmieniamy drzewo greedy → inne (i potencjalnie liczniejsze) rozwiązania.

Pytanie: które polityki rozkładu dają największą **różnorodność** rozwiązań (materiał na koniunkcje/komutatory =
makroruchy), i jak się mają do siebie?

## 2. Cztery osie różnorodności metryki

Rozkład `M` (orientacja kubika, macierz signed-permutacji dla obrotów 90°) można wariować na cztery ortogonalne sposoby.
Wszystkie zaimplementowane w jednym lokalnym mieszanym Givensie (`TGreedyDiversity.RotCount`):

- **perm** — permutacja osi przed QR: `A = PᵀMP` (`A[y,x] = M[perm[y], perm[x]]`). N! wariantów.
- **order** — kolejność płaszczyzn w QR (reguła propagacji zer, jak `TRubikCube.GetOrder`). Kilka–kilkanaście wariantów.
- **reversed** — rozkład transpozycji `Mᵀ` (droga „odwrotna").
- **mode** — per płaszczyzna wybór, którą stronę przekątnej zerować (patrz §3). 2^P wariantów (P = liczba płaszczyzn).

Baseline = standardowy QR = `perm=id, order=std, reversed=false, mode=0`.

## 3. Mieszana redukcja — matematyka (rdzeń odkrycia)

Ruch kostki obraca orientację **z lewej** (`M → G·M`). Standardowy QR zeruje elementy **pod** przekątną lewymi Givensami.
Można jednak per płaszczyzna zerować **nad** przekątną — i są na to trzy różne prymitywy (`A` = bieżąca macierz,
konwencja kolumnowa; `A.Rotate` = lewe/wiersze, `A.RotatePost` = prawe/kolumny):

| prymitywa | zeruje | mnożenie | pivot | `a, b` → `A.Rotate/RotatePost(a1,a2, a/r, ±b/r)` |
|---|---|---|---|---|
| **pod**   | sub `[a2,a1]`   | lewe (`Rotate`)     | `A[a1,a1]` | `a=A[a1,a1]`, `b=A[a2,a1]`, `Rotate(..., a/r, −b/r)` |
| **nad-L** | super `[a1,a2]` | **lewe** (`Rotate`) | `A[a2,a2]` | `a=A[a2,a2]`, `b=−A[a1,a2]`, `Rotate(..., a/r, −b/r)` |
| **nad-R** | super `[a1,a2]` | prawe (`RotatePost`)| `A[a1,a1]` | `a=A[a1,a1]`, `b=A[a1,a2]`, `RotatePost(..., a/r, +b/r)` |

Weryfikacja `nad-L` (lewe zerowanie nad przekątną — to była najmniej oczywista prymitywa):
```
new[a1,a2] = cos·A[a1,a2] + sin·A[a2,a2] = (A[a2,a2]/r)·A[a1,a2] + (−A[a1,a2]/r)·A[a2,a2] = 0  ✓
```

**Wybrana oś `mode` = per płaszczyzna {pod, nad-L}, 2^P wariantów, wszystko LEWE.** To jest kluczowe: skoro oba są lewe,
redukcja `G_k…G_1·M = I` daje rozkład `M` jako złożenie **samych lewych** obrotów = **prawdziwą sekwencję ruchów kostki**.
(Dodanie `nad-R` wprowadza obroty prawe → sprzężenia `L·M·R`, nie pojedyncze ruchy — na razie pominięte.)

Niektóre mody zastępują wcześniej uzyskane zera i nie sprowadzają `M` do `I` — filtruje je **solve-check** na liściu
greedy (`o.OrthoPack() == identityPack`), więc liczą się tylko sekwencje faktycznie układające.

## 4. Metodyka pomiaru

- **Domena:** cała grupa orientacji jednego kubika (domknięcie tożsamości ruchami): 24 dla N=3, **192** dla N=4.
- **Pełne drzewo greedy** pod każdą metryką (rozgałęzianie na *wszystkich* kandydatach, nie losowo) → zbiór sekwencji.
- **Correct** (`TRubikGenome.Correct`) składa sąsiednie ruchy tej samej płaszczyzny/warstwy (mod 4). Porównujemy manewry
  po Correct — zwija to `2×90° ≡ 180°` (effect-równoważne co do celu).
- **Slice jest bezczynny dla liczby distinct** (odkrycie): slice jest funkcją deterministyczną trajektorii i niezmienniczy
  wzdłuż same-plane runs, więc `Slice=0` daje identyczne liczby jak realny slice. Test działa przy dowolnym Size.
- **Ground-truth = efekt na całej kostce** (`Verify`): każdy manewr aplikowany na prawdziwej kostce (`cube.Turn`), cel
  ustawiony na orientację, realny slice = `tgt.GetPos(oś)`, po każdym manewrze cofnięcie ruchem odwrotnym. Hash
  `CubeHash` koduje pozycję + `OrthoPack` każdego kubika. Dwa manewry są tym samym makroruchem ⟺ ten sam hash.
- **Generyczny cel:** kubik o różnych odległościach od środca (trywialny stabilizator, `coords (0,1,…,N−1)`, wymaga
  `Size ≥ 2N−1` = 7 dla N=4) — żeby symetria pozycji nie sklejała sztucznie manewrów. Klaster tego kubika ma rozmiar =
  liczba orientacji (`2^(N−1)·N!`); znajduje go *Orientation Cluster*.

## 5. Wyniki

### 5.1. Correct-string (diagnostyk *Greedy Diversity*), każda oś vs baseline

**N=3** (24 orientacje, baseline 56):

| oś | distinct | +% |
|---|---|---|
| perm | 64 | +14,3% |
| order | 62 | +10,7% |
| reversed | 56 | **+0%** |
| **mode** | 77 | **+37,5%** |

**N=4** (192 orientacje, baseline 2092):

| oś | distinct | +% |
|---|---|---|
| perm | 3259 | +55,8% |
| order | 2856 | +36,5% |
| reversed | 2092 | **+0%** |
| **mode** | 10752 | **+414%** |

Histogram długości (N=4, EXTRA vs baseline): długości **4 i 5 pochodzą WYŁĄCZNIE od `mode`** (5487 + 2297), perm/order/
reversed tkwią w minimum (≤3). Baseline maksuje na długości 3.

### 5.2. Prawdziwe makroruchy (diagnostyk *Verify Manoeuvres*), efekt na całej kostce

**N=3, Size 5** — `Correct-string == efekt` dla KAŻDEJ metody (0% duplikatów): 55=55, 63=63, 61=61, 76=76, 78=78.

**N=4, Size 7** — solve rate **100%** (11080/11080). Rigorystyczna tabela:

| metoda | Correct-string | **prawdziwe makroruchy (efekt)** | +% vs baseline |
|---|---|---|---|
| baseline | 2091 | **1546** | — |
| + perm | 3258 | **2302** | +48,9% |
| + order | 2855 | **2046** | +32,3% |
| + **mode** | 10751 | **6344** | **+310%** |
| combined | 11080 | **6571** | +325% |

## 6. Kluczowe odkrycia

1. **`mode` (mieszana redukcja pod/nad-L) to jedyny generator makroruchów.** Perm/order/reversed tylko przeetykietowują
   rozkład → zachowują długość → nigdy nie wyjdą poza minimum. `mode` generuje sekwencje **nie-minimalne** (dłuższe), a
   to *są* makroruchy. Rigorystycznie (efekt): **+310% vs baseline**, przy perm +49%, order +32%. Rośnie lawinowo z N
   (N=3: 76 makroruchów, N=4: 6344).

2. **`mode` niemal subsumuje resztę:** combined (6571) to tylko +3,6% ponad sam mode (6344).

3. **`reversed` jest martwy** (+0%, całkowicie subsumowany), **`order` jest ~subsumowany przez perm** (perm×order ledwie
   ponad sam perm).

4. **Teoria przemienności tłumaczy różnicę Correct-string vs efekt** (0% dla N=3, 26–41% dla N=4):
   - Efekt to element grupy; różne słowa (ciągi ruchów) mogą dawać ten sam element — jak niejednoznaczność kątów Eulera.
   - `Correct` składa tylko **sąsiednie** ruchy, nie **reorderuje**. Gdy ruchy **komutują**, można je przestawić tak, że
     dopiero wtedy same-plane ruchy stają się sąsiednie i by się złożyły — czego `Correct` nie widzi.
   - Dwie płaszczyzny komutują ⟺ są **rozłączne** (nie dzielą osi). **N=3: każda para płaszczyzn dzieli oś → brak
     przemienności → 0% duplikatów.** **N=4: są pary rozłączne `(0,1)&(2,3)` itd. → przemienność → duplikaty.** Skok
     0% → 41% wynika wprost z pojawienia się rozłącznych płaszczyzn dopiero od N=4. Elementarna teoria grup, nie hand-waving.

5. **Przykład makroruchu (N=3):** `Rz²·Rx² = Ry²` co do rotacji celu (`diag(-1,-1,1)·diag(1,-1,-1)=diag(-1,1,-1)`), więc
   oba układają orientację `M=Y₁₈₀` **identycznie**. Ale jako ruchy kostki są RÓŻNE: `Ry²` rusza jedną warstwę, `Rz²Rx²`
   dwie → inny kolateral → różny stan globalny → **distinct**. To jest istota makroruchu: ten sam efekt na celu, inny
   kolateral (materiał na komutator). Baseline znajduje `Ry²` (minimalne), `mode` dokłada `Rz²Rx²`.

6. **Pomiar po pełnej konfiguracji kostki jest właściwy:** `Rz²Rx² ≠ Ry²` (różny kolateral, prawdziwy makroruch), ale
   `Rz²Rx² = Rx²Rz²` (przemienne, ten sam stan — słusznie sklejone). Correct-string zawyża (nie kwocjentuje przemienności),
   efekt-na-celu zaniża (traci makroruchy) — pełna konfiguracja trafia w środek.

7. **Wszystko zweryfikowane empirycznie:** 100% solve rate — każdy mode-manewr faktycznie układa cel na prawdziwej kostce.

## 7. Implikacja dla produkcji (do decyzji)

Pierwotnie celowaliśmy we wpięcie **permutacji** w metrykę greedy. Rigorystycznie jednak **`mode` jest bezkonkurencyjny**
(+310% vs perm +49%). Mechanizm wpięcia (z diagnostyka): greedy enumeruje **realne** ruchy, `mode` wpływa tylko na
permutowany/mieszany `RotCount` — **bez back-mapu ruchów**. Uproszczenie: `Turn` używa `SetAngle(move.Angle)` (bez `+1`,
po zmianie konwencji „90° = bity 01"), więc geny 1..3 aplikują się wprost.

**Otwarta decyzja:** czy do produkcyjnego seedowania GA wpiąć `mode` (materiał na komutatory, kosztem dłuższych sekwencji),
oraz czy zbadać jeszcze oś `nad-R` (prawe obroty = sprzężenia).

## 8. Narzędzia (menu File)

- **Greedy Diversity** (`TGreedyDiversity.Run`) — tabela osi × długość, Correct-string. Dowolny Size.
- **Orientation Cluster** (`FindOrientationCluster`) — znajduje klaster rozmiaru = liczba orientacji (generyczny kubik);
  histogram rozmiarów klastrów. Wymaga `Size ≥ 2N−1` dla generycznego.
- **Verify Manoeuvres** (`Verify`) — empiryczna weryfikacja + tabela per metoda: Correct-string → prawdziwy efekt.
  Wymaga `Size ≥ 2N−1` (generyczny cel). Dla N=4 użyj Size 7.
