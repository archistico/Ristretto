using TextExpander.Application.Features.Input;
using TextExpander.Infrastructure.Platform.Windows;

namespace Ristretto.WindowsOutputLab;

/// <summary>
/// Prova esplicita, in SOLA LETTURA, su un campo Edit classico ESTERNO.
/// Nessun testo, selezione, PID o HWND viene stampato, tracciato o salvato.
/// </summary>
internal static class ExternalClassicEditTextCommand
{
    public static async Task<int> Run()
    {
        Console.WriteLine("Ristretto R5 — osservazione esplicita Unicode in un Win32 Edit esterno.");
        Console.WriteLine("ATTENZIONE: legge TEMPORANEAMENTE in memoria fino a 4096 unità UTF-16 del campo attivo.");
        Console.WriteLine("Usa esclusivamente un documento DI PROVA, senza password o dati personali.");
        Console.WriteLine("Non registra contenuto, non invia tasti, non modifica testo e non abilita espansioni.");
        Console.WriteLine("Tra 5 secondi posiziona il cursore in un controllo Edit classico esterno.");
        await Task.Delay(TimeSpan.FromSeconds(5));
        var before = WindowsExternalClassicEditTextReader.Capture();
        if (!before.IsObserved)
        {
            Console.WriteLine($"NOT_RUN: primo snapshot non qualificato ({before.Status}).");
            Console.WriteLine("Nota: Notepad moderno può usare un controllo non Edit e quindi essere escluso.");
            return 3;
        }

        Console.WriteLine("Per i prossimi 5 secondi aggiungi testo Unicode nel campo di prova, senza cambiare finestra.");
        await Task.Delay(TimeSpan.FromSeconds(5));
        var after = WindowsExternalClassicEditTextReader.Capture();
        if (!after.IsObserved)
        {
            Console.WriteLine($"NOT_RUN: secondo snapshot non valido ({after.Status}).");
            return 3;
        }

        var result = ExternalClassicEditTextDeltaPolicy.Evaluate(before.Document, after.Document,
            before.Target == after.Target);
        Console.WriteLine($"Esito: {result.Status}.");
        Console.WriteLine("Provenienza umana: NON dimostrata. Sostituzione automatica esterna: NON autorizzata.");
        return result.CandidateObserved ? 0 : 3;
    }
}
