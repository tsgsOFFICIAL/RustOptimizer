using System.Collections.ObjectModel;
using RustOptimizer.ViewModels.Mvvm;
using RustOptimizer.Interface;
using System.Collections.Generic;
using RustOptimizer.Service;
using System.Linq;
using System;

namespace RustOptimizer.ViewModels;

/// <summary>
/// Backs the manual macro builder: a list of cycle stages, each with one or more command lines built
/// from the curated <see cref="ConvarEditorCatalog"/> (a slider/toggle/text control per convar) or
/// typed as a raw custom command. "Add cycle stage" appends another stage - reaching 2+ stages is what
/// turns the result into a <c>~...</c> cycling bind at all. <see cref="PreviewText"/> shows the exact
/// string <see cref="KeybindCommandBuilder"/> would write, updating live as any control changes.
/// <see cref="CloseRequested"/> carries the built command string, or <see langword="null"/> on cancel.
/// </summary>
public sealed class BindBuilderDialogViewModel : ViewModelBase
{
    private readonly IReadOnlyList<ConvarPickerOption> _options;

    public BindBuilderDialogViewModel(ILocalizationService localization) : base(localization)
    {
        _options =
        [
            new ConvarPickerOption(null, Localization["BindBuilderCustomCommandOption"]),
            .. ConvarEditorCatalog.All.Select(entry => new ConvarPickerOption(entry, Localization[entry.LabelKey])),
        ];

        Stages = [new BindStageEditorRow(localization, _options)];
        Stages.CollectionChanged += (_, _) => RaisePreviewChanged();
        HookStage(Stages[0]);

        AddStageCommand = new RelayCommand(() =>
        {
            BindStageEditorRow stage = new(Localization, _options);
            HookStage(stage);
            Stages.Add(stage);
        });
        RemoveStageCommand = new RelayCommand<BindStageEditorRow>(stage =>
        {
            if (stage is not null && Stages.Count > 1)
                Stages.Remove(stage);
        });
        SaveCommand = new RelayCommand(() =>
        {
            string command = KeybindCommandBuilder.Build(Stages.Select(stage => stage.ToStage()).ToList());
            if (!string.IsNullOrWhiteSpace(command))
                CloseRequested?.Invoke(command);
        });
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(null));
    }

    /// <summary>Every cycle stage currently in the builder - a single stage just runs its lines together with no cycling.</summary>
    public ObservableCollection<BindStageEditorRow> Stages { get; }

    /// <summary>Whether a stage can be removed - the builder always keeps at least one.</summary>
    public bool CanRemoveStage => Stages.Count > 1;

    /// <summary>The exact command string that would be written if saved right now.</summary>
    public string PreviewText => KeybindCommandBuilder.Build(Stages.Select(stage => stage.ToStage()).ToList());

    /// <summary>Whether there's anything meaningful to save - at least one line has non-blank text.</summary>
    public bool CanSave => !string.IsNullOrWhiteSpace(PreviewText);

    /// <summary>Appends another cycle stage.</summary>
    public RelayCommand AddStageCommand { get; }

    /// <summary>Removes a stage, as long as one would still remain.</summary>
    public RelayCommand<BindStageEditorRow> RemoveStageCommand { get; }

    /// <summary>Builds the final command from every stage/line and closes with it.</summary>
    public RelayCommand SaveCommand { get; }

    /// <summary>Closes without building anything.</summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>Raised when Save produces a command, or Cancel is pressed with <see langword="null"/>.</summary>
    public event Action<string?>? CloseRequested;

    /// <summary>Re-evaluates <see cref="PreviewText"/>/<see cref="CanSave"/> whenever anything in a stage's lines changes, including lines added to it later.</summary>
    private void HookStage(BindStageEditorRow stage)
    {
        foreach (BindLineEditorRow line in stage.Lines)
            HookLine(line);

        stage.Lines.CollectionChanged += (_, e) =>
        {
            if (e.NewItems is not null)
                foreach (BindLineEditorRow line in e.NewItems)
                    HookLine(line);

            RaisePreviewChanged();
        };
    }

    private void HookLine(BindLineEditorRow line) => line.PropertyChanged += (_, _) => RaisePreviewChanged();

    private void RaisePreviewChanged()
    {
        OnPropertyChanged(nameof(PreviewText));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanRemoveStage));
    }
}