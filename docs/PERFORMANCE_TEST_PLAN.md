# Piano di misura, stress e stabilità — v2.7

**Stato: DISEGNO TEST, NON ESEGUITO.** In questa fase nessun benchmark di Beeftext/Espanso o della futura applicazione è stato eseguito. Riferimenti: [specifica prestazioni](PERFORMANCE_RESOURCE_LIFECYCLE.md), [matrice casi](../data/PERFORMANCE_SCENARIOS_v2.7.csv), [budget](../data/PERFORMANCE_BUDGETS_v2.7.csv).

**Aggiornamento 13 novembre 2026, alpha.4:** prima misura T0 del Core F1b eseguita con 100/10k snippet, 11 scenari e cinque ripetizioni per dataset; oltre un milione di eventi per versione nel confronto prima/dopo, senza errori nelle decisioni. [Rapporto e limiti](CORE_BENCHMARK.md). Il piano v2.7 completo, gli altri budget, i competitor e i test nativi/soak restano non eseguiti; le tabelle sottostanti descrivono il piano originario.

## 1. Protocollo per ogni rapporto di misura

Registrare: `RunId`, data, commit della futura app, versioni .NET/Avalonia, OS/compositor, CPU/frequenza/power plan, RAM, SSD, DPI, periferiche/layout, cold/warm, build `Release`, tiered compilation, numero snippet attivi/disabilitati, lunghezza trigger e body, tipo di ricerca, metodo di inserimento, stato clipboard/IME/focus, sample size, P50/P95/P99/max, process CPU, RSS, allocazioni, eccezioni, handle/thread count, pass/fail con evidenza di baseline. Non fare confronto fra Windows/macOS/Linux senza riportare queste differenze.

### Dati sintetici, **mai** registrare input reale

| Dataset | Contenuto | Perché |
|---|---|---|
| D0 | libreria vuota + 1 snippet | regressioni avvio/zero-state |
| D1 | 100 snippet + 5 gruppi | utilizzo quotidiano |
| D10 | 10.000 snippet + 200 gruppi, 1.000 keyword emoji | baseline P0; trigger brevi/lunghi, contenuti 20–2k, variazioni Unicode |
| D50 | 50.000 snippet + 500 gruppi, 10.000 alias emoji | stress P1, non promise del MVP |
| DAdversarial | trigger con prefissi comuni, nomi simili, testo molto lungo entro limiti, collisioni, UTF-16 surrogate, ZWJ, combining marks e regex *solo se futura feature* | peggiori casi controllati |

Generatori documentali deterministic seed, numero e distribuzioni stabili. Separare query che non hanno match, che ne hanno uno e che producono molte corrispondenze. Quota di Unicode e lunghezze devono essere riportate; dataset anonimi. Eseguire normalizzazione dei dataset **solo per le chiavi del matching**, non del payload originale.

## 2. Fasi di esecuzione (future)

**T0 — Core puro, niente GUI o hook.** Benchmark `MatcherSnapshot.Build`, lookup per carattere, collisioni, confini, Backspace/invalidazione, allocazioni; test di correttezza associati (matrice Unicode v2.5) obbligatori. Il matcher deve prima essere corretto, poi veloce. Fail se falso match anche se budget rispettato.

**T1 — Application con adapter fake.** Threading/serializzazione, coda bounded, overflow, snapshot swap, cancel, single-flight, input-session reset, scheduling. Testare input simulato ad alta frequenza e flush del buffer; contare output e revisioni, non conservare testo dell'utente.

**T2 — Storage e picker GUI.** Cold load, warm load, rebuild ricerca, import/export, backup concorrente, typing in ricerca, risultati stale, virtualizzazione e navigazione via tastiera. Visualizzare soltanto risultati correnti; testare anche tema live e localizzazione con migliaia di record.

**T3 — Native spikes Windows/macOS/X11/Wayland.** Misurare callback e focus safety con consenso e dati sintetici, mai campi reali protetti; cambiare app, layout, IME, sleep/wake, remote session, permessi. `Unavailable` è esito corretto se il backend non supporta la funzione. Un ottimo P99 del core non sostituisce questo gate.

**T4 — Soak/stress.** 1h, 8h, 24h di idle, apertura/chiusura finestra, ricerca ripetuta, cambio tema/lingua, import e update snapshot, riavvio dopo interruzione. Contare leak di risorse, crescita persistente RSS/GC, handle, thread e eventi rimasti in coda. Diagnosi con strumenti OS, non inferenze da un solo campione.

**T5 — Release gates.** Ogni release deve dimostrare i budget approvati sul set minimo per OS. Risultati hardware-specifici e regressioni rispetto alla baseline riportati. Sono indispensabili gli smoke test di accessibilità, IME, focus e resilienza: migliorare una media peggiorando la correttezza è un regressione.

## 3. Metodo statistico

- Warmup distinto dal cold start; almeno 5 ripetizioni dei tempi di startup e almeno 10.000 eventi sintetici per percentili del matcher, con run multipli. Le soglie sono **criteri metodologici candidati**.
- Raccogliere P50/P95/P99 e worst-case, throughput e allocation/event; un singolo valore medio non prova la stabilità interattiva.
- Ambienti minimamente realistici (nessun debugger in process); test con power-saving e carico concorrente in set separato.
- Per CPU idle campionare a intervalli e calcolare `% di un core` con formula esplicita. Sulla piattaforma riportare uso dei thread e OS scheduling quando possibile.
- Memoria dopo warmup e dopo GC **non sono** equivalenti a RSS stabile: memorizzare entrambe e trend temporale.
- Visualizzare differenza fra risultato `SubmittedUnverified`, `VerifiedComplete` e `Unknown` nell'analisi delle iniezioni; non contare un evento SendInput accettato come inserimento riuscito.

## 4. Gate automatici e manuali

| Gate | Come si qualifica | Esecuzione oggi |
|---|---|---|
| G-PERF-CORRECT | Nessun falso match, nessun replay dopo overflow/partial | **NOT_RUN** |
| G-PERF-CORE | PB-01/02/03 con raccolta percentili e allocazioni | **NOT_RUN** |
| G-PERF-UX | PB-04/05, responsiveness e query-stale UI | **NOT_RUN** |
| G-PERF-LIFE | PB-06/08/10/12, tray, suspend/resume, stop | **NOT_RUN** |
| G-PERF-DATA | Import/backup snapshot senza perdita dati né blocchi input | **NOT_RUN** |
| G-PERF-NATIVE | almeno un editor rappresentativo per OS, input Unicode e permessi | **NOT_RUN** |

La matrice CSV contiene casi d'uso distinti, non un claim di percentuale copertura. I casi P0 prevalgono su P1. Prima dell'implementazione deduplicare con CSV v2.1–v2.6 e assegnare ogni test a `Unit/Integration/Desktop/Native/Soak`; non generare migliaia di test a partire dalle righe senza analisi.

## 5. Strumenti, privacy e limiti

Strumenti candidati: `BenchmarkDotNet` **solo se la dipendenza è giustificata**, `dotnet-counters`, `dotnet-trace`, ETW/PerfView, Instruments, `perf`, `Stopwatch` e metriche locali. Limitare lo overhead di tracing; nei log salvare tempo, stato e codici errore, mai input, snippet o query. La documentazione ufficiale di [Avalonia](https://docs.avaloniaui.net/docs/app-development/threading) richiede dispatcher UI; [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) offre indicatori GC/CPU e allocazioni. Nessun invio automatico di telemetria.
