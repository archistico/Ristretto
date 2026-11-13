# v2.8 — storage e concorrenza

SQLite è l'archivio confermato; WAL locale proposto con **un writer seriale** e letture brevi, senza I/O nel callback. Il matcher è snapshot immutabile derivato, con revisione allineata a `app_meta.library_revision`. Un commit seguito da rebuild fallito sospende le espansioni fino a una snapshot corrente. `Microsoft.Data.Sqlite` non realizza I/O asincrono effettivo tramite metodi ADO.NET Async, quindi i comandi DB si eseguono in worker che non bloccano la GUI. I budget prestazionali v2.7 restano **non misurati**. [Specifica](SQLITE_STORAGE.md) · [ADR-0007](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md).

---

# Prestazioni, concorrenza e ciclo di vita — specifica canonica v2.7

**Stato: PROPOSTA DI STUDIO (2026-11-13).** Nessun target misurato, nessuna implementazione C# o prova runtime. Riferimenti: [architettura](ARCHITECTURE.md), [modello](DOMAIN_MODEL.md), [inserimento](OUTPUT_COORDINATION.md), [prove](PERFORMANCE_TEST_PLAN.md), [audit dei quattro sorgenti](../research/audit/FOUR_SOURCE_PERFORMANCE_AUDIT_v2.7.md), [ADR-0006](adr/ADR-0006-RESOURCE-LIFECYCLE-AND-CONCURRENCY.md).

## 1. Obiettivo e ambito

Rendere il text expander **invisibile quando non serve**: minimo consumo in tray, nessuna pausa percepibile durante la digitazione, picker istantaneo, ricerca efficace e dati durabili. Il motore deve restare utilizzabile con decine di migliaia di snippet; ciò non significa caricare ogni testo, immagine, traduzione o dataset emoji nella memoria residente. I numeri seguenti sono **obiettivi iniziali da validare**, non prestazioni promesse.

### Distinguere le metriche

- **Hook callback latency**: durata dall'ingresso al ritorno dal callback nativo; non include il trattamento da parte dell'editor. Deve restare strettamente limitata e non fare I/O, rendering, normalizzazione onerosa o attese.
- **Matcher latency**: da evento di testo committato e contesto disponibile a `MatchResult` su snapshot già pronto; non include callback nativo, variabili, cancellazione o incolla.
- **Recognition-to-plan latency**: include preflight puramente applicativo; può richiedere input per variabili o carico di uno snippet, da misurare a parte.
- **Expansion end-to-end**: dall'inizio dei controlli di sicurezza al risultato riportato. Dipende dalla app destinataria, input method, clipboard, focus e permessi; non si assume un limite universale.
- **Search first-results**: modifica della query → primi N risultati visualizzati; distinta da ranking in core e rendering Avalonia.
- **Ready-to-expand**: processo avviato → hook funzionante, impostazioni validate e matcher coerente; distinto da tempo per visualizzare la finestra principale.
- **Memory resident set / managed heap / native heap**: misure separate. Non confondere allocazioni cumulative con memoria residente.

## 2. Budget iniziali, non attestati

Dataset di riferimento: **D10** = 10.000 snippet attivi, trigger misti, testi da 20–2.000 caratteri, 200 gruppi, italiano/inglese/CJK/emoji e collisioni sintetiche; 1.000 keyword emoji, editor in stato idle; configurazione costruita una volta. Macchina di riferimento da definire (CPU, disco SSD, RAM, OS, configurazione potenza); build `Release`, JIT caldo e cold start riportati separatamente; warmup e campioni replicati.

| Codice | Misura e condizioni | Target candidato | Cosa escludere |
|---|---|---:|---|
| PB-01 | Matching evento P99, D10, snapshot già pubblicato | ≤ 1 ms | I/O, composizione IME, tempo hook OS |
| PB-02 | Matching evento P99, D50 (50k), sola ricerca esplorativa | ≤ 2 ms | Iniezione e regex non profilate |
| PB-03 | Elaborazione callback P99 in assenza di overflow | ≤ 0,5 ms | Tempo di delivery degli eventi OS |
| PB-04 | Picker, prima pagina risultati P95, D10, query già nella finestra aperta | ≤ 120 ms | Attivazione/focus del picker |
| PB-05 | Picker: apertura P95, indice caldo D10 | ≤ 300 ms | Ripristino focus di una app esterna |
| PB-06 | Ready-to-expand P95, cold start D10 | ≤ 3 s | Avvio del sistema operativo / SSD sotto carico |
| PB-07 | Memoria RSS in tray stabilizzata D10 su macchina di riferimento | ≤ 150 MiB | Memoria di processi e servizi OS |
| PB-08 | CPU idle, media 10 min dopo 2 min di assestamento | ≤ 0,5% di **un core** | Controlli updater espliciti e import in corso |
| PB-09 | Import 10k snippet: UI responsiva | Nessun blocco continuo UI > 100 ms | Totale operazione; va misurato |
| PB-10 | Arresto normale, senza operazioni native pendenti | ≤ 2 s P95 | Timeout OS/hook irreversibili |
| PB-11 | Ricompilazione snapshot D10 senza fermare l'hook | no gap osservabile nel matcher, swap coerente | Durata totale di compilazione |
| PB-12 | Stress idle 24h | Nessun incremento monotono non spiegato di handle/thread/RSS | Fluttuazioni GC/OS |

`D50` = 50.000 snippet, prova di scalabilità **non** promessa per MVP. I valori devono essere rivisti dopo la **prima baseline** su almeno un sistema operativo; non fissare limiti di marketing prima di osservare dati reali. Aggiungere P50/P95/P99, deviazione e numero campioni, non solo medie. Unità di CPU, OS e convenzioni di misura vanno sempre riportate nel report.

## 3. Modello di processo e assembly

**Un processo** (`TextExpander.Desktop`) con quattro assembly `Core / Application / Infrastructure / Desktop` come in [ARCHITECTURE](ARCHITECTURE.md). Una finestra chiusa non chiude il processo se l'utente ha scelto di rimanere in tray. **Nessun daemon esterno, service, IPC o process pool nel MVP.** La modalità CLI/diagnostica futura, se utile, deve riusare core/application senza imporre un quinto prodotto.

Responsabilità minime: `Core` contiene matcher compilato, normalizzazione, parsing puro; `Application` governa lifecycle, sessioni, snapshot e un tentativo di espansione alla volta; `Infrastructure` fornisce callback native, clipboard, file/storage, processo singolo e OS capability; `Desktop` usa dispatcher Avalonia solo per View/ViewModel e input della GUI.

```text
Native input callback ──[evento minimo, senza testo persistito]──> bounded ingress
                                                                  │
                                                               matcher loop
                                                                  │
                                                       immutable active snapshot
                                                                  │
                                                    match candidate / preflight
                                                                  │
                                                single-flight expansion coordinator
                                                                  │
                                                       platform effect adapter

Library/Settings write ──> validate ──> compile next snapshot (background)
                                  │                     │
                          storage commit          atomic publish / revision

Picker query ──> cancellable search worker ──> latest-only result page ──> Avalonia dispatcher
```

La visualizzazione non è una nuova architettura a servizi: indica **confini di responsabilità**, non un thread per box. Il coordinatore di output può essere asincrono, ma **mai** chiamato direttamente da un hook. Non introdurre `Task.Run` per ogni tasto o un actor framework obbligatorio.

## 4. Gestione degli eventi e backpressure

1. Callback OS: verifica basilare del contesto e dell'evento, restituisce rapidamente il controllo. Non attende DB, UI, rete, clipboard o plugin. Non esegue allocazioni proporzionali a grandezza degli snippet.
2. L'ingress è una coda **bounded** con limite misurato; se piena, non saltare caratteri e poi continuare a cercare trigger: la sessione va marcata **untrusted/resync needed**, azzerata, e si sospende il matching fino a un nuovo confine sicuro. Nessuna cancellazione del testo esterno a seguito di overflow.
3. L'ordine degli eventi è importante: una sola sequenza di elaborazione per sessione d'input; evitare il parallelismo per-evento. Identificatore di sessione e revisione dello snapshot su ogni evento candidato; non contenuto di tasti nei log.
4. Eventi di cambio focus/layout, mouse, lock-screen, secure input, IME unknown/overflow invalidano il buffer temporaneo. Le capacità OS determineranno quali segnali sono osservabili.
5. **Un solo tentativo di inserimento attivo per destinatario/contesto**. Eventuali richieste aggiuntive ricevono `Busy`/`Cancelled` oppure attendono soltanto in un buffer limitato e verificabile (decisione da ratificare). No retry silenzioso.
6. I progressi di import o ricerca possono essere **coalesced**; **mai** coalescere eventi di digitazione, eliminazione o focus necessari alla correttezza. A differenza dei dati di GUI, la perdita di un evento di tastiera non deve produrre un falso match.
7. `CancellationToken` governa operazioni cancellabili come compilazione indice, ricerca e import; per operazioni native già committate al target, la cancellazione produce stato *uncertain*, non rollback inventato.

**Perché non usare code illimitate:** i sorgenti Espanso mostrano canali `unbounded` per alcune comunicazioni interne. Non è prova di bug nel prodotto, ma non adotteremo quel pattern come default nella pipeline di input del nostro processo perché in un sovraccarico prolungato la pressione di memoria non avrebbe un limite progettato.

## 5. Snapshot del matcher, revisioni e consistenza

`LibraryRevision`, `MatcherSnapshotRevision`, `InputSessionRevision` e `OperationId` sono **identificatori di coerenza**; non serve introdurre quattro entità persistenti. Gli snippet vengono modificati tramite una slice (validazione → eventuale precompilazione → commit storage → pubblicazione atomica nuova snapshot). Le revisioni già pubblicate restano leggibili per il breve tempo necessario ai tentativi correnti; il motore non osserva collezioni mutabili della GUI.

- Snapshot immutabile con trigger compilati, id, scope, politica Unicode, priorità deterministiche, senza duplicare indiscriminatamente tutti i corpi delle espansioni.
- Compilazione lontano dal callback OS. Durante la compilazione, l'hook usa **interamente** lo snapshot precedente. Un singolo swap atomico rende visibile la nuova revisione. Non è un locking globale del matcher.
- Se la compilazione fallisce, nessuna snapshot parziale è pubblicata. Se il commit su disco fallisce, nessuna revisione nuova è dichiarata attiva. Se il commit riesce e il processo si arresta prima dello swap, al riavvio si ricompila dalla versione salvata: non si promette atomicità transprocesso.
- Se cambiano trigger, modalità di confronto, layout o gruppi attivi, il buffer della sessione corrente viene invalidato. In caso di modifiche non rilevanti si valuterà il mantenimento soltanto con prove.
- Nessuna collezione di snapshot storici illimitata; i tentativi in corso mantengono un riferimento breve e sono soggetti a cancellazione/timeout di sicurezza.
- Indicizzazione e ricerca per il picker sono separate dalla compilazione del matcher: tempi di caricamento e ranking non devono ricadere sul callback nativo.

## 6. Politica della memoria

- **Buffer digitazione:** volatile, dimensione massima da derivare dal trigger più lungo ammesso e dal boundary, con **cap rigido** e comportamento `NoMatch/NeedsResync` al superamento; vietato memorizzare tutta la cronologia di tasti.
- **Payload snippet:** caricare soltanto le parti necessarie per la preparazione; una cache bounded per snippet molto usati è candidata, non obbligatoria. Criteri di eviction e limiti in byte da misurare. Una query su disco può essere consentita in `Prepare`, mai nell'hook.
- **Catalogo emoji:** dizionario lessicale separato, eventualmente inizializzazione lazy e indice linguistico in base alla lingua di ricerca; no parsing JSON completo a ogni apertura picker.
- **Picker:** virtualizzare le righe, limitare i risultati mostrati e non istanziare ViewModel per 50.000 elementi. Il risultato usa `ItemId`, titolo/trigger e anteprima corta; il testo completo si apre solo nell'editor.
- **Import:** streaming/staging con dimensioni, profondità, file e conteggi massimi; non duplicare libreria e backup integralmente in memoria quando è evitabile.
- **UI:** caricare icone on demand, sfumature statiche leggere, nessun timer ad alta frequenza per decorazioni, nessuna ricreazione sistematica delle schermate se non necessaria.
- **Clipboard:** lease breve best-effort, limiti byte e formati, nessun buffer perpetuo del contenuto copiato.

## 7. Concorrenza: regole e lock

- **Input** serializzato; **output** single-flight; **storage** serializzato in transazioni, con concorrenza letture controllata. Non tenere mai un lock storage durante l'attesa di una finestra di input o di una API native.
- **UI** su dispatcher Avalonia; usare `Post`/`InvokeAsync` quando necessari, mai per ogni carattere. `ObservableCollection` e controlli solo sul dispatcher appropriato.
- **Snapshot publication** atomica come riferimento immutabile; niente `ReaderWriterLockSlim` nel path per-tasto salvo dimostrazione profilo che sia indispensabile.
- **Search** usa cancellazione e query ID: soltanto l'ultimo risultato è pubblicato. Un vecchio task non deve sovrascrivere la ricerca nuova.
- **Import, backup, check update** sono operazioni opzionali e a bassa priorità; possono attendere senza bloccare input. Una operazione di I/O lenta non deve provocare una coda di progressi illimitata.
- **Sicurezza di shutdown:** non usare thread foreground non arrestabili, `async void` per operazioni critiche, timer non disposti o watchdog che riavviano il motore di nascosto.

## 8. Ciclo di vita del processo

| Stato | Comportamento necessario | Transizioni consentite |
|---|---|---|
| `Starting` | Istanza unica, percorsi/dati validati, carico minimo, controlli capability | `Ready`, `Degraded`, `Stopping` |
| `Ready` | Motore attivo, tray/status corretti; GUI opzionale | `Paused`, `Degraded`, `Stopping`, `Suspended` |
| `Paused` | Nessuna espansione; comportamento hook/hotkey definito chiaramente | `Ready`, `Stopping`, `Suspended` |
| `Degraded` | Backend non disponibile o snapshot non coerente; fallback manuale dove sicuro | `Ready` dopo verifica, `Stopping` |
| `Suspended` | Sleep/lock/session switch; svuota input volatile e interrompe output quando possibile | `Ready` solo dopo revalidazione, `Degraded`, `Stopping` |
| `Stopping` | Blocca nuove richieste, cancella job, libera hook e hotkey, finalizza transazioni in modo bounded | `Stopped` |
| `Stopped` | Nessuna attività nativa residua | nuovo avvio esplicito |

La chiusura della finestra principale non implica `Stopping` se modalità tray attiva. Il comando **Esci** sì. Seconda istanza: comunicazione minima per attivare la finestra esistente; non lancia una seconda istanza del motore. Su macOS e Linux la presenza di tray può dipendere dall'ambiente desktop: la scelta di nascondere la finestra deve preservare sempre una via d'uscita/accesso, senza fare affidamento esclusivo sulla tray.

### Sequenza di avvio candidata

1. Acquisire mutex/lock di istanza in modo specifico per OS (nessuna scansione dei processi come unico controllo).
2. Leggere settings con recovery e localizzazione; non aprire finestre complesse per inizializzare il motore.
3. Recuperare storage e compilare snapshot attiva; il processo mostra `Starting`/`NotReady` finché non è realmente disponibile.
4. Verificare permessi e capability, poi installare/abilitare gli hook. Se mancano permessi, scegliere `Degraded` e mostrare istruzioni, non fingere funzionamento.
5. Attivare la GUI solo se richiesto, al primo avvio o da tray/scorciatoia. In una futura modalità portabile, salvare dati nel percorso portabile esplicitamente selezionato.

### Sequenza di arresto candidata

1. Segnalare `Stopping`, impedire nuove espansioni e bloccare l'ingresso (nessuna finestra che si chiude e lascia il motore attivo invisibilmente).
2. Invalidare il buffer; annullare lavori non ancora committati; output già avviato va portato a esito prudente senza retry.
3. Restituire il controllo agli hook del sistema e deregistrare hotkey/event tap, rilasciare clipboard lease e timer; in caso di crash l'OS deve rimuovere gli hook legati al processo ove applicabile, non promettere cleanup finale.
4. Finalizzare salvataggi in corso e backup con timeout e integrità. Nessuna scrittura di libreria troncata.
5. Liberare risorse e terminare; log privi di input utente.

Suspend/resume: cancel dei tentativi pendenti quando sicuro, sempre nuova qualificazione di target/layout/IME/sessione prima di riprendere. Cambio utente/sessione remota/dock/monitor devono essere casi espliciti nelle prove; non affidarsi a un timer fisso.

## 9. Profiling e diagnostica privacy-safe

Metriche interne aggregate (opzionali e locali): `input.events.received`, `input.events.discarded` per motivo, `matcher.duration`, `matcher.snapshot.revision`, `picker.search.duration`, `insertion.outcome` per categoria, `library.load.duration`, `snapshot.build.duration`, `process.rss`, `process.threads`, `process.handles`, `dotnet.gc` e allocation rate. **Vietato** salvare testo digitato, contenuto snippet, parole cercate nel picker, titolo della finestra sensibile o payload clipboard. Id/op/target devono essere aggregati o anonimi; evitare label dinamiche ad alta cardinalità.

Strumenti candidati: `Stopwatch.GetTimestamp`, `System.Diagnostics.Metrics`/`EventSource`, `dotnet-counters`, `dotnet-trace`, profiler OS (ETW/PerfView Windows, Instruments macOS, perf Linux). La misura del callback nativo deve essere consentita dal backend e campionata per non alterare la latenza. Nessuna telemetria online obbligatoria; benchmark esportabili dall'utente con conferma e dati redatti.

Riferimenti ufficiali: [Avalonia threading](https://docs.avaloniaui.net/docs/app-development/threading), [metriche .NET](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-runtime), [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters), [Hosted services](https://learn.microsoft.com/en-us/dotnet/core/extensions/hosted-services) (solo principi di lifecycle, non obbligo di usare Generic Host).

## 10. Condizioni di qualità e compromessi

- **Correttezza prevale sul tempo:** meglio `NeedsResync` e nessuna espansione che cancellazioni scorrette. Il benchmark di un sistema che salta eventi non è un risultato positivo.
- **Semplicità prevale sulla micro-ottimizzazione:** scegliere trie/hash/snapshot semplice dopo benchmark, non librerie native o allocatori custom prima.
- **No espansione quando troppo lento:** overload, timeout o context unknown portano a esito conservativo; non usare mai la pressione di coda per ignorare focus/secure input.
- **Ricerca non compromette input:** se necessario si riduce/cancella il lavoro di ranking, non si rallenta il path caldo.
- **Metriche non promettono compatibilità:** aver misurato P99 di un matcher non garantisce Unicode corretto, focus sicuro o input effettivamente arrivato al target.
- **Misure comparative oneste:** registrare hardware, OS, .NET/Avalonia versioni, cold/warm, energia e carico, asset della libreria, durata e ripetizioni; osservare CPU in unità di un core.

## 11. Decisioni da ratificare

1. Budget preliminari PB-01..PB-12 e baseline di prova (macchina e ambiente) — **proposte**.
2. `bounded ingress` e fail-closed su overflow (proposta P0); esatta soglia da profilare con adapter reale.
3. Revisioni snapshot e ordine `pre-compile → storage commit → atomic publish`, con recovery al riavvio (proposta P0).
4. Criteri UX per `Paused`, `Degraded`, `Suspended`, tray non disponibile e seconda istanza.
5. Ricerca/picker separata dal matcher, virtualizzazione, cancellazione delle query obsolete (proposta P0/P1).
6. Target memoria/CPU da misurare, non elevare prematuramente a SLA pubblico.
7. Policy caches e limiti della libreria: valori reali solo dopo profiling.

**Prossimo gate:** revisione del [piano di test](PERFORMANCE_TEST_PLAN.md) con workload sintetici, verifica di fattibilità nativa e, solo su autorizzazione successiva, primo benchmark C# riproducibile.
