# Benchmark mirato — trigger, ambiguità, target e output

**Revisione:** v1.6 · **Consultazione fonti pubbliche:** 12-11-2026 · **Metodo:** analisi delle guide ufficiali e, per strumenti di automazione, documentazione tecnica; nessuna prova diretta, benchmark di latenza o reverse engineering binario. `D` documentato dal produttore / progetto; `I` inferenza; `V` da validare.

## Matrice a colpo d'occhio

| Prodotto | Modalità trigger/word | Conflitti | Inserimento/compatibilità | Valore per noi |
|---|---|---|---|---|
| Beeftext | Automatico/manuale, matching dalle analisi C++ precedenti | In alcuni casi scelta casuale (sorgente storico) | Clipboard+simulated keys; lag/clipboard | punto di partenza, evitare random |
| Espanso | `word`, `left_word`, `right_word`, separatori configurabili; `propagate_case` | Da validare in runtime | backend keys/clipboard e config per app | flessibilità configurabile, GUI semplice |
| PhraseExpress | prefisso/suffisso, word start/end/middle/whole, pausa, conferma, regex | menu fra frasi identiche | VK_PACKET/SendKeys/clipboard e ritardi per app | chooser e troubleshooting |
| FastKeys | immediato/delimitatore, keep/drop, inside-word, case, raw | conferma per alternative | clipboard e timing avanzato | opzioni UX compatte |
| TextExpander | condizioni prima dell'abbreviazione, case/case adaptation, group scope | conflitti indicati con colori e risoluzione | opzioni paste e app scopes | editor autocorrettivo |
| aText | opzioni per gruppo, a delimitatore e app/contesto | verificare | clipboard long text, Secure Input FAQ | diagnostica e fallback |
| Keyboard Maestro | typed string con prefissi/boundaries, ignore-case/diacritical | longest; a parità conflict palette | elimina testo tramite tasti; esplicito rischio | sicurezza cancellazione |
| Text Blaze | StartWord / StartEndWord / Anywhere (per cartella) | prevenzione con prefissi | browser extension e desktop | semplicità della policy |
| Raycast | immediate/after delimiter keep/drop, expand within words | override system snippets sperimentale | tempi injection/clipboard configurabili | precedenza e boundary comprensibili |
| Alfred | keyword specifico, affissi collection, app exclude e mid-string | workflow precedence documentata | clipboard/paste; slow down key events | picker e diagnostic UX |
| AutoHotkey | end chars, `*`, `?`, `B0`, reset; contesto | ordine dichiarazione (documentato da guida riprodotta) | SendInput/SendEvent/Text; limiti | suite di casi negativi |
| Typinator | Quick Search, autocorrect, forms | dettaglio pubblico ND | dichiarata copertura app/locale | UX e set dizionari |
| Karabiner | rimappatura eventi fisici, input Unicode via metodi mirati | non è snippet expander | EventViewer, input source | strumentazione macOS |
| AutoKey, Hammerspoon, TypeIt4Me, macOS replacement | strumenti di riferimento | esito specifico da verificare | tool OS / macro / picker | backlog di prove |

*La tabella non misura chi “funziona meglio”: non abbiamo misurazioni comparabili. ND = non documentato nelle fonti raccolte.*

## Lezioni per prodotto (con URL e grado evidenza)

### 1. Espanso (D)
La guida `Matches Basics` illustra trigger semplici, `word: true`, controlli `left_word`/`right_word`, `word_separators` e `propagate_case`; i trigger Unicode e i metodi di output `clipboard`/`keys` sono citati direttamente. È un ottimo riferimento per **regole del motore esplicite**, ma la configurazione YAML è una barriera per utenti non tecnici. **Candidatura:** GUI che genera regole testabili, senza obbligare a scrivere YAML. **Non dedurre:** corretto funzionamento del proprio backend su ogni IME/Wayland.
Fonti: https://espanso.org/docs/matches/basics/ ; https://espanso.org/docs/configuration/basics/

### 2. PhraseExpress (D)
La guida trigger contiene inizio/fine/metà/intera parola, prefissi/suffissi, opzioni dopo pausa o conferma e menu di scelta per duplicati, oltre a RegEx per esperti. Expert options descrive backends e delay per focus/paste. **Candidatura:** separare riconoscimento automatico e proposta all'utente, esplicitare conflitti. **Costo:** troppe opzioni in scheda principale aumenterebbero l'attrito; nasconderle in un pannello avanzato.
Fonti: https://www.phraseexpress.com/doc/edit/trigger/ ; https://www.phraseexpress.com/doc/settings/expert/ ; https://www.phraseexpress.com/support/faq/

### 3. FastKeys (D)
Il manuale del Text Expander elenca `Trigger immediately`, `Delete ending character`, `Case sensitive`, `Don't delete string`, `Trigger inside a word`, `Send raw text`, `Manual confirmation`, `Use clipboard`. La guida Quick Find mostra selezione delle alternative e suggerimenti mentre si scrive. **Candidatura:** piccolo editor avanzato con keep/drop, preview e manual confirmation, più semplice della struttura originale FastKeys. **Da testare:** comportamento internazionale e casi particolari con editor moderni.
Fonti: https://www.fastkeysautomation.com/documentation6/TextExpander.html ; https://www.fastkeysautomation.com/documentation/Gettingstarted.html ; https://fastkeysautomation.com/documentation6/UserPreferences.html

### 4. TextExpander (D)
Ha una pagina dedicata a abbreviazioni conflittuali, con segnalazione visiva e azioni suggerite, regole di applicazione per gruppi e adattamento del case. Il suo `Inline Search` mostra risultati nei pressi del cursore. **Candidatura:** validazione interattiva alla creazione/importazione e fix assistito; inserimento non distruttivo da picker. **Rischio:** case-adaptation di SQL e codice è spesso inappropriata, tenerla opzionale.
Fonti: https://textexpander.com/learn/using/snippets/conflicting-snippet-abbreviations ; https://textexpander.com/learn/using/preferences/expansion-settings ; https://textexpander.com/learn/using/searching-snippets/inline-search

### 5. aText (D)
FAQ ufficiale: nel caso di mancata espansione verifica attivazione e diagnostica, con Secure Input macOS non tenta di eludere la protezione. Se il browser inserisce male testi lunghi suggerisce clipboard; se viene incollato il contenuto sbagliato suggerisce di disattivare il ripristino o aumentare delay. **Candidatura:** assistente di diagnosi facilmente accessibile, distinguere errore di matching da errore di incolla. **Rischio:** non trattare incrementi di delay come garanzia universale.
Fonte: https://www.trankynam.com/atext/doc/

### 6. Keyboard Maestro (D)
I Typed String Triggers documentano non-ASCII e rischio di cancellazioni in contesti non testuali; le sequenze possono essere annullate da azioni di navigazione, click, cambio app o intervallo di tempo. Trigger più lungo ha precedenza; pari lunghezza porta alla `conflict palette`. **Candidatura:** reset del buffer e comportamento deterministico, con `fail-closed` in target non testuale. **Da verificare:** differenze tra cancellazione di codice Unicode e grapheme.
Fonte: https://wiki.keyboardmaestro.com/trigger/Typed_String

### 7. Text Blaze (D)
La FAQ ufficiale specifica tre regole per cartella: Started by Word Break, Started and Ended by Word Break, Anywhere; l'esempio accentato `\\e` → `é` mostra un uso intuitivo. Le guide suggeriscono prefissi diversi per raccolte personali e condivise. **Candidatura:** configurazione base a tre scelte e suggerimenti di prefisso. **Rischio:** le regole “parola” non sono identiche per tutte le scritture.
Fonti: https://blaze.today/faq/ ; https://blaze.today/guides/quickstart/

### 8. Raycast (D)
La guida v2 chiarisce Immediate, After Delimiter Keeping/Discarding, Expand Within Words, caratteri consentiti nei trigger, timing d'injection, risposta clipboard e override sperimentale dei System Snippets. **Candidatura:** le tre opzioni più esplicative per l'utente e un pannello compatibilità. **Rischio:** l'override di altri espansori non è portabile né necessariamente desiderabile.
Fonte: https://manual.raycast.com/snippets

### 9. Alfred (D)
L'espansione automatica suggerisce prefissi non alfanumerici; la pagina Advanced documenta app exclude e rallentamento eventi; troubleshooting dedica sezioni a Secure Input, Google Docs, interferenze con altri manager e problemi di cancellazione. Workflows hanno una precedenza dichiarata rispetto alle keyword snippet. **Candidatura:** accompagnare l'utente quando l'espansione fallisce, senza messaggi di errore tecnici nel flusso ordinario.
Fonti: https://www.alfredapp.com/help/features/snippets/auto-expansion/ ; https://www.alfredapp.com/help/troubleshooting/snippets/ ; https://www.alfredapp.com/help/workflows/triggers/snippet/

### 10. AutoHotkey (D da documentazione riprodotta, ricontrollare fonte primaria)
`Hotstrings` mostra end chars, `*`, `?`, `B0`, differenze SendInput/SendEvent/SendText, reset dopo navigazione e focus change. **Candidatura:** test di confini, backspace e input sintetici esclusi dal matching. Non importare syntax/DSL AutoHotkey nella v1.0; usare solo come catalogo problemi. **Attenzione:** nel risultato esaminato la documentazione è ospitata su un mirror non ufficiale; verificare la versione primaria prima di fissare compatibilità.
Fonte consultata: https://doggy8088.github.io/AutoHotkeyDocs/docs/Hotstrings.htm

### 11. Typinator (D: funzioni pubbliche, non interni)
La pagina ufficiale documenta Quick Search, suggerimenti durante la digitazione, autocorrezione in più lingue, form e posizione del cursore. **Candidatura:** editor semplice/picker e dizionari installabili in futuro. **Non dedurre:** algoritmo interno o benchmark di latenza dalle affermazioni commerciali.
Fonte: https://ergonis.com/en/typinator/features/

### 12. Karabiner-Elements (D: strumento tecnico, non sostituto diretto)
La guida illustra rimappature e metodi per immettere caratteri Unicode tramite clipboard o Unicode Hex Input. EventViewer è prezioso per distinguere *tasti* e *caratteri*, ma non sostituisce un motore di espansione testuale. **Candidatura:** un piano di diagnosi macOS basato su strumenti OS, non una dipendenza obbligatoria.
Fonte: https://karabiner-elements.pqrs.org/docs/getting-started/features/

## Opportunità e decisioni necessarie

| Opportunità | Beneficio | Complessità/rischio | Priorità candidata | Fonte ispiratrice |
|---|---|---|---|---|
| UI modalità trigger con esempi live | meno errori di configurazione | bassa | P0 | Raycast, Text Blaze |
| Evidenza conflitti + fix | niente sostituzioni casuali | media | P0 | TextExpander |
| Chooser per duplicati espliciti | supporta snippet con alias condiviso | media e focus | P1 | PhraseExpress, KM |
| Segmentazione Unicode consapevole | evita falsi match/dati corrotti | alta e OS-specific | P0 | .NET/Unicode + casi KM |
| Buffer volatile per target con reset | evita matches oltre spostamenti | media | P0 | AutoHotkey, KM |
| Diagnostica guidata | utenti indipendenti | media | P1 | Alfred, aText |
| Timing per-app | compatibilità app lente | alta e difficile da convalidare | P1 | Espanso/PhraseExpress |
| Form per snippet | modelli avanzati | media | P2 | Text Blaze/Typinator |
| Regex trigger | flessibilità molto alta | alta e abuso | P2/P3 | KM/PhraseExpress |
| Autocorrezione multilingue | valore quotidiano | alta, falsi positivi | P2 | Typinator |

## Domande ancora aperte

- Quanto deve durare un trigger parziale? Non fare auto-espansione su un contesto stale.
- Cosa accade se `;sig` collide con una scorciatoia OS o con un altro expander? Rilevazione solo delle collisioni *interne*, supporto a pausa per-app e diagnostica; non promettere osservabilità universale.
- Un trigger Unicode composto dev'essere confrontato per code points identici o equivalenti NFC? Stabilire comportamento e segnalarlo all'editor.
- Su app che non espongono il target/caret in modo sufficientemente affidabile, è accettabile il picker in modalità copia manuale? Proposta sì.
- Autocorrect e snippet sono semanticamente lo stesso tipo? Consiglio **no**; condividono matcher ma hanno politiche di sicurezza e boundary diverse.
- L'importazione da software concorrenti deve preservare regole complesse o marcarle come non supportate? Proposta: conversione esplicita e report delle perdita semantica, non import opaco.

**Limite principale della ricerca:** nessun concorrente è stato installato/eseguito in questa sessione. Le strategie applicative, le differenze di versione e i comportamenti in specifici editor richiedono test riproducibili.
