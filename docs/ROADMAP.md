# Roadmap unica — Ristretto

**Revisione:** 15 novembre 2026 · **Baseline funzionale:** 3.9.0-alpha.20.28.

**Obiettivo:** scrivere un trigger come `;firma` in un'applicazione compatibile e ottenere automaticamente lo snippet. Il picker e le hotkey manuali restano disponibili.

Questa è la fonte delle attività future. [Stato corrente](CURRENT_STATE.md) descrive ciò che esiste; [changelog](../CHANGELOG.md) e rapporti conservano il lavoro svolto. Le versioni future indicate sotto sono candidate, non release già completate.

## 1. Punto di partenza

| Area | Evidenza disponibile | Ancora aperto |
| --- | --- | --- |
| Build e controlli | Gate CI locale e da ZIP nuovo: 870 xUnit, 32 Node, 23 regressioni degli strumenti, 10 CLI e contratto .NET→Node passati | Audit dipendenze online e attestazione dell'esecuzione remota della CI |
| Libreria e dati | SQLite schema 2, migrazione da 1, backup e import/export verificati su dati sintetici | Recupero dopo interruzioni e aggiornamento completo osservato sul desktop |
| Picker e hotkey per snippet | Flussi base in Blocco note e nuova conferma hotkey dopo la 20.27; 21 casi nativi con collisioni fra processi passati in 20.28 | Orchestrazione Desktop.App, appunti, focus, durata e altri editor |
| Form Registra hotkey | 20.27: 22 casi UI headless e due di stato aggiuntivi; Shift+Tab e disposizione dei pulsanti corretti | Acquisizione e sospensione/ripristino reali sul desktop |
| R5 | Lettore Edit, WinEvent e anteprima di trigger disponibili; test inclusi nella suite | Provenienza dell'input, IME e capacità di sostituzione esterna non qualificate |
| Espansione automatica esterna | Contratti e laboratori controllati disponibili | Adapter di prodotto da qualificare e implementare |
| Distribuzione | ZIP sorgenti e manifest | Pacchetto eseguibile, licenza, notices e aggiornamento qualificato |
| Immagini | 32 anteprime Preview 9, Alpha 7 | Screenshot del prodotto corrente, inclusa la form Registra… |

I risultati di laboratorio, un test unitario e una conferma manuale hanno perimetri diversi. `NOT_RUN` non equivale a `PASS`.

## 2. Ordine dei prossimi incrementi

| Ordine | Incremento candidato | Passi | Risultato richiesto |
| --- | --- | --- | --- |
| 1 | **Prossimo incremento — Flusso completo delle hotkey** | Completare R2.01/R2.08/R2.09 dopo UI headless 20.27 e registrazioni native 20.28; correzioni R2.07 | Save/import e notifiche pendenti gestiti nella vera app, tastiera/DPI provati, assegnazioni salvate ripristinate |
| 2 | **Affidabilità del flusso manuale** | R2.02–R2.06, R4.03–R4.04 | Picker, dati, appunti e ciclo di vita verificati con una matrice ripetibile |
| 3 | **R5 — Decisione sulla provenienza** | R5.01–R5.05 | Un target qualificabile con evidenze, oppure un esito negativo motivato |
| 4 | **R6 — Primo automatismo opt-in** | R6.01–R6.06, solo dopo R5 positivo | Espansione e sostituzione verificate su un solo target dichiarato |
| Ramo parallelo | **Alpha Windows distribuibile** | R1 residui e R4 | Binari, aggiornamento, recupero, licenze e misure |
| Successivi | **Snippet avanzati e altri OS** | R7, R8 | Funzioni aggiuntive senza alterare gli snippet esistenti |

R5 può avanzare con esperimenti isolati mentre si qualifica R2. L'installer completo non blocca gli esperimenti; dati e recupero devono però essere affidabili prima dell'uso del nuovo automatismo. R3 resta sospesa e non blocca gli altri rami.

## 3. R1 — Fondamenta e verifiche riproducibili

**Stato:** baseline C# validata. Conservare il lavoro già completato; chiudere le evidenze operative rimaste.

- [x] **R1.01 — Profilo unico.** Directory condivisa da storage e singola istanza, `--data-dir`, controllo senza GUI e percorso normale conservato.
- [ ] **R1.02 — Isolamento completo.** Verificare alias junction/symlink, due profili paralleli, doppio avvio e rilascio dei lock dopo errore/uscita. Usare cartelle sintetiche; i conflitti globali delle hotkey confluiscono in R2.09. **Chiusura:** nessun database condiviso attraverso due proprietari e nessuna attivazione dell'istanza sbagliata.
- [ ] **R1.03 — Versione e provenienza.** La versione centralizzata esiste già. Collegare la prossima consegna a revisione/tag effettivi, stato delle modifiche e hash dei sorgenti compilati; distinguere revisione solo documentale e incremento funzionale. **Chiusura:** il pacchetto è riconducibile al codice validato, senza attribuire al solo HEAD le modifiche non committate.
- [x] **R1.04 — Strumenti.** Audit e packaging C# sostituiscono Python; rilevamento Node mediante esecuzione reale.
- [x] **R1.05 — Gate distinti.** SOURCE, CORE, CI e FULL con PASS/FAIL/NOT_RUN; prove desktop separate dai gate senza GUI.
- [ ] **R1.06 — CI remota.** Il workflow esiste. Eseguirlo sul repository di riferimento, raccogliere rapporti e artifact, controllare fallimenti e dipendenze mancanti. **Chiusura:** esecuzione remota attestata; il PASS del comando CI locale resta un'evidenza distinta.
- [ ] **R1.07 — Licenza e dipendenze.** Inventariare NuGet, font, icone e asset; raccogliere la scelta della licenza del progetto, preparare LICENSE, notices e distinta dei componenti. **Chiusura:** obblighi documentati prima della pubblicazione pubblica, senza dedurre la licenza dal nome della libreria.
- [ ] **R1.08 — Audit online.** Eseguire l'audit delle dipendenze da una sorgente disponibile, registrare versioni, data ed esiti; risolvere o motivare i problemi trovati. **Chiusura:** restore locale e audit vulnerabilità sono riportati separatamente.

## 4. R2 — Libreria, picker e hotkey Windows

**Priorità immediata:** R2.01 → R2.08 → R2.09. La prova positiva in Blocco note non chiude l'intera milestone.

**Avanzamento 20.27–20.28:** prove UI headless, salvataggio SQLite e disposizione in cinque lingue/due temi; corretti Shift+Tab e pulsanti fuori dal bordo. Aggiunti 21 casi con worker/coordinatore e registrazioni Windows reali, incluse collisioni fra processi e rilascio dopo terminazione del figlio. [UI headless](ALPHA20_27_CAPTURE_INTEGRATION.md) · [ricevute native](ALPHA20_28_NATIVE_HOTKEY_REGISTRATIONS.md). R2.08/R2.06 restano aperte per hardware/DPI; R2.09 per orchestrazione completa, flussi UI/import e notifiche pendenti.

- [ ] **R2.01 — Ambiente e matrice.** La [matrice iniziale è predisposta](WINDOWS_QUALIFICATION_PLAN.md); ambiente, fixture ed esecuzione sono ancora aperti. Preparare un profilo nuovo, snippet sintetici e un documento esterno; annotare versione app/Windows/editor, layout, tema e DPI. Per ciascun caso registrare atteso, osservato, metodo e stato. **Chiusura:** una persona diversa può ripetere i passaggi senza usare la libreria personale.
- [ ] **R2.08 — Form Registra hotkey.** Provare `ALT + CTRL + SHIFT + B`, modificatori sinistri/destri, ripetizioni, tasti non supportati e più tasti principali. Verificare che il display resti leggibile al rilascio e corrisponda alla combinazione applicata. Provare Usa combinazione, Annulla, Esc, chiusura, rimozione, Tab/Shift+Tab e perdita di focus. **Chiusura:** solo la conferma aggiorna la bozza; solo Salva persiste; nessun gesto incompleto produce una hotkey diversa da quella mostrata.
- [ ] **R2.09 — Registrazioni reali degli snippet.** Acquisire anche una combinazione già assegnata; provare due snippet, collisione con picker/altre app/altro profilo, disabilitazione di snippet/gruppo, eliminazione e uscita. Ripetere apertura/chiusura della form, inclusi annullamento e reload pendente. **Chiusura:** nessun inserimento durante la cattura; al termine restano attive soltanto le assegnazioni salvate e disponibili, con messaggi corretti per i conflitti.
- [ ] **R2.02 — UI e ciclo di vita.** Verificare avvio, tray, apertura libreria/picker, doppio avvio, ricerca, frecce e Invio sul selezionato. Provare elenco vuoto, caricamento lento, doppio comando e riapertura. **Chiusura:** nessuna selezione o richiesta della sessione precedente viene eseguita in quella nuova.
- [ ] **R2.03 — Appunti effettivi.** Provare Copia e Inserisci con multilinea, CRLF/LF, NFC/NFD, emoji ZWJ, RTL e CJK. Forzare appunti occupati o cambiati, scrittura lenta e comando concorrente. **Chiusura:** testo corretto o rifiuto esplicito, nessun incollamento accodato e nessun ripristino alla cieca degli appunti.
- [ ] **R2.04 — Destinatario controllato.** Collegare il percorso reale del prodotto a un Edit posseduto da un laboratorio e verificare contenuto/cursore prima e dopo. **Chiusura:** esito ripetibile con ricevuta osservata, distinto dal supporto di un editor esterno.
- [ ] **R2.05 — Editor esterno e focus.** Partire da Blocco note e registrare casi con caret/selezione, cambio finestra/campo, chiusura del target, modificatori, campi di sola lettura e protetti sintetici. Annotare anche i rifiuti verso app elevate. **Chiusura:** matrice per editor/versione e metodo; il numero di eventi SendInput accettati non è conteggiato come testo verificato.
- [ ] **R2.06 — Resa, accessibilità e screenshot.** Verificare cinque lingue, due temi, dimensioni minime, DPI 100/125/150/200%, focus e nomi accessibili. Aggiornare il tool di anteprima per la nuova form usando dati sintetici e registrazioni native disattivate; acquisire immagini correnti di libreria, picker e cattura. **Chiusura:** immagini coerenti con i comandi effettivi e nessuna etichetta o azione tagliata; README aggiornato con provenienza delle immagini.
- [ ] **R2.07 — Correzioni e riconferma.** Correggere i difetti dimostrati, aggiungere regressioni utili e ripetere i casi coinvolti e i gate pertinenti. **Chiusura:** nessun caso bloccante aperto nelle funzioni dichiarate qualificate.

**Consegna R2:** matrice desktop, rapporti delle correzioni e screenshot attuali. La sola crescita del numero di test non chiude questa milestone.

## 5. R3 — VS Code, ramo sospeso

Riprendere solo per scelta esplicita oppure dopo una decisione R5 che richieda questo target.

- [ ] **R3.01 — Avvio di base.** Diagnosticare il precedente crash `Code.exe / 0x80000003` in una sessione normale, prima del runner e dell'estensione. **Atteso:** causa documentata o avvio stabile, senza cicli automatici di riavvio.
- [ ] **R3.02 — Host isolato.** Verificare preparazione, profilo ed estensioni separati, senza installazioni nel profilo personale.
- [ ] **R3.03 — Suite host.** Eseguire i 14 scenari già scritti e richiedere rapporto della suite e uscita del processo.
- [ ] **R3.04 — Flusso manuale.** Provare comando, Undo/Redo, focus, selezione/versione mutate, sola lettura e workspace non attendibile su documenti sintetici.
- [ ] **R3.05 — Export reale.** Ripetere l'uso dell'export di produzione nel vero host; il contratto .NET→Node è già validato e non dimostra da solo questo passaggio.
- [ ] **R3.06 — Compatibilità.** Dichiarare soltanto versioni e casi osservati; documentare attivazione e disattivazione.

**Chiusura:** host e casi manuali qualificati. I 32 Node passati restano un perimetro diverso.

## 6. R4 — Alpha Windows affidabile e distribuibile

Può avanzare in parallelo a R5. La consegna pubblica dipende da R1.07 e dalla chiusura dei flussi manuali dichiarati in R2.

- [ ] **R4.01 — Misure.** Misurare startup, indice, ricerca e apertura con 100 e 10.000 snippet; 50.000 come stress esplorativo. Separare tempi del motore da dispatcher/rendering, cold/warm, percentili e memoria. **Atteso:** rapporto su macchina e dataset dichiarati.
- [ ] **R4.02 — Budget.** Confrontare le misure con i budget candidati, scegliere soglie sulla macchina di riferimento e correggere solo i colli di bottiglia dimostrati. **Chiusura:** soglie motivate e regressioni misurabili, non latenze GUI dedotte dagli xUnit.
- [ ] **R4.03 — Aggiornamento e recupero.** Provare passaggio schema 1→2, backup/restauro con hotkey, JSON legacy, conflitti di import, file corrotti, directory indisponibili e interruzione durante le operazioni. Ripetere l'aggiornamento da cartella nuova e con profilo assoluto personalizzato. **Chiusura:** libreria/impostazioni conservate, sorgente del backup intatta e recupero verificato dopo errore.
- [ ] **R4.04 — Durata e accessibilità.** Ripetere tray, cattura hotkey, ricerca, reload e sleep/wake per sessioni progressive; osservare handle, thread, memoria e focus. **Chiusura:** nessuna crescita persistente o hotkey rimasta occupata, comandi utilizzabili da tastiera.
- [ ] **R4.05 — Binari.** Scegliere architettura e runtime dipendente/autocontenuto, produrre un pacchetto e provarlo su ambiente pulito. Definire percorsi dei dati, collegamenti ed eventuale avvio automatico. **Chiusura:** l'utente può avviare senza compilare; requisiti e procedura sono verificati, senza presentare lo ZIP sorgenti come installer.
- [ ] **R4.06 — Release.** Collegare tag, versioni, hash, licenze/notices e rapporti; verificare installazione, aggiornamento, disinstallazione e ritorno con backup compatibile. Aggiornare README e matrice delle capacità. **Chiusura:** sorgenti e binari corrispondono alle evidenze e nessuna funzione sospesa viene dichiarata disponibile.

L'aggiornamento automatico richiede una decisione separata su origine dei pacchetti, integrità, recupero e consenso; non è incluso implicitamente nella creazione di un installer.

## 7. R5 — Provenienza dell'input e scelta del primo target

**Obiettivo:** decidere se un'applicazione concreta fornisce garanzie sufficienti per R6. Una lettura o un delta WinEvent plausibile non autorizzano una modifica.

- [ ] **R5.01 — Contratto di qualificazione.** Elencare prove richieste per testo committato, origine ammessa, conclusione IME, identità di finestra/campo/documento, revisione, caret/range e stato protetto/sola lettura. Riutilizzare il [gate di ammissione](F3_ADMISSION_GATE.md). **Atteso:** tabella garanzia → sorgente → casi che la invalidano.
- [ ] **R5.02 — Candidati e API.** Valutare le capacità effettive dei target Windows e di un eventuale adapter cooperante; consultare documentazione ufficiale aggiornata delle API studiate. Il picker funzionante in Blocco note non qualifica automaticamente Blocco note per l'automatismo. **Atteso:** candidati scartati o mantenuti con motivazione, senza estendere a tutta Windows un risultato locale.
- [ ] **R5.03 — Esperimenti di provenienza in sola lettura.** Produrre lo stesso testo mediante digitazione, paste, IME, modifiche programmatiche e Undo/Redo. Confrontare sequenze e coalescenza degli eventi; aggiungere Unicode, selezione, timeout e riavvio della sessione. **Chiusura:** ogni caso è classificato con evidenza oppure resta Unknown; un hook o un evento VALUECHANGE da solo non viene promosso a prova.
- [ ] **R5.04 — Contesto e sostituzione.** Forzare cambio focus/documento/caret, chiusura target, revisione concorrente, stato protetto e interruzione. Studiare se il target consente confronto e sostituzione nello stesso contesto con ricevuta dell'esito. **Atteso:** matrice dei rifiuti e delle capacità mancanti, senza scrivere su documenti personali.
- [ ] **R5.05 — Decisione finale.** Registrare target/versioni, prove superate, limiti e costo dell'adapter. Esiti ammessi: qualificabile per R6; serve una cooperazione diversa; non qualificabile con le API provate. **Chiusura:** decisione esplicita. Un esito negativo può chiudere lo studio, ma lascia R6 bloccata.

**Consegna R5:** dossier delle evidenze e decisione sull'adapter. Il laboratorio continua a mostrare contatori e stati, senza log del testo e senza collegamento prematuro all'inserimento di prodotto.

## 8. R6 — Prima espansione automatica opt-in

**Prerequisiti:** R5 positivo, flusso manuale R2 stabile e recupero dati R4.03 qualificato. Il rilascio pubblico richiede anche R4.06.

- [ ] **R6.01 — Attivazione e pausa.** Limitare l'automatismo a un target qualificato e a un consenso esplicito; prevedere pausa immediata, esclusioni e stato comprensibile. Nessuna attivazione globale per app sconosciute.
- [ ] **R6.02 — Adapter condizionale.** Preparare il range dal documento verificato, ricontrollare identità/revisione/testo/caret e applicare la sostituzione con l'API qualificata. Vietata la cancellazione ricavata soltanto dalla lunghezza UTF-16 del trigger.
- [ ] **R6.03 — Riconoscimento ed esito.** Collegare trigger e delimitatori all'adapter, preservando contenuto, Unicode e politica concordata sui separatori. Verificare il testo risultante; gestire Undo/Redo quando il target lo consente.
- [ ] **R6.04 — Fallimenti e concorrenza.** Provare IME attivo, paste non ammesso, selezione, modifiche/focus concorrenti, revoca del consenso, timeout, chiusura e risposta parziale. **Atteso:** nessuna modifica quando il gate fallisce e nessun retry dopo effetto incerto.
- [ ] **R6.05 — Integrazione progressiva.** Passare da osservazione a documento sintetico e poi a prova opt-in delimitata. Conservare rapporti per caso; il picker e le hotkey manuali devono restare utilizzabili.
- [ ] **R6.06 — Compatibilità pubblica.** Dichiarare esattamente target/versione/casi supportati e limiti; mostrare pausa o indisponibilità negli altri casi. **Chiusura:** almeno un flusso `;firma` → snippet verificato realmente, con tutti i casi bloccanti concordati superati.

Un tentativo inviato ma non verificato non viene contato come espansione automatica qualificata.

## 9. R7 — Snippet avanzati

Dopo una baseline manuale stabile. Le funzioni che richiedono posizionamento del caret dipendono dalle capacità dell'adapter.

- [ ] **R7.01 — Sintassi e compatibilità.** Definire template opt-in; gli snippet esistenti restano letterali, anche se contengono simboli simili alla futura sintassi.
- [ ] **R7.02 — Variabili locali.** Introdurre una funzione per volta, partendo da valori deterministici come data/ora; chiarire lingua, fuso, escaping, anteprima e gestione degli errori. Eventuali letture degli appunti devono essere esplicite.
- [ ] **R7.03 — Form e placeholder.** Definire campi obbligatori/default, navigazione e annullamento senza effetti. Il dialogo Registra hotkey già esistente non costituisce questa funzione.
- [ ] **R7.04 — Caret e distribuzione.** Supportare punti di inserimento solo con un adapter che ne verifichi il range; aggiornare schema/JSON, migrazioni e documentazione soltanto quando il contratto è definito. Nessuna esecuzione implicita di shell o script.

## 10. R8 — Linux e macOS

La portabilità di Avalonia non prova la disponibilità delle funzioni native.

- [ ] **R8.01 — Libreria e dati.** Compilare e provare avvio, GUI, import/export, profili, backup e aggiornamento sui sistemi scelti.
- [ ] **R8.02 — Hotkey e inserimento manuale.** Qualificare API, permessi e limiti per sistema/sessione; distinguere macOS, X11 e Wayland. Dichiarare indisponibili le capacità non dimostrate.
- [ ] **R8.03 — Automatismo.** Ripetere il processo R5/R6 per ciascun adapter, senza riutilizzare automaticamente le garanzie Windows.
- [ ] **R8.04 — Pacchetti e supporto.** Qualificare installazione, firma dove necessaria, aggiornamento, layout, temi e matrice di versioni.

## 11. Regole di chiusura e aggiornamento del piano

Ogni incremento funzionale riceve una nuova versione e un rapporto con perimetro, ambiente, hash, PASS/FAIL/NOT_RUN e limiti. Una revisione dei soli documenti mantiene la versione del codice e dichiara i controlli effettivamente ripetuti.

Prima della consegna: gate pertinenti, manifest, estrazione in cartella nuova e verifica della corrispondenza dei sorgenti. Ripetere build/test dell'app quando cambia il codice; per soli documenti eseguire gli audit documentali e degli strumenti.

Chiudere un task solo con la sua evidenza. Le decisioni ancora da prendere sono: target automatico, licenza del progetto, forma del pacchetto eseguibile e soglie di prestazione. Vanno preparate con alternative e prove; non fermano il lavoro indipendente già autorizzato.
