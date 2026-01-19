using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TGL.GA.Configuration;

/// <summary>
/// A named preset containing a GAConfig.
/// </summary>
public class NamedPreset
{
    public string Name { get; set; } = "";
    public bool IsBuiltIn { get; set; }
    public GAConfigData Config { get; set; } = new();
}

/// <summary>
/// Serializable version of GAConfig for JSON persistence.
/// </summary>
public class GAConfigData
{
    public int PopulationSize { get; set; } = GADefaults.PopulationSize;
    public int EliteCount { get; set; } = GADefaults.EliteCount;
    public int GenomeLength { get; set; } = GADefaults.GenomeLength;
    public string Selection { get; set; } = GADefaults.Selection.ToString();
    public double SelectionRatio { get; set; } = GADefaults.SelectionRatio;
    public int TournamentSize { get; set; } = GADefaults.TournamentSize;
    public string Crossover { get; set; } = GADefaults.Crossover.ToString();
    public double CrossoverRate { get; set; } = GADefaults.CrossoverRate;
    public string Mutation { get; set; } = GADefaults.Mutation.ToString();
    public double MutationRate { get; set; } = GADefaults.MutationRate;
    public int MaxGenerations { get; set; } = GADefaults.MaxGenerations;
    public double TargetFitness { get; set; } = GADefaults.TargetFitness;
    public int StagnationLimit { get; set; } = GADefaults.StagnationLimit;

    public static GAConfigData FromGAConfig(GAConfig config)
    {
        return new GAConfigData
        {
            PopulationSize = config.PopulationSize,
            EliteCount = config.EliteCount,
            GenomeLength = config.GenomeLength,
            Selection = config.Selection.ToString(),
            SelectionRatio = config.SelectionRatio,
            TournamentSize = config.TournamentSize,
            Crossover = config.Crossover.ToString(),
            CrossoverRate = config.CrossoverRate,
            Mutation = config.Mutation.ToString(),
            MutationRate = config.MutationRate,
            MaxGenerations = config.Termination.MaxGenerations,
            TargetFitness = config.Termination.TargetFitness,
            StagnationLimit = config.Termination.StagnationLimit
        };
    }

    public GAConfig ToGAConfig()
    {
        return new GAConfig
        {
            PopulationSize = PopulationSize,
            EliteCount = EliteCount,
            GenomeLength = GenomeLength,
            Selection = Enum.TryParse<SelectionStrategy>(Selection, out var sel) ? sel : SelectionStrategy.Tournament,
            SelectionRatio = SelectionRatio,
            TournamentSize = TournamentSize,
            Crossover = Enum.TryParse<CrossoverStrategy>(Crossover, out var cross) ? cross : CrossoverStrategy.SinglePoint,
            CrossoverRate = CrossoverRate,
            Mutation = Enum.TryParse<MutationStrategy>(Mutation, out var mut) ? mut : MutationStrategy.SingleGene,
            MutationRate = MutationRate,
            Termination = new TerminationConfig
            {
                MaxGenerations = MaxGenerations,
                TargetFitness = TargetFitness,
                StagnationLimit = StagnationLimit
            }
        };
    }
}

/// <summary>
/// Data structure for persisting presets and settings to JSON.
/// </summary>
public class PresetStorage
{
    public List<NamedPreset> CustomPresets { get; set; } = new();
    public string LastUsedPreset { get; set; } = "Default";
}

/// <summary>
/// Manages GA presets including built-in defaults and user-defined presets.
/// Persists custom presets to JSON file.
/// </summary>
public class PresetManager
{
    private readonly string _filePath;
    private readonly List<NamedPreset> _presets = new();
    private string _lastUsedPreset = "Default";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Event raised when presets change.
    /// </summary>
    public event Action? PresetsChanged;

    /// <summary>
    /// Gets all available presets (built-in and custom).
    /// </summary>
    public IReadOnlyList<NamedPreset> Presets => _presets;

    /// <summary>
    /// Gets or sets the last used preset name.
    /// </summary>
    public string LastUsedPreset
    {
        get => _lastUsedPreset;
        set
        {
            if (_lastUsedPreset != value)
            {
                _lastUsedPreset = value;
                Save();
            }
        }
    }

    /// <summary>
    /// Gets preset names for UI binding.
    /// </summary>
    public IEnumerable<string> PresetNames
    {
        get
        {
            foreach (var preset in _presets)
                yield return preset.Name;
        }
    }

    /// <summary>
    /// Creates a new PresetManager.
    /// </summary>
    /// <param name="filePath">Path to the JSON file for custom presets.</param>
    public PresetManager(string filePath)
    {
        _filePath = filePath;
        LoadBuiltInPresets();
    }

    private void LoadBuiltInPresets()
    {
        _presets.Add(new NamedPreset
        {
            Name = "Default",
            IsBuiltIn = true,
            Config = GAConfigData.FromGAConfig(GAPresets.Default)
        });

        _presets.Add(new NamedPreset
        {
            Name = "Exploratory",
            IsBuiltIn = true,
            Config = GAConfigData.FromGAConfig(GAPresets.Exploratory)
        });

        _presets.Add(new NamedPreset
        {
            Name = "Exploitative",
            IsBuiltIn = true,
            Config = GAConfigData.FromGAConfig(GAPresets.Exploitative)
        });

        _presets.Add(new NamedPreset
        {
            Name = "LongRun",
            IsBuiltIn = true,
            Config = GAConfigData.FromGAConfig(GAPresets.LongRun)
        });

        _presets.Add(new NamedPreset
        {
            Name = "Fast",
            IsBuiltIn = true,
            Config = GAConfigData.FromGAConfig(GAPresets.Fast)
        });
    }

    /// <summary>
    /// Loads custom presets from the JSON file.
    /// </summary>
    public void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;

            var json = File.ReadAllText(_filePath);
            var storage = JsonSerializer.Deserialize<PresetStorage>(json, JsonOptions);

            if (storage != null)
            {
                // Load last used preset
                if (!string.IsNullOrEmpty(storage.LastUsedPreset))
                {
                    _lastUsedPreset = storage.LastUsedPreset;
                }

                // Load custom presets
                foreach (var preset in storage.CustomPresets)
                {
                    preset.IsBuiltIn = false;
                    // Don't add if name already exists
                    if (!_presets.Exists(p => p.Name == preset.Name))
                    {
                        _presets.Add(preset);
                    }
                }
            }
        }
        catch (Exception)
        {
            // Ignore load errors
        }
    }

    /// <summary>
    /// Saves custom presets and settings to the JSON file.
    /// </summary>
    public void Save()
    {
        try
        {
            var storage = new PresetStorage
            {
                CustomPresets = _presets.FindAll(p => !p.IsBuiltIn),
                LastUsedPreset = _lastUsedPreset
            };
            var json = JsonSerializer.Serialize(storage, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception)
        {
            // Ignore save errors
        }
    }

    /// <summary>
    /// Adds a new preset with the given name and configuration.
    /// </summary>
    /// <param name="name">The name for the preset.</param>
    /// <param name="config">The GA configuration.</param>
    /// <returns>True if added successfully, false if name already exists.</returns>
    public bool AddPreset(string name, GAConfig config)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (_presets.Exists(p => p.Name == name)) return false;

        _presets.Add(new NamedPreset
        {
            Name = name,
            IsBuiltIn = false,
            Config = GAConfigData.FromGAConfig(config)
        });

        Save();
        PresetsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Removes a custom preset by name. Built-in presets cannot be removed.
    /// </summary>
    /// <param name="name">The name of the preset to remove.</param>
    /// <returns>True if removed, false if not found or is built-in.</returns>
    public bool RemovePreset(string name)
    {
        var preset = _presets.Find(p => p.Name == name);
        if (preset == null || preset.IsBuiltIn) return false;

        _presets.Remove(preset);
        Save();
        PresetsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Gets a preset by name.
    /// </summary>
    /// <param name="name">The preset name.</param>
    /// <returns>The preset, or null if not found.</returns>
    public NamedPreset? GetPreset(string name)
    {
        return _presets.Find(p => p.Name == name);
    }

    /// <summary>
    /// Gets the GAConfig for a preset by name.
    /// </summary>
    /// <param name="name">The preset name.</param>
    /// <returns>The GAConfig, or the Default config if not found.</returns>
    public GAConfig GetConfig(string name)
    {
        var preset = GetPreset(name);
        return preset?.Config.ToGAConfig() ?? GAPresets.Default;
    }

    /// <summary>
    /// Gets the index of a preset by name.
    /// </summary>
    /// <param name="name">The preset name.</param>
    /// <returns>The index, or -1 if not found.</returns>
    public int GetIndex(string name)
    {
        return _presets.FindIndex(p => p.Name == name);
    }
}
