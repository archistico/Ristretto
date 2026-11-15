# Roadmap unica — Ristretto

**Versione sorgente corrente:** `3.9.0-alpha.20.19.3` (15 novembre 2026). **Piano autorevole R1–R8**, con stati ed evidenze correnti in [CURRENT_STATE.md](CURRENT_STATE.md). I rapporti delle Alpha precedenti sono storici, non prove della build attuale.

**Priorità presente: stabilizzare il gate `CORE` Windows (Alpha 20.19.3), poi R5 → R6.** L'utente ha verificato nella Alpha 20.19.2 che restore, solution, xUnit e quattro tool passano; il blocco residuo era la decodifica UTF-8 di `stderr` in un test Python del vero entry point. La nuova correzione cattura byte e tratta i messaggi diagnostici non UTF-8 come errori attesi, senza abbassare la verifica UTF-8 dei successi. [Dettagli](ALPHA20_19_3_STARTUP_CLI_ENCODING.md). Il picker/Notepad Unicode ha avuto una precedente prova positiva confermata dall'utente. R3 VS Code resta facoltativo, non parte del percorso prioritario R5–R6.

**Gate Windows:** correggere il test `test_exact_multiline_unicode_matches_when_saved` e mantenere la verifica `FAIL` per i fine riga corrotti. Correzione applicata nella Alpha 20.19.1; compilazione .NET e nuovi xUnit ancora da eseguire. [Dettagli](ALPHA20_19_1_CRLF_REGRESSION_FIX.md).

**Sequenza immediata:** (1) controllare la nuova barriera con xUnit e prova opt-in su Windows; (2) identificare un canale che fornisca *testo Unicode committato* associato a un destinatario verificabile; (3) qualificare revisione, caret, IME e modifica condizionale su un target specifico; (4) solo allora aprire R6 con espansione disattivabile. Le prove R2 desktop ancora aperte e la distribuzione R4 proseguono indipendentemente. R3 VS Code rimane un ramo facoltativo. Non dedurre la capacità di espandere dai soli WinEvent o keycode.

## 1. Punto di partenza

| Area | Evidenza disponibile | Verifica ancora aperta |
| --- | --- | --- |
| Solution e quattro strumenti | Build Release e **588 xUnit**, inclusi **50 ViewModel**, passati anche da ZIP estratto in cartella nuova | Nuove verifiche per ogni modifica funzionale |
| Ricerca e picker | Indice Unicode, caricamento annullabile, richieste Inserisci monouso e comandi appunti coordinati implementati | Dispatcher/finestre, hotkey, clipboard e incolla effettivo sul desktop |
| Laboratori Win32 | Precedente esecuzione di **31 scenari**, smoke cooperante e remoto superati | Sono controlli posseduti; non attestano editor esterni |
| Adapter VS Code manuale | Precedenti **32 test Node** e contratto .NET→Node passati; suite host di 14 scenari scritta | Nessuno scenario host eseguito; renderer non avviato e crash `Code.exe` segnalato |
| Espansione automatica in altre app | Motore di riconoscimento e contratti di ammissione disponibili | Adapter di prodotto **non implementato e non qualificato** |
| Prestazioni, distribuzione e altri OS | Specifiche e proposte conservate | Misure GUI, pacchetti eseguibili qualificati e capacità native per OS |

Riferimenti: [ultima validazione](ALPHA20_5_STARTUP_PROFILES.md), [baseline nativa precedente](ALPHA20_3_VALIDATION.md), [host VS Code e crash](ALPHA20_3_VSCODE_HOST_TESTS.md).

## 2. Direzione e dipendenze

Prima piattaforma: **Windows**. Primo risultato di prodotto: **alpha Windows con libreria e picker verificati sul desktop**. Obiettivo successivo: **prima espansione automatica qualificata in un editor esplicitamente supportato**.

| Milestone | Priorità | Risultato | Dipende da |
| --- | --- | --- | --- |
| **R1** | P0, prossima | Profili isolati, versioni e validazione riproducibile | Baseline attuale |
| **R2** | P0 | Picker Windows verificato con UI e appunti reali | R1 |
| **R3** | P3, facoltativa | Adapter manuale nel vero host VS Code | R1 e avvio normale di VS Code funzionante |
| **R4** | P0 | Prima alpha Windows utilizzabile, misurata e distribuibile | R2; R3 per qualificare anche l'estensione |
| **R5** | P0, percorso prioritario | Scelta dell'adapter per il primo automatismo | R2 e qualificazione del candidato; R3 se il candidato è VS Code |
| **R6** | P0, condizionata | Espansione automatica su un target qualificato | R4 e risultato positivo di R5 |
| **R7** | P2 | Template e form, per incrementi compatibili | R4; R6 solo per le funzioni che richiedono un adapter automatico |
| **R8** | P2 | Altri OS e distribuzione per capacità verificate | R4; prove specifiche per ciascun OS |

R1 e le verifiche senza GUI possono avanzare senza riaprire VS Code. Un blocco di R3 non impedisce di qualificare il picker in Notepad o di lavorare su dati, misure e template. R6 si apre solo dopo la decisione tecnica di R5. Le date si assegnano dopo le prime qualificazioni desktop; il numero di test non è una misura di completezza funzionale.

## 3. R1 — Profili isolati e fondamenta di consegna

**Obiettivo:** poter avviare e validare Ristretto senza usare per errore il profilo personale, con una provenienza chiara di sorgenti ed esiti.

- [x] **R1.01 — Profilo di avvio unico.** Risoluzione centralizzata in `ApplicationProfile`, percorso precedente conservato, `--data-dir` e diagnostica `--check-profile`; opzioni/profili indisponibili rifiutati senza fallback. [Implementazione e test Alpha 20.5](ALPHA20_5_STARTUP_PROFILES.md).
- [ ] **R1.02 — Isolamento completo.** Usare lo stesso profilo per database, impostazioni e `SingleInstanceHost`; verificare mutex/pipe per directory distinte, doppio avvio e conflitti hotkey, senza cambiare la configurazione dell'istanza personale. Fixture sintetiche e cartella nuova per ogni prova.
  **Parziale Alpha 20.5:** storage, backup, mutex/pipe e doppio avvio verificati con fixture sintetiche; salvataggio hotkey corretto e persistente. Restano conflitti globali sul desktop e prova con alias symlink/junction. Nessun dato personale usato.
- [ ] **R1.03 — Versione e provenienza.** Identificare il repository sorgente di questa estrazione e collegare revisioni/tag alle consegne. Centralizzare la versione usata da progetti, tool e script; assegnare una nuova versione a ogni prossima consegna funzionale e conservare la baseline attuale. **Parziale Alpha 20.6:** versione canonica in `Directory.Build.props` e fingerprint SHA-256; collegamento al repository e tag ancora aperto.
- [x] **R1.04 — Rilevamento tool (implementato, prova Windows aperta).** Correggere il rilevamento di Python/Node/npm: nella sessione precedente gli script li saltavano pur essendo eseguibili direttamente. Verificare avvio e versione del binario, includendo percorsi con spazi e installazioni Scoop.
- [x] **R1.05 — Gate espliciti (implementati, prova Windows aperta).** Distinguere validazione core e completa. Produrre un riepilogo `PASS`/`FAIL`/`NOT_RUN` per build, xUnit, audit Python, Node, contratto export e native; un requisito mancante fa fallire il relativo gate completo. Separare restore online, restore locale e audit vulnerabilità non eseguito.
- [ ] **R1.06 — Automazione (workflow implementato, integrazione aperta).** `.github/workflows/ci.yml` include Linux source e Windows headless `ci` con Node/contratto; desktop/native restano esclusi. I rapporti contengono versione, hash sorgenti e commit *solo se disponibile*. Mancano il collegamento al repository reale e la prima esecuzione verificata su GitHub Actions. [Dettagli](ALPHA20_7_CI.md).
- [ ] **R1.07 — Preparazione al rilascio.** Inventariare dipendenze, asset e licenze; raccogliere la decisione sulla licenza del progetto e predisporre notices/SBOM. La scelta della licenza è necessaria prima della pubblicazione pubblica.

**Consegna:** profilo di prova isolato, script affidabili, rapporto strutturato e pacchetto sorgenti versionato.

**Chiusura:** due profili non condividono dati o attivazione; un ZIP nuovo supera i gate dichiarati; nessun passaggio saltato viene presentato come eseguito. Il percorso dati normale conserva gli snippet esistenti.

## 4. R2 — Qualificazione reale del picker Windows

**Obiettivo:** dimostrare i flussi di utilizzo sul desktop, oltre ai test con callback simulati.

- [x] **R2.01 — Matrice e ambiente (specifica pronta, esecuzione aperta).** Fixture, risultati attesi e ambiente Windows/app/DPI/layout in [matrice dedicata](R2_PICKER_WINDOWS_ACCEPTANCE.md); da popolare con esiti della prima sessione desktop usando profili isolati. Non equivale all'accettazione R2.
- [x] **P0 Alpha 20.15 — regressione Invio:** fix confermato dall’utente: compilazione e test passati, Invio dal picker nuovamente funzionante in Notepad. Conservare la regressione in suite e matrice R2.
- [ ] **R2.02 — UI e ciclo di vita (regressione hotkey ripetuta Alpha 20.8 e annullamento pre-dispatch Alpha 20.14; prove desktop aperte).** Provare avvio, tray, hotkey configurabile/disattivata/in conflitto, secondo avvio, apertura/chiusura, tastiera, ricerca, selezione e ricaricamenti concorrenti. Aggiungere integrazione sul dispatcher dove rende ripetibile una regressione concreta.
- [ ] **R2.03 — Clipboard effettiva (verifiche contenuto/seq. Alpha 20.10, preflight Alpha 20.13 e annullamento atomico pre-dispatch Alpha 20.14; runtime aperto).** Provare Copia con Unicode, NFD, emoji ZWJ, RTL e multilinea; clipboard occupata, scrittura lenta, doppio comando, selezione diversa, chiusura/riapertura ed errori. Verificare che non parta un comando accodato dopo il rilascio dell'attesa.
- [ ] **R2.04 — Destinatario controllato.** Collegare il percorso reale Inserisci a un target sintetico in processo figlio posseduto dal laboratorio e verificare testo/range risultanti. Tenere distinto questo esito dal supporto a Notepad o altri editor.
- [ ] **R2.05 — Editor esterno (prima prova Unicode Notepad Alpha 20.18 PASS; matrice desktop ancora aperta).**
  `prepare`, `seed`, `status`, `launch`, `verify` guidano una prova isolata del picker verso Notepad. Il profilo SQLite viene creato con `SqliteLibraryStore`; una sola classificazione read-only distingue documento invariato, esito corretto, doppio inserimento, posizione errata, differenza Unicode NFC/NFD e altre anomalie. La guida e il terminale usano l'italiano; fallire l'apertura di Notepad non fa perdere il token di sessione. **Evidenza aggiornata 15 novembre 2026:** primo documento sintetico Unicode salvato verificato `PASS` su Windows; l'utente conferma l'inserimento tramite picker. Restano aperti casi negativi focus, clipboard, destinatario, ripetibilità e altri editor. Il `PASS` non certifica espansione automatica. [Guida attuale](ALPHA20_18_NOTEPAD_DIAGNOSTICS.md).
- [ ] **R2.06 — Temi e lingue.** Verificare chiaro/scuro, cinque lingue, tastiera e fattori DPI rappresentativi; rigenerare le anteprime necessarie dopo l'integrazione e controllare azioni, focus e testi tagliati.
- [ ] **R2.07 — Correzione e riconferma.** Correggere le regressioni emerse, rieseguire i casi coinvolti e i gate pertinenti. Conservare la matrice distinguendo accettazione di `SendInput` e testo effettivamente osservato nell'editor.

**Consegna:** rapporto UI/clipboard/Notepad ripetibile, limiti di compatibilità aggiornati, sorgenti e test delle correzioni.

**Chiusura:** flussi principali osservati sul desktop, nessun invio al destinatario sbagliato nei casi previsti, nessun retry automatico dopo esito incerto e nessun crash irrisolto nei flussi qualificati. I test unitari da soli non chiudono R2.

## 5. R3 — VS Code manuale nel vero host (facoltativo, priorità subordinata a R5–R6)

**Obiettivo:** verificare l'adapter esistente e diagnosticare il blocco di avvio senza attribuirne prematuramente la causa all'estensione.

- [ ] **R3.01 — Avvio di base.** Verificare VS Code in una sessione desktop normale, prima senza runner/estensione. Distinguere ambiente, installazione, profilo portable e renderer; registrare il codice di errore e raccogliere diagnostica disponibile. Il precedente `0x80000003` non ha ancora una causa confermata.
- [ ] **R3.02 — Avvio isolato.** Verificare `-PrepareOnly`, profilo e directory estensioni nuovi, override portable e argomenti standard. Eseguire una prova controllata; al primo crash fermare quel percorso e diagnosticare, senza cicli di riavvio o opzioni grafiche sperimentali.
- [ ] **R3.03 — Suite host.** Eseguire i 14 scenari esistenti e correggere runner/test/adapter soltanto in base alle evidenze; richiedere sia esito del processo sia report della suite.
- [ ] **R3.04 — Casi manuali.** Verificare comando da palette, un Undo/Redo, focus di finestra, documento/selection/versione concorrenti, sola lettura, workspace non attendibile, file grandi e schemi non supportati. Usare documenti sintetici.
- [ ] **R3.05 — Dati reali di produzione.** Ripetere il contratto export .NET→Node con libreria sintetica creata dal servizio reale, poi l'uso di quell'export nel vero host. Documentare che il collegamento usa un export e non sincronizza SQLite automaticamente.
- [ ] **R3.06 — Compatibilità dichiarata.** Pubblicare app/OS/versioni realmente testati e istruzioni di attivazione, aggiornamento e disattivazione dell'adapter. L'intervallo nel manifest non equivale a una matrice interamente verificata.

**Consegna:** diagnosi del blocco oppure report del suo superamento, 14 esiti host e matrice manuale.

**Chiusura:** 14 scenari host passati e casi manuali verificati, senza modifiche al documento sbagliato. Se il renderer non parte, R3 resta aperta con causa/limite documentato; i test Node non la sostituiscono.

## 6. R4 — Prima alpha Windows utilizzabile

**Obiettivo:** una consegna Windows qualificata per libreria, ricerca, Copia e Inserisci manuale, con prestazioni e integrità osservate.

- [ ] **R4.01 — Prestazioni del picker.** Misurare costruzione indice, query, apertura, cancellazione e memoria con D1 (100 snippet), D10 (10.000) e casi avversi; D50 rimane stress esplorativo. Separare Core/indice, dispatcher e rendering, cold/warm; riportare P50/P95/P99 e allocazioni.
- [ ] **R4.02 — Soglie e correzioni.** Confrontare le misure con i budget candidati in [PERFORMANCE_TEST_PLAN.md](PERFORMANCE_TEST_PLAN.md), scegliere macchina/soglie di riferimento e intervenire sui colli di bottiglia dimostrati. Non promettere latenze GUI dai soli tempi dei test unitari.
- [ ] **R4.03 — Dati e recupero.** Qualificare import, export, backup e ripristino con modifiche concorrenti, file corrotti, directory non scrivibili e riavvio dopo interruzione. Verificare gli snippet risultanti e il percorso di recupero su profili sintetici.
- [ ] **R4.04 — Stabilità e accessibilità.** Eseguire sessioni prolungate di tray/ricerca/apertura/chiusura e controllare handle, thread e memoria. Verificare tab order, nomi accessibili, focus, contrasto e testi nelle cinque lingue.
- [ ] **R4.05 — Pacchetto eseguibile.** Scegliere runtime dipendente/autocontenuto, produrre un archivio binari ed eseguire installazione/avvio/aggiornamento su ambiente pulito. Dichiarare dove risiedono i dati: un archivio di binari non implica già modalità dati portabile.
- [ ] **R4.06 — Rilascio.** Completare licenza/notices, audit dipendenze e checklist. Consegnare sorgenti, binari, manifest, rapporti e matrice di capacità; qualificare l'estensione soltanto se R3 è chiusa.

**Consegna:** candidata alpha Windows per il flusso manuale, con binari utilizzabili e limiti espliciti.

**Chiusura:** R2 chiusa, integrità/recupero dimostrati, misure registrate e pacchetto funzionante su ambiente pulito. Questa milestone non dichiara l'espansione automatica globale.

## 7. R5 — Decisione tecnica sul primo automatismo

**Obiettivo:** scegliere un editor/adattatore realmente qualificabile prima di collegare il motore all'input di altre applicazioni.

- [ ] **R5.01 — Requisiti del target.** Riutilizzare [F3_ADMISSION_GATE.md](F3_ADMISSION_GATE.md) e [ALPHA17_EXTERNAL_ADAPTER_CAPABILITIES.md](ALPHA17_EXTERNAL_ADAPTER_CAPABILITIES.md): input committato, identità/revisione, focus, stato protetto/sola lettura, IME, range Unicode, sostituzione condizionale ed esito.
- [ ] **R5.02 — Candidati e fonti.** Esaminare API/documentazione ufficiale aggiornata del candidato. VS Code è un candidato grazie all'adapter manuale, ma gli eventi di modifica del documento non dimostrano da soli digitazione umana o conclusione IME.
- [ ] **R5.03 — Spike isolato.** Verificare quali garanzie sono ottenibili con un protocollo/API specifica; distinguere digitazione, paste, undo, macro, modifiche programmatiche e composizione. Partire in osservazione con dati sintetici e senza sostituzioni arbitrarie.
- [ ] **R5.04 — Esperimenti negativi.** Forzare focus/caret/documento cambiati, selezione, conflitti di revisione, timeout e permessi. Una risposta positiva di una sonda Win32 read-only non autorizza la scrittura.
- [ ] **R5.05 — Decisione documentata.** Registrare target/versione, garanzie soddisfatte, garanzie mancanti e risultati degli esperimenti. Esito ammesso: candidato qualificabile per R6, necessità di adapter cooperante diverso oppure automatismo non qualificabile con le API studiate.

**Consegna:** decisione architetturale, spike e matrice delle prove, con un target scelto o una motivazione concreta del mancato ingresso in R6.

**Chiusura:** tutte le garanzie richieste hanno evidenza oppure sono marcate mancanti. Il lavoro di studio può essere concluso con esito negativo; l'implementazione automatica rimane condizionata e il picker resta utilizzabile.

## 8. R6 — Prima espansione automatica qualificata

**Prerequisito:** risultato positivo di R5; nessuna attivazione universale dedotta dal riconoscimento del trigger.

- [ ] **R6.01 — Adapter di produzione.** Collegare osservazione committata → sessione → riconoscimento → gate → piano → un singolo tentativo di sostituzione per il target qualificato. Conservare Core/Application indipendenti da Avalonia e API OS.
- [ ] **R6.02 — Controlli operativi.** Implementare opt-in, pausa/ripresa, esclusioni per app e reset del contesto; rendere lo stato coerente fra motore, tray e finestra prima di abilitare l'automatismo.
- [ ] **R6.03 — Modalità trigger.** Qualificare prima un sottoinsieme dichiarato, ad esempio delimitatore mantenuto; aggiungere immediato e delimitatore scartato come incrementi distinti con casi Unicode e confini di parola.
- [ ] **R6.04 — Aggiornamento libreria.** Pubblicare cataloghi coerenti con la revisione; se rebuild o validazione falliscono, sospendere quel percorso e mantenere il fallback manuale. Non aggiungere letture SQLite a ogni tasto.
- [ ] **R6.05 — Esiti e lifecycle.** Integrare cancellazione prima degli effetti, buffer volatile, perdita focus, composizione, overflow/permessi e chiusura pulita. Conservare separati rifiutato, verificato, inviato/non verificato e incerto; nessun retry dopo effetto incerto.
- [ ] **R6.06 — E2E e pilot.** Dimostrare trigger → testo atteso, delimitatore e caret nel vero editor qualificato, insieme ai casi di mancata espansione. Eseguire pilot controllato e pubblicare l'elenco preciso dei contesti supportati.

**Consegna:** alpha con il primo automatismo limitato e disattivabile, test E2E ed elenco dei target supportati.

**Chiusura:** espansione effettiva dimostrata nel target e rifiuti corretti nei casi esclusi. Editor non qualificati continuano a usare il picker; nessuna cancellazione basata soltanto su keycode o lunghezza UTF-16.

## 9. R7 — Template, variabili e form

**Obiettivo:** aggiungere contenuti dinamici preservando gli snippet letterali esistenti. Può avanzare sul flusso manuale dopo R4 anche se R5 ha esito negativo.

- [ ] **R7.01 — Contratto compatibile.** Definire sintassi, escaping, errori e attivazione esplicita del template per snippet. Gli snippet già salvati conservano il significato letterale finché non vengono convertiti dall'utente.
- [ ] **R7.02 — Primo incremento puro.** Implementare data/ora e testo costante con clock, fuso e locale deterministici/iniettabili; distinguere lingua del contenuto e lingua UI. Aggiungere anteprima e test prima dell'inserimento.
- [ ] **R7.03 — Cursore e riferimenti.** Aggiungere riferimenti aciclici, limiti di profondità/dimensione e posizione cursore solo dove l'adapter può governarla. Il solo incolla del picker non offre già questa capacità.
- [ ] **R7.04 — Form.** Introdurre progressivamente testo, textarea, scelta e data, validazione e anteprima. Annullare prima degli effetti su target/appunti; riconfermare il contesto prima dell'output.
- [ ] **R7.05 — Variabili con effetti.** Aggiungere eventuale lettura clipboard solo su richiesta contestuale esplicita, con dipendenza sostituibile nei test; shell, rete e script non fanno parte del primo incremento.
- [ ] **R7.06 — Dati e UI.** Introdurre versioni/migrazioni necessarie per SQLite ed export, backup prima dell'upgrade, comportamento per formati sconosciuti e prove round-trip. Estendere l'editor con i controlli funzionali necessari e verificare lingue/DPI.

**Consegna:** template opt-in, anteprima, form per incrementi e compatibilità dei dati dimostrata.

**Chiusura:** testo letterale preservato, valutazione prevedibile, nessuna modifica al destinatario quando l'utente annulla, capacità del cursore dichiarata per adapter.

## 10. R8 — Altri sistemi operativi e distribuzione

**Obiettivo:** trasferire capacità verificate una alla volta, senza trattare la sola apertura della GUI come supporto all'espansione.

- [ ] **R8.01 — Matrice per OS.** Definire versioni/sessioni di riferimento e righe separate per build, storage, tray, picker, Copia, hotkey, Inserisci e automatismo.
- [ ] **R8.02 — Core e GUI.** Eseguire build/test e round-trip SQLite/JSON su macOS/Linux; qualificare file system, directory dati, risorse, font, lingue e input della ricerca.
- [ ] **R8.03 — Hotkey e output.** Studiare e qualificare API/permessi del sistema; distinguere macOS, X11 e singole sessioni Wayland. Pubblicare `Unsupported`/`Unknown` e fallback quando una capacità non è ottenibile.
- [ ] **R8.04 — Adapter automatici.** Ripetere il percorso R5–R6 per ogni adapter nativo; il risultato Windows non certifica altre piattaforme o IME.
- [ ] **R8.05 — Pacchetti.** Scegliere bundle/archivio/installer necessari, runtime e architetture; verificare firma/notarizzazione quando previste, installazione, upgrade, disinstallazione e conservazione degli snippet.
- [ ] **R8.06 — Rilascio per capacità.** Aggiornare CI, SBOM/notices, checksum, istruzioni e rapporti per OS; distribuire soltanto le combinazioni provate, con procedure di recupero dei dati.

**Consegna:** pacchetti per OS e matrice dettagliata delle capacità, aggiornati insieme alle evidenze.

**Chiusura:** ogni capacità dichiarata dispone di prove su quella combinazione OS/sessione/versione; le parti ancora sperimentali restano esplicite.

## 11. Sequenza immediata e decisioni

La Alpha 20.19 apre il percorso R5 con barriere di focus, senza autorizzare espansioni. La prima prova manuale R2.05 sul file sintetico Notepad è positiva, ma la matrice multi-editor e i test negativi non sono chiusi. Priorità successiva: qualificare testo Unicode committato, destinatario e modifica condizionale; continuare in parallelo R2/R4. Nessuna prova nativa Windows è stata eseguita per questa consegna.

Decisioni da chiudere al relativo punto: repository/CI di riferimento e politica versioni (R1), licenza e tipo di pacchetto prima della pubblicazione (R1/R4), primo editor automatico e garanzie ottenibili (R5), ordine degli altri OS e configurazioni realmente supportate (R8).

Le modifiche del picker già coperte dai test non richiedono ulteriori cicli di ritocco generico: i nuovi interventi devono chiudere un task del piano, una regressione osservata o un requisito verificabile. Il redesign generale rimane successivo alla stabilizzazione dei flussi fondamentali; le correzioni di accessibilità e le UI necessarie alle nuove funzioni rientrano nelle rispettive milestone.

## 12. Regole di consegna e completamento

- Ogni task implementativo comprende codice, verifica pertinente, esito e limiti; il numero di test cresce quando serve a verificare comportamento, non come obiettivo autonomo.
- Ogni milestone si chiude con le evidenze del suo criterio di uscita. Un laboratorio cooperante, un mock e un editor reale hanno report distinti.
- Ogni consegna mantiene lo **ZIP completo con radice `Ristretto/`**, sorgenti, test, script, documentazione e `MANIFEST_SHA256.txt`; gli eseguibili, quando qualificati, sono artefatti separati dichiarati.
- Le modifiche funzionali richiedono build/test e nuova estrazione secondo i gate applicabili. Un aggiornamento solo documentale richiede audit di collegamenti/coerenza e non implica una nuova qualificazione runtime.
- Stato corrente sintetico in [CURRENT_STATE.md](CURRENT_STATE.md), attività future soltanto qui, risultati storici in [reports](../reports/README.md). La semplice presenza di un task nel piano non significa che sia implementato o autorizzi effetti su editor esterni.
