# Tracciabilità del modello di dominio ai quattro sorgenti — audit statico v2.4

**Metodo:** lettura diretta degli ZIP forniti dall'utente. Percorsi e linee riferiti alle root degli archivi (`Beeftext-master/`, `espanso-dev/`, `XMiLib-master/`, `emojilib-main/`). Nessuna compilazione o esecuzione dei quattro progetti; le scelte C# sono *proposte*, non funzioni già implementate negli originali. SHA-256 e provenienza archivio nella [baseline](LOCAL_SOURCE_BASELINE_v2.1.md) e nella [traccia submodule](SUBMODULE_AUDIT_SUMMARY_v2.2.md).

| Osservazione direttamente leggibile | Sorgente e righe | Decisione per il dominio | Stato |
|---|---|---|---|
| Combo contiene UUID, nome, keyword, snippet, descrizione, gruppo, matching mode, case policy, enabled e date | `Beeftext/Combo/Combo.h:35-102` | `Snippet` e `TriggerDefinition`; separare UI, template e parsing | confermato staticamente |
| `Combo::isUsable()` usa stato di combo e gruppo | `Beeftext/Combo/Combo.h:69-72` | abilitazione effettiva calcolata da snapshot; non duplicata in storage | proposta |
| `ComboList` è contemporaneamente `QAbstractTableModel` | `Beeftext/Combo/ComboList.cpp:70-83` | separare storage, dominio e modello GUI | confermato staticamente |
| `ComboManager` sceglie un elemento random se più combo corrispondono | `Beeftext/Combo/ComboManager.cpp:194-223` | `MatchResult.Ambiguous`, nessuna scelta casuale | confermato staticamente/proposta |
| Trigger emoji shortcode gestiti in passaggio dedicato | `Beeftext/Combo/ComboManager.cpp:230-264` | emoji catalog separato e opt-in, non accorpato ai trigger per default | confermato staticamente/proposta |
| Valutazione template precede cancellazione, ma poi usa lunghezza keyword UTF-16 | `Beeftext/Combo/Combo.cpp:406-452`, in particolare `:433` | piano puro e rimozione OS senza `string.Length` | confermato staticamente/proposta |
| Il matcher Espanso espone stato e vettore risultati `MatchResult` | `espanso-match/src/lib.rs:29-56` | risultato discriminato e snapshot, no output dal matcher | confermato staticamente/proposta |
| Espanso distingue `MatchCause::Trigger` e `MatchCause::Regex` | `espanso-config/src/matches/mod.rs:79-86` | literal P0; regex separato P1/P2 | confermato staticamente/proposta |
| Render Espanso ha `Success`, `Aborted`, `Error` | `espanso-render/src/lib.rs:26-44` | preparazione può fallire/cancellarsi senza modificare target | confermato staticamente/proposta |
| Template Espanso separa corpo, variabili, dipendenze | `espanso-render/src/lib.rs:73-87` | token sicuri e risoluzione completa prima dell'output | confermato staticamente/proposta |
| XMiLib usa evento Unicode Win32 con key-up flag errato e non ispeziona `SendInput` | `XMiLib/SystemUtils.cpp:169-181` | `InsertionOutcome` tipizzato; mai interpretare chiamata API come prova di output | confermato staticamente/proposta |
| EmojiManager richiede JSON mappato shortcode → oggetto con `char` | `Beeftext/Emoji/EmojiManager.cpp:133-159` | importatori/catal. separati dal modello di dominio | confermato staticamente |
| emojilib 4.0.3 espone catalogo `dist/emoji-en-US.json` con schema diverso | `emojilib-main/package.json` e `emojilib-main/dist/emoji-en-US.json` | dati catalogo versionati; lingua keyword separata da lingua UI | confermato staticamente/proposta |

## Differenze intenzionali e gap

- **Non è evidenza di bug runtime** il fatto che le versioni sorgente siano snapshot di branch senza commit storici; non possiamo certificare versione originale/submodule exact.
- Beeftext usa Qt e componenti Windows; Espanso ha moduli Rust GPLv3. Il progetto C# adotta i confini concettuali senza incorporare codice GPLv3 non compatibile con la licenza scelta.
- Nessuna fonte dimostra che un adapter esterno possa conoscere con certezza gli eventi `CommittedText` per tutti gli IME, o che sappia sempre posizionare il cursore per emoji composte. I contratti riportano quindi `Unknown` e `SubmittedUnverified`.
- Il dataset emoji è un fatto esterno e la sua licenza/notices va mantenuta se in futuro incluso. Nessun runtime Node necessario per JSON statico.

## Metodologia di validazione futura

Prima dell'implementazione, pinare revisioni degli archivi (quando disponibili) e verificare revisioni Git dei submodule. Per la prima slice native, registrare editor, layout, lingua IME, compositor, permessi e backend; confrontare testo effettivo mediante osservazione indipendente. [Piano test](../../docs/DOMAIN_TEST_PLAN.md).
