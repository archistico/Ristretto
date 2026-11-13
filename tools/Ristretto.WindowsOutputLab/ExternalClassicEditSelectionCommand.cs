using System.Text.Json;
using TextExpander.Infrastructure.Platform.Windows;

namespace Ristretto.WindowsOutputLab;

/// <summary>Opt-in, one-shot, content-free external EDIT selection experiment.</summary>
internal static class ExternalClassicEditSelectionCommand
{
    public static async Task<int> Run(string? reportPath)
    {
        Console.WriteLine("Alpha 11 — esperimento READ-ONLY su un Win32 Edit esterno.");
        Console.WriteLine("Entro cinque secondi seleziona il campo di test nell'altra applicazione.");
        Console.WriteLine("Solo EM_GETSEL con timeout: nessuna lettura del testo, nessun invio o hook.");
        await Task.Delay(TimeSpan.FromSeconds(5));
        var evidence = WindowsClassicEditSelectionProbe.Capture();
        var assessment = WindowsClassicEditSelectionPolicy.Evaluate(evidence);
        Console.WriteLine($"Preflight: {evidence.Preflight?.Status}; selezione: {assessment.Status}.");
        Console.WriteLine("Sostituzione esterna: NON autorizzata, anche in caso di esito osservato.");

        if (reportPath is not null)
        {
            // Never overwrite a report. Does not retain offsets, text or target identifiers.
            using var output = new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write);
            JsonSerializer.Serialize(output, new
            {
                Schema = "ristretto/classic-edit-selection-probe/1",
                TimestampUtc = DateTimeOffset.UtcNow,
                Preflight = evidence.Preflight?.Status.ToString(),
                Status = assessment.Status.ToString(),
                FocusConsistent = evidence.FocusConsistent,
                MessageCompleted = evidence.MessageCompleted,
                CanReadCommittedText = assessment.CanReadCommittedText,
                CanReplaceText = assessment.CanReplaceText,
                ContainsUserText = false,
                ContainsSelectionOffsets = false
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        return assessment.SelectionQueryObserved ? 0 : 3;
    }
}
