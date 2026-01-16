using System.Collections.ObjectModel;
using System.Diagnostics;
using GA;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using TGL;

namespace RubikCube.Maui;

public partial class MainPage : ContentPage
{
    // GA and cube state
    private TGA<TRubikGenome>? _ga;
    private TRubikCube _rubikCube = null!;
    private TShape _root = new TShape();
    private List<TMove> _moves = new();
    private Dictionary<string, List<int>> _solutions = new();

    // Animation state
    private int _moveNo;
    private int _frameNo;
    private const int FrameCount = 10;
    private TShape? _actSlice;
    private bool _isPaused = true;
    private IDispatcherTimer? _moveTimer;

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
    }

    // State grid drawable
    private StateGridDrawable _stateGridDrawable = null!;

    private void InitializeCube()
    {
        _rubikCube = new TRubikCube();
        _rubikCube.Parent = _root;
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

    #endregion

    #region Button Handlers

    private void OnSolveClicked(object? sender, EventArgs e)
    {
        if (_moveTimer?.IsRunning == true) return;

        _movesCount = 0;
        _time = TimeSpan.Zero;
        _gaCount = 0;
        _isPaused = false;
        _highScore = 0;

        LoadSolutions();
        Solve();
    }

    private void OnShuffleClicked(object? sender, EventArgs e)
    {
        if (_moveTimer?.IsRunning == true) return;

        _isPaused = true;
        var size = TRubikCube.Size;
        var rnd = TChromosome.Rnd;

        for (int i = 0; i < 10; i++)
        {
            _rubikCube.ActiveCubie = _rubikCube.Cubies[rnd.Next(_rubikCube.Cubies.Length)];
            var freeMoves = _rubikCube.GetFreeMoves();
            var code = freeMoves[rnd.Next(freeMoves.Count)];
            var move = TMove.Decode(code);
            _moves.Add(move);
            _rubikCube.ActiveCubie.State = _rubikCube.ActiveCubie.State;
        }

        _moveTimer?.Start();
    }

    private void OnPauseClicked(object? sender, EventArgs e)
    {
        _isPaused = !_isPaused;
        PauseBtn.BackgroundColor = _isPaused ? Colors.Red : Colors.LightGray;

        if (!_isPaused)
        {
            _fitnessValues.Clear();
            _moveTimer?.Start();
        }
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
        _rubikCube = new TRubikCube();
        _rubikCube.Parent = _root;
        CubeViewControl.Root = _root;
        CubeViewControl.Invalidate();
        StateGridView.Invalidate();
        _moves.Clear();
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
        if (_moveNo < _moves.Count)
        {
            TMove move = _moves[_moveNo];

            if (_frameNo == 0)
                Group(_rubikCube.SelectSlice(move));

            _frameNo++;

            if (_frameNo <= FrameCount)
            {
                double angle = 90 * (move.Angle + 1);
                if (angle > 180) angle -= 360;
                angle *= (double)_frameNo / FrameCount;
                _actSlice!.Transform = TAffine.CreateRotation(move.Plane, angle);
            }
            else
            {
                UnGroup();
                _rubikCube.Turn(move);
                _frameNo = 0;
                _moveNo++;
            }

            CubeViewControl.Invalidate();
        }
        else if (_moveNo > 0)
        {
            _moveNo = 0;
            _moves.Clear();

            ErrorLabel.Text = _highScore.ToString("F2");
            var unsolved = _rubikCube.Code.Count(x => x != '\0');
            MovesLabel.Text = _movesCount.ToString();
            SolutionLabel.Text = unsolved.ToString();
            GACountLabel.Text = (++_gaCount).ToString();
            StateGridView.Invalidate();
        }
        else
        {
            TimeLabel.Text = _time.ToString(@"hh\:mm\:ss");
            _moveTimer?.Stop();

            if (_ga != null && !_isPaused)
                Solve();
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

    private void Solve()
    {
        if (_highScore == 0)
        {
            _rubikCube.NextCluster();
            if (_rubikCube.ActiveCubie != null)
                TRubikGenome.FreeMoves = _rubikCube.GetFreeMoves();
            _highScore = double.MaxValue;
        }

        if (_rubikCube.ActiveCluster != null)
        {
            _watch = Stopwatch.StartNew();
            _iterElapsed = TimeSpan.Zero;
            _fitnessValues.Clear();

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

            TRubikGenome.FreeMoves = _rubikCube.GetFreeMoves();
            _ga.Execute();

            if (_ga.HighScore == 0 && _rubikCube.ActiveCluster.Count > 1)
            {
                SaveSolution(_ga.Best);
            }

            if (_ga.HighScore < _highScore)
            {
                _highScore = _ga.HighScore;
                for (int i = 0; i < _ga.Best.MovesCount; i++)
                    _moves.Add(TMove.Decode((int)_ga.Best.Genes[i]));
            }

            _movesCount += _moves.Count;
            _time += _watch.Elapsed;
            _moveTimer?.Start();
        }
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

    private double OnEvaluate(TRubikGenome specimen)
    {
        specimen.Check();
        specimen.Fitness = double.MaxValue;

        var cube = new TRubikCube(_rubikCube);

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
        var code = _rubikCube.Code;
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

        SolutionLabel.Text = _solutions.Count.ToString();
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
