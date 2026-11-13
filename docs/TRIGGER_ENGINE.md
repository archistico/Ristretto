# Addendum canonico v2.5 — Policy Unicode e boundary (PROPOSTA)

**La sezione sottostante v1.6 resta utile per lo storico delle ipotesi, ma sui conflitti di default vale la proposta v2.5.** È uno studio, non una ratifica: [ADR-0004](adr/ADR-0004-UNICODE-MATCHING-AND-WORD-BOUNDARIES.md). Specifica di riferimento per i test: [piano matcher](MATCHER_TEST_PLAN.md).

## Matrice delle dimensioni indipendenti

| Dimensione | Valori proposti | P0 | Nota |
|---|---|---|---|
| Equivalenza | `Exact`, `NFC` | `NFC` proposto | MatchKey separata dal testo originale; differenza rispetto al default conservativo v1.6 |
| Case | `Sensitive`, `InvariantInsensitive` | `Sensitive` | Case-insensitive non significa accent-insensitive né full folding |
| Boundary | `Anywhere`, `PrecedingBoundary`, `WholeWord` | `Anywhere` per trigger con prefisso; `PrecedingBoundary` opzionale | `WholeWord` richiede bordo destro verificato, non match immediato speculativo |
| Attivazione | `Immediate`, `AfterDelimiterKeep`, `AfterDelimiterDrop`, `ManualOnly` | Immediate per prefisso esplicito; altrimenti Keep | Delimitatore è un evento di testo effettivo, non qualsiasi tasto |
| Scope | Gruppo, app, contesto consentito, esclusioni | Global se non protetto | Le collisioni vanno controllate **dopo** il filtro degli scope |
| Safety | Focus e deletion disponibili | obbligatoria | Match valido ≠ autorizzazione a modificare il target |

## Contratto osservabile

1. `RawInput` e testo persistito sono immutati; il matcher produce `MatchKey = NFC(RawInput)` (o `Exact`). Il mapping alla sequenza di eventi committati deve restare esplicito: la lunghezza della chiave NFC non è quella dei caratteri originali né dei Backspace.
2. Una chiave non può essere fatta corrispondere a **metà** di un grafema incompleto. Buffer incrementale accetta soltanto commit affidabili; può trattenere un prefisso in attesa di altri input. La concatenazione di segmenti NFC non sostituisce la normalizzazione della sequenza completa.
3. Un delimitatore valido è testo committato configurato (` `, `.`, `,`, ecc.). Non riconoscere per default Enter/Tab come caratteri di testo: sono eventi azione con effetti sul target; richiedono capability appropriata e policy specifica.
4. `WholeWord` non può garantire il futuro; deve aspettare l'evidenza del confine destro, che può essere delimitatore o nuova sequenza committata. In contesti CJK senza spazi il word boundary non è sinonimo di pausa digitazione: P0 consiglia prefissi/shortcode espliciti.
5. Prima della preparazione il resolver emette `NoMatch`, `Pending`, `Unique`, `Ambiguous`, `Suppressed`, `Unsupported`; un conflitto effettivo blocca l'espansione automatica, senza selezione random.
6. `Preflight` controlla l'identità del target e la possibilità **reale** di cancellazione e inserimento. Un `Unique` non implica `CanSafelyErase`.
7. Delimitatori Keep/Drop preservano esattamente il carattere e la posizione logica dell'utente, non sostituiscono arbitrariamente con uno spazio. `Keep` non deve duplicare il delimitatore già digitato; deve documentare se si rimuove/riemette o se rimane sul target.
8. Stringhe direzionali (RTL), invisibili (ZWJ, ZWNJ, VS16) e confusables non vanno rimosse automaticamente. Nella GUI mostrare avvisi e una visualizzazione tecnica opzionale dei code point.

### Priorità della specifica rispetto a eventuali incompatibilità

`ADRs non ratificati` → le modalità effettive sono ancora ipotesi. `No output modification when uncertain` è già un requisito di sicurezza condiviso; l'esatto boundary, case folding e NFC default vanno approvati nel gate prima del codice. Non considerare il matcher un sostituto dell'IME o dell'adapter nativo.

---

# Motore dei trigger — contratto funzionale candidato

**Revisione dossier:** 1.6, 12 novembre 2026  
**Stato:** proposta analitica, non implementata, non testata.  
**Scopo:** definire semantica coerente per Windows, macOS, Linux X11 e Wayland senza confondere il matcher puro con il provider di eventi OS.

## 1. Termini e invarianti

- **Trigger:** sequenza logica associata a uno snippet, diversa dall'insieme di tasti fisici. Può contenere caratteri Unicode, ma un evento tastiera non equivale necessariamente a testo committato dall'IME.
- **Match:** risultato *puro*, senza effetti esterni. Contiene `TriggerId`, `SnippetId`, `MatchSpan`, `BoundaryEvidence`, `ActivationPolicy`, `ContextFingerprint` e una diagnosi delle eventuali collisioni.
- **Espansione:** operazione con effetti esterni. Rimuovere testo o incollare non è transazionale attraverso applicazioni esterne; un match valido non autorizza automaticamente l'inserimento.
- **Contesto:** processo/identità app, sessione, superficie di input, capacità native, fonte e attendibilità del segnale. Le API potrebbero non poter determinare se un campo è editabile: **in caso di incertezza non cancellare tasti**.
- **Privacy:** buffer volatile e limitato di testo committato; mai keylogging persistente, nessun testo digitato nei log, nessuna ispezione password con intento di aggirare protezioni.

## 2. Contratto candidato di `TriggerRule`

| Campo | Semantica | Proposta predefinita | Da validare |
|---|---|---|---|
| `TriggerId` | Identificatore stabile, distinto dal contenuto | UUID | import Beeftext |
| `Text` | Sequenza Unicode inserita dall'utente | conserva contenuto originale | limiti non arbitrari |
| `Activation` | `Immediate`, `AfterDelimiterKeep`, `AfterDelimiterDrop`, `ManualOnly` | `Immediate` solo per trigger espliciti con prefisso, altrimenti suggerire `AfterDelimiterKeep` | configurazione UX |
| `Boundary` | `Anywhere`, `StartOfWord`, `WholeWord` | `StartOfWord` per trigger di linguaggio naturale; trigger simbolici gestiti esplicitamente | regole Unicode UAX#29 |
| `CaseMode` | `Sensitive`, `InvariantInsensitive`, `PropagateCase` (facoltativo) | `Sensitive` per sicurezza predicibile | turco / greco / tedesco |
| `NormalizationForMatch` | `Exact` o `NFC` **solo per confronto** | Exact finché non siano testate collisioni | import/IME/macOS |
| `Scope` | elenco applicazioni consentite/escluse, gruppo attivo | globale, con esclusioni sicurezza | OS-specific identity |
| `Enabled` | attivazione per snippet e gruppo | true | persistenza |
| `Priority` | criterio deterministico per contesti specifici | nessuna priorità arbitraria su duplicati equivalenti | conflitti reali |

Una regola con `AfterDelimiterKeep` deve produrre il contenuto dello snippet e conservare esattamente il delimitatore riconosciuto (non sostituirlo sempre con spazio). Con `Drop` deve consumarlo; Enter/Tab possono avere semantica nel target e non sono sempre testo: trattarli come **azioni** oltre che delimitatori.

### UI prevista per la scelta del trigger

Sezione ordinaria: `Abbreviazione`, `Espandi quando: [Subito / dopo spazio o punteggiatura / manualmente]`, `Riconosci solo parola intera`, `Sensibile a maiuscole`, `Applicazioni`. Pannello avanzato: delimitatori personalizzati, conservazione, matching accent-insensitive (non predefinito), per-app override e diagnostica. Anteprima con cinque esempi `si espande / non si espande`.

## 3. Pipeline di input — riconoscimento senza effetti

```text
[Native input adapter]
  |  raw key changes / committed text / focus & composition status
  v
[Event normalizer] -- reject synthetic/self-generated events
  |
  +--> unsupported / permission missing: expose capability, do not guess
  v
[Private bounded per-target input suffix]
  |  reset on context invalidation, nav, composition uncertainty
  v
[Matcher (pure)] --> [Scope + boundary + activation checks]
  |                                      |
  |                              no candidate: ignore
  v
[Ambiguity resolution (pure)] --> reject / optional chooser
  v
[Expansion preparation (pure)] --> parser + variables + forms
  v
[Native preflight target/focus/permission]
  |
  +--> uncertainty -> abort or copy-only fallback (explicit)
  v
[Deletion strategy] -> [Insertion strategy] -> [Outcome + redacted diagnostic]
```

**Mai** considerare i tasti grezzi equivalenti a caratteri Unicode. Distinguerli è necessario per AltGr, dead keys, layout modificati, lettere accentate e IME CJK. Non assumere che ogni API restituisca `compositionend` o committed text: in piattaforme senza eventi di composizione affidabili può essere necessario disabilitare il matching automatico per quel contesto anziché indovinare.

### Eventi logici da modellare nei test (non necessariamente interfacce finali)

- `CommittedText(text, target, source)`; `CompositionStarted/Updated/Committed/Cancelled`; `Backspace` che modifica la sequenza osservata **solo quando** mappatura e caret sono affidabili.
- `FocusChanged`, `CaretMoved/SelectionChanged` (se osservabili), `NavigationOrShortcut`, `InputSourceChanged`, `TargetLost`, `PauseChanged`, `PermissionChanged`.
- `SyntheticInputStarted/Finished` evita loop; non affidarsi all'idea che tutti gli OS marchino sempre input iniettato: usare contesto e coordinamento interni.

## 4. Buffer e reset

Il buffer non è la cronologia della tastiera. Conservare soltanto il suffisso minimo necessario ai trigger attivi, con limite configurabile e scadenza. Non persistente. **Non inserire caratteri captati in metriche, eccezioni o crash dump**. Resettare su cambio app/finestra o superficie, navigazione o selezione che invalidi l'ipotesi "caret alla fine", interruzione IME, comandi Ctrl/Alt/Meta che cambiano contenuto e perdita dei permessi; su Backspace aggiornare solo se l'evento corrisponde realmente a una cancellazione testuale.

Caso tipico: l'utente digita `;em`, sposta il cursore con freccia sinistra, digita `ail`. Il matcher non può collegare le due parti come `;email`. Similmente durante composizione giapponese `nihongo -> 日本語` nessuna espansione su sequenze provvisorie.

## 5. Politica dei conflitti (proposta, NON ancora ADR adottato)

1. Eliminare i candidati disabilitati, fuori scope, incapaci di soddisfare boundary e activation.
2. Se il suffisso termina con trigger diversi, proporre **match più specifico / più lungo** soltanto quando lo stesso punto di commit lo permette, documentando la scelta nell'editor.
3. A parità di trigger efficace e scope, **nessun random**: indicare conflitto nell'editor; in esecuzione automatica preferire **non espandere** finché l'utente non risolve, oppure aprire un chooser **solo** quando il contesto/focus consente la selezione sicura.
4. Favorire un trigger esplicitamente limitato a un'app rispetto al suo omologo globale **solo dopo** ratifica: evitare priorità implicite non visibili.
5. Trattare collisioni Unicode da NFC/case folding e conflitti con sostituzioni OS/app di terzi come classi distinte. Il nostro software può rilevare i propri duplicati; **non può garantire il rilevamento universale di altre utility**.

Competitor: Keyboard Maestro documenta longest-match e conflict palette, PhraseExpress chooser, TextExpander warning conflitti, Alfred priorità workflow rispetto a snippet, Raycast override sperimentale degli snippet OS. Non sono uguali: la nostra decisione è **candidata**, da approvare in ADR-020.

## 6. Sicurezza del target e della cancellazione

Prima di toccare il testo: controllare che l'operazione sia abilitata, che il contesto non sia protetto, che il target coincida con quello del matching, che il metodo di inserimento sia disponibile e che lo snippet sia già stato preparato. Se la cancellazione del trigger fallisce, non procedere a un incolla presumendo che tutto sia andato bene. Se l'inserimento fallisce dopo una cancellazione, segnalare `PartialFailure` senza dichiarare un rollback universale.

`Ctrl+Backspace` e ripetere `Backspace` per la lunghezza .NET `string.Length` sono **entrambi inadatti come algoritmi universali** per trigger Unicode/emoji. Il conteggio in UTF-16 non equivale ai tasti da cancellare; persino il conteggio a grafemi non coincide con il modello di edit dell'app destinataria. Comporre una strategia per target che possa dichiarare `Unsupported`.

**Fallimento sicuro:** non inviare sequenze di cancellazione in browser non editabili, terminali, canvas, videogiochi, password field o finestre sconosciute; la capacità si valida per app/contesto. Il picker può fornire `Copia contenuto` quando è impossibile iniettare automaticamente.

## 7. Matching internazionale

- Storage: Unicode senza modifica automatica (NFC/NFD originale preservato). Confronto trigger esatto per default, NFC opzionale e tracciato per evitare collisioni invisibili.
- `CaseInsensitive` è diverso da `AccentInsensitive` e da `PropagateCase`; non raggrupparli in un'unica checkbox. `İ/ı/I/i`, `Σ/σ/ς`, `ß/SS` richiedono esempi verificati e policy di cultura. Mai applicare trasformazioni di case indistintamente a codice, email e template.
- Boundary: UAX #29 è un riferimento, ma le parole giapponesi/cinesi e le regole specifiche dei singoli editor richiedono test e possono necessitare un boundary manuale/esplicito. Non supporre "lettera" = `[A-Za-z]`.
- Grapheme cluster: per la **visualizzazione e il piano di cursore**, usare `StringInfo` / API .NET adeguate, ma non assumere che ogni edit control elimini un grapheme per un Backspace.
- IME: nessun trigger basato su testo provvisorio; in caso di assenza di un affidabile evento commit, disattivare l'automazione per la modalità interessata e lasciare picker/copia.

## 8. Policy per modalità d'uso (raccomandazioni)

| Modalità | Esperienza | Rischio | Roadmap |
|---|---|---|---|
| Trigger con prefisso (`;sig`, `#sqlcliente`) | rapido, prevedibile | basso se focus valido | P0 |
| Dopo delimitatore keep/drop | evita espansioni premature | medio (Enter/Tab) | P0 |
| Trigger qualsiasi posizione | utile per simboli | falsi positivi | avanzata P1 |
| Snippet su picker manuale | utile su app difficili | focus/colla | proposto P0 (da ratificare) |
| Autocorrect intera parola | utile | sostituzioni inattese | P2 |
| Regex / fuzzy matching | potente | ambiguità, costo | P2/P3 |
| Trigger che eseguono script | automazione | sicurezza elevata | P3, mai default |

## 9. Osservabilità non invasiva

Pannello `Diagnostica -> Esegui prova`: `OS + sessione (X11/Wayland)`, `permessi`, `app target`, `backend selezionato`, `matching enabled`, `last operation status`, senza trigger o payload. In modalità debug locale, rilevare soltanto le categorie (`BoundaryRejected`, `Conflict`, `CompositionPending`, `FocusChanged`, `PermissionDenied`, `PasteUnsupported`, `PartialInsert`). Nessun logger che memorizzi ogni tasto.

## 10. Non decisioni e condizioni di uscita dall'analisi

1. Decisione normativa su `AfterDelimiterKeep/Drop` per Space/Enter/Tab.
2. Definizione compatibile e documentata del comparatore Unicode e dei gruppi `word`.
3. Matrìce delle capacità native per `committed text`, `secure input`, `target identity`, `deletion reliability`, `paste`.
4. Scelta picker P0/P1: l'uso come fallback di sicurezza lo rende candidato a P0, ma necessita approvazione.
5. Test reali per app: Notepad, editor web contenteditable, VS Code, Office, terminali, editor CJK/RTL, desktop remoto.
6. Test di regressione per ogni bug scoperto e screenshot di riferimento GUI dove utili.

**Fonti incrociate:**
- https://www.phraseexpress.com/doc/edit/trigger/
- https://wiki.keyboardmaestro.com/trigger/Typed_String
- https://textexpander.com/learn/using/snippets/conflicting-snippet-abbreviations
- https://manual.raycast.com/snippets
- https://espanso.org/docs/matches/basics/
- https://www.fastkeysautomation.com/documentation6/TextExpander.html
- https://blaze.today/faq/
- https://www.alfredapp.com/help/troubleshooting/snippets/
- https://www.trankynam.com/atext/doc/
- https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction
