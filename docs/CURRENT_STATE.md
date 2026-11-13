# Stato corrente — Ristretto 3.9.0-alpha.20.5

**Data:** 14 novembre 2026 · **GUI:** Preview 9 congelata, escluso il pulsante Inserisci · **Roadmap autorevole:** [ROADMAP.md](ROADMAP.md).

**Piano:** resta valida la roadmap R1–R8. Alpha 20.5 completa **R1.01** e la parte senza GUI di R1.02: dati, impostazioni, backup, mutex e pipe separati. Restano conflitti hotkey reali/alias e le fondamenta R1.03–R1.07.

## Baseline verificata

- **Alpha 20.5 — Profili:** `--data-dir`, controllo senza GUI e salvataggio hotkey corretto. Build Release di solution/quattro tool e **588/588 xUnit**, inclusi 30 nuovi casi; dieci controlli CLI, audit e sette Python passati. [Evidenze correnti](ALPHA20_5_STARTUP_PROFILES.md).
- **Alpha 20.4 — Invio:** inserimento del primo risultato filtrato tramite il percorso Inserisci esistente. Build Release di solution/quattro tool e **558/558 xUnit**, inclusi 50 ViewModel, passati; audit e sette Python passati. Routing tastiera/clipboard desktop non eseguiti. [Evidenze correnti](ALPHA20_4_ENTER_INSERT.md).
- **Incremento comandi appunti:** Copia e Inserisci del picker non si sovrappongono; completamenti di copie vecchie non aggiornano la nuova sessione. Build Release di solution/quattro tool e **550/550 xUnit** passati, inclusi 42 ViewModel; audit e sette Python passati. Nessun avvio grafico o clipboard reale. [Evidenze correnti](ALPHA20_3_PICKER_CLIPBOARD.md).
- **Incremento caricamento picker:** indice su worker, annullamento e protezione dai risultati/errori precedenti. Build Release di solution/quattro tool e **528/528 xUnit** passati, inclusi 20 test del ViewModel reale senza backend Avalonia; audit e sette Python passati. Nessun avvio grafico. [Evidenze correnti](ALPHA20_3_PICKER_LOADING.md).
- **Incremento ricerca picker:** indice riutilizzabile, prefissi limitati a grafemi interi e query Unicode incomplete gestite. Build Release di solution/quattro strumenti e **506/506 xUnit** passati (30 ricerca), audit e sette Python passati; nessun avvio grafico. [Evidenze correnti](ALPHA20_3_PICKER_SEARCH.md).
- **Incremento ciclo di vita picker:** build Release di solution e quattro tool e **484/484 xUnit**, inclusi 36 casi picker, superati. Richieste monouso, annullamento delle sessioni precedenti ed errori nativi gestiti; controlli statici e sette Python passati. Nessuna prova grafica eseguita. [Evidenze correnti](ALPHA20_3_PICKER_SESSION.md).
- **Alpha 20.2:** build e test Windows superati (conferma dell'utente). In questa sede non sono stati rieseguiti.
- **Alpha 20.3:** build Release di solution e quattro tool, **466/466 xUnit**, **31/31 scenari EDIT posseduto**, smoke cooperante e remoto superati su Windows; **32/32 Node** e contratto .NET→Node superati. Restore dalla cache NuGet locale; audit vulnerabilità online non eseguito. [Correzioni ed evidenze](ALPHA20_3_VALIDATION.md).
- **Incremento suite host VS Code:** aggiunti runner isolato e 14 scenari sul vero editor. Sintassi e 32 test Node verificati; l'avvio di VS Code 1.127.0 fallisce nel renderer (codice 49), quindi **nessuno scenario host eseguito**. [Istruzioni ed evidenze](ALPHA20_3_VSCODE_HOST_TESTS.md).
- I report storici Alpha 7 restano conservati. Le prove Alpha 8–20 non devono essere considerate validate soltanto sulla base dei report Alpha 7.

## Capacità attuali

| Area | Stato reale |
| --- | --- |
| Libreria, gruppi, SQLite, import/export, backup | Disponibili |
| Profili dati separati | `--data-dir` condiviso da storage e singola istanza; `--check-profile` senza GUI; conflitti hotkey e alias reali ancora da qualificare |
| Picker / ricerca e Copia | Disponibili; Invio inserisce il primo risultato filtrato, Copia è sul pulsante; caricamento e appunti coordinati |
| Hotkey Windows `Ctrl+Shift+Space`, configurabile | Implementata, richiamo anche dalla tray; prova runtime specifica consigliata |
| **Picker Inserisci** | Richiesta monouso e annullabile; 36 test di contratto/layout/ciclo di vita passati. Incolla effettivo e integrazione UI ancora da provare in editor esterni |
| Espansione automatica digitando il trigger in altre app | **NON implementata** |
| Motore riconoscimento Unicode | Implementato e coperto da test del Core |
| Win32 laboratori nativi controllati | Esperimenti isolati; non autorizzano editing globale |
| VS Code adapter | Comando esplicito da export JSON; 32 test Node passati. Suite host aggiunta, esecuzione bloccata dall'avvio del renderer |
| Linux/macOS insert/hotkey globale | Non implementati |

## Inserisci: confini

La finestra/campo esterni vengono identificati all'apertura del picker. **Inserisci** non si abilita se Ristretto stesso è in primo piano, il campo è sconosciuto oppure protetto/sola lettura secondo le informazioni Win32 osservabili. Dopo il clic il picker si nasconde; il servizio tenta di riattivare il destinatario e controlla nuovamente finestra, focus, modificatori e sequenza degli appunti prima di inviare il solo comando Incolla. Gli appunti rimangono contenenti lo snippet. Un comando inviato non è prova che l'editor abbia accettato il testo. Nessuna cancellazione del trigger preesistente, nessun retry dopo esito incerto.

Per editor complessi (browser, Office, editor elevati) il controllo del campo protetto non è esaustivo; non usare Inserisci per campi password o altri contesti sensibili. Leggere [specifica e matrice di prove](ALPHA20_3_PICKER_INSERT.md).

## Prossimo gate

1. **R1:** completare le prove rimaste di isolamento R1.02 e proseguire con versione unica, rilevamento tool e gate R1.03–R1.05. Conservare la baseline di 588 xUnit e il relativo [rapporto](ALPHA20_5_STARTUP_PROFILES.md).
2. **R2–R3:** qualificare UI/hotkey/clipboard/Inserisci sul desktop e, separatamente, diagnosticare l'avvio di VS Code prima del vero host. Nessun test grafico viene eseguito dalla sola ripianificazione.
3. **R4–R6:** consegnare una alpha Windows manuale qualificata, poi studiare e implementare il primo automatismo solo su un target che soddisfi le garanzie richieste. Dettagli e dipendenze nella [roadmap](ROADMAP.md).

La documentazione tecnica precedente è indicizzata in [INDEX.md](INDEX.md); l'errore CS0117 corretto nella Alpha 20.2 è documentato in [ALPHA20_2_CS0117_FIX.md](ALPHA20_2_CS0117_FIX.md).
