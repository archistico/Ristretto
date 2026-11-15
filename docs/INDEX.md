# Indice documentale — Ristretto Alpha 20.19.3

**Documenti autorevoli:** [stato corrente](CURRENT_STATE.md), [roadmap unica](ROADMAP.md), [istruzioni di build](BUILD_AND_TEST.md), [handoff](HANDOFF.md). I rapporti Alpha sono storici, salvo la [consegna corrente](ALPHA20_19_3_STARTUP_CLI_ENCODING.md). Non usare i risultati di una vecchia Alpha come test della versione nuova.

**Esito controlli corrente:** [report strutturato](../reports/ALPHA20_19_3_VALIDATION_STATUS.json) (vedere gli esiti effettivi e i NOT_RUN).

**Incrementi recenti:** [Alpha 20.19.3 — diagnostica CLI e codepage Windows](ALPHA20_19_3_STARTUP_CLI_ENCODING.md) · [Alpha 20.19.2 — correzione gate PowerShell](ALPHA20_19_2_VERSION_GATE_FIX.md) · [Alpha 20.19.1 — correzione test CRLF su Windows](ALPHA20_19_1_CRLF_REGRESSION_FIX.md) · [Alpha 20.19 — barriere di focus R5](ALPHA20_19_FOCUS_BOUNDARY.md) ·  [Alpha 20.18.1 — warning CS0618 e PASS Notepad](ALPHA20_18_1_CLIPBOARD_CS0618.md) ·  [Alpha 20.18 — diagnosi e procedura in italiano](ALPHA20_18_NOTEPAD_DIAGNOSTICS.md) · [Alpha 20.17 — guida Notepad e stato sessione](ALPHA20_17_NOTEPAD_GUIDED_SESSION.md) ·  [Alpha 20.16 — profilo SQLite sintetico](ALPHA20_16_SYNTHETIC_PROFILE.md) · [Alpha 20.15 — regressione Invio](ALPHA20_15_ENTER_REGRESSION_FIX.md) ·  [Alpha 20.14 — cancellation/dispatch](ALPHA20_14_INSERT_LIFECYCLE.md) · [Alpha 20.13 — preflight prima degli appunti](ALPHA20_13_PRE_CLIPBOARD_GUARD.md) · [Alpha 20.12 — qualifica Notepad sintetico](ALPHA20_12_NOTEPAD_QUALIFICATION.md) · [Alpha 20.11 — ricevute di verifica nel controllo posseduto](ALPHA20_11_PICKER_RECEIPTS.md) · [20.10 — clipboard/identità](ALPHA20_10_CLIPBOARD_IDENTITY.md) · [20.9 — laboratorio multilinea](ALPHA20_9_PICKER_OWNED_EDIT.md) · [20.8 — lifecycle](ALPHA20_8_PICKER_LIFECYCLE.md) · [20.7 — CI](ALPHA20_7_CI.md) · [20.6 — gate](ALPHA20_6_QUALITY_GATES.md).

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

