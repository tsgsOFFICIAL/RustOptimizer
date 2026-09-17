using System.Collections.ObjectModel;
using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System.Collections.Generic;
using System.Linq;

namespace RustOptimizer.ViewModels;

/// <summary>
/// One cycle stage in the manual macro builder: one or more <see cref="BindLineEditorRow"/>s that
/// fire together on that stage's turn. Starts with a single empty line; "Add command" appends another
/// line to the same stage (all commands there fire together, per <see cref="KeybindCommandBuilder"/>'s
/// grouping rule).
/// </summary>
public sealed class BindStageEditorRow : ViewModelBase
{
    private readonly IReadOnlyList<ConvarPickerOption> _options;

    public BindStageEditorRow(ILocalizationService localization, IReadOnlyList<ConvarPickerOption> options) : base(localization)
    {
        _options = options;
        Lines = [new BindLineEditorRow(localization, options)];
        Lines.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CanRemoveLine));

        AddLineCommand = new RelayCommand(() => Lines.Add(new BindLineEditorRow(Localization, _options)));
        RemoveLineCommand = new RelayCommand<BindLineEditorRow>(line =>
        {
            if (line is not null && Lines.Count > 1)
                Lines.Remove(line);
        });
    }

    /// <summary>Every command line in this stage, in the order they'll be joined.</summary>
    public ObservableCollection<BindLineEditorRow> Lines { get; }

    /// <summary>Whether a second (or later) line can be removed - a stage always keeps at least one.</summary>
    public bool CanRemoveLine => Lines.Count > 1;

    /// <summary>Appends another command line to this stage.</summary>
    public RelayCommand AddLineCommand { get; }

    /// <summary>Removes a line from this stage, as long as one would still remain.</summary>
    public RelayCommand<BindLineEditorRow> RemoveLineCommand { get; }

    /// <summary>Converts this row to the immutable <see cref="BindStage"/> the builder consumes.</summary>
    public BindStage ToStage() => new(Lines.Select(line => line.ToLine()).ToList());
}