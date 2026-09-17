using System.Collections.Generic;
using RustOptimizer.Interface;
using System.Linq;

namespace RustOptimizer.Service;

/// <summary>
/// Assembles a manually-built macro's <see cref="BindStage"/>s into the exact opaque command string
/// keys.cfg would store, following the cycling rules confirmed against this app's ground-truth
/// reference keys.cfg (see <see cref="BindMacroExamples"/>): a single stage never cycles at all (its
/// lines just run together), two or more single-line stages cycle with the plain <c>~a;b;c</c> form,
/// and only a stage with 2+ lines needs the <c>meta.exec</c> grouping form - which wraps each line in
/// its own quotes, so any quote already inside the line's own text (e.g. a <c>showtoast</c> message)
/// must be escaped first or the result nests unescaped quotes and breaks, exactly like the reference
/// file's own <c>showtoast 3 \"FOV: 70\"</c> shows. This is the one place that assembles the final
/// string - every other part of the builder treats <see cref="BindCommandLine.RawText"/> as
/// already-correct opaque text, never re-derived from <see cref="BindCommandLine.ConvarKey"/> here.
/// </summary>
internal static class KeybindCommandBuilder
{
    /// <summary>Builds the command string for <paramref name="stages"/>, or "" if every line is blank.</summary>
    public static string Build(IReadOnlyList<BindStage> stages)
    {
        List<List<string>> stageLines = stages
            .Select(stage => stage.Lines.Select(line => line.RawText.Trim()).Where(text => text.Length > 0).ToList())
            .Where(lines => lines.Count > 0)
            .ToList();

        if (stageLines.Count == 0)
            return "";

        if (stageLines.Count == 1)
            return string.Join(";", stageLines[0]);

        bool needsGrouping = stageLines.Any(lines => lines.Count > 1);
        if (!needsGrouping)
            return "~" + string.Join(";", stageLines.Select(lines => lines[0]));

        return "~" + string.Join(";", stageLines.Select(lines =>
            "meta.exec " + string.Join(" ", lines.Select(text => $"\"{text.Replace("\"", "\\\"")}\""))));
    }
}