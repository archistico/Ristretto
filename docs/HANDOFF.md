# Handoff — Ristretto Alpha 20.20

## Baseline confermata

L'utente ha confermato **Alpha 20.19.5: compilazione e test superati**. La precedente migrazione degli strumenti Python a C# è quindi validata nell'ambiente Windows dell'utente. L'inserimento dal picker in Notepad è stato verificato con documento Unicode sintetico.

## Modifiche in Alpha 20.20

- Aggiunto `src/TextExpander.Application/Features/Input/OwnedCommittedInputBridge.cs`: racchiude il collegamento tra `CommittedEditDeltaVerifier`, `OwnedEditProvenancePolicy` e `TriggerActivationSession`. Converte in barriera gli input non verificati e richiede una baseline dopo la perdita del contesto.
- Integrato `tools/Ristretto.WindowsOutputLab/OwnedEditRecognitionSession.cs` con il nuovo bridge: il laboratorio mantiene l'algoritmo di sostituzione condizionale, ma non duplica più le verifiche del testo committato.
- Aggiunti `tests/TextExpander.Tests/OwnedCommittedInputBridgeTests.cs`: test Unicode, provenienza, cambi focus/target, revisione, IME, campi protetti e riservatezza degli esiti.
- Nessuna modifica a GUI, picker, hotkey, schema SQLite o adapter VS Code. Nessuno script Python.

## Verifiche aperte

**Non dichiarare PASS per Alpha 20.20 senza compilazione e test .NET**: questo ambiente di preparazione non dispone dell'SDK 10. Il comportamento nativo del laboratorio deve essere verificato in Windows. Tutte le verifiche dell'Alpha 20.19.5 rimangono evidenza storica, non sostituiscono la validazione del nuovo bridge.

## Prossimi passi

1. Windows: `scripts\validate-source.cmd` e `scripts\validate.cmd` per compilare e validare la nuova versione.
2. Sessione interattiva Windows: `scripts\validate-native.cmd` (laboratorio EDIT posseduto, non documenti personali).
3. Se entrambi passano, lavorare su una sorgente qualificata di testo committato per editor Windows esterni, senza introdurre hook globali prematuramente.
4. Mantenere aggiornati [CURRENT_STATE](CURRENT_STATE.md), [ROADMAP](ROADMAP.md), [BUILD_AND_TEST](BUILD_AND_TEST.md) e [CHANGELOG](../CHANGELOG.md); lo storico delle Alpha resta consultabile.

[Relazione tecnica](ALPHA20_20_OWNED_INPUT_BRIDGE.md)
