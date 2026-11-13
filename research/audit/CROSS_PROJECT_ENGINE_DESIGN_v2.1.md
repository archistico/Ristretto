# Beeftext vs Espanso — scelte per il nostro motore C#/Avalonia

Questo documento è un **confronto di architettura**, non un'autorizzazione a copiare codice. Fonti locali e tracce: [baseline](LOCAL_SOURCE_BASELINE_v2.1.md), [Beeftext](BEEFTEXT_DIRECT_SOURCE_AUDIT_v2.1.md), [Espanso](ESPANSO_DIRECT_SOURCE_AUDIT_v2.1.md).

## Decisioni suggerite

| Tema | Beeftext | Espanso | Proposta nostra |
|---|---|---|---|
| Organizzazione | manager globali C++/Qt | workspace di molti crate e middleware | 3–5 progetti solo se necessari, feature folders Vertical Slice |
| Trigger | buffer, strict/loose, risoluzione casuale dei duplicati | trie rolling + regex con risultati multipli | matcher puro deterministico, collision report + picker opzionale |
| Espansione | rendering snippet dentro `Combo` | render/executor separati | `PrepareExpansion` puro + `ExecuteExpansion` nativo |
| Input | hook Windows `ToUnicodeEx` | backends OS differenziati | osservatori per piattaforma, capability esplicite, nessuna inferenza IME falsa |
| Output | clipboard o eventi Windows | injector evento/clipboard adattivo | scelta per testo/app/OS, preferenza manuale avanzata, diagnostica |
| Errori | timer e try/catch | log/errori per executor | stati `NoChange`, `Succeeded`, `FailedBeforeDelete`, `PartialInsert` |
| Persistenza | JSON + QSettings | YAML + store modulari | candidata SQLite + export JSON; ADR ancora aperta |
| UI | Qt Widgets e picker | focus sul backend/tray | Avalonia moderna, compatta, temi e i18n live |
| Licenza | MIT | GPLv3 | licenza della nuova app da decidere; studi tecnici sì, copie GPL no senza compatibilità |

## Contratti minimi (pseudodescrizione, non implementazione)

- `IInputSource`: invia eventi normalizzati, con `CommitState=Unknown/Committed/Composing`; se `Unknown`, non dichiarare falsamente supporto IME.
- `TriggerRecognizer`: mantiene un buffer volatile per target, applica word boundary, delimitatori, case folding e priorità esplicite. Non conosce API OS né GUI.
- `ExpansionPlanner`: risolve variabili e frammenti in `ExpansionPlan`, prima degli effetti esterni; blocca scripting non autorizzato.
- `ExpansionCoordinator`: esegue preflight su target/focus/permesse, rimozione solo quando sicura, inserimento, gestione del conflitto clipboard e risultato.
- `ITextInserter`: può dichiarare `Events`, `Clipboard`, `Accessibility` o `Unsupported` per specifico contesto, con motivazione e capabilities. Un risultato fallito **non** è una transazione annullabile universalmente.

Nessun framework mediator, bus di eventi o macchina a stati generica obbligatoria. Il livello di astrazione deve essere minimo e nascere da almeno due implementazioni reali o da un confine di test concreto.

## Unicode: norme di progetto

1. Memorizzazione: UTF-8/versionato in import-export; round-trip esatto anche con combinazioni NFC/NFD, ZWJ, flag, skin tone, RTL, CJK e newline.
2. Matching: definire chiaramente sensibilità alle maiuscole, equivalenza canonica opzionale e regole di confine Unicode. **Mai normalizzare distruttivamente il testo memorizzato**.
3. Cancellazione: nel codice C# `string.Length` misura unità UTF-16, **non** backspace necessarie; perfino il numero di grapheme non garantisce gli effetti in ciascun editor.
4. Inserimento: operare su stringhe complete e verificare ogni native backend; strategia clipboard può preservare meglio Unicode ma deve gestire i conflitti con nuove copie dell'utente.
5. Test: editor desktop nativi, browser, terminali, IDE, input protetti, RDP, IME e differenti display server; esito per combinazione e non un generico 'Linux supported'.

## Sequenza del primo sviluppo (dopo fase analisi)

1. Matcher/parser puro con caratteri Unicode e collisioni — test senza OS.
2. Contratto `ExpansionPlan` e importazione non esecutiva — test pure/fixture.
3. Spike Windows di input→output, focus e clipboard; poi macOS, X11 e Wayland con capability.
4. Primo workflow GUI essenziale Avalonia: lista → editor → picker → espansione.
5. Storage, backup, import Beeftext/Espanso, preferenze, temi, i18n, packaging e regressioni.

**Gate obbligatorio:** niente claim di supporto a emoji/IME multi-app finché i test nativi non dimostrano il risultato.
