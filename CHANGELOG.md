## 3.9.0-alpha.20.28 — Verifica delle registrazioni Windows

- Nuovo laboratorio e comando `validate-snippet-hotkeys.cmd`: 21 casi con i worker del prodotto e registrazioni Windows reali, senza inviare input o usare appunti/profili personali.
- Verificati conflitti fra processi, rilascio dopo uscita/terminazione del figlio, disabilitazione/rimozione/cambio delle assegnazioni, preparazione/annullamento del picker e coordinatore di cattura con ripristino.
- Rapporto JSON per caso, timeout e rifiuto di sovrascrivere evidenze precedenti. Nuova prova inclusa in FULL e `validate-native.cmd`; CORE/CI mantengono il perimetro senza registrazioni.
- Registrata la nuova conferma utente del richiamo tramite hotkey dopo la 20.27. GUI, picker, inserimento e dati invariati. [Dettagli](docs/ALPHA20_28_NATIVE_HOTKEY_REGISTRATIONS.md) · [verifiche](reports/ALPHA20_28_CHECKS.md).
- 870 xUnit, 32 Node, gate CI e 21/21 casi nativi passati anche da ZIP nuovo; FULL non eseguito, orchestrazione Desktop.App ancora da qualificare.

## 3.9.0-alpha.20.27 — Integrazione UI della cattura hotkey

- Corretta la perdita della combinazione acquisita durante Shift+Tab. Tab e Shift+Tab conservano l'ultima combinazione completa; un nuovo tentativo non valido o il reset del focus la invalidano.
- Azioni del dialogo su più righe quando manca spazio: eliminato il superamento del bordo osservato nelle traduzioni francese e tedesca.
- 24 nuovi casi: 22 con i controlli Avalonia reali su backend headless e due di stato. Provati navigazione, conferma/annullamento/rimozione, libreria e salvataggio SQLite; disposizione in cinque lingue, due temi e due larghezze.
- **870 xUnit, 32 Node e GATE CI passati.** Dipendenza headless solo nei test, allineata ad Avalonia 11.3.22. Tastiera fisica, DPI e registrazioni native ancora da qualificare. [Dettagli](docs/ALPHA20_27_CAPTURE_INTEGRATION.md) · [rapporto](reports/ALPHA20_27_CHECKS.md).

## 3.9.0-alpha.20.26 — Acquisizione visuale delle hotkey

- Pulsante Registra… nell'editor dello snippet e form con visualizzazione dei tasti premuti, per esempio `ALT + CTRL + SHIFT + B`. Conferma dopo il rilascio, annullamento senza modifica e rimozione della bozza della hotkey.
- Sospensione temporanea dei worker di Ristretto e ripristino delle registrazioni senza scrivere impostazioni temporanee. Notifiche obsolete invalidate; nessun inserimento o picker durante l'acquisizione.
- 26 nuove regressioni: **846 xUnit passati**, inclusi i casi di coerenza fra display e combinazione confermata. Testi in cinque lingue, schema/JSON invariati.
- Registrata la conferma utente del richiamo tramite hotkey in Blocco note della 20.25; nuova form ancora da qualificare sul desktop. [Dettagli](docs/ALPHA20_26_HOTKEY_CAPTURE.md).

### Revisione documentale — 15 novembre 2026

- README riorganizzato nell'ordine richiesto: titolo, screenshot, descrizione, funzionamento, installazione e aggiornamento. Lo storico delle Alpha resta nel changelog.
- Immagini Preview 9 identificate come anteprime storiche; pianificate acquisizioni aggiornate dopo la qualificazione della GUI corrente.
- Nuova guida a installazione, profili assoluti, backup, migrazione schema 2 e ritorno con backup compatibile. Consolidati stato, build/test, indice e handoff.
- Roadmap R1–R8 dettagliata con task, priorità, dipendenze, decisioni e criteri di chiusura. Predisposta la matrice Windows per acquisizione hotkey, registrazioni e flussi manuali.
- Versione del codice invariata: 3.9.0-alpha.20.26. Le verifiche della revisione sono separate dalla baseline di 846 xUnit/32 Node. [Rapporto](reports/DOCS_ROADMAP_REVIEW.md).

## 3.9.0-alpha.20.25 — Hotkey per snippet e correzione CS0246

- Corretto l'import mancante nei test dell'anteprima R5: riprodotti e risolti i due errori CS0246 segnalati dall'utente. Baseline CORE poi superata con 779 xUnit.
- Hotkey facoltativa nell'editor di ogni snippet, registrata su Windows per l'inserimento diretto senza picker; conflitti locali rifiutati e disponibilità Windows mostrata nel campo.
- Registrazioni aggiornate con il catalogo; snippet/gruppi disabilitati esclusi, richieste obsolete annullate. Attesa limitata per il rilascio dei tasti, controllo di focus/appunti, nessuna riattivazione di finestre e nessun retry.
- Schema SQLite 2 con migrazione da 1, backup legacy migrati solo nello staging, campo JSON v1 facoltativo `hotkey`. Hash del gate esteso anche a XAML e traduzioni.
- 41 nuovi casi, **820 xUnit passati**; verifiche desktop delle nuove hotkey ancora aperte. [Dettagli](docs/ALPHA20_25_SNIPPET_HOTKEYS.md).

## 3.9.0-alpha.20.24 — Anteprima dei trigger Unicode su WinEvent (R5)

- Nuovo percorso **solo diagnostico**: confronta delta Unicode verificati come modifiche plausibili con il matcher dei trigger sintetici.
- Modalità opt-in `observe-classic-edit-preview.cmd`, senza lettura del database personale e senza modifiche negli editor esterni.
- Reset su focus, errori di lettura e modifiche non contigue; nessuna attivazione retroattiva, nessuna prova di provenienza dell'input.
- Regressioni C# per Unicode, delimitatori, testo preesistente e permessi di scrittura sempre negati.
- App desktop, picker, hotkey, database e integrazione VS Code invariati. Build e xUnit della 20.24 da verificare su Windows.

## 3.9.0-alpha.20.23 — Esperimento WinEvent R5

- Osservazione temporanea e opt-in degli eventi di modifica e focus in un `Edit` Win32 esterno, senza hook tastiera o scritture.
- Sessione che azzera la baseline dopo cambi di destinatario, errori o modifiche Unicode non valide; gli esiti non autorizzano l'espansione.
- `CaptureForControl` impedisce di leggere un campo diverso da quello che ha generato la notifica. Nuovi test C#, script di prova e contratti DevTools.
- Documentazione e roadmap aggiornate in italiano. Applicazione principale e SQLite invariati. Build/xUnit della 20.23 **NOT_RUN**.

## 3.9.0-alpha.20.22 — Picker selezione e campo testo reattivo; regressione offset UTF-16

- Corretto il test xUnit sul delta Unicode: per `a☕b` l'indice corretto del caret dopo l'inserimento è 2; conservato il rifiuto del caret spostato (3). Aggiunti test con unità UTF-16 surrogate.
- L'area **Testo da espandere** si estende orizzontalmente e verticalmente al ridimensionamento della libreria; eliminati `Height=225` e `MaxWidth=830`.
- Nel picker, `Su/Giù` seleziona il risultato anche dalla ricerca e `Invio` inserisce **il selezionato** (fallback al primo se non esiste una selezione), usando il consueto percorso protetto.
- Nuovi test xUnit su frecce, selezione, copy concorrente e regressione Unicode. Contratti statici DevTools aggiornati, senza Python.
- Nessuna modifica al servizio Win32 Inserisci, al database o alla hotkey. Build e test .NET della nuova versione da confermare su Windows.

## 3.9.0-alpha.20.21 — Osservazione Unicode Win32 opt-in (R5)

- Baseline Alpha 20.20 compilata e testata con successo dall'utente.
- Nuovo `ExternalClassicEditTextDeltaPolicy` per confrontare due stati Unicode di un editor esterno: solo esiti classificati, nessun permesso di espansione.
- Nuovo lettore Win32 `WindowsExternalClassicEditTextReader` con timeout, controlli di focus e limite 4096 unità UTF-16.
- Nuovo comando manuale `observe-classic-edit-text.cmd` e regressioni xUnit `ExternalClassicEditTextDeltaTests`.
- Testo letto solo temporaneamente in RAM, nessun log del contenuto, nessun hook o scrittura.
- Picker, GUI, hotkey, SQLite e Inserisci invariati; build e nuovi test 20.21 ancora da eseguire su Windows.

## 3.9.0-alpha.20.20 — Collegamento del testo committato al motore trigger (R5)

- Confermata dall'utente la baseline **Alpha 20.19.5**, compilazione e test passati.
- Nuovo `OwnedCommittedInputBridge` in Application: integra delta Unicode verificato, provenienza associata alla revisione e attivazione dei trigger con reset su input non affidabili.
- Il laboratorio Win32 `OwnedEditRecognitionSession` usa il nuovo bridge; continua a eseguire sostituzioni soltanto nel proprio controllo qualificato.
- Nuova suite xUnit `OwnedCommittedInputBridgeTests`, senza dipendenze Python e senza accesso nativo globale.
- GUI, picker, hotkey, SQLite e adapter VS Code invariati. Compilazione/test della nuova versione da eseguire su Windows.

## 3.9.0-alpha.20.19.5 — Hotfix compilazione DevTools CS9174

- Corrette due collection expression in `NotepadQualification.cs` che su Windows impedivano la compilazione (`CS9174`, righe 173 e 198 della Alpha 20.19.4).
- Introdotto `WithoutUtf8Bom(ReadOnlySpan<byte>)`, controllo esplicito e condiviso dell'intestazione BOM UTF-8, senza buffer aggiuntivi.
- Aggiunti tre test C# per documento UTF-8 con BOM, BOM parziale e fixture BOM preparata e riaperta.
- Rafforzato `SourceAudit` per segnalare il ritorno della sintassi problematica, e allineato il test sulla versione corrente.
- Applicazione desktop, picker, hotkey e database non modificati. Compilazione .NET/xUnit della nuova versione da eseguire su Windows.


## 3.9.0-alpha.20.19.4 — Strumenti C# e warning xUnit2000

- Conferma utente: gate CORE della Alpha 20.19.3 superato.
- Eliminata la dipendenza dagli script Python per audit, gate, controlli statici, prove CLI, qualificazione Notepad e packaging; introdotto il progetto C# `Ristretto.DevTools`.
- Aggiornati script di avvio e workflow CI per utilizzare .NET 10.
- Corretto warning `xUnit2000` su `Assert.Equal` in `WindowsExternalAdapterCapabilityTests`.
- Aggiunti test C# degli strumenti e consolidata la documentazione in italiano.
- Build e test della nuova release **da validare** su .NET 10 Windows; il picker non è stato modificato.
