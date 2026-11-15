# Handoff — Ristretto Alpha 20.29

**15 novembre 2026.** Codice e documentazione aggiornati a 3.9.0-alpha.20.29.

## Obiettivo e punto di ripresa

La richiesta più recente è uniformare Registra hotkey al resto del software. **20.29:** classi compact/ristretto-dialog, testo/display 13 DIP, titolo 16, note 11 condivise con il campo hotkey della libreria, campo e pulsanti compatti, istruzioni brevi e spazio riservato allo stato. 80 render offscreen in cinque lingue/due temi/due larghezze/quattro stati; due immagini aggiornate incluse. Nessun nuovo test unitario per i valori di stile; il flusso usa i test esistenti. [Dettagli e anteprime](ALPHA20_29_HOTKEY_STYLE.md).

**Registra…** nella libreria acquisisce i tasti e mostra per esempio `ALT + CTRL + SHIFT + B`. La 20.27 prova il flusso con i controlli Avalonia reali su backend headless e SQLite sintetico: la conferma cambia la bozza, soltanto Salva persiste. Corretti perdita della combinazione con Shift+Tab e pulsanti fuori dal bordo in francese/tedesco. L'utente ha confermato che il richiamo via hotkey della 20.25 funziona in Blocco note Windows.

La 20.28 aggiunge **21 casi nativi** con i worker/coordinatore del prodotto: collisioni fra processi, rilascio dopo Dispose/terminazione del figlio, catalogo e ripristino. L'utente ha nuovamente confermato «tramite hotkey funziona» dopo la 20.27, senza specificare editor/versione/combinazione. GUI e inserimento invariati.

Il prossimo passo è **completare il flusso Desktop.App**: save/import o reload durante la cattura, notifiche in coda, conflitti mostrati nella UI, focus/tastiera fisica e DPI. R2.01 → R2.08 → R2.09 rimane aperto. Le ricevute dei worker non provano tutta l'orchestrazione dell'applicazione.

La [roadmap](ROADMAP.md) è l'unico piano autorevole; include dipendenze e criteri di chiusura di R1–R8. [Stato corrente](CURRENT_STATE.md) e [rapporti](../reports/README.md) distinguono implementazione, esecuzione e conferme dell'utente.

## Baseline da conservare

- Alpha 20.29: gate CI PASS locale e da ZIP nuovo, 870 xUnit/32 Node inclusi i 50 casi della cattura; 80 render offscreen ripetuti nell'estrazione. Verificati 518 hash e sorgenti identici, anteprime incluse uguali byte per byte a quelle rigenerate. La baseline 20.28 conserva 21/21 nativi passati, non rieseguiti per la modifica visuale.
- Restore NuGet da cache/feed locale; due pacchetti headless mancanti scaricati da NuGet con HTTPS verificato. Audit vulnerabilità online non eseguito.
- Schema SQLite 2 dalla 20.25, migrazione da 1, backup legacy migrato solo nello staging, hotkey nel JSON v1 facoltativo.
- Registra…: ai 26 casi della 20.26 si aggiungono 22 UI headless e due di stato della 20.27. Verificati navigazione/azioni, libreria e persistenza, cinque lingue/due temi/due larghezze. La 20.28 prova inoltre il coordinatore con worker Windows reali, separatamente da Desktop.App.
- Native 20.28: 21/21 PASS, Windows 10 build 19045, zero callback osservate. Include 20 cicli di Dispose immediato. FULL non eseguito; soltanto il nuovo comando nativo è attestato.
- R5: motore/bridge e diagnostica disponibili; provenienza esterna non qualificata. Nessun collegamento dall'anteprima all'espansione di prodotto.
- R3: Node e contratto export validati; vero host non qualificato. Crash precedente `Code.exe / 0x80000003` senza causa confermata, nessun riavvio automatico.

[Evidenze della baseline](../reports/ALPHA20_28_CHECKS.md) · [ricevute native](ALPHA20_28_NATIVE_HOTKEY_REGISTRATIONS.md) · [perimetro dell'integrazione UI](ALPHA20_27_CAPTURE_INTEGRATION.md).

## Dettagli utili

- `SnippetHotkeyRegistrationLab` è nel solo laboratorio: avvia un figlio dedicato come proprietario di una combinazione sintetica. Usa gli stessi worker del prodotto e timeout; nessun input, appunti o profilo personale. `validate-snippet-hotkeys.cmd` produce un rapporto nuovo e non sovrascrive evidenze precedenti.
- La prova è inclusa in FULL e validate-native; CORE/CI mantengono le registrazioni native escluse. Non promuovere `native_windows` del CI a PASS sulla base di un solo comando eseguito separatamente.
- `HotkeyCaptureState` gestisce display, ripetizioni, tasti non supportati, rilascio e correzione del gesto.
- `Navigate()` conserva solo un gesto completo già rilasciato, per Tab/Shift+Tab; nuovo tasto principale e reset invalidano la memoria. Due regressioni di stato coprono il confine.
- `HotkeyCaptureWindow` offre Usa combinazione, Annulla e Rimuovi hotkey; Esc/chiusura annullano.
- Le azioni usano un WrapPanel. I test verificano che i pulsanti restino nell'area client a 440/540 DIP; non sono screenshot o prove DPI.
- `HeadlessTestApp` carica stili/risorse del prodotto senza lifetime desktop. `MainWindow.ShowSnippetHotkeyCaptureAsync` è il punto di sostituzione del dialogo nei test; la produzione mantiene il percorso App/coordinatore esistente.
- `Avalonia.Headless.XUnit` è solo nei test e deve restare alla stessa patch di Avalonia (11.3.22).
- `HotkeyCaptureCoordinator` mantiene sospese le notifiche durante pausa, dialogo e ripristino.
- `App.HotkeyCapture` ferma/ricrea i worker senza impostazioni temporanee nel database; usa il picker effettivamente attivo e lo snapshot più recente.
- Le notifiche obsolete del picker e le richieste di inserimento pendenti sono invalidate. Il worker picker gestisce anche la disposizione prima del completamento dell'avvio.
- Schema, JSON e contenuti degli snippet restano quelli della baseline; non ridisegnare il picker durante una correzione del dialogo.

## Lavoro indipendente dalla prova desktop

Se l'ambiente non permette la qualificazione, avanzare su R1 residui, recupero dati/distribuzione R4 e studio R5 in sola lettura. Registrare ciò che manca, senza dichiarare completa R2.

R5 deve confrontare digitazione, paste, IME, programmatiche e contesto con evidenze; WinEvent o un hook da soli non sono un'autorizzazione. R6 dipende da una decisione positiva per un target concreto.

## Regole della documentazione

**Preferenza dell'utente da mantenere:** README con titolo, screenshot, descrizione del progetto, funzionamento, installazione, aggiornamento e riferimenti. Linguaggio chiaro e diretto; storico delle versioni nel changelog, procedure tecniche nei documenti dedicati.

Le immagini main/picker restano anteprime Preview 9 della Alpha 7. La form hotkey ha due render offscreen aggiornati alla 20.29: indicarne la provenienza. Nessun render o mockup va presentato come prova manuale.

Aggiornare insieme README, stato, roadmap, changelog e indice quando cambia il comportamento. Le specifiche e i rapporti delle vecchie Alpha restano storici. I documenti di studio non dimostrano implementazione o qualificazione attuale.

## Verifica e consegna

Per modifiche funzionali: nuova versione coerente, gate pertinenti, rapporto e archivio verificato da cartella nuova. Per soli documenti: mantenere la versione del codice, eseguire gate SOURCE/audit documentali e registrare che i test applicativi non sono stati rieseguiti.

La pipeline di progetto è C#/.NET 10. Python non è richiesto; Node serve a CI/FULL per il ramo VS Code. Non disabilitare TLS o verifiche delle firme per risolvere problemi di restore.

Conservare dati e backup fuori dalle cartelle delle versioni. Un `--data-dir` relativo cambia significato se cambia la directory di lavoro; per riutilizzare un profilo usare un percorso assoluto.

[Installazione e aggiornamento](INSTALLATION_AND_UPDATE.md) · [comandi e perimetri dei gate](BUILD_AND_TEST.md).
