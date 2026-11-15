# Indice documentale — Ristretto Alpha 20.25

**Ultimo incremento:** [hotkey per snippet e correzione CS0246](ALPHA20_25_SNIPPET_HOTKEYS.md) · [verifiche 20.25](../reports/ALPHA20_25_CHECKS.md). Suite a 820 xUnit passati; nuove hotkey sul desktop ancora da qualificare. I rapporti seguenti conservano gli esiti storici.

**Fonti correnti:** [Anteprima trigger Alpha 20.24](ALPHA20_24_TRIGGER_PREVIEW.md) · [Verifiche 20.24](../reports/ALPHA20_24_CHECKS.md) · [Osservazione WinEvent 20.23](ALPHA20_23_LIVE_EDIT_OBSERVER.md) · [Verifiche 20.23](../reports/ALPHA20_23_CHECKS.md) ·  [Correzioni e interazione 20.22](ALPHA20_22_PICKER_EDITOR_AND_CARET.md) · [Verifiche 20.22](../reports/ALPHA20_22_CHECKS.md) · [Osservazione esterna 20.21](ALPHA20_21_EXTERNAL_EDIT_OBSERVATION.md) · [Verifiche 20.21](../reports/ALPHA20_21_CHECKS.md) · [Alpha 20.20 — Bridge Unicode](ALPHA20_20_OWNED_INPUT_BRIDGE.md) · [Verifiche 20.20](../reports/ALPHA20_20_CHECKS.md) · [Stato](CURRENT_STATE.md) · [Roadmap unica](ROADMAP.md) · [Compilazione e test in italiano](BUILD_AND_TEST.md) · [Handoff](HANDOFF.md) · [Hotfix CS9174](ALPHA20_19_5_CS9174_FIX.md) · [Strumenti C#](ALPHA20_19_4_CSHARP_TOOLS.md).

La Alpha 20.20 ha superato compilazione e test sul PC dell'utente. Nella 20.21 è fallito un test per un offset UTF-16 sbagliato nella fixture. La 20.22 corregge il test e aggiorna il comportamento di Invio e del campo editor; la nuova build e il rendering Windows devono ancora essere verificati. I rapporti delle Alpha precedenti rappresentano solo lo **storico**: possono menzionare script Python non più presenti nella versione attuale.

## Punti di ingresso (fonti canoniche)

- [README](../README.md): uso e requisiti in breve.
- [Stato corrente](CURRENT_STATE.md): che cosa è operativo, sperimentale o non verificato.
- [Roadmap unica R1–R8](ROADMAP.md): attività numerate, dipendenze, consegne e criteri di uscita; [quadro F0–F6](FUNCTIONAL_ROADMAP.md) solo come indice.
- [Build e test](BUILD_AND_TEST.md): comandi completi Windows/Linux/macOS, laboratori, adapter VS Code.
- [Handoff](HANDOFF.md): passaggio alla prossima iterazione.
- [Profili dati Alpha 20.5](ALPHA20_5_STARTUP_PROFILES.md): directory esplicita, controllo senza GUI, istanze separate e persistenza hotkey.
- [Invio nel picker Alpha 20.4](ALPHA20_4_ENTER_INSERT.md): primo risultato filtrato, inserimento e chiusura del picker.
- [Validazione Alpha 20.3](ALPHA20_3_VALIDATION.md): correzioni e risultati Windows correnti.
- [Ciclo di vita Inserisci](ALPHA20_3_PICKER_SESSION.md): richieste monouso, annullamento e 484 test .NET.
- [Ricerca picker](ALPHA20_3_PICKER_SEARCH.md): indice riutilizzabile, confini Unicode e 506 test .NET.
- [Caricamento picker](ALPHA20_3_PICKER_LOADING.md): worker, annullamento e risultati correnti, 528 test .NET.
- [Comandi appunti picker](ALPHA20_3_PICKER_CLIPBOARD.md): Copia/Inserisci coordinati, sessioni e 550 test .NET.
- [Suite host VS Code](ALPHA20_3_VSCODE_HOST_TESTS.md): gate isolato di 14 scenari, con stato di esecuzione distinto dai test Node.
- [Hotfix CS0117 Alpha 20.2](ALPHA20_2_CS0117_FIX.md), [audit precedente Alpha 20.1](ALPHA20_1_CODE_AUDIT.md), [changelog](CHANGELOG.md), [rapporti](../reports/README.md).

## Architettura e dati

[Architettura](ARCHITECTURE.md) · [Vertical Slice](VERTICAL_SLICES.md) · [Modello dominio](DOMAIN_MODEL.md) · [Contratti](DOMAIN_CONTRACTS.md) · [SQLite](SQLITE_STORAGE.md) · [Portabilità JSON](JSON_PORTABILITY.md) · [Import/export e backup](DATA_IMPORT_BACKUP.md) · [Import Beeftext/Espanso](IMPORT_BEEFTEXT_ESPANSO.md) · [Sicurezza e privacy](SECURITY_PRIVACY.md) · [Unicode/IME](UNICODE_IME.md).

## Interfaccia e picker

[GUI corrente](GUI.md) · [Design system](DESIGN_SYSTEM.md) · [Verifiche visuali](GUI_VISUAL_ACCEPTANCE.md) · [Anteprime](../design/README.md) · [Hotkey globale Windows Alpha 12](ALPHA12_PICKER_HOTKEY.md) · [Inserisci picker Windows Alpha 20.3](ALPHA20_3_PICKER_INSERT.md).

## Riconoscimento, output e adapter

- [F1a](F1_RECOGNITION_ENGINE.md) e [F1b](F1B_ACTIVATION_POLICY.md): riconoscimento, confini e delimitatori.
- [F2a](F2A_WINDOWS_CONTROLLED_LAB.md) · [F2b](F2B_CONDITIONAL_REPLACEMENT.md) · [EDIT posseduto](F2B_WINDOWS_OWNED_EDIT.md) · [Ammissione F3](F3_ADMISSION_GATE.md).
- [Alpha 9 cooperazione tra processi](ALPHA9_COOPERATING_PROCESS.md) · [Alpha 10 sonda esterna](ALPHA10_EXTERNAL_EDITOR_PROBE.md) · [Alpha 11 selezione read-only](ALPHA11_CLASSIC_EDIT_SELECTION.md).
- [Alpha 13 delta committato](ALPHA13_COMMITTED_INPUT.md) · [Alpha 14 provenienza](ALPHA14_MUTATION_PROVENANCE.md) · [Alpha 15 continuità](ALPHA15_EDIT_CONTINUITY.md) · [Alpha 16 event pump](ALPHA16_EVENT_PUMP.md).
- [Alpha 17 matrice capacità](ALPHA17_EXTERNAL_ADAPTER_CAPABILITIES.md) · [Alpha 18 editor posseduto remoto](ALPHA18_REMOTE_OWNED_EDIT.md).
- [Alpha 19 VS Code](ALPHA19_VSCODE_ADAPTER.md) · [Alpha 20 contratto VS Code](ALPHA20_VSCODE_CONTRACT.md).

## Test, governance e ricerca

[Test strategy](TEST_STRATEGY.md) · [Benchmark Core](CORE_BENCHMARK.md) · [Scenari nativi](NATIVE_INPUT_OUTPUT.md) · [Matrice test manuali](ALPHA8_MANUAL_TEST_MATRIX.md) · [Decisioni](DECISIONS.md) · [ADR](adr/ADR-0001-VERTICAL-SLICE-BOUNDARIES.md) · [Matrici di dati](../data/README.md) · [Ricerca concorrenti](../research/COMPETITOR_INDEX.md).

Le specifiche delle singole Alpha, i test plan e le ADR sono conservati per tracciabilità; non vanno interpretati come stato attuale o come attestazione che le prove successive siano state eseguite.

