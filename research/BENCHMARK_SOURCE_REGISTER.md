# Registro fonti primarie — primo benchmark dei 9 concorrenti

**Consultate il 12 novembre 2026.** Indicazioni [V] = documentato; [I] = inferenza; [P] = proposta; [T] = test non eseguito. La descrizione del funzionamento interno dei prodotti proprietari non deriva da reverse engineering. Le pagine dei fornitori possono mutare in seguito. Le fonti sono riportate per permettere il riesame.

| ID | Tema | Fonte primaria | Nota |
|---|---|---|---|
| S01 | Espanso matching/backend/Unicode | https://espanso.org/docs/matches/basics/ | `force_mode`, backend e Unicode, trigger e vars |
| S02 | PhraseExpress metodi di inserimento | https://www.phraseexpress.com/doc/settings/expert/ | `VK_PACKET`, `Sendkey`, clipboard, Enter/Tab |
| S03 | KM metodo di inserimento | https://wiki.keyboardmaestro.com/action/Insert_Text | Typing, Pasting, Keystroke |
| S04 | KM ripristino clipboard e race | https://wiki.keyboardmaestro.com/Frequently_Asked_Questions | FAQ su restore clipboard |
| S05 | Text Blaze perdita clipboard non testuale | https://community.blaze.today/t/why-does-using-a-snippet-clear-my-clipboard/67056 | Risposta staff con rettifica, 15/09/2026 |
| S06 | Unicode grapheme cluster | https://www.unicode.org/reports/tr29/ | UAX #29, standard internazionale |
| S07 | .NET StringInfo | https://learn.microsoft.com/en-us/dotnet/api/system.globalization.stringinfo.gettextelementenumerator?view=net-10.0 | Unità di testo percepita / grapheme |
| S08 | PhraseExpress ambiguità | https://www.phraseexpress.com/features/ | Autotext duplicato → popup di scelta |
| S09 | KM typed string e conflitti | https://wiki.keyboardmaestro.com/trigger/Typed_String | Longest match, palette, diacritici e password |
| S10 | FastKeys editor e conferma manuale | https://www.fastkeysautomation.com/documentation6/TextExpander.html | Per-trigger settings |
| S11 | Espanso Linux/Wayland e privilegi | https://espanso.org/docs/install/linux/ | evdev/uinput, capability, limiti Linux |
| S12 | Espanso repository e licenza | https://github.com/espanso/espanso | GPL-3.0, Rust, dipendenze Linux |
| S13 | Espanso forms | https://espanso.org/docs/matches/forms/ | Form strutturati |
| S14 | Espanso GUI search | https://espanso.org/docs/configuration/options/ | Search UI e hotkey |
| S15 | Espanso backend profili/ritardi | https://espanso.org/docs/configuration/options/ | `backend`, `clipboard_threshold`, clipboard e app-specific |
| S16 | PhraseExpress feature list | https://www.phraseexpress.com/features/ | **Lista principalmente Windows** |
| S17 | FastKeys sito e piattaforma | https://www.fastkeysautomation.com/ | All-in-one Windows, giugno 2026 |
| S18 | FastKeys text expander docs | https://www.fastkeysautomation.com/documentation6/TextExpander.html | Delimitatori, popup, raw, clipboard |
| S19 | FastKeys send/forms | https://www.fastkeysautomation.com/documentation6/Send.html | Multiple substitutes, macro fields |
| S20 | FastKeys team sharing | https://www.fastkeysautomation.com/documentation6/TeamSharing.html | File/cartelle condivise |
| S21 | TextExpander prodotto/piattaforme | https://textexpander.com/ | Sito aggiornato, piani e device |
| S22 | TextExpander sezioni opzionali | https://textexpander.com/learn/using/snippets/snippet-fill-ins/optional-section-fill-in | Fill-in annidati, campi ripetuti |
| S23 | TextExpander supporto lingue | https://textexpander.com/learn/getting-started/getting-around/textexpander-supported-languages | Output vs trigger/UI: differenza esplicita |
| S24 | TextExpander conflitti sincronizzazione | https://textexpander.com/learn/troubleshooting/general/why-are-snippets-in-the-app-and-on-the-website-different | Conflict groups |
| S25 | aText funzionalità | https://www.trankynam.com/atext/ | Windows/Mac, portable, sync |
| S26 | aText FAQ | https://www.trankynam.com/atext/doc/ | Secure Input, Clipboard, ritardi e picker |
| S27 | Karabiner funzionalità | https://karabiner-elements.pqrs.org/docs/getting-started/features/ | Remap e Unicode via clipboard/hex |
| S28 | Karabiner licenza | https://github.com/pqrs-org/Karabiner-Elements/blob/main/LICENSE.md | Unlicense / public domain declaration |
| S29 | Karabiner pipeline eventi | https://karabiner-elements.pqrs.org/docs/manual/misc/event-modification-chaining/ | Hardware → modifiche → virtual keyboard |
| S30 | Keyboard Maestro sito | https://www.keyboardmaestro.com/main/ | Automazione macOS |
| S31 | Text Blaze sito | https://blaze.today/ | Estensione Chrome + desktop Win/Mac |
| S32 | Text Blaze dynamic commands | https://blaze.today/commands/ | Sintassi e nested commands |
| S33 | Text Blaze forms | https://blaze.today/guides/forms/ | Tipi, validazione, piano gratuito limitato |
| S34 | Typinator sito / feature list | https://ergonis.com/en/typinator/features/ | Set, form, autocorrect, quick search |
| S35 | Typinator stato Windows beta e novità | https://ergonis.com/en/typinator/download/ ; https://ergonis.com/en/typinator/ | Mac 10.3 ott. 2026, Windows beta |
| S36 | Typinator set/estensioni | https://ergonis.com/en/typinator/extensions/ | Autocorrect multilingua, set condivisi |

## Valutazione delle evidenze

- **Livello A:** manuale ufficiale con descrizione del comportamento (Espanso backend, PhraseExpress injection, KM clipboards, Karabiner pipeline).
- **Livello B:** feature list/marketing del produttore, non testato (compatibilità con «tutte» le applicazioni, performance, privacy).
- **Livello C:** testimonianza tecnica sul forum con risposta dello staff (Text Blaze 15/09/2026); puntuale, non estendibile automaticamente ad altre versioni.
- **Livello D:** nostre inferenze e proposte esplicitate. Un problema documentato di un concorrente **non** è prova del comportamento della nostra applicazione né di ogni concorrente.

## Controlli in futuro

1. Verificare le versioni software e licenze al momento dello spike tecnico.
2. Eseguire test reali senza dati sensibili su Windows/macOS/Linux, almeno quattro famiglie di editor per OS.
3. Distinguere Unicode output, Unicode trigger, IME e caratteri composti; non bastano screenshot promozionali.
4. Verificare la compatibilità legale della scelta di licenza del nuovo progetto prima dell'eventuale riutilizzo di codice open source.


Aggiornamento struttura v1.9: le fonti storiche qui riportate restano consultabili senza la vecchia cartella di copie.
