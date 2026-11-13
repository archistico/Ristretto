using System.Text.Json;
using TextExpander.Infrastructure.Platform.Windows;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// Single, opt-in, read-only qualification research. There is no path from this
/// command to SendInput, WM_SETTEXT or conditional replacement.
/// </summary>
internal static class ExternalAdapterCapabilityCommand
{
    public static async Task<int> Run(string? reportPath)
    {
        Console.WriteLine("Alpha 17 — matrice delle capacità di un editor esterno (SOLA LETTURA).");
        Console.WriteLine("Entro 5 secondi attiva un controllo di testo non protetto in un'altra applicazione.");
        Console.WriteLine("Non legge il testo, non registra tasti e non invia alcun input.");
        await Task.Delay(TimeSpan.FromSeconds(5));

        // The underlying selection experiment already checks foreground identity and
        // the metadata gate before AND after its bounded EM_GETSEL query.
        var observation = WindowsClassicEditSelectionProbe.Capture();
        var result = WindowsExternalAdapterCapabilityPolicy.Evaluate(observation);
        Console.WriteLine($"Ricognizione: {result.Status}; evidenze: {result.Observed}.");
        Console.WriteLine($"Garanzie per sostituzione ancora assenti: {result.Missing}.");
        Console.WriteLine("Lettura testo committato: NO. Sostituzione: NO. Nessuna eccezione.");

        if (reportPath is not null)
        {
            // CreateNew: never overwrite earlier evidence. Do not persist the original
            // observation, selection offsets, HWND, PID, text, app names or titles.
            using var output = new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write);
            JsonSerializer.Serialize(output, new
            {
                Schema = "ristretto/external-adapter-capabilities/1",
                TimestampUtc = DateTimeOffset.UtcNow,
                ResearchStatus = result.Status.ToString(),
                ObservedEvidence = result.Observed.ToString(),
                MissingGuarantees = result.Missing.ToString(),
                Qualified = false,
                CanReadCommittedText = result.CanReadCommittedText,
                CanReplaceText = result.CanReplaceText,
                ContainsUserText = false,
                ContainsTargetIdentifiers = false,
                ContainsSelectionOffsets = false
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        return result.CandidateForFurtherResearch ? 0 : 3;
    }
}
