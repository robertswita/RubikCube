using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using GA;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using TGL;

namespace RubikCube.Maui;

public static class DebugLog
{
    // Save to app data directory
    public static readonly string LogPath = Path.Combine(FileSystem.AppDataDirectory, "debug.log");

    public static void WriteLine(string message)
    {
        try
        {
            File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}\n");
        }
        catch { }
    }

    public static void Clear()
    {
        try { File.Delete(LogPath); } catch { }
    }
}

public partial class MainPage : ContentPage
{
    // GA and cube state
    private TGA<TRubikGenome>? _ga;
    private TRubikCube _rubikCube = null!;
    private TRubikCube _gaCube = null!; // Separate cube for GA calculations
    private TShape _root = new TShape();
    private Dictionary<string, List<int>> _solutions = new();

    // Thread-safe move queue
    private readonly ConcurrentQueue<TMove> _moveQueue = new();
    private TMove? _currentMove;

    // Animation state
    private int _frameNo;
    private const int FrameCount = 10;
    private TShape? _actSlice;
    private IDispatcherTimer? _moveTimer;

    // GA background task
    private CancellationTokenSource? _gaCts;
    private Task? _gaTask;
    private bool _isGaRunning;

    // Statistics
    private int _gaCount;
    private TimeSpan _iterElapsed;
    private TimeSpan _time;
    private int _movesCount;
    private double _highScore;
    private Stopwatch? _watch;

    // Chart data
    public ObservableCollection<ISeries> Series { get; set; } = new();
    public ObservableCollection<Axis> XAxes { get; set; } = new();
    public ObservableCollection<Axis> YAxes { get; set; } = new();
    private ObservableCollection<ObservableValue> _fitnessValues = new();

    // Rotation tracking
    private double _lastPanX;
    private double _lastPanY;

    public MainPage()
    {
        InitializeComponent();

        // Initialize chart
        Series.Add(new LineSeries<ObservableValue>
        {
            Values = _fitnessValues,
            Fill = null,
            GeometrySize = 0
        });
        XAxes.Add(new Axis { Name = "Generation" });
        YAxes.Add(new Axis { Name = "Fitness" });

        BindingContext = this;

        // Log startup
        DebugLog.Clear();
        DebugLog.WriteLine($"App started. Log path: {DebugLog.LogPath}");

        // Initialize cube
        InitializeCube();

        // Setup timer
        _moveTimer = Dispatcher.CreateTimer();
        _moveTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
        _moveTimer.Tick += OnMoveTimerTick;

        // Initialize state grid drawable
        _stateGridDrawable = new StateGridDrawable(() => _rubikCube);
        StateGridView.Drawable = _stateGridDrawable;

        // Set initial slider values
        DimensionSlider.Value = TAffine.N;
        DimensionLabel.Text = TAffine.N.ToString();
        SizeSlider.Value = TRubikCube.Size;
        SizeLabel.Text = TRubikCube.Size.ToString();

        // Subscribe to scroll wheel events
        CubeViewControl.ScrollWheelChanged += OnCubeViewScrollWheelChanged;
    }

    // State grid drawable
    private StateGridDrawable _stateGridDrawable = null!;

    private void InitializeCube()
    {
        // Create both cubes fresh - they start in the same solved state
        _rubikCube = new TRubikCube();
        _rubikCube.Parent = _root;
        _gaCube = new TRubikCube(); // Separate cube for GA, same initial state
        CubeViewControl.Root = _root;
        CubeViewControl.Invalidate();
    }

    #region Gesture Handlers

    private void OnCubeViewPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _lastPanX = 0;
                _lastPanY = 0;
                break;

            case GestureStatus.Running:
                var deltaX = e.TotalX - _lastPanX;
                var deltaY = e.TotalY - _lastPanY;

                var rotY = 180 * deltaX / CubeViewControl.Width;
                var rotX = 180 * deltaY / CubeViewControl.Height;

                _root.Rotate(1, rotY);
                _root.Rotate(0, rotX);
                CubeViewControl.Invalidate();

                _lastPanX = e.TotalX;
                _lastPanY = e.TotalY;
                break;
        }
    }

    private void OnCubeViewPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Running)
        {
            // Use pinch for ZW rotation (4D)
            _root.Rotate(5, (e.Scale - 1) * 30);
            CubeViewControl.Invalidate();
        }
    }

    private void OnCubeViewScrollWheelChanged(object? sender, Controls.ScrollWheelEventArgs e)
    {
        // Rotate on axes 2 and 3 (XW and YW planes in 4D) like the original WinForms
        // deltaY is the main scroll direction
        var rotationAmount = e.DeltaY / 5f;
        _root.Rotate(Math.Min(TAffine.Planes.Length - 1, 2), rotationAmount);
        _root.Rotate(Math.Min(TAffine.Planes.Length - 1, 3), rotationAmount);
        CubeViewControl.Invalidate();
    }

    #endregion

    #region Button Handlers

    private void OnSolveClicked(object? sender, EventArgs e)
    {
        if (_isGaRunning) return;

        // Reset statistics but don't touch animation - it can continue
        _movesCount = 0;
        _time = TimeSpan.Zero;
        _gaCount = 0;
        _highScore = 0;
        _fitnessValues.Clear();
        _evalCount = 0; // Reset debug counter
        DebugLog.Clear(); // Clear previous log

        // Debug: log cube state before solving
        DebugLog.WriteLine($"OnSolve: _gaCube unsolved={_gaCube.Cubies.Count(c => c.State != 0)}, " +
            $"_rubikCube unsolved={_rubikCube.Cubies.Count(c => c.State != 0)}");

        LoadSolutions();
        StartGaBackground();
    }

    private void OnShuffleClicked(object? sender, EventArgs e)
    {
        // Stop GA if running
        StopGa();

        DebugLog.WriteLine($"Shuffle START: _gaCube unsolved={_gaCube.Cubies.Count(c => c.State != 0)}");

        var rnd = TChromosome.Rnd;

        // Generate shuffle moves using _gaCube (which is always "ahead")
        // Apply each move to _gaCube immediately, queue for _rubikCube animation
        for (int i = 0; i < 10; i++)
        {
            _gaCube.ActiveCubie = _gaCube.Cubies[rnd.Next(_gaCube.Cubies.Length)];
            var freeMoves = _gaCube.GetFreeMoves();
            var code = freeMoves[rnd.Next(freeMoves.Count)];
            var move = TMove.Decode(code);

            // Apply to _gaCube immediately
            _gaCube.Turn(move);

            // Queue for _rubikCube animation
            _moveQueue.Enqueue(move);
        }

        DebugLog.WriteLine($"Shuffle END: _gaCube unsolved={_gaCube.Cubies.Count(c => c.State != 0)}");

        // Start animation if not running
        if (_moveTimer?.IsRunning != true)
            _moveTimer?.Start();
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        StopGa();
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        // Stop everything
        StopGa();
        _moveTimer?.Stop();

        // Clear the move queue
        while (_moveQueue.TryDequeue(out _)) { }

        // Reset animation state
        if (_actSlice != null)
        {
            UnGroup();
        }
        _currentMove = null;
        _frameNo = 0;

        // Recreate both cubes fresh - they start in the same solved state
        RecreateCube();

        // Reset statistics
        _movesCount = 0;
        _time = TimeSpan.Zero;
        _gaCount = 0;
        _highScore = 0;
        _fitnessValues.Clear();

        // Update UI
        ErrorLabel.Text = "0";
        TimeLabel.Text = "00:00:00";
        MovesLabel.Text = "0";
        SolutionLabel.Text = "0";
        GACountLabel.Text = "0";
    }

    private void StopGa()
    {
        _gaCts?.Cancel();
        _isGaRunning = false;
        SolveBtn.IsEnabled = true;
        StopBtn.BackgroundColor = Colors.OrangeRed;
    }

    private void OnDimensionSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        int dimension = (int)Math.Round(e.NewValue);
        DimensionLabel.Text = dimension.ToString();

        if (TAffine.N != dimension)
        {
            TAffine.N = dimension;
            RecreateCube();
        }
    }

    private void OnSizeSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        int size = (int)Math.Round(e.NewValue);
        SizeLabel.Text = size.ToString();

        if (TRubikCube.Size != size)
        {
            TRubikCube.Size = size;
            RecreateCube();
        }
    }

    private void RecreateCube()
    {
        _root = new TShape();
        // Create both cubes fresh - they start in the same solved state
        _rubikCube = new TRubikCube();
        _rubikCube.Parent = _root;
        _gaCube = new TRubikCube(); // Separate cube for GA, same initial state
        CubeViewControl.Root = _root;
        CubeViewControl.Invalidate();
        StateGridView.Invalidate();
    }

    private void OnTransparencyChanged(object? sender, CheckedChangedEventArgs e)
    {
        CubeViewControl.IsTransparencyOn = e.Value;
        CubeViewControl.Invalidate();
    }

    #endregion

    #region Animation Timer

    private void OnMoveTimerTick(object? sender, EventArgs e)
    {
        // If we have a current move being animated
        if (_currentMove != null)
        {
            _frameNo++;

            if (_frameNo <= FrameCount)
            {
                double angle = 90 * (_currentMove.Angle + 1);
                if (angle > 180) angle -= 360;
                angle *= (double)_frameNo / FrameCount;
                _actSlice!.Transform = new TAffine();
                _actSlice!.Transform.Rotate(_currentMove.Plane, angle);
            }
            else
            {
                // Animation complete for this move
                UnGroup();
                _rubikCube.Turn(_currentMove);
                _currentMove = null;
                _frameNo = 0;
                _movesCount++;

                // Update UI
                MovesLabel.Text = _movesCount.ToString();
                StateGridView.Invalidate();
            }

            CubeViewControl.Invalidate();
        }
        // Try to get next move from queue
        else if (_moveQueue.TryDequeue(out var nextMove))
        {
            _currentMove = nextMove;
            _frameNo = 0;
            Group(_rubikCube.SelectSlice(nextMove));
            CubeViewControl.Invalidate();
        }
        // Queue is empty
        else
        {
            // Update time display
            if (_watch != null)
            {
                TimeLabel.Text = _time.ToString(@"hh\:mm\:ss");
            }

            // Stop timer if GA is not running and queue is empty
            if (!_isGaRunning)
            {
                _moveTimer?.Stop();
            }
        }
    }

    private void Group(List<TCubie> selection)
    {
        _actSlice = new TShape();
        foreach (var cubie in selection)
            cubie.Parent = _actSlice;
        _actSlice.Parent = _rubikCube;
    }

    private void UnGroup()
    {
        if (_actSlice == null) return;
        for (int i = _actSlice.Children.Count - 1; i >= 0; i--)
            _actSlice.Children[i].Parent = _rubikCube;
        _actSlice.Parent = null;
    }

    #endregion

    #region GA Solver

    private void StartGaBackground()
    {
        if (_isGaRunning) return;

        _isGaRunning = true;
        _gaCts = new CancellationTokenSource();
        var token = _gaCts.Token;

        // Update UI
        SolveBtn.IsEnabled = false;
        StopBtn.BackgroundColor = Colors.Red;

        // _gaCube is already in the correct state (shuffle moves were applied to it)
        // No need to copy - both cubes started from same state and receive same moves
        _highScore = 0;

        // Start animation timer if not already running
        if (_moveTimer?.IsRunning != true)
            _moveTimer?.Start();

        // Run GA on background thread
        _gaTask = Task.Run(() => RunGaLoop(token), token);
    }

    private void RunGaLoop(CancellationToken token)
    {
        _watch = Stopwatch.StartNew();

        while (!token.IsCancellationRequested)
        {
            // Find next cluster to solve
            if (_highScore == 0)
            {
                _gaCube.NextCluster();
                if (_gaCube.ActiveCubie != null)
                    TRubikGenome.FreeMoves = _gaCube.GetFreeMoves();
                _highScore = double.MaxValue;

                // Debug: log cluster info
                DebugLog.WriteLine($"NextCluster: ActiveCubie={_gaCube.ActiveCubie != null}, " +
                    $"ActiveCluster={((_gaCube.ActiveCluster?.Count) ?? 0)}, " +
                    $"FreeMoves={TRubikGenome.FreeMoves?.Count ?? 0}, " +
                    $"UnsolvedCount={_gaCube.Cubies.Count(c => c.State != 0)}");
            }

            if (_gaCube.ActiveCluster == null)
            {
                // All clusters solved
                DebugLog.WriteLine("All clusters solved - exiting GA loop");
                break;
            }

            _iterElapsed = TimeSpan.Zero;

            TChromosome.GenesLength = 30;
            _ga = new TGA<TRubikGenome>
            {
                GenerationsCount = 100,
                WinnerRatio = 0.1,
                MutationRatio = 1,
                SelectionType = TGA<TRubikGenome>.TSelectionType.Unique,
                Evaluate = OnEvaluate,
                Progress = OnProgress,
                HighScore = _highScore
            };

            TRubikGenome.FreeMoves = _gaCube.GetFreeMoves();

            try
            {
                _ga.Execute();
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (token.IsCancellationRequested) break;

            if (_ga.HighScore == 0 && _gaCube.ActiveCluster.Count > 1)
            {
                SaveSolution(_ga.Best);
            }

            if (_ga.HighScore < _highScore)
            {
                _highScore = _ga.HighScore;

                // Queue moves for animation
                for (int i = 0; i < _ga.Best.MovesCount; i++)
                {
                    var move = TMove.Decode((int)_ga.Best.Genes[i]);
                    _moveQueue.Enqueue(move);
                    // Also apply to GA cube immediately
                    _gaCube.Turn(move);
                }

                // Only move to next cluster when this one is fully solved (fitness = 0)
                // _highScore is already set to _ga.HighScore above
                // If it's 0, NextCluster() will be called on next iteration
                // If it's > 0, we continue trying to improve this cluster
            }

            _time += _watch.Elapsed;
            _watch.Restart();

            // Update UI on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ErrorLabel.Text = _ga.HighScore.ToString("F2");
                GACountLabel.Text = (++_gaCount).ToString();
                TimeLabel.Text = _time.ToString(@"hh\:mm\:ss");
            });
        }

        // GA finished or cancelled
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isGaRunning = false;
            SolveBtn.IsEnabled = true;
            StopBtn.BackgroundColor = Colors.OrangeRed;
        });
    }

    private void OnProgress(TRubikGenome specimen)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _fitnessValues.Add(new ObservableValue(specimen.Fitness));

            var iterTime = _watch!.Elapsed - _iterElapsed;
            IterTimeLabel.Text = $"Iter time: {iterTime.Milliseconds}ms";
            _iterElapsed += iterTime;
        });
    }

    private static int _evalCount = 0;
    private double OnEvaluate(TRubikGenome specimen)
    {
        // Check for cancellation to allow stopping mid-GA
        if (_gaCts?.Token.IsCancellationRequested == true)
            throw new OperationCanceledException();

        specimen.Correct();
        specimen.Fitness = double.MaxValue;

        // Use the GA cube copy for evaluation
        var cube = new TRubikCube(_gaCube);

        // Debug first few evaluations
        if (_evalCount < 3)
        {
            DebugLog.WriteLine($"OnEvaluate #{_evalCount}: " +
                $"_gaCube.ActiveCubie={_gaCube.ActiveCubie != null}, " +
                $"cube.ActiveCubie={cube.ActiveCubie != null}, " +
                $"cube.ActiveCluster={cube.ActiveCluster?.Count ?? 0}, " +
                $"cube.SolvedCubies count={cube.Cubies.Count(c => c.State == 0)}");
        }

        double initialFitness = cube.Evaluate();

        for (int i = 0; i < specimen.Genes.Length; i++)
        {
            var move = TMove.Decode((int)specimen.Genes[i]);
            cube.Turn(move);

            double fitness = cube.Evaluate();
            if (fitness < specimen.Fitness)
            {
                specimen.Fitness = fitness;
                specimen.MovesCount = i + 1;
            }
        }

        if (_evalCount < 3)
        {
            DebugLog.WriteLine($"  Initial fitness: {initialFitness}, Best found: {specimen.Fitness}");
            _evalCount++;
        }

        return specimen.Fitness;
    }

    #endregion

    #region Solution Persistence

    private string SolutionPath => Path.Combine(FileSystem.AppDataDirectory, "solutions.bin");

    private void LoadSolutions()
    {
        _solutions.Clear();

        try
        {
            if (File.Exists(SolutionPath))
            {
                using var reader = new BinaryReader(File.OpenRead(SolutionPath));
                while (reader.BaseStream.Position != reader.BaseStream.Length)
                {
                    var key = reader.ReadString();
                    var movesCount = reader.ReadInt32();
                    var genes = new List<int>(movesCount);
                    for (int i = 0; i < movesCount; i++)
                        genes.Add(reader.ReadInt32());
                    _solutions[key] = genes;
                }
            }
        }
        catch { /* Ignore errors */ }

        SolutionLabel.Text = _solutions.Count.ToString();
    }

    private void SaveSolution(TRubikGenome solution)
    {
        var code = _gaCube.Code;
        if (!_solutions.ContainsKey(code))
        {
            try
            {
                using var writer = new BinaryWriter(File.Open(SolutionPath, FileMode.Append));
                writer.Write(code);
                writer.Write(solution.MovesCount);

                var genes = new List<int>(solution.MovesCount);
                for (int i = 0; i < solution.MovesCount; i++)
                {
                    genes.Add((int)solution.Genes[i]);
                    writer.Write(genes[i]);
                }
                _solutions[code] = genes;
            }
            catch { /* Ignore errors */ }
        }

        // Update UI on main thread
        var count = _solutions.Count;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SolutionLabel.Text = count.ToString();
        });
    }

    #endregion
}

/// <summary>
/// Drawable for rendering the cube's state grid visualization.
/// Displays a colored bitmap based on RubikCube.StateGrid.
/// </summary>
public class StateGridDrawable : IDrawable
{
    private readonly Func<TRubikCube> _getCube;

    public StateGridDrawable(Func<TRubikCube> getCube)
    {
        _getCube = getCube;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var cube = _getCube();
        if (cube == null) return;

        var grid = cube.StateGrid;
        if (grid == null) return;

        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        if (rows == 0 || cols == 0) return;

        // Use square area (minimum of width/height)
        float size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        float cellSize = size / Math.Max(rows, cols);

        // Center the grid in the available space
        float offsetX = (dirtyRect.Width - cellSize * cols) / 2;
        float offsetY = (dirtyRect.Height - cellSize * rows) / 2;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                Color color;
                int value = grid[y, x];

                if (value == 0)
                {
                    color = Colors.White;
                }
                else
                {
                    // Extract RGB components from the value (same as WinForms)
                    float r = (float)(255.0 / 16.0 * (1 + (value & 0xF))) / 255f;
                    float g = (float)(255.0 / 16.0 * (1 + ((value >> 4) & 0xF))) / 255f;
                    float b = (float)(255.0 / 16.0 * (1 + ((value >> 8) & 0xF))) / 255f;
                    color = new Color(r, g, b);
                }

                canvas.FillColor = color;
                canvas.FillRectangle(offsetX + x * cellSize, offsetY + y * cellSize, cellSize, cellSize);
            }
        }
    }
}
