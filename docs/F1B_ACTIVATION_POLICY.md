# F1b — Policy di attivazione, senza input globale

**Sorgenti `3.9.0-alpha.4` · 13 novembre 2026 · stato: Core compilato, suite di 137 test passata; primo benchmark locale 100/10k eseguito.** [Misure e metodo](CORE_BENCHMARK.md).

La fase F1b distingue **riconoscere** un trigger da **decidere quando attivarlo**. Il nuovo `Core/Matching/TriggerActivationSession.cs` riutilizza l'indice `CompiledTriggerCatalog` di F1a, ma non è integrato nel desktop e **non** contiene hook, inserimento testo, simulazione di Backspace, accesso alla clipboard o I/O su disco.

## Regole scelte

| Regola | Default | Motivazione |
|---|---|---|
| `Mode` | `OnDelimiter` | Non sostituisce una parte di parola mentre si sta ancora scrivendo; `Immediate` è disponibile esplicitamente |
| `Delimiters` | `UnicodeWhitespace` | Spazio e spazi Unicode, incluso NBSP; `UnicodeWhitespaceOrPunctuation` è opt-in |
| `StartBoundary` | `WordStart` | Un trigger alfanumerico non corrisponde alla coda di una parola più lunga |
| `MaxIdleMilliseconds` | 15.000 | Il contesto scade se non arrivano inserimenti affidabili; 0 disabilita questa scadenza |
| Case e normalizzazione | NFC, case-sensitive | Come F1a, senza equiparare lettere differenti |

**Confini di parola:** i trigger che iniziano con caratteri non appartenenti a una parola (come `;firma`) sono prefissi espliciti e non richiedono un separatore a sinistra. I trigger che iniziano con lettere, numeri, segni combinanti o underscore sono accettati solo se il carattere precedente **osservato e committato** non è parte di parola. Se il buffer inizia esattamente sul trigger, è necessaria `InitialWordBoundaryEvidence.Verified` fornita da un futuro adapter che possa dimostrarla; `Unknown` è l'impostazione prudente. Un evento non può autenticare retroattivamente la posizione del cursore nell'editor esterno.

**Delimitatori:** in modalità predefinita attivano soltanto gli spazi Unicode validi come testo committato. La modalità opzionale accetta la punteggiatura Unicode ma **non** i connettori di identificatore come `_`. I controlli `Tab` e `Invio` restano barriere di sicurezza e non sono mai interpretati come delimitatori affidabili (F1a/F1b bloccano i caratteri di controllo). Il delimitatore è già presente nella finestra di destinazione: non va rimosso implicitamente.

## Hotfix 3.9.0-alpha.2.1

Il test Windows `ImmediateAnywhereModeCanMatchInsideWordWhenExplicitlySelected` falliva: per `brb` il carattere finale `b` costituiva anche un prefisso proprio di lunghezza 1, e tale prefisso rinviava erroneamente l'activation nonostante il match completo lungo 3. In modalità `Immediate` l'attesa di altri caratteri è giustificata solo da un prefisso candidato di lunghezza **maggiore o uguale** a quella del match esatto. In `OnDelimiter` le regole non cambiano. Aggiunti test per trigger autonomi, suffissi di parole e veri prefissi condivisi.

## Casi sui prefissi

Con snippet `;sig` e `;signature`:

- Default `OnDelimiter`: `;sig` = `WaitingForDelimiter`, `;sig ` = `Ready` per lo snippet breve; `;signature ` = `Ready` per il lungo.
- Opzione `Immediate`: `;sig` = `WaitingForMoreInput` finché il lungo è ancora possibile, `;signature` = `Ready` per il lungo; `;sig ` può sbloccare il breve.
- `;sigx` non viene mai trasformato **retroattivamente** in `;sig` + `x`. Non si cancellano caratteri ormai committati su base congetturale.
- Duplicati con stesso trigger dopo NFC danno `Ambiguous`, mai una scelta arbitraria.
- Una sottostringa in mezzo a un evento bulk può essere osservata ma non attivata in modo sicuro: se il riconoscimento terminale cade prima della fine del chunk, viene restituito `Suppressed` e il contesto è resettato.

Il record `TriggerActivationDecision` informa su `None`, `WaitingForMoreInput`, `WaitingForDelimiter`, `Ready`, `Ambiguous`, `Suppressed`. Anche `Ready` **non** è un `ExpansionPlan` e non autorizza modifiche. Non vengono restituiti né un conteggio Backspace né un offset di cancellazione.

## Isolamento e sicurezza

L'API `Observe(CommittedInputObservation observation, long monotonicMilliseconds, InitialWordBoundaryEvidence ...)` richiede input già **verificato come testo committato** in campo editabile non protetto e un timestamp monotono in millisecondi. La sorgente nativa futura dovrà attestare tali condizioni: `KeyDown` da solo non basta e il tempo di calendario non è un orologio monotono. Un cambio di `TargetEpoch`, pausa, aggiornamento della libreria, scadenza temporale o reset elimina il contesto precedente. Timestamp retrogrado, IME in composizione, campi ignoti/protetti, input sintetico o malformato generano `Suppressed`.

Il buffer è bounded a 512 unità UTF-16, i chunk a 1024; il taglio avviene a confini di cluster grafemici e perde la prova del bordo iniziale. Non vengono registrate o salvate sequenze di digitazione. Le stringhe .NET non garantiscono cancellazione crittografica dopo il reset.

**Limite importante:** F1b non osserva caret, selezioni, modifiche esterne alla sequenza o origine nativa delle paste. La decisione prodotta è **soltanto informativa**. F2/F3 dovranno qualificare tali condizioni prima di considerare una sostituzione sicura. Il Core non misura ancora la latenza del percorso nativo.

## Test aggiunti e verifica

`tests/TextExpander.Tests/TriggerActivationSessionTests.cs` include prove per delimitatori, prefissi sovrapposti, doppioni, nomi all'interno di parole, Unicode/IME, emoji ZWJ, NBSP, giapponese, campo protetto, epoch, pause, timeout, revisioni snapshot, corruzione di input, chunk bulk e catalogo da 10.000 trigger. Il test da 10k verifica **correttezza e regressioni strutturali**, non è un benchmark temporizzato.

Su Windows, quando possibile, eseguire dalla radice di un'estrazione nuova:

```bat
scripts\validate.cmd
```

I controlli statici non sostituiscono .NET build/xUnit. F1b non introduce alcuna differenza visiva o comportamento automatico osservabile nell'app. Per arrivare all'espansione reale servono F2a (output controllato e preflight), F2b (rimozione qualificata) e F3 (adapter di eventi affidabili e orchestrazione).
