# Documentazione — Ristretto Alpha 20.29

**Aggiornamento:** 15 novembre 2026. Versione dell'applicazione 3.9.0-alpha.20.29.

## Da dove iniziare

| Esigenza | Documento |
| --- | --- |
| Capire il progetto e usarlo | [README](../README.md) |
| Installare, aggiornare e recuperare i dati | [Installazione e aggiornamento](INSTALLATION_AND_UPDATE.md) |
| Sapere cosa funziona e cosa è ancora aperto | [Stato corrente](CURRENT_STATE.md) |
| Vedere i prossimi passi e i criteri di chiusura | [Roadmap unica R1–R8](ROADMAP.md) |
| Leggere le modifiche già realizzate | [Changelog corrente](../CHANGELOG.md) |
| Compilare ed eseguire i gate | [Build e test](BUILD_AND_TEST.md) |
| Riprendere lo sviluppo | [Handoff](HANDOFF.md) |
| Verificare le evidenze | [Rapporti](../reports/README.md) |

La 20.28 aggiunge 21 casi con registrazioni native Windows e collisioni fra processi: [rapporto](../reports/ALPHA20_28_CHECKS.md). Il dialogo resta coperto da integrazione headless; l'orchestrazione completa sul desktop è ancora da qualificare. Il richiamo tramite hotkey è stato nuovamente confermato dall'utente.

La 20.29 uniforma la form hotkey allo stile compatto di libreria/picker; include [anteprime aggiornate e metriche](ALPHA20_29_HOTKEY_STYLE.md) e 80 render offscreen. [Verifiche attuali](../reports/ALPHA20_29_CHECKS.md).

## Uso, dati e interfaccia

- [Stile condiviso della form hotkey, Alpha 20.29](ALPHA20_29_HOTKEY_STYLE.md)
- [Registrazioni Windows e conflitti, Alpha 20.28](ALPHA20_28_NATIVE_HOTKEY_REGISTRATIONS.md)
- [Prove UI e correzioni della cattura, Alpha 20.27](ALPHA20_27_CAPTURE_INTEGRATION.md)
- [Registra hotkey, Alpha 20.26](ALPHA20_26_HOTKEY_CAPTURE.md)
- [Hotkey per snippet, Alpha 20.25](ALPHA20_25_SNIPPET_HOTKEYS.md)
- [Matrice di qualificazione Windows](WINDOWS_QUALIFICATION_PLAN.md)
- [Picker e area editor, Alpha 20.22](ALPHA20_22_PICKER_EDITOR_AND_CARET.md)
- [Profili separati](ALPHA20_5_STARTUP_PROFILES.md)
- [SQLite](SQLITE_STORAGE.md) · [JSON](JSON_PORTABILITY.md) · [import e backup](DATA_IMPORT_BACKUP.md) · [importatori esterni](IMPORT_BEEFTEXT_ESPANSO.md)
- [GUI](GUI.md) · [design system](DESIGN_SYSTEM.md) · [verifiche visuali](GUI_VISUAL_ACCEPTANCE.md)
- [Anteprime Preview 9, Alpha 7](../design/README.md): immagini storiche da aggiornare dopo la qualificazione

## Architettura e riconoscimento

- [Architettura](ARCHITECTURE.md) · [vertical slice](VERTICAL_SLICES.md)
- [Dominio](DOMAIN_MODEL.md) · [contratti](DOMAIN_CONTRACTS.md) · [ciclo di espansione](EXPANSION_LIFECYCLE.md)
- [Unicode e IME](UNICODE_IME.md) · [riconoscimento F1](F1_RECOGNITION_ENGINE.md) · [attivazione](F1B_ACTIVATION_POLICY.md)
- [Sostituzione condizionale](F2B_CONDITIONAL_REPLACEMENT.md) · [laboratorio EDIT posseduto](F2B_WINDOWS_OWNED_EDIT.md) · [gate di ammissione](F3_ADMISSION_GATE.md)

Gli studi e i contratti definiscono requisiti; la loro presenza non attesta che ogni adapter esterno sia implementato o qualificato. Il quadro [F0–F6](FUNCTIONAL_ROADMAP.md) è una mappa tecnica, non una seconda roadmap.

## Esperimenti e adapter

- [R5: snapshot esterni 20.21](ALPHA20_21_EXTERNAL_EDIT_OBSERVATION.md)
- [R5: osservazione WinEvent 20.23](ALPHA20_23_LIVE_EDIT_OBSERVER.md)
- [R5: anteprima trigger 20.24](ALPHA20_24_TRIGGER_PREVIEW.md)
- [Bridge committato nel laboratorio 20.20](ALPHA20_20_OWNED_INPUT_BRIDGE.md)
- [Capacità adapter esterni](ALPHA17_EXTERNAL_ADAPTER_CAPABILITIES.md)
- [VS Code manuale](ALPHA19_VSCODE_ADAPTER.md) · [contratto export](ALPHA20_VSCODE_CONTRACT.md) · [vero host e blocco di avvio](ALPHA20_3_VSCODE_HOST_TESTS.md)

Il ramo VS Code è sospeso. Gli esperimenti R5 non modificano i documenti e non autorizzano espansioni automatiche.

## Qualità, rilascio e storico

- [Strategia di test](TEST_STRATEGY.md) · [scenari nativi](NATIVE_INPUT_OUTPUT.md)
- [Benchmark Core](CORE_BENCHMARK.md) · [piano prestazioni](PERFORMANCE_TEST_PLAN.md)
- [Sicurezza e privacy](SECURITY_PRIVACY.md)
- [Scelta della licenza](OPEN_SOURCE_LICENSE_DECISION.md) · [decisioni](DECISIONS.md) · [ADR](adr/ADR-0001-VERTICAL-SLICE-BOUNDARIES.md)
- [Dati di prova](../data/README.md) · [ricerca concorrenti](../research/COMPETITOR_INDEX.md)
- [Changelog storico](CHANGELOG.md)

Le pagine delle singole Alpha conservano gli stati alla loro data: possono riportare un NOT_RUN poi superato o descrivere la pipeline Python rimossa. Per la situazione attuale usare Stato corrente, roadmap e rapporti con versione esplicita.

