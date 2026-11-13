# Concorrenza e riferimenti tecnici — indice v1.5

**Perimetro:** 9 prodotti inizialmente richiesti + 7 nuovi riferimenti, più Beeftext come baseline separata = **16 comparatori + 1 riferimento originario**. Queste non sono 16 alternative identiche: alcuni sono launcher, engine di scripting, remapper o una funzionalità del sistema operativo.

| Profilo | Tipo | Lezione principale | Target da noi |
|---|---|---|---|
| [Beeftext](competitors/01-beeftext.md) | Baseline | GUI gruppi/snippet e formato import | Migrazione/compatibilità |
| [Espanso](competitors/02-espanso.md) | Text expander open source | Backend per contesto + forme/sintassi | P0/P1 |
| [PhraseExpress](competitors/03-phraseexpress.md) | Suite professionale | Compatibilità Unicode / focus / archivi | P0/P1 |
| [FastKeys](competitors/04-fastkeys.md) | Automazione Windows | Autocomplete e contesto | P2 |
| [TextExpander](competitors/05-textexpander.md) | Snippet collaboration | Form e librerie condivise | P1/P3 |
| [aText](competitors/06-atext.md) | Text expander | Clipboard e fallback | P0 |
| [Karabiner-Elements](competitors/07-karabiner.md) | Remapper | Pipeline input/output macOS | Studio nativo |
| [Keyboard Maestro](competitors/08-keyboard-maestro.md) | Automazione Mac | Trigger/clipboard/injection | P0 tecnico |
| [Text Blaze](competitors/09-text-blaze.md) | Snippet dinamici | Forms e formule | P1/P2 |
| [Typinator](competitors/10-typinator.md) | Text expander | UX compatta/autocorrect | P1/P2 |
| [AutoKey](competitors/11-autokey.md) | Linux automation | X11 vs Wayland, layout | Studio nativo |
| [AutoHotkey](competitors/12-autohotkey.md) | Hotstrings Windows | Modalità trigger, delimitatori | P0/P1 |
| [Alfred](competitors/13-alfred.md) | Launcher + snippets | Picker, raccolte, compatibilità app | P1 |
| [Raycast](competitors/14-raycast.md) | Launcher + snippets | Triggers delimiters, tags, import | P0/P1 |
| [TypeIt4Me](competitors/15-typeit4me.md) | Text expander Mac | Prompt/placeholder/cursore | P1 |
| [Hammerspoon](competitors/16-hammerspoon.md) | Automation API Mac | Osservazione input vs keyStrokes | Studio nativo |
| [macOS Replacements](competitors/17-macos-system.md) | OS built-in | Conflitti con replacement di sistema | Test P0 |

## Approfondimenti già esistenti

I nove concorrenti iniziali rimangono tracciati nelle schede individuali, nel [registro fonti](BENCHMARK_SOURCE_REGISTER.md) e nella [sintesi precedente](LEGACY_FINDINGS_RETAINED.md); i PDF e le versioni ridondanti sono stati eliminati per richiesta.

## Risultati maggiori della comparazione

1. **Problema fondamentale = focus/clipboard più che GUI.** PhraseExpress e Alfred documentano ritardi/azioni specifiche: non forzare una promessa di affidabilità totale non testata.
2. **Modello trigger distinguibile.** AutoHotkey e Raycast espongono delimitatore, case/scope, keyword e modalità immediate: una GUI chiara può rappresentarli meglio di script.
3. **Interfaccia dei launcher = reference per picker.** Alfred/Raycast hanno ricerca rapida, tag, collection e azioni “inserisci/copia”.
4. **Unicode come pipeline.** Font, grapheme, IME, input strategy, target possono fallire indipendentemente.
5. **Wayland non è X11.** Espanso ha un backend sperimentale con permessi speciali, AutoKey documenta X11 e inadeguatezza in Wayland.
6. **I data formats sono importanti per adottare il prodotto.** Raycast importa JSON e dati da altre app; il nostro import deve mostrare anteprima e non eseguire script.
7. **Scripting/collaboration sono opzionali.** Non copiare la complessità di suite generaliste.

## Metodo e cautele

- Schede basate su documentazione ufficiale e precedente studio documentale. **Nessuno dei prodotti è stato installato e misurato in questa attività.**
- `V` = documentato, `I` = inferenza tecnica, `P` = proposta e `T` = test necessario. I vantaggi/svantaggi sono qualitativi e non classifiche prestazionali.
- Prezzi, piani e versioni cambiano; questa ricerca NON fissa prezzi o limiti numerici commerciali, salvo caratteristica esplicitamente ricavata dalla fonte.
- `research/SOURCES.md` contiene URLs con area e tipo di evidenza; `data/COMPETITOR_MATRIX_v1.5.csv` è una matrice ragionata, non un test.

## Aggiornamento v1.6 — confronto trasversale trigger

La sintesi dei comportamenti confrontati e delle relative fonti si trova nel [Benchmark mirato](TRIGGER_RECOGNITION_BENCHMARK.md). I profili individuali sono stati aggiornati in appendice senza sostituire i precedenti. Priorità di ricerca: PhraseExpress, Keyboard Maestro, TextExpander, Raycast, Espanso, FastKeys, Text Blaze, Alfred, aText; strumenti come Karabiner sono riferimenti tecnici non benchmark "pari categoria". Tutte le caratteristiche rappresentano funzioni *documentate*, non prove di compatibilità.
