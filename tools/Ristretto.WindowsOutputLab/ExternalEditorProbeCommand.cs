using System.Text.Json;
using TextExpander.Infrastructure.Platform.Windows;

namespace Ristretto.WindowsOutputLab;

/// <summary>Explicit one-shot observation only: no global observer and no target modification.</summary>
internal static class ExternalEditorProbeCommand
{
    public static async Task<int> Run(string? reportPath)
    {
        Console.WriteLine("Alpha 10 — ricognizione SOLA LETTURA di un editor Windows esterno.");
        Console.WriteLine("Porta in primo piano un editor non protetto entro 5 secondi.");
        Console.WriteLine("Non vengono letti testo, titoli, nomi di processi, clipboard o tasti.");
        await Task.Delay(TimeSpan.FromSeconds(5));
        var observation = WindowsExternalEditorProbeReader.Capture();
        var assessment = WindowsExternalEditorProbePolicy.Evaluate(observation);
        Console.WriteLine($"Famiglia controllo: {observation.Family}; valutazione: {assessment.Status}.");
        Console.WriteLine("Possibilita' di sostituzione: NO (qualsiasi risultato). Nessun input inviato.");
        if (reportPath is not null)
        {
            // CreateNew: never overwrite previous evidence. No personal text, HWND, PID,
            // raw class name, target process name, title, caret or clipboard are written.
            using var output = new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write);
            JsonSerializer.Serialize(output, new
            {
                Schema = "ristretto/external-editor-read-only-probe/1",
                TimestampUtc = DateTimeOffset.UtcNow,
                Family = observation.Family.ToString(),
                Status = assessment.Status.ToString(),
                CandidateForFurtherResearch = assessment.CandidateForFurtherResearch,
                Qualified = false,
                CanReplaceText = assessment.CanReplaceText,
                CanReadCommittedText = assessment.CanReadCommittedText,
                ContainsUserText = false,
                ObservationType = "OneShotWindowMetadataOnly"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        return assessment.Status is ExternalEditorProbeStatus.MissingObservation or
            ExternalEditorProbeStatus.ForegroundUnavailable or ExternalEditorProbeStatus.FocusUnavailable ? 3 : 0;
    }
}
