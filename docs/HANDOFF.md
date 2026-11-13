# Handoff — Ristretto 3.9.0-alpha.20.5

**14 novembre 2026.** Alpha 20.5 aggiunge [profili dati separati e diagnostica senza GUI](ALPHA20_5_STARTUP_PROFILES.md), conservando [Invio sul primo risultato filtrato](ALPHA20_4_ENTER_INSERT.md). Qualificazione desktop e automatismo restano aperti. La [roadmap R1–R8](ROADMAP.md) è l'unica fonte delle attività future.

## Prossimo incremento: completamento R1.02 e fondamenta R1.03–R1.05

1. R1.01 completata: `ApplicationProfile`, `--data-dir`, `--check-profile`, help/versione; `Program` e `App` condividono il percorso. Database/impostazioni/backup e singola istanza separati e provati senza GUI.
2. R1.02 parziale: mutex/pipe e secondo avvio provati, lock esclusivo aggiunto. Restano conflitti hotkey sul desktop e alias symlink/junction reali. Usare cartelle sintetiche, mai il profilo personale. Corretto il rifiuto di `picker.hotkey` nello store.
3. Seguono fonte unica della versione e provenienza R1.03, rilevamento Python/Node R1.04 e gate espliciti R1.05. Questa estrazione non contiene `.git`; non inventare una revisione sorgente.
4. Non riavviare VS Code automaticamente: il precedente crash resta senza causa confermata. Le prove desktop R2/R3 devono essere distinte dalle build e dai controlli senza GUI.

I dettagli rimangono nella [roadmap](ROADMAP.md): questa lista identifica solo il punto di ripresa.

## Baseline ed evidenze

- Solution e quattro strumenti compilati in Release; **588/588 xUnit**, inclusi **30 nuovi casi profilo** e 50 picker ViewModel, dieci controlli CLI, audit sorgenti/statici e sette Python passati. [Ultimo incremento e limiti](ALPHA20_5_STARTUP_PROFILES.md).
- Picker: [richieste monouso](ALPHA20_3_PICKER_SESSION.md), [indice Unicode](ALPHA20_3_PICKER_SEARCH.md), [caricamenti annullabili](ALPHA20_3_PICKER_LOADING.md), Copia/Inserisci coordinati. Gli ultimi test usano callback sintetici e non inizializzano Avalonia o clipboard reali.
- Precedente [baseline Windows](ALPHA20_3_VALIDATION.md): 31 scenari nativi su EDIT posseduto, smoke cooperante/remoto, 32 Node e contratto .NET→Node passati. Non sono verifiche runtime degli ultimi incrementi GUI.
- [Host VS Code](ALPHA20_3_VSCODE_HOST_TESTS.md): 14 scenari scritti, zero eseguiti; renderer non avviato, crash `Code.exe` segnalato (`0x80000003`). Causa non confermata; opzioni grafiche sperimentali rimosse. `-PrepareOnly` prepara il runner senza aprire VS Code.
- [Stato corrente](CURRENT_STATE.md), [build/test](BUILD_AND_TEST.md), [indice documenti](INDEX.md) e [rapporti](../reports/README.md) separano implementazione, test eseguiti e test pendenti.

## Gate e ambiente

Per modifiche funzionali: audit, build/test pertinenti e validazione della consegna in cartella nuova. I comandi attuali sono in [BUILD_AND_TEST.md](BUILD_AND_TEST.md). Il restore precedente ha usato cache NuGet locale per un errore TLS; audit vulnerabilità online non eseguito. Python/Node possono richiedere avvio diretto finché R1.04 non corregge il rilevamento degli script.

Prove native/UI in sessione desktop dedicata, con dati sintetici e profilo isolato. Per VS Code, verificare prima l'avvio normale e diagnosticare il blocco secondo R3. Non ripetere automaticamente un percorso che termina in crash.

## Confini da preservare

- Quattro assembly di produzione; Core/Application indipendenti da Avalonia e API OS; SQLite locale e JSON versionato.
- Nessuna cancellazione calcolata dalla sola lunghezza UTF-16; verifiche di focus, revisione, IME, range e provenienza secondo il contratto dell'adapter.
- Una sonda esterna o `SendInput` accettato non provano una modifica verificata. Nessun retry dopo un effetto incerto.
- Appunti già scritti non ripristinati alla cieca; nessun log di snippet, digitato o contenuti clipboard.
- L'espansione automatica in altre app è futura: R5 studia le garanzie e R6 è condizionata al suo esito. Il picker rimane il percorso manuale disponibile.
- GUI Preview 9 conservata; i render storici non attestano le modifiche recenti del picker. Redesign generale dopo i flussi fondamentali, verifiche di accessibilità nelle milestone pertinenti.

## Consegna corrente

Consegnare Alpha 20.5 completa, con versione coerente nei cinque progetti espliciti, script, documentazione, rapporti e manifest. Validare da ZIP estratto in una cartella nuova. Le prove desktop filtro → Invio → incolla del primo risultato → picker nascosto restano separate dai test del ViewModel.
