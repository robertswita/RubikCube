using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using GA;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using RubikCube;
using TGL;
using TGL.GA;
using TGL.GA.Configuration;

namespace RubikCube.Maui;

public partial class MainPage : ContentPage
{
    // GA and cube state
    private RubikGASolver? _solver;
    private TRubikCube _rubikCube = null!;
    private TRubikCube _gaCube = null!; // Separate cube for GA calculations
    private TShape _root = new TShape();
    private SolutionDatabase _solutionDb = null!;

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

    // GA Configuration (from UI)
    private SolverMode _selectedSolverMode = SolverMode.Iterative;
    private GAConfig _selectedGAConfig = GAPresets.Default;
    private int _generationsPerIteration = 100;
    private PresetManager _presetManager = null!;

    // Chart data
    private const int MaxChartPoints = 500;
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
        DebugLog.ApplicationType = AppType.Maui;
        DebugLog.Clear();
        DebugLog.WriteLine($"App started. Log path: {DebugLog.LogPath}");

        // Initialize solution database
        var solutionPath = Path.Combine(FileSystem.AppDataDirectory, "solutions.bin");
        _solutionDb = new SolutionDatabase(solutionPath);
        _solutionDb.Load();
        _solutionDb.SolutionSaved += count =>
        {
            MainThread.BeginInvokeOnMainThread(() => SolutionLabel.Text = count.ToString());
        };

        // Initialize preset manager
        var presetPath = Path.Combine(FileSystem.AppDataDirectory, "presets.json");
        _presetManager = new PresetManager(presetPath);
        _presetManager.Load();
        _presetManager.PresetsChanged += RefreshPresetPicker;
        RefreshPresetPicker();

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

        // Initialize GA configuration UI
        SolverModePicker.SelectedIndex = 0; // Iterative

        // Select the last used preset
        var lastUsedIndex = _presetManager.GetIndex(_presetManager.LastUsedPreset);
        PresetPicker.SelectedIndex = lastUsedIndex >= 0 ? lastUsedIndex : 0;

        SelectionPicker.SelectedIndex = 0; // Unique
        CrossoverPicker.SelectedIndex = 0; // SinglePoint
        MutationPicker.SelectedIndex = 0; // SingleGene
        UpdateGAConfigLabels();

        // Subscribe to scroll wheel events
        CubeViewControl.ScrollWheelChanged += OnCubeViewScrollWheelChanged;

        // Subscribe to mouse drag events (for Windows rotation support)
        CubeViewControl.MouseDragChanged += OnCubeViewMouseDragChanged;
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

    private void OnCubeViewMouseDragChanged(object? sender, Controls.MouseDragEventArgs e)
    {
        // Apply rotation based on mouse drag delta (Windows only)
        // Same logic as OnCubeViewPanUpdated but using direct delta values
        var rotY = 180 * e.DeltaX / (float)CubeViewControl.Width;
        var rotX = 180 * e.DeltaY / (float)CubeViewControl.Height;

        _root.Rotate(1, rotY);
        _root.Rotate(0, rotX);
        CubeViewControl.Invalidate();
    }

    #endregion

    #region Button Handlers

    private void OnSolveClicked(object? sender, EventArgs e)
    {
        DebugLog.WriteLine($"OnSolveClicked called, _isGaRunning={_isGaRunning}");

        if (_isGaRunning) return;

        // Reset statistics but don't touch animation - it can continue
        _movesCount = 0;
        _time = TimeSpan.Zero;
        _gaCount = 0;
        _highScore = 0;
        _fitnessValues.Clear();

        // Debug: log cube state before solving
        DebugLog.WriteLine($"OnSolve: _gaCube unsolved={_gaCube.Cubies.Count(c => c.State != 0)}, " +
            $"_rubikCube unsolved={_rubikCube.Cubies.Count(c => c.State != 0)}");

        // Show current solution count
        SolutionLabel.Text = _solutionDb.Count.ToString();
        StartGaBackground();
    }

    private void OnShuffleClicked(object? sender, EventArgs e)
    {
        // Stop GA if running
        StopGa();

        int shuffleMoves = (int)Math.Round(ShuffleMoveSlider.Value);
        DebugLog.WriteLine($"Shuffle START: {shuffleMoves} moves, _gaCube unsolved={_gaCube.Cubies.Count(c => c.State != 0)}");

        var rnd = TChromosome.Rnd;

        // Generate shuffle moves using _gaCube (which is always "ahead")
        // Apply each move to _gaCube immediately, queue for _rubikCube animation
        for (int i = 0; i < shuffleMoves; i++)
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

    private async void OnClearSolutionsClicked(object? sender, EventArgs e)
    {
        if (_isGaRunning) return;

        bool confirm = await DisplayAlert(
            "Clear Solutions",
            "Are you sure you want to delete the solution database?",
            "Yes", "No");

        if (confirm)
        {
            _solutionDb.Clear();
            SolutionLabel.Text = "0";
        }
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
        ErrorLabel.Text = "0.000";
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

    private void OnShuffleMoveSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        int moves = (int)Math.Round(e.NewValue);
        ShuffleMoveLabel.Text = moves.ToString();
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
                _actSlice!.Transform = TAffine.CreateRotation(_currentMove.Plane, angle);
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

    #region GA Configuration UI

    private void OnSolverModeChanged(object? sender, EventArgs e)
    {
        if (SolverModePicker.SelectedIndex < 0) return;

        _selectedSolverMode = SolverModePicker.SelectedIndex switch
        {
            0 => SolverMode.Iterative,
            1 => SolverMode.Complete,
            2 => SolverMode.Adaptive,
            _ => SolverMode.Iterative
        };
    }

    private void OnPresetChanged(object? sender, EventArgs e)
    {
        if (PresetPicker.SelectedItem is string presetName)
        {
            _selectedGAConfig = _presetManager.GetConfig(presetName);
            _presetManager.LastUsedPreset = presetName;
        }

        // Update sliders to match preset
        PopulationSlider.Value = _selectedGAConfig.PopulationSize;
        MutationSlider.Value = _selectedGAConfig.MutationRate;
        GenerationsSlider.Value = _selectedGAConfig.Termination.MaxGenerations;
        EliteSlider.Value = _selectedGAConfig.EliteCount;
        ChromosomeLengthSlider.Value = _selectedGAConfig.GenomeLength;

        // Update pickers to match preset
        SelectionPicker.SelectedIndex = _selectedGAConfig.Selection switch
        {
            SelectionStrategy.Unique => 0,
            SelectionStrategy.Tournament => 1,
            SelectionStrategy.Rank => 2,
            SelectionStrategy.Roulette => 3,
            SelectionStrategy.RouletteRank => 4,
            _ => 0
        };
        CrossoverPicker.SelectedIndex = _selectedGAConfig.Crossover switch
        {
            CrossoverStrategy.SinglePoint => 0,
            CrossoverStrategy.TwoPoint => 1,
            CrossoverStrategy.Uniform => 2,
            CrossoverStrategy.SegmentPreserving => 3,
            _ => 0
        };
        MutationPicker.SelectedIndex = _selectedGAConfig.Mutation switch
        {
            MutationStrategy.SingleGene => 0,
            MutationStrategy.Random => 1,
            MutationStrategy.Swap => 2,
            MutationStrategy.Inversion => 3,
            MutationStrategy.Scramble => 4,
            MutationStrategy.Conjugation => 5,
            MutationStrategy.Commutator => 6,
            MutationStrategy.Neighbor => 7,
            MutationStrategy.Simplify => 8,
            MutationStrategy.InverseSequence => 9,
            MutationStrategy.Insert => 10,
            MutationStrategy.Shift => 11,
            _ => 0
        };

        UpdateGAConfigLabels();
    }

    private void RefreshPresetPicker()
    {
        var selectedIndex = PresetPicker.SelectedIndex;
        PresetPicker.ItemsSource = _presetManager.PresetNames.ToList();
        if (selectedIndex >= 0 && selectedIndex < PresetPicker.ItemsSource.Count)
            PresetPicker.SelectedIndex = selectedIndex;
        else if (PresetPicker.ItemsSource.Count > 0)
            PresetPicker.SelectedIndex = 0;
    }

    private void UpdateGAConfigFromUI()
    {
        // Get selection strategy
        var selection = SelectionPicker.SelectedIndex switch
        {
            0 => SelectionStrategy.Unique,
            1 => SelectionStrategy.Tournament,
            2 => SelectionStrategy.Rank,
            3 => SelectionStrategy.Roulette,
            4 => SelectionStrategy.RouletteRank,
            _ => SelectionStrategy.Unique
        };

        // Get crossover strategy
        var crossover = CrossoverPicker.SelectedIndex switch
        {
            0 => CrossoverStrategy.SinglePoint,
            1 => CrossoverStrategy.TwoPoint,
            2 => CrossoverStrategy.Uniform,
            3 => CrossoverStrategy.SegmentPreserving,
            _ => CrossoverStrategy.SinglePoint
        };

        // Get mutation strategy
        var mutation = MutationPicker.SelectedIndex switch
        {
            0 => MutationStrategy.SingleGene,
            1 => MutationStrategy.Random,
            2 => MutationStrategy.Swap,
            3 => MutationStrategy.Inversion,
            4 => MutationStrategy.Scramble,
            5 => MutationStrategy.Conjugation,
            6 => MutationStrategy.Commutator,
            7 => MutationStrategy.Neighbor,
            8 => MutationStrategy.Simplify,
            9 => MutationStrategy.InverseSequence,
            10 => MutationStrategy.Insert,
            11 => MutationStrategy.Shift,
            _ => MutationStrategy.SingleGene
        };

        _selectedGAConfig = _selectedGAConfig with
        {
            PopulationSize = (int)PopulationSlider.Value,
            MutationRate = MutationSlider.Value,
            EliteCount = (int)EliteSlider.Value,
            GenomeLength = (int)ChromosomeLengthSlider.Value,
            Selection = selection,
            Crossover = crossover,
            Mutation = mutation
        };
        _generationsPerIteration = (int)GenerationsSlider.Value;
    }

    private async void OnSavePresetClicked(object? sender, EventArgs e)
    {
        // Show input dialog to get preset name
        string presetName = await DisplayPromptAsync("Save Preset", "Enter a name for the preset:");
        if (string.IsNullOrWhiteSpace(presetName)) return;

        // Get current config from UI
        UpdateGAConfigFromUI();

        // Try to add the preset
        if (_presetManager.AddPreset(presetName, _selectedGAConfig))
        {
            // Select the new preset
            int index = _presetManager.GetIndex(presetName);
            if (index >= 0)
                PresetPicker.SelectedIndex = index;

            await DisplayAlert("Save Preset", $"Preset '{presetName}' saved successfully.", "OK");
        }
        else
        {
            await DisplayAlert("Save Preset", $"A preset with the name '{presetName}' already exists.", "OK");
        }
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (SelectionPicker.SelectedIndex < 0) return;

        var selection = SelectionPicker.SelectedIndex switch
        {
            0 => SelectionStrategy.Unique,
            1 => SelectionStrategy.Tournament,
            2 => SelectionStrategy.Rank,
            3 => SelectionStrategy.Roulette,
            4 => SelectionStrategy.RouletteRank,
            _ => SelectionStrategy.Unique
        };

        _selectedGAConfig = _selectedGAConfig with { Selection = selection };
    }

    private void OnCrossoverChanged(object? sender, EventArgs e)
    {
        if (CrossoverPicker.SelectedIndex < 0) return;

        var crossover = CrossoverPicker.SelectedIndex switch
        {
            0 => CrossoverStrategy.SinglePoint,
            1 => CrossoverStrategy.TwoPoint,
            2 => CrossoverStrategy.Uniform,
            3 => CrossoverStrategy.SegmentPreserving,
            _ => CrossoverStrategy.SinglePoint
        };

        _selectedGAConfig = _selectedGAConfig with { Crossover = crossover };
    }

    private void OnMutationStrategyChanged(object? sender, EventArgs e)
    {
        if (MutationPicker.SelectedIndex < 0) return;

        var mutation = MutationPicker.SelectedIndex switch
        {
            0 => MutationStrategy.SingleGene,
            1 => MutationStrategy.Random,
            2 => MutationStrategy.Swap,
            3 => MutationStrategy.Inversion,
            4 => MutationStrategy.Scramble,
            5 => MutationStrategy.Conjugation,
            6 => MutationStrategy.Commutator,
            7 => MutationStrategy.Neighbor,
            8 => MutationStrategy.Simplify,
            9 => MutationStrategy.InverseSequence,
            10 => MutationStrategy.Insert,
            11 => MutationStrategy.Shift,
            _ => MutationStrategy.SingleGene
        };

        _selectedGAConfig = _selectedGAConfig with { Mutation = mutation };
    }

    private void OnGAParamChanged(object? sender, ValueChangedEventArgs e)
    {
        UpdateGAConfigLabels();

        // Update config with current slider values
        _selectedGAConfig = _selectedGAConfig with
        {
            PopulationSize = (int)PopulationSlider.Value,
            MutationRate = MutationSlider.Value,
            EliteCount = (int)EliteSlider.Value,
            GenomeLength = (int)ChromosomeLengthSlider.Value
        };
        _generationsPerIteration = (int)GenerationsSlider.Value;
    }

    private void UpdateGAConfigLabels()
    {
        PopulationLabel.Text = ((int)PopulationSlider.Value).ToString();
        MutationLabel.Text = $"{(int)(MutationSlider.Value * 100)}%";
        GenerationsLabel.Text = ((int)GenerationsSlider.Value).ToString();
        EliteLabel.Text = ((int)EliteSlider.Value).ToString();
        ChromosomeLengthLabel.Text = ((int)ChromosomeLengthSlider.Value).ToString();
    }

    private void OnResetGAConfigClicked(object? sender, EventArgs e)
    {
        // Reset to original settings (Default preset, Iterative mode)
        _selectedSolverMode = SolverMode.Iterative;
        _selectedGAConfig = GAPresets.Default;
        _generationsPerIteration = 100;

        // Update UI
        SolverModePicker.SelectedIndex = 0;
        PresetPicker.SelectedIndex = 0;
        PopulationSlider.Value = _selectedGAConfig.PopulationSize;
        MutationSlider.Value = _selectedGAConfig.MutationRate;
        GenerationsSlider.Value = _selectedGAConfig.Termination.MaxGenerations;
        EliteSlider.Value = _selectedGAConfig.EliteCount;
        ChromosomeLengthSlider.Value = _selectedGAConfig.GenomeLength;
        SelectionPicker.SelectedIndex = 0; // Unique
        CrossoverPicker.SelectedIndex = 0; // SinglePoint
        MutationPicker.SelectedIndex = 0; // SingleGene
        UpdateGAConfigLabels();
    }

    #endregion

    #region GA Solver

    private void StartGaBackground()
    {
        DebugLog.WriteLine("StartGaBackground called");

        if (_isGaRunning) return;

        _isGaRunning = true;
        _gaCts = new CancellationTokenSource();
        var token = _gaCts.Token;

        // Update UI
        SolveBtn.IsEnabled = false;
        StopBtn.BackgroundColor = Colors.Red;

        // _gaCube is already in the correct state (shuffle moves were applied to it)
        _highScore = 0;

        // Start animation timer if not already running
        if (_moveTimer?.IsRunning != true)
            _moveTimer?.Start();

        DebugLog.WriteLine("Starting background task");

        // Run GA on background thread
        _gaTask = Task.Run(() => RunGaLoop(token), token);
    }

    private void RunGaLoop(CancellationToken token)
    {
        DebugLog.WriteLine("RunGaLoop started");
        _watch = Stopwatch.StartNew();

        try
        {
            // Configure GA using UI-selected settings (use preset's GenomeLength, default is 50)
            var gaConfig = _selectedGAConfig;

            var solverConfig = new SolverConfig
            {
                Mode = _selectedSolverMode,
                GenerationsPerIteration = _generationsPerIteration
            };

            DebugLog.WriteLine($"Creating solver. Mode={_selectedSolverMode}, Population={gaConfig.PopulationSize}, " +
                $"Mutation={gaConfig.MutationRate:P0}, Generations={_generationsPerIteration}");
            DebugLog.WriteLine($"Cube unsolved={_gaCube.Cubies.Count(c => c.State != 0)}");

            // Create solver with the GA cube and solution database
            _solver = new RubikGASolver(_gaCube, gaConfig, solverConfig);
            _solver.SolutionDb = _solutionDb;

            // Subscribe to solver events
            _solver.GenerationCompleted += OnGenerationCompleted;
            _solver.MovesReady += OnMovesReady;
            _solver.IterationCompleted += OnIterationCompleted;
            _solver.ClusterChanged += OnClusterChanged;

            DebugLog.WriteLine("Starting solver.Solve()");
            var result = _solver.Solve(token);

            DebugLog.WriteLine($"Solver completed: {result.TerminationReason}, " +
                $"TotalGenerations={result.TotalGenerations}, Fitness={result.Fitness}");
        }
        catch (OperationCanceledException)
        {
            DebugLog.WriteLine("Solver cancelled");
        }
        catch (Exception ex)
        {
            DebugLog.WriteLine($"Solver exception: {ex.GetType().Name}: {ex.Message}");
            DebugLog.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        // GA finished or cancelled
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isGaRunning = false;
            SolveBtn.IsEnabled = true;
            StopBtn.BackgroundColor = Colors.OrangeRed;
        });
    }

    private void OnGenerationCompleted(GAState<TRubikGenome> state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (state.Best != null)
            {
                // Remove oldest point if we're at the limit (queue behavior)
                if (_fitnessValues.Count >= MaxChartPoints)
                {
                    _fitnessValues.RemoveAt(0);
                }
                _fitnessValues.Add(new ObservableValue(state.Best.Fitness));
            }

            var iterTime = _watch!.Elapsed - _iterElapsed;
            IterTimeLabel.Text = $"Iter time: {iterTime.Milliseconds}ms";
            _iterElapsed += iterTime;
        });
    }

    private void OnMovesReady(List<TMove> moves)
    {
        // Queue moves for animation
        foreach (var move in moves)
        {
            _moveQueue.Enqueue(move);
        }
    }

    private void OnIterationCompleted(SolverResult result)
    {
        _time += _watch!.Elapsed;
        _watch.Restart();
        _highScore = result.Fitness;

        // Solution saving is now handled by the solver via SolutionDb

        // Update UI on main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ErrorLabel.Text = result.Fitness.ToString("F3");
            GACountLabel.Text = (++_gaCount).ToString();
            TimeLabel.Text = _time.ToString(@"hh\:mm\:ss");
        });
    }

    private void OnClusterChanged(TRubikCube cube)
    {
        DebugLog.WriteLine($"ClusterChanged: ActiveCubie={cube.ActiveCubie != null}, " +
            $"ActiveCluster={cube.ActiveCluster?.Count ?? 0}, " +
            $"FreeMoves={TRubikGenome.FreeMoves?.Count ?? 0}, " +
            $"UnsolvedCount={cube.Cubies.Count(c => c.State != 0)}");

        _iterElapsed = TimeSpan.Zero;
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
