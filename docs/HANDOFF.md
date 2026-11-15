# Handoff — Alpha 20.19.5

## Hotfix corrente

Nella Alpha 20.19.4 l’utente ha eseguito `validate-source.cmd` e `validate.cmd`: la compilazione del nuovo `Ristretto.DevTools` è fallita con CS9174 nelle righe 173 e 198 di `NotepadQualification.cs`. Nella Alpha 20.19.5 è stata sostituita l’ambigua chiamata `Span<byte>.StartsWith([0xEF, 0xBB, 0xBF])` con `WithoutUtf8Bom(ReadOnlySpan<byte>)` basata su confronto diretto dei byte. Aggiunti tre test C# e una guardia in `SourceAudit`. I due gate devono ancora essere eseguiti su Windows. Nessun cambiamento ai sorgenti dell'app o alla UI.

## Attività precedente

L'utente ha confermato `GATE CORE: PASS` su Alpha 20.19.3 e ha richiesto di eliminare Python dall'architettura degli strumenti, oltre a correggere `xUnit2000`.

Nella Alpha 20.19.4 tutti gli script Python eseguibili sono stati rimossi e le responsabilità sono trasferite a `tools/Ristretto.DevTools` (C#/.NET 10): audit, statici, regressioni, gate, startup CLI, Notepad e packaging. Gli avviatori `.cmd`/`.sh` richiamano `.NET`; Node rimane soltanto per l'adapter VS Code facoltativo. Il warning xUnit2000 è stato corretto in `WindowsExternalAdapterCapabilityTests.cs`.

## Verifiche

Non dichiarare verificati su Windows i nuovi tool C# prima di aver eseguito compilazione e test. La baseline utente precedente è valida per la 20.19.3, non per questa migrazione/hotfix. La 20.19.4 fallisce con CS9174 prima dei test. La GUI, il picker, la hotkey e il database non sono cambiati.

## Prossimi passi

1. Eseguire `scripts\validate-source.cmd`, poi `scripts\validate.cmd` in una estrazione nuova; verificare CS9174 e correggere eventuali altri errori della migrazione.
2. Verificare il gate C# CI e la prova Notepad con file sintetici, senza usare dati personali.
3. Solo dopo riprendere l'espansione automatica Windows, distinguendo gli eventi tastiera dal testo committato.

[Guida operativa](BUILD_AND_TEST.md) · [Stato](CURRENT_STATE.md) · [Roadmap](ROADMAP.md) · [Report hotfix](ALPHA20_19_5_CS9174_FIX.md) · [Report migrazione](ALPHA20_19_4_CSHARP_TOOLS.md)
