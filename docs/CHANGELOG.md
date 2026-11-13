# Ristretto 3.9.0-alpha.20.5 — 14 novembre 2026

## Profilo unico e directory separate

- Risoluzione dati condivisa da avvio, SQLite, backup e singola istanza; `--data-dir` per profili separati, percorso normale conservato. `--check-profile`, help e versione disponibili senza GUI; errori espliciti senza fallback.
- Mutex/pipe normalizzati e lock esclusivo del file di profilo. Corretto il rifiuto SQLite della chiave `picker.hotkey`, che impediva di salvare la scorciatoia.
- **588 xUnit**, inclusi 30 nuovi casi, e dieci controlli CLI passati; solution/quattro strumenti compilati, audit e sette Python passati. R1.01 completata; hotkey/alias reali e UI restano da qualificare. [Evidenze](ALPHA20_5_STARTUP_PROFILES.md).

---

# Ristretto 3.9.0-alpha.20.4 — 13 novembre 2026

## Invio inserisce il primo risultato del picker

- Il tasto Invio accetta il primo elemento dell'elenco filtrato anche quando è selezionata un'altra riga. Il pulsante Inserisci continua a usare la selezione; Copia rimane sul relativo pulsante.
- Azione al rilascio del tasto, instradamento prima del comportamento dei controlli e reset del gesto su chiusura/perdita focus. Riutilizzato il percorso Inserisci che nasconde il picker e conserva i controlli del destinatario.
- Versioni e gate allineati ad Alpha 20.4; istruzioni nelle cinque lingue aggiornate. **558 xUnit** passati, inclusi 50 ViewModel e otto nuovi casi. Prova tastiera/clipboard reale ancora aperta. [Dettagli](ALPHA20_4_ENTER_INSERT.md).

---

# Ristretto 3.9.0-alpha.20.3 — 13 novembre 2026

## Ripianificazione della roadmap (documentazione)

- Riorganizzato il piano canonico in R1–R8 con attività numerate, dipendenze, consegne e criteri di chiusura. Prossimo incremento: profilo di avvio e isolamento dati/istanza, prima della qualificazione desktop.
- Distinte alpha Windows manuale, qualificazione host VS Code, studio di fattibilità automatismo e successiva implementazione condizionata; template e altri OS hanno percorsi espliciti. Aggiornati handoff, quadro F0–F6 e punti di ingresso.
- La baseline funzionale resta quella dei 550 xUnit; questa revisione non esegue nuovi test runtime o avvii grafici.

## Copia e Inserisci del picker

- Stato condiviso nel ViewModel per evitare sovrapposizioni; Copia e Inserisci disabilitati mentre un comando è pendente, senza accodamenti o retry automatici.
- Copia conserva il testo scelto al clic ma non aggiorna messaggi di sessioni o selezioni successive; errori localizzati senza dettagli grezzi delle eccezioni. Messaggi di attesa/errore in cinque lingue.
- **22 nuovi casi**, **550 xUnit totali** passati, inclusi 42 sul ViewModel reale. Solution e quattro tool compilati; nessun avvio grafico o clipboard reale. [Evidenze e limiti](ALPHA20_3_PICKER_CLIPBOARD.md).

## Caricamento picker

- Indice costruito su worker con token di annullamento; risultati ed errori delle letture precedenti non sovrascrivono la richiesta corrente. Copia/Inserisci disabilitati durante l'attesa e messaggio di caricamento nelle cinque lingue.
- `ReloadAsync` restituisce se ha pubblicato la snapshot: il chiamante non sostituisce più errori/cancellazioni con il messaggio di assenza destinatario. UiReview adattato all'attesa asincrona, senza eseguirlo.
- Tre regressioni riprodotte prima del fix; **22 nuovi casi**, **528 xUnit totali** passati, inclusi 20 test del ViewModel reale senza inizializzare Avalonia. Solution e quattro tool compilati. [Evidenze](ALPHA20_3_PICKER_LOADING.md).

## Ricerca picker

- Corretto il prefisso contenuto: fino a 4.096 unità UTF-16 senza spezzare grafemi, emoji ZWJ, bandiere o accenti combinanti. Query incomplete e campi indicizzati malformati non interrompono la ricerca.
- Aggiunto `PickerSearchIndex`, riutilizzato dal ViewModel per normalizzare i campi e ordinare i criteri secondari una volta per ricaricamento. Ranking e testi originali conservati; nessuna riscrittura dei dati.
- Quattro regressioni riprodotte prima del fix; **30 test ricerca e 506 xUnit totali** passati, build Release di solution e quattro tool. Nessun avvio grafico o benchmark GUI. [Evidenze](ALPHA20_3_PICKER_SEARCH.md).

## Ciclo di vita Inserisci

- Richieste monouso, pulsante disabilitato dopo il clic e annullamento su Esc, chiusura, libreria o nuova apertura; le continuazioni precedenti non aggiornano una nuova sessione.
- Gestite anche le eccezioni nella validazione iniziale del destinatario; errore prima dell'invio distinto dall'effetto nativo incerto. Nessun retry o ripristino automatico degli appunti.
- 18 nuovi casi gestiti, **36 test picker e 484 xUnit totali** superati; solution e quattro tool compilati in Release. Nessun avvio di VS Code o altro test grafico. [Evidenze e limiti](ALPHA20_3_PICKER_SESSION.md).

## Suite per Extension Development Host

- Dopo la segnalazione utente di un crash `Code.exe` (`0x80000003`), interrotti gli avvii desktop; rimosse le opzioni grafiche sperimentali e aggiunto `-PrepareOnly` per preparare il test senza eseguire VS Code. Nessun nuovo tentativo runtime dopo la segnalazione.
- Aggiunti 14 scenari per comando reale, Undo/Redo, Unicode, multilinea, documenti grandi e concorrenza durante la lettura dell'export.
- Nuovo `scripts/validate-vscode-host.cmd` con runner PowerShell: profilo e workspace sintetici, isolamento anche per installazioni portable, timeout, report di lancio e nessuna sovrascrittura di evidenze.
- Verificati sintassi JS, parsing PowerShell e 32 test Node; il renderer di VS Code 1.127.0 termina con codice 49 prima del test host. **Test host non eseguiti**, gate ancora aperto. [Dettagli](ALPHA20_3_VSCODE_HOST_TESTS.md).

## Consolidamento Windows

- Corretto `GUITHREADINFO` del picker: reinserito `hwndCaret` prima del rettangolo, per passare la dimensione corretta a `GetGUIThreadInfo`. Test di layout x64 riprodotto in errore prima del fix e superato dopo.
- Corretto il laboratorio remoto: UTF-8 senza BOM su stdin del figlio; mantenuto il rifiuto delle richieste malformate e dei replay. Corretta la simulazione di controllo discendente nei test del focus; aggiunte regressioni per controllo distrutto e BOM.
- Superati build Release di solution e quattro strumenti, **466 xUnit**, **31 scenari nativi**, smoke cooperante/remoto, **32 Node**, contratto .NET→Node e **7 Python**. Restore locale; prova manuale picker/host VS Code ancora pendente. [Rapporto](ALPHA20_3_VALIDATION.md).

## Picker: Inserisci in applicazione esterna (Windows, su richiesta)

- Aggiunto **Inserisci** accanto a **Copia testo**, senza rifare la GUI Preview 9. Il comando identifica finestra/processo/controllo focalizzato prima di mostrare il picker; il pulsante rimane disabilitato se non si può individuare un destinatario esterno.
- Servizio Win32 separato: ripristino del focus, controllo anti-cambio destinatario, rifiuto campi Win32 Edit password/sola lettura, verifica modificatori e numero sequenza clipboard; una sola chiamata `SendInput` con `Ctrl+V`, senza retry.
- Supporto multilinea ed emoji tramite appunti, che dopo l'inserimento contengono lo snippet. L'esito è sempre **inviato/non verificato** e il trigger digitato resta nel documento.
- Nuovi test xUnit simulati del contratto di paste; aggiornate cinque localizzazioni, versioni, documentazione, audit strutturale e pacchetto completo.
- Alla prima preparazione, build e prove .NET/Windows non erano state eseguite; il consolidamento sopra raccoglie la successiva verifica effettiva.
- [Contratto e matrice di prova](ALPHA20_3_PICKER_INSERT.md).

---

# Ristretto 3.9.0-alpha.20.2 — 13 novembre 2026

## Hotfix CS0117: API Unicode tra Core e Application

- Corretto `CommittedInputSession.IsValidText`: da `internal static` a `public static`, senza modificare le regole Unicode. Rende compilabile la chiamata cross-assembly in `CommittedEditDeltaVerifier`.
- Nuovo test C# di regressione e due test Python di audit per accesso ai metodi `internal static` tra progetti.
- Rafforzato il gate PowerShell, versioni e documentazione. GUI, hotkey, persistenza SQLite, VS Code e logica di espansione invariate.
- Il test Windows Alpha 20.1 dell'utente ha segnalato CS0117; **build e test .NET della 20.2 NON sono stati eseguiti qui**.
- [Dettagli del fix](ALPHA20_2_CS0117_FIX.md).

---

# Ristretto 3.9.0-alpha.20.1 — 13 novembre 2026

## Revisione codice e documentazione

- Corretto il disallineamento `alpha.19` nei quattro `.csproj` degli strumenti esterni alla solution. Allineati `VERSION.txt`, progetto Desktop e gate di validazione.
- Aggiunto audit offline `scripts/audit_source.py` (namespace e `using` interni, graph `ProjectReference`, versioni, progetti in solution e import xUnit), con cinque test unitari e script Windows/Bash.
- Rafforzati il gate `verify-release.ps1` e i percorsi `validate.cmd` / `validate.sh`; nessuna modifica a motore, GUI, hotkey, SQLite o formato dati.
- Consolidati `README`, `CURRENT_STATE`, `ROADMAP` e `HANDOFF`; rimossi elenchi cronologici duplicati dai punti di ingresso conservando specifiche e report delle Alpha precedenti.
- **Eseguiti:** 32/32 test Node, 5/5 test Python e controlli statici. **Non eseguiti:** build .NET, xUnit nuovi, native Windows e reale VS Code.
- [Report e criteri della revisione](ALPHA20_1_CODE_AUDIT.md).

---

# Ristretto 3.9.0-alpha.20 — 13 novembre 2026

## Alpha 20 — adapter VS Code, sicurezza e compatibilità export


**Implementati:** il comando manuale fissa editor, versione, selezione e focus **prima** di iniziare la lettura asincrona del JSON; se il contesto cambia, non invia modifiche. Aggiunta validazione più rigorosa per GUID, nomi e descrizioni nell'export. Il percorso di test incrociato genera una libreria di prova tramite `PortableLibraryService.ExportAsync` e verifica l'output con il vero parser Node (`scripts/validate-export-contract.cmd` / `.sh`). Nessun dato utente viene usato. **32 test Node eseguiti; contratto .NET e host VS Code da eseguire.** [Dettagli](ALPHA20_VSCODE_CONTRACT.md).


## Alpha 19 — adapter VS Code sperimentale

- Aggiunta estensione locale opt-in per Visual Studio Code: legge export JSON v1, espande su **comando manuale** tramite `TextEditor.edit`, non tramite hook o eventi di digitazione.
- Confini di parola, NFC/NFD, duplicati rifiutati, gruppi disabilitati, controllo del focus/versione/selection e divieto di retry dopo esiti incerti.
- Test Node indipendenti, script `validate-vscode.cmd/.sh`, fixture sintetica; nessuna dipendenza npm per l'estensione stessa.
- GUI, hotkey, SQLite, Core .NET e laboratorio Windows invariati tranne versioni/validazione. [Contratto](ALPHA19_VSCODE_ADAPTER.md).
- Test Node eseguiti; build/xUnit/native Windows e avvio reale nell'Extension Development Host **NON ESEGUITI**.

# Ristretto 3.9.0-alpha.18 — 13 novembre 2026 (validazione Windows aperta)

- Esperimento cross-process controllato: comando monouso e risposta priva di testo, con verifica locale del risultato nel processo EDIT proprietario.
- Due scenari nativi aggiunti: sostituzione Unicode e rifiuto dello snapshot stantio; replay bloccato.
- Nuovi test xUnit puri del protocollo, sessione, PID, input malformati e duplicati.
- Nuova [specifica Alpha 18](ALPHA18_REMOTE_OWNED_EDIT.md), script `validate-native.cmd` esteso.
- GUI Preview 9, hotkey del picker e SQLite non modificati. **Build e test nuovi non eseguiti in questo ambiente.**

---

# Ristretto 3.9.0-alpha.17 — 13 novembre 2026 (validazione Windows aperta)

- Contratto fail-closed per editor esterni: separazione rigorosa tra metadati Win32 osservati, caret/selezione e capacità di sostituzione.
- Matrice di **otto garanzie non dimostrate** che serviranno a un adapter esterno; `CanReplaceText=false` e `CanReadCommittedText=false` per ogni valutazione.
- Diagnostica manuale `--assess-external-adapter` nel laboratorio, report JSON senza contenuti, identificativi o offset; nessun nuovo hook o modifica esterna.
- Nuovi test xUnit puri sulla matrice delle capacità e sui rifiuti. GUI Preview 9, hotkey e SQLite non modificati. Build/xUnit/native **NOT_RUN** in questo ambiente.
- [Specifica Alpha 17](ALPHA17_EXTERNAL_ADAPTER_CAPABILITIES.md).

# Ristretto 3.9.0-alpha.16 — 13 novembre 2026 (da validare)

- Demo Win32 a **eventi anziché polling ogni 10 ms**: osservazione dopo i messaggi processati uno alla volta, `WaitMessage()` a coda vuota; anche il processo figlio cooperante usa il nuovo ciclo.
- Gestione conservativa degli eventi `WM_CHAR` contenenti una coppia UTF-16 con una sola osservazione del carattere completo; interruzioni invalidano la continuità.
- **3 regressioni native nuove**: burst `;firma `, interruzione con navigazione, surrogate emoji. Totale previsto: **31 casi nativi**. Build/xUnit/native **NOT_RUN** qui.
- Corretto il disallineamento delle versioni ereditato da Alpha 15: `VERSION.txt`, `.csproj`, `verify-release.ps1`, `validate.sh`, `validate.cmd`, `validate-native.cmd` ora coincidono nella verifica dell'Alpha 16.
- GUI Preview 9, hotkey globale configurabile, SQLite e Core di matching invariati. Nessuna espansione in app esterne. [Contratto e procedura](ALPHA16_EVENT_PUMP.md).

# Ristretto 3.9.0-alpha.15 — 13 novembre 2026 (da validare)

- Nuova policy pura `OwnedEditDispatchPolicy` per distinguere messaggi stampabili e azioni che interrompono la continuità del trigger.
- `OwnedEditTarget` invalida `TargetEpoch` per navigazione, comandi di editing, clipboard, input IME, focus e mouse, anche senza variazioni del testo; resta limitato al laboratorio Win32 posseduto.
- Nuovi xUnit e **3 scenari nativi aggiuntivi** (totale previsto 28). Prove native e build C# **NON ESEGUITE** in questa consegna.
- GUI Preview 9, hotkey globale picker, database SQLite, Core matcher e adapter esterni **invariati**.
- Il classificatore non garantisce origine umana né vede tutti i `SendMessage` diretti: [specifiche e limiti](ALPHA15_EDIT_CONTINUITY.md).

# Ristretto 3.9.0-alpha.14 — 13 novembre 2026 (da validare)

- Introdotta una policy di provenienza delle mutazioni dell’EDIT controllato, correlata a `TargetEpoch` e `TextRevision`: richiede *sia* delta Unicode verificato (Alpha 13) *sia* origine locale osservata.
- La demo interattiva rifiuta fixture, paste/comandi sconosciuti, IME e cambi senza un messaggio di carattere correlato; le fixture restano permesse solo nella suite sintetica. `WM_CHAR` non è una prova di digitazione umana.
- Aggiunti test xUnit puri e 2 scenari nativi per verificare revisione e falsi segnali di origine umana. GUI Preview 9, hotkey, SQLite e riconoscitore Core invariati.
- Controlli statici e integrità ZIP da distinguere da compilazione/.NET e Windows: **test nativi e xUnit Alpha 14 NON ESEGUITI**. Ultima baseline valida Alpha 7.
- [Contratto e matrice di verifica](ALPHA14_MUTATION_PROVENANCE.md).

# Ristretto 3.9.0-alpha.13 — 13 novembre 2026 (da validare)

- Introdotto `CommittedEditDeltaVerifier` nel modulo Application: convalida, senza effetti sul destinatario, due snapshot successive di un EDIT locale posseduto, con identità/epoch/revisione, sicurezza, focus, IME, modificatori, selezione, caret, confini grafemici e Unicode esatto.
- `OwnedEditRecognitionSession` del laboratorio Win32 usa ora il verificatore al posto del confronto di snapshot privato; una prova verificata di confine parola consente trigger alfabetici dopo spazi/punteggiatura, impedendo l'attivazione a metà parola.
- Nuovi test xUnit e 2 scenari nativi (test `brb` dopo spazio / dentro parola) per Unicode NFC/NFD, emoji ZWJ, alfabeti non latini, mismatch di stato e timeout logico di revisione, input non contiguo, campi protetti, IME, limiti di input e dati riservati nei messaggi diagnostici.
- Le osservazioni potrebbero provenire anche da incolla o modifica programmatica: la verifica del delta **non prova** l'origine umana. Nessun hook né sostituzione in editor terzi; hotkey picker Alpha 12 e GUI Preview 9 invariati.
- Controlli statici da eseguire; build/test Windows e test nativi alpha.8–13 restano **NOT_RUN** fino a prova effettiva. L'ultima validazione attestata resta alpha.7 (180 xUnit, 21 nativi).
- [Contratto, limiti e matrice di prova](ALPHA13_COMMITTED_INPUT.md).

# Ristretto 3.9.0-alpha.12 — 13 novembre 2026 (da validare)

- **Hotkey globale configurabile per il picker su Windows:** `Ctrl+Shift+Space` predefinito, modifica e disattivazione (`None`) dalle Impostazioni, memorizzazione SQLite e funzionamento con finestra nascosta nella tray.
- Registrazione `RegisterHotKey` senza hook o raccolta dei tasti; cambio preparato prima del salvataggio e poi confermato, con rollback in caso di collisioni o errori di persistenza. Il picker continua a copiare manualmente.
- Test puri del parser su normalizzazione, combinazioni errate e disattivazione. **Test Windows e build NON eseguiti in questa consegna.** La GUI Preview 9 cambia solo nella sezione delle impostazioni.
- Proseguimento della caratterizzazione F3 del testo committato e degli editor esterni **ancora da fare**; gli editor esterni non sono qualificati. [Guida e test manuali](ALPHA12_PICKER_HOTKEY.md).

# Ristretto 3.9.0-alpha.11 — 13 novembre 2026 (da validare)

- Esperimento esplicito `--inspect-classic-edit` nel laboratorio Windows: `EM_GETSEL` read-only, timeout 150 ms, focus e metadata ricontrollati prima/dopo.
- Stato della selezione aggregato senza testo, offset, nomi dei processi, HWND/PID, clipboard o titoli. Report JSON facoltativo solo CreateNew.
- Nuovi xUnit puri per packed range Win32, overflow, rifiuti, timeout, cambio focus e policy permanentemente senza permesso di scrittura.
- NESSUNA qualifica di editor terzi, hook o espansione esterna. GUI Preview 9, Core, SQLite, picker e adapter di sostituzione preesistenti invariati.
- Build/xUnit e native su Windows **NOT_RUN**; le Alpha 8–11 restano aperte; evidenza alpha.7 conservata separatamente.
- [Procedura e limiti](ALPHA11_CLASSIC_EDIT_SELECTION.md).

# Ristretto 3.9.0-alpha.10 — 13 novembre 2026 (da validare)

- Nuova sonda Windows **solo metadata** e **solo su invocazione esplicita** per analizzare un controllo con focus in un'altra applicazione. Non legge testo, titoli, nomi di processi, tasti o clipboard; non invia input.
- Classificatore fail-closed: focus/processo/radice, visibilità e abilitazione, tipo Win32 `Edit`/RichEdit/altro, `ES_PASSWORD` e `ES_READONLY` solo per `Edit`; classificazione priva di autorizzazione a scrivere.
- Comando `--probe-external [--report nuovo-file.json]` nel laboratorio separato, report opzionale senza testo/PID/HWND. Nessun collegamento alla GUI o al gate di espansione globale.
- Nuovi xUnit puri di regressione. GUI Preview 9, SQLite e sostituzione nel processo cooperante invariati. **Build/.NET e nativi non eseguiti qui**; alpha.8/9 ancora da validare su Windows.
- [Scope, procedura e limiti](ALPHA10_EXTERNAL_EDITOR_PROBE.md).

# Ristretto 3.9.0-alpha.9 — 13 novembre 2026 (da validare)

- Laboratorio **cooperante a due processi**: un processo figlio separato possiede il proprio EDIT Win32 e osserva/sostituisce soltanto il testo nel suo campo. Il genitore riceve esclusivamente READY e contatori, con verifica sessione/PID e risultato fail-closed.
- Aggiunti `--cooperating-demo [--report nuovo-file.json]` e `--cooperating-smoke` con sostituzione sintetica verificata nel processo figlio; nessuna modifica in editor esterni arbitrari o integrazione nel prodotto Ristretto.
- Contratto `CooperatingLabProtocol` e test xUnit per ricevute non valide, correlazione e regole di successo; report senza dati digitati.
- GUI Preview 9, persistenza SQLite, picker e contratto di sostituzione esistente invariati. Nessun hook globale, backspace simulato o SendInput nel nuovo percorso.
- **Da eseguire su Windows:** compilazione/.NET 10, xUnit, test nativi e prova interattiva in processo figlio. Le evidenze alpha.7 restano storiche.

# Ristretto 3.9.0-alpha.8 — 13 novembre 2026 (da validare)

- Introduzione del gate F3 `ExpansionAdmissionPolicy` e del dispatcher fail-closed per il percorso osservazione → riconoscimento → sostituzione, **integrato esclusivamente nel laboratorio Win32 posseduto**. Richiede consenso esplicito, input Unicode committato verificato, snapshot del destinatario, stato del campo, focus, IME e assenza di pausa/esclusioni/modificatori.
- Scope editor esterni rifiutato: non esistono hook globali o un adapter di sostituzione esterno qualificato. Nessun cambiamento della GUI Preview 9, SQLite, formato di import/export o backup.
- Demo interattiva: guardia foreground/focus prima della sostituzione, riepilogo con PASS/FAIL e `--report` JSON facoltativo senza dati dei testi; nuovo script `scripts\validate-native.cmd` per la suite Win32 esplicita.
- Nuovi xUnit di rifiuto e sicurezza. **Nessuna build, esecuzione xUnit o prova nativa alpha.8 dichiarata in questa consegna**. Baseline storica alpha.7: 180/180 xUnit e 21/21 nativi passati.

---

## Riordino documentazione — 13 novembre 2026 (sorgenti alpha.7 invariati)

- Riuniti i riferimenti GUI in `GUI.md`, nel design system aggiornato ai token effettivi e nella checklist manuale. Indice, stato, roadmap, handoff e istruzioni di build allineati alla versione corrente.
- Spostate le 32 anteprime definitive e il registro originale in `design/current/`; vecchi screenshot, concept e render intermedi rimossi dall'albero attivo.
- Conservati l'ultima validazione alpha.7, le misure originali del benchmark e il risultato del laboratorio simulato. Rapporti duplicati e audit di preview superate esclusi dal progetto corrente; storia e motivazioni utili conservate nel changelog e nelle specifiche.
- Il controllo automatico ha bloccato la cancellazione definitiva. I 178 file superati sono quindi conservati in `.local-cache/cleanup-recovery`, esclusa dal pacchetto, con spostamenti reversibili. Nessuna nuova versione dell'app né nuova esecuzione delle prove funzionali.
- [Guida GUI](GUI.md), [materiali correnti](../design/README.md), [rapporti conservati](../reports/README.md).
- [Verifica del riordino](../reports/CLEANUP_VALIDATION.json): hash di sorgenti, anteprime ed evidenze invariati; collegamenti locali, XML/JSON, token di palette e gate dei sorgenti controllati. Script Python non eseguiti per assenza di un interprete utilizzabile.

## 3.9.0-alpha.7 — 13 novembre 2026 (sostituzione nativa nel proprio EDIT Win32)

- Esteso `Ristretto.WindowsOutputLab` con un target Win32 reale creato e posseduto sul proprio thread UI. Nessun HWND esterno accettato. Confronto della snapshot, sostituzione e verifica di testo/caret senza message pump fra i passaggi.
- Aggiunto ponte per inserimenti contigui effettivamente letti dal controllo; raw NFD conservato, delimitatore preservato, reset su cambi non qualificabili. Il risultato generato non viene riutilizzato come input.
- `--replacement-test [--report <nuovo-file>]`: **21/21 scenari nativi passati**, inclusi caret a 65.536, cambi di revisione/identità/readonly/password, cancellazione, thread errato, replay, ricevuta persa ed eccezione dopo effetto. Report senza payload e con hash dell'eseguibile.
- `--replacement-demo`: demo disponibile per digitare `;firma` + spazio nel solo campo proprio; input umano non verificato in questa sessione. IME disabilitato soltanto nel thread CLI di prova, non qualificato in generale.
- **180/180 xUnit passati**, build Release solution e quattro tool senza errori/avvisi. GUI Preview 9, dati e logica applicativa invariati; l'interfaccia aggiorna soltanto il commento descrittivo dei backend disponibili.
- Non sono qualificati SendInput o editor arbitrari. [Scope, fonti e limiti](F2B_WINDOWS_OWNED_EDIT.md).

## 3.9.0-alpha.6 — 13 novembre 2026 (GUI Preview 9 — tema e stile)

- Riaperta la GUI su richiesta dell'utente, priorità «Tema e stile generale»: palette chiara calda e scura grafite, superfici distinte, tipografia compatta più leggibile, titolo documentale e trigger su chip.
- Corretto il segnaposto poco leggibile in tema scuro con focus e allineati gli stati disabilitati del template Fluent. Colori di focus condivisi e barra di stato contenuta nella larghezza disponibile.
- Aggiunto `Ristretto.UiReview` senza nuove dipendenze: carica XAML reali con store sintetico in memoria; prodotti 32 render chiaro/scuro, cinque lingue, minimi IT/DE, picker vuoto e tavole controlli. Bounds delle azioni applicative verificati. Render a 96 DPI, non test manuali completi o screenshot del desktop.
- `validate.cmd` completo: solution + quattro tool senza errori/avvisi; **180/180 xUnit passati**. Code-behind, ViewModel, traduzioni e logica Core/Application/Infrastructure invariati. Output nativo non ripetuto.
- [Anteprime, metodo e limiti](GUI.md). Restano i controlli manuali DPI e tastiera.

## 3.9.0-alpha.5 — 13 novembre 2026 (F2b — sostituzione condizionale simulata)

- Implementati planner di sostituzione ed esecutore a uso unico in Application: evidenza raw originale, token/epoch/revisioni, verifica stato/caret, limiti di testo e confini grafemici. Il delimitatore già committato viene preservato. Nessun calcolo Backspace.
- Introdotto port `IConditionalReplacementTarget` con confronto/modifica indivisibili, implementato soltanto da `SimulatedReplacementTarget` sotto lock. Receipt completa distinta da invio non verificato ed errore dopo possibili effetti; nessun retry o rollback automatico.
- Nuovo `Ristretto.ReplacementLab --self-test`: percorso F1b → piano → sostituzione in memoria e scenari di replay, dati modificati, receipt persa, errore dopo effetto. **5/5 passati**.
- Aggiunti **43 casi xUnit**, suite **180/180 passati**. `validate.cmd` completo: solution e tre tool compilati senza errori/avvisi; restore locale. Script e archivio aggiornati alla alpha.5.
- GUI Preview 8, matcher, SQLite e logica Win32 invariati. Prova nativa non ripetuta, nessuna espansione in app esterne abilitata. [Contratto e limiti](F2B_CONDITIONAL_REPLACEMENT.md).

## 3.9.0-alpha.4 — 13 novembre 2026 (benchmark e allocazioni Core)

- Aggiunto `Ristretto.CoreBenchmark`, eseguibile separato senza nuove dipendenze: dati sintetici 100/10k, 11 scenari, cinque ripetizioni, percentili, allocazioni, GC, controllo delle decisioni e SHA-256 assembly. Rapporti esistenti mai sovrascritti.
- Baseline misurata sulla alpha.3 e confronto con alpha.4: 1.101.210 eventi per versione, nessuna decisione errata. `CompiledTriggerCatalog` usa lookup ordinali su `ReadOnlySpan<char>` al posto di sottostringhe allocate per suffisso. D10 buffer pieno: media 24.320 → 4.592 byte/evento (-81%); nessuna promessa di speedup generale.
- Due test di regressione per prefissi Unicode lunghi: suite **137/137 passati**. Script di validazione compilano anche il benchmark senza avviarlo.
- Prova Windows F2a tentata sulla alpha.3: `target_changed`, uscita 3, nessun `SendInput`. Ricezione Unicode e compatibilità con editor esterni ancora non verificate. GUI Preview 8, SQLite e logica dell'adapter Windows invariati.
- [Misure, riproduzione e limiti](CORE_BENCHMARK.md).

## Verifica alpha.3 — 13 novembre 2026 (Windows)

- Eseguiti build Release della solution e del laboratorio con .NET SDK 10.0.403: nessun errore o avviso. Suite xUnit: **135 passati, 0 falliti, 0 ignorati**.
- Completato `scripts\validate.cmd` con dipendenze locali, dopo un errore TLS nel restore online. Audit NuGet non eseguito nella verifica offline.
- Disattivata la telemetria di build negli script Windows/Bash per evitare scritture del componente Avalonia nel profilo; corretto il controllo versione obsoleto di `validate.sh`.
- Esclusi cache locale e risultati temporanei dal pacchetto sorgenti. La GUI e il codice funzionale restano invariati. Prova interattiva F2a e test Linux/macOS non eseguiti. I rapporti di questa build storica sono stati sostituiti dall'[ultima validazione conservata](../reports/README.md).

## 3.9.0-alpha.3 — 13 novembre 2026 (F2a — laboratorio Windows Unicode controllato)

- Ricevuta conferma dall'utente: **compilazione e tutti i test della alpha.2.1 superati**. F1a/F1b validati nella suite Windows; benchmark ancora da misurare.
- Aggiunto il gate puro `WindowsExplicitTestTargetGate` e i relativi test xUnit per il rifiuto fail-closed di cambi focus/PID, password, controlli di classe diversa, invisibili, disabilitati e readonly.
- Introdotto tool separato `Ristretto.WindowsOutputLab`: esecuzione soltanto su richiesta `--self-test`, processo figlio con campo Win32 `EDIT` controllato, verifica destinatario, invio della fixture Unicode tramite l'adapter esistente e confronto esatto del testo ricevuto nel solo campo di test.
- `validate.cmd` compila il laboratorio ma **non lo avvia**. Nessuna modifica GUI, schema SQLite, adapter esistente o riconoscimento F1. Nessun global hook e nessuna sostituzione di trigger.
- **Verificato qui:** controlli statici e archivio. **Non verificato qui:** build .NET10/xUnit alpha.3 e funzionamento reale Win32 (richiede prova Windows esplicita). [Specifica F2a](F2A_WINDOWS_CONTROLLED_LAB.md).

## 3.9.0-alpha.2.1 — 13 novembre 2026 (hotfix F1b — attivazione Immediate)

- Ricevuto risultato reale Windows su alpha.2: **120 test, 119 superati, 1 fallito** (`ImmediateAnywhereModeCanMatchInsideWordWhenExplicitlySelected`). La compilazione del codice ha raggiunto la fase xUnit; il comando complessivo termina in errore per il test fallito.
- Corretto `TriggerActivationSession.EvaluateAfterAppend`: un prefisso proprio più corto del match esatto non può bloccarne l'attivazione in modalità `Immediate`. Rimangono in attesa i prefissi **di lunghezza almeno pari** al match esatto, quando un trigger più lungo è possibile.
- Aggiunti tre test di regressione: riconoscimento immediato a inizio parola con boundary verificato, prefisso più corto e non pertinente, vero prefisso comune con un trigger più lungo. Conservato il test originario che falliva.
- Nessuna modifica a GUI, SQLite, adapter Win32, configurazione o comportamenti esterni. Nessun hook né espansione globale.
- **Verificati qui:** coerenza delle modifiche, controlli statici e ZIP/manifest. **Non eseguiti qui:** .NET build e xUnit; il nuovo pacchetto richiede la verifica Windows.

## 3.9.0-alpha.2 — 13 novembre 2026 (F1b — regole di attivazione pure)

- Confermata dall'utente **build e test passati per Preview 8**; al momento della consegna alpha.2 non erano disponibili risultati xUnit; l'esito Windows ricevuto successivamente è riportato nel hotfix alpha.2.1.
- Aggiunta `TriggerActivationSession`: policy configurabile `OnDelimiter` (default) o `Immediate`, prefissi sovrapposti, boundary verificato per trigger alfanumerici, delimitatori Unicode, timeout monotono e ambiguità esplicita.
- Riutilizzato `CompiledTriggerCatalog` con API interne per suffix validi e proper-prefix senza duplicare l'indice. Invariato l'endpoint `CommittedInputSession` di F1a.
- Aggiunta suite `TriggerActivationSessionTests` con sequenze Unicode, timestamp, contesto protetto, chunk bulk, overlap e indice da 10k.
- Nessun hook, nessuna cancellazione/iniezione, nessuna modifica a SQLite, XAML, ViewModel o adapter Win32. L'output rimane copia manuale nel picker.
- Documentato [contratto F1b](F1B_ACTIVATION_POLICY.md), roadmap e handoff. **Verificato qui:** static check e integrità pacchetto; **NON eseguiti:** build .NET, xUnit, benchmark di latenza e prove in editor Windows.

## 3.9.0-alpha.1 — 13 novembre 2026 (F1a — motore Unicode non collegato)

- Dopo conferma dell'utente che la Preview 8 compila e supera i test su Windows, **pausa del redesign**. Nessuna modifica al codice Avalonia, agli XAML, ai ViewModel e al database.
- Introdotto `CompiledTriggerCatalog`: indice di trigger NFC case-sensitive compilato fuori dal percorso di digitazione, rispettoso dei flag di gruppo/snippet, dei duplicati e dei prefissi lunghi.
- Introdotto `CommittedInputSession`: buffer effimero con limiti espliciti, reset su epoch/IME/contesto sconosciuto/protetto/sintetico, pausa e nuova snapshot; nessun effetto nativo.
- Aggiunti nuovi test xUnit con Unicode internazionale, emoji ZWJ, ambiguità, focus, sicurezza e un catalogo sintetico da 10.000 trigger.
- Riorganizzata la roadmap attiva F0–F6; conservato il piano GUI storico.
- **Verificabile qui:** controlli statici e ZIP. **Non verificabili qui:** build .NET 10, xUnit, esecuzione native Windows e prestazioni; i test Preview 8 superati non sono prove della nuova alpha.1.

## 3.8.0-preview.8 — 13 novembre 2026 (editor a densità controllata)

- **Baseline Windows confermata dall'utente:** Preview 7 compila e supera i test; screenshot reali acquisiti ma grafica ancora da migliorare.
- Finestra Main 980×640 con sidebar 226 DIP; editor con campo contenuto iniziale 225 DIP, campi in ScrollViewer e barra azioni persistente al resize; intestazione riorganizzata con azione prima del nome.
- Picker 600×390: anteprima 84 DIP senza cornice completa, risultati più densi, minimo 500×365; Invio/Esc, anteprima e copia manuale preservati.
- Piccolo affinamento dei controlli compact (29/32 DIP). Intatti VM, SQLite, lingua, tema, importatori e output Windows scollegato.
- Validatori statici aggiornati e screenshot Preview 7 conservati all'epoca; verificati XAML/risorse/ZIP. **Non verificati allora:** build .NET, xUnit e resa reale Preview 8. Audit poi consolidato nella [guida GUI](GUI.md).

## 3.8.0-preview.7 — 13 novembre 2026 (riduzione della densità visiva)

- Dopo build e test della Preview 6 confermati dall'utente, analizzati gli screenshot reali: comandi e editor ancora sovradimensionati; picker con eccesso di spazio vuoto.
- Main 1060×690, sidebar 236, toolbar/controlli da 30–34 DIP, titolo 17, lista snippet su due righe e workspace senza margine/cornice esterna.
- Picker 650×480, risultati a tutta larghezza, preview breve sotto la lista; rimangono la selezione e la copia manuale, con viewport minimo per tre elementi.
- Introdotta la classe visiva `compact` solo nelle finestre operative; mantenuti i template, i token neutri e i dialog precedenti. Nessun cambiamento ai ViewModel, SQLite, importatori o backend.
- Aggiornati i validatori statici alla nuova geometria, versione, documentazione e riferimento alle schermate Preview 6.
- **Verificato allora:** controlli statici e integrità ZIP. **Non verificato allora:** build .NET, xUnit, resa e usabilità della Preview 7. Audit poi consolidato nella [guida GUI](GUI.md).

## 3.8.0-preview.6 — 13 novembre 2026 (reset grafico implementato, da validare)

- Applicato il design system **neutral-first** all'applicazione, eliminando le superfici rame/petrolio, le grandi intestazioni colorate e i valori cromatici hardcoded dalle finestre principali.
- Introdotti template espliciti per `Button` e `ListBoxItem`, con states normal/hover/pressed/focus/disabled; rimosso il foreground globale dei `TextBlock` che poteva rendere il testo dei pulsanti illeggibile.
- Nuovi layout coerenti di finestra principale e picker, selezioni neutre, ricerca senza wrapper con bordo doppio, tipografia 24/16/14/13 px e picker ridimensionabile con almeno tre righe previste.
- Finestra di importazione e dialoghi ereditano la stessa palette tramite stili semantici; nessun cambiamento a dominio, SQLite, importatori, backup, adapter Windows isolato o ViewModel.
- Aggiornati validatore statico, versione, documentazione, roadmap, stato e handoff. README rimane volutamente essenziale.
- **Verificato:** XML/XAML e validator statici; integrità sorgente e ZIP. **Non verificato:** compilazione .NET, xUnit, comportamento del template e rendering GUI su Windows/macOS/Linux.


## Unreleased — studio desktop UI 2026 (documentazione soltanto, 13 novembre 2026)

- Analizzati i 5 articoli sui trend UI desktop 2026 indicati dall'utente e confrontati con Fluent 2, Apple HIG, GNOME HIG, WCAG e documentazione Avalonia.
- Registrati i difetti reali della Preview 5 tramite screenshot e audit dei file `App.axaml`, `MainWindow.axaml`, `PickerWindow.axaml`.
- Redatti design system neutral-first, matrice degli stati UI e gate correttivi da D0 a D6. Creato un **UI Lab HTML offline** per discutere gerarchia, tema chiaro/scuro e picker prima del porting XAML.
- Aggiornati indice, roadmap, stato corrente e handoff; README volutamente invariato.
- **Nessuna modifica a codice, XAML, test, SQLite o versione dell'app. Nessuna nuova build dichiarata.**

---

# Changelog — Ristretto

## 3.8.0-preview.5 — 13 novembre 2026 (G6 implementato, da validare)

- Rinnovata **ImportReviewWindow** con lo stesso linguaggio cromatico della Preview 4: testata rame, riepilogo sorgente e quantità, avviso rischi, ricerca separata, strumenti di selezione, righe leggibili, paginazione e footer per confermare.
- Gli elementi importabili suggeriti sono preselezionati come prima. Gli elementi con collisioni, contenuti dinamici e opzioni non supportate sono riconoscibili anche tramite testo; non vengono nascosti e restano selezionabili deliberatamente dall'utente. **Nessun cambiamento alle regole dell'importazione, al commit SQLite o alla disattivazione degli snippet importati.**
- Uniformati i dialog di conferma (cancellazione, bozze, ripristino) alla palette chiaro/scuro mediante stili riutilizzabili definiti in `App.axaml`. Messa a fuoco iniziale su **Annulla** e supporto per **Esc**, evitando conferme involontarie.
- Corretto anche il dimensionamento minimo del picker: il viewport della lista riserva ora 290 px per almeno tre righe comprensive di padding e margini; altezza minima finestra 640 px, resize sempre attivo.
- Nessuna dipendenza aggiuntiva, né modifiche a Core/Application/Infrastructure/test, schema SQLite o adapter Windows. README invariato e limitato a descrizione, avvio e compilazione.
- **Verificato:** controlli statici dei sorgenti e delle risorse, integrità ZIP e documentazione. **Non verificato:** compilazione/.NET, test xUnit e rendering Avalonia della Preview 5.

## 3.8.0-preview.4 — 2026-11-13 (da validare)

- Riprogettazione significativa della GUI rispetto alla Preview 3, che compilava e superava i test ma non soddisfaceva i requisiti estetici.
- Palette originale caldo/rame in tema chiaro/scuro; gerarchia attraverso superfici, contrasto, titoli e accenti.
- Finestra principale con navigazione compatta, pagina dello snippet con intestazione editoriale e barra azioni distinta.
- Titolo della pagina basato sul nome dello snippet; aggiornamento reattivo durante l'editing, con fallback localizzato per nuova bozza.
- Picker ridimensionabile con almeno tre righe utilizzabili e pannello di anteprima dedicato, mantenendo la copia manuale.
- Nessuna modifica allo schema SQLite, ai dati persistenti o al backend di iniezione Windows.
- Verifiche XAML statiche eseguite; compilazione, xUnit e verifica grafica della Preview 4 ancora da eseguire.


Questo file registra **solo modifiche effettive** del progetto. Il lavoro futuro è in [ROADMAP.md](ROADMAP.md). Gli audit grafici superati sono consolidati nella guida GUI; le specifiche tecniche conservano le motivazioni ancora utili.

## v3.8.0-preview.3 — 13 novembre 2026 · Design system e picker ridisegnati (compilata e testata su Windows; grafica non approvata)

- Riorganizzati `App.axaml`, `MainWindow.axaml` e `PickerWindow.axaml` con un nuovo linguaggio visivo: gerarchia esplicita H1/sezione/label/metadati, icone vettoriali locali, sidebar della libreria e editor principale distinti.
- Palette light/dark più morbida, superfici leggermente elevate, accenti limitati ai comandi e gradient soltanto sul piccolo marchio. Transizioni hover/selected brevi e senza spostamenti del layout.
- Picker realmente ridimensionabile, non più `SizeToContent=Height`, con minimo 520×530 e area di risultato alta almeno 265 px: tre righe complete possono comparire anche nel layout minimo. Altezza iniziale 485 px.
- Conservati nomi di controlli, binding, handler, comportamento di copia manuale e protezione bozze; nessuna modifica ai moduli di dominio, infrastruttura o database.
- Aggiornati gate di validazione, versioni, script e riferimenti agli screenshot reali della preview.2. `README.md` mantiene esclusivamente descrizione, avvio e compilazione.
- **Verifica da eseguire:** `validate.cmd`, controlli UI reali, focus, clipping, ridimensionamento, motion e traduzioni. Nessun risultato di build/test della preview.3 viene dichiarato senza evidenze.

## v3.8.0-preview.2 — 13 novembre 2026 · Seconda iterazione GUI (da validare)

- Analizzati due **screenshot reali** della preview.1; l'utente ha confermato build e test superati ma ha respinto la resa grafica.
- **Riorganizzata la finestra principale in due aree**: sidebar unica con gruppi, ricerca e snippet; editor dedicato con ampio spazio di lavoro. Eliminata la terza colonna e ridotti i separatori.
- Sostituiti i grandi expander neri con piccoli menu a comparsa per creare e gestire i gruppi e per testare il matching; tutte le azioni precedenti rimangono raggiungibili.
- Nome e trigger dell'editor affiancati, stato Abilitato vicino al titolo, salva/elimina/anteprima in una barra finale essenziale; snippet list con titolo, trigger e prima riga del contenuto.
- Picker senza titolo duplicato nella zona contenuti, risultati più informativi e footer compatto. Leggero ritocco della palette chiara/scura.
- All'inizializzazione viene selezionato il primo snippet disponibile, se presente: non compare una bozza vuota davanti a una libreria popolata.
- Aggiornati piano GUI, controlli statici, script di validazione, audit visivo e immagini di riferimento; README lasciato essenziale.
- **Verificato:** XAML/XML, traduzioni, test statici e integrità del pacchetto. **Da verificare:** build .NET, xUnit e screenshot reali preview.2. Nessun output globale attivato.

## v3.8.0-preview.1 — 13 novembre 2026 · Prima implementazione redesign (da validare)

- Risorse di tema semantiche Light/Dark, stili condivisi per testi, pulsanti, campi e liste; rimosso il grande header blu.
- Barra superiore compatta, menu azioni archivio e flyout tema/lingua; pannelli con superficie unica e splitter ridimensionabili.
- Gruppi gestiti tramite expander, ricerca snippet più evidente; editor flessibile, anteprima collassabile e barra di stato monoriga leggibile.
- Picker più compatto, altezza guidata dai risultati entro un massimo e copia manuale invariata.
- Tradotte tre nuove etichette in cinque lingue; aggiunto un controllo statico dedicato al redesign; aggiornata la verifica della versione su Windows.
- Nessuna modifica ai dati SQLite, al motore di trigger o all'adapter Windows disconnesso.
- **NON VERIFICATO:** compilazione della preview, xUnit, resa visiva reale, clipping e comportamento dei flyout; v3.7 resta baseline nota funzionante.

## Non rilasciato — Piano GUI v3.8 (13 novembre 2026)

**Solo documentazione, nessuna modifica al codice applicativo e nessuna nuova release:** README ridotto a presentazione/avvio/compilazione; roadmap riscritta per gate G0–G7; aggiunti piano redesign, design system, matrice di accettazione e riferimento alle due schermate reali v3.7; stato corrente e handoff allineati alla validazione confermata. `VERSION.txt` resta `Ristretto 3.7.0`.

## v3.7 — 13 novembre 2026 · Baseline validata su Windows

- Test dei surrogate UTF-16 isolati convertiti in due `[Fact]` distinti, senza stringhe malformate in `InlineData`.
- Aggiunti `VERSION.txt` e verifica preventiva versione/sorgenti in `validate.cmd` per evitare esecuzioni da estrazioni obsolete.
- **Confermato dall'utente:** compilazione e suite di test automatici della v3.7 superate su Windows. Non equivale a test GUI o macOS/Linux superati.

## v3.6 — 13 novembre 2026 · Correzioni al test Unicode e loader Avalonia

- I valori UTF-16 non validi vengono costruiti a runtime nei test; aggiunte prove per surrogate isolati/misti e `U+FFFD` valido.
- Aggiunto costruttore pubblico senza parametri a `PickerWindow`, mantenendo quello con ViewModel.
- Nessuna modifica alla validazione del backend Win32, ai dati SQLite o al matcher.

## v3.5 — 13 novembre 2026 · Ristretto e correzioni compilazione

- Nome pubblico **Ristretto**, solution `Ristretto.sln`, eseguibile `Ristretto.exe`, ZIP sempre con cartella principale `Ristretto/`.
- Risolto `AVLN2000` nell'editor XAML; sostituiti eventi obsoleti `Checked/Unchecked`; corrette asserzioni xUnit per gli analyzer.
- Preservati namespace interni `TextExpander.*`, percorso SQLite storico e nomi di coordinamento per retrocompatibilità del profilo.

## v3.4 — 13 novembre 2026 · Spike Windows isolato

- Preparato `UnicodeOutputPlan` e adapter Win32 `SendInput` con controlli su target, UTF-16 e modificatori e risultati non sovrastimati.
- Test con API simulate. **Nessun collegamento alla GUI, nessun hook o espansione globale.**

## v3.3 — 13 novembre 2026 · Revisione selettiva importazione

- Selezione per singolo snippet, filtro e paginazione nella preview import.
- Segnalazione collisioni NFC, importazione condizionata a revisione SQLite, blocco preview obsolete e doppio commit.

## v3.2 — 13 novembre 2026 · Importatori Beeftext/Espanso

- Importazione non esecutiva Beeftext v10 JSON ed Espanso YAML con anteprima, avvisi e snippet esterni disattivati.
- Parser YAML mediante YamlDotNet, aggiornamento GUI e test predisposti.

## v3.1 — 13 novembre 2026 · Gruppi e portabilità

- Rinomina, abilitazione, disabilitazione ed eliminazione di soli gruppi vuoti; protezione delle bozze non salvate.
- Import/export JSON UTF-8 additivo e transazionale, distinto da backup completo SQLite.

## v3.0 — 13 novembre 2026 · Picker locale e tray

- Ricerca locale Unicode, picker con copia manuale, tray e istanza unica.
- Verifica `PRAGMA synchronous=FULL` per le connessioni SQLite scrivibili.

## v2.9 — 13 novembre 2026 · Prima Vertical Slice

- Prima solution C#/.NET 10 + Avalonia (Core, Application, Infrastructure, Desktop e test xUnit).
- CRUD libreria SQLite, backup/ripristino, gruppi/snippet, matcher locale, GUI e localizzazione base.

## Prima del codice (v1.0–v2.8) — Studio e decisioni

- Analisi Beeftext, Espanso, XMiLib, emojilib e concorrenza; requisiti GUI, Unicode/IME, sicurezza, adapter multipiattaforma e Vertical Slice Architecture.
- **Decisione approvata:** SQLite locale come archivio e formato di backup completo; JSON solo portabilità.
- Lo studio è conservato nei documenti `docs/`, `research/` e `data/`, senza duplicati `archive/`.
