# Benchmark Core e riduzione allocazioni — alpha.4

**13 novembre 2026.** Tool autonomo `tools/Ristretto.CoreBenchmark`, senza nuove dipendenze NuGet, GUI, hook, clipboard o accesso alla libreria dell'utente. Usa soltanto snapshot sintetiche e il vero `TriggerActivationSession` del Core.

## Risultato osservato

Prima e dopo l'ottimizzazione: **110 misure, 1.101.210 osservazioni misurate per versione**, oltre al warmup; nessun errore di correttezza. Due dataset da 100 e 10.000 snippet, 11 scenari, cinque ripetizioni in un singolo processo per versione. I P99 di tutti gli scenari sono inferiori al budget **proposto** di 1 ms su questa macchina; questo non è un gate di release multipiattaforma.

Confronto D10, media dei byte gestiti allocati per evento sulle cinque ripetizioni; latenza come massimo dei cinque P99, in microsecondi:

| Scenario | Byte prima | Byte dopo | Riduzione | P99 prima µs | P99 dopo µs |
|---|---:|---:|---:|---:|---:|
| ASCII con delimitatore | 364 | 324 | 11% | 1,2 | 1,2 |
| Accenti NFD | 416 | 339 | 18% | 2,9 | 1,6 |
| Emoji ZWJ | 355 | 312 | 12% | 0,8 | 2,4 |
| Immediate con prefissi sovrapposti | 414 | 294 | 29% | 0,9 | 0,8 |
| Prefisso condiviso lungo | 1.069 | 483 | 55% | 3,5 | 1,9 |
| Nessun match, buffer pieno | 24.320 | 4.592 | **81%** | 24,5 | 15,0 |

La latenza non migliora in ogni scenario; tiered compilation, scheduling e GC restano visibili. L'evidenza principale è la riduzione delle allocazioni delle sottostringhe, senza cambiamenti intenzionali alle regole del matcher. Rimangono allocazioni per normalizzazione, trimming grafemico e decisioni.

## Modifica

`CompiledTriggerCatalog` cerca ora i suffissi con `ReadOnlySpan<char>` e i lookup alternativi ordinali di [Dictionary](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2.getalternatelookup?view=net-10.0) e [HashSet](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.getalternatelookup?view=net-10.0). I confronti case-sensitive/NFC e la precedenza dei prefissi restano gli stessi; nessuna sottostringa temporanea per ogni ricerca.

Due test aggiuntivi coprono prefissi al limite di 128 unità UTF-16 con emoji che condividono un surrogate iniziale, completamento dopo una storia lunga senza match e disponibilità successiva di un trigger breve. Suite al momento della misura alpha.4: **137 test passati**; la successiva suite alpha.7 ne comprende 180.

## Ripetere

Dalla radice del progetto, senza debugger:

```powershell
dotnet run -c Release --project tools/Ristretto.CoreBenchmark/Ristretto.CoreBenchmark.csproj -- --output reports/core-benchmark-nuova-misura.json
```

Il percorso di output deve essere nuovo: il tool rifiuta di sovrascrivere rapporti esistenti. Codice di uscita 0 = tutte le decisioni controllate sono corrette; 1 = almeno un errore di correttezza; 2 = uso non valido/errore di esecuzione. I tempi sono informativi, non determinano il codice di uscita. Debug e debugger collegato vengono rifiutati. Gli script di validazione compilano il tool senza eseguirlo.

## Metodo e limiti

- Dataset deterministici: esattamente 100/10.000 snippet abilitati e 5/200 gruppi. Undici trigger riservati ai casi di verifica; fra i restanti, 20% CJK. Payload sintetico costante, nessun contenuto personale.
- Almeno 10.000 eventi di warmup e almeno 10.000 eventi misurati per scenario/ripetizione, completando sempre l'ultima sequenza. Singoli scalari Unicode per osservazione; i frammenti NFD e ZWJ restano sequenze originali. Non simulano eventi keyboard nativi.
- `Stopwatch.GetTimestamp` immediatamente attorno a `Observe`; [allocazioni gestite del thread corrente](https://learn.microsoft.com/en-us/dotnet/api/system.gc.getallocatedbytesforcurrentthread?view=net-10.0) nello stesso tratto. Fixture, record di input e array dei campioni sono preparati prima. L'overhead degli strumenti non viene sottratto. Memoria nativa e RSS non sono misurate.
- Percentili nearest-rank per ripetizione, nessuna rimozione degli outlier; riportati P50/P95/P99/max, byte/evento, collezioni GC e costo separato del costruttore sessione con compilazione indice. Non sono misure cold-start.
- Nessun GC forzato, affinità CPU o modifica al piano energetico. Tiered compilation predefinita. Windows 10.0.22631 X64, .NET 10.0.14, 20 processori logici; CPU, limiti memoria e SHA-256 dell'assembly Core sono nei JSON. Piano energetico non registrato.
- Gli scenari includono ASCII, NFD, CJK, arabo, emoji ZWJ, collisione NFC ambigua, prefissi Immediate, prefissi lunghi, buffer pieno, campo protetto e composizione IME simulata. Il controllo verifica identità dello snippet, numero candidati e assenza di attivazioni premature, anche nel warmup.
- È una prima misura locale del Core F1b. Non misura tutti i budget PB-01–PB-12, F1a isolatamente, 50k snippet, input reale, GUI, consumo a riposo o stabilità prolungata. Non dimostra sicurezza o compatibilità di F2/F3.

## Evidenze

- [Baseline alpha.3](../reports/core-benchmark-alpha3.json).
- [Core ottimizzato alpha.4](../reports/core-benchmark-alpha4.json).
- [Confronto D10](../reports/core-benchmark-comparison.json).

I tre JSON originali conservano metodo, risultati e identità degli assembly misurati. I log duplicati della misura sono stati rimossi; non si presenta la misura alpha.4 come un benchmark della alpha.7. L'ultima regressione funzionale è nel [TRX alpha.7](../reports/windows-alpha7-tests.trx); [inventario dei rapporti](../reports/README.md).
