# Soluzioni tecniche osservate nei concorrenti

**Formato di ogni pattern:** problema → evidenza concreta → nostra proposta → tradeoff e verifica. Non è codice implementato.

## TP-01 — Evento trigger e delimitatore

- **Problema:** scattare a metà parola o consumare una punteggiatura involontariamente.
- **Evidenza:** Raycast distingue espansione immediata / dopo delimitatore mantenuto / scartato, keyword Unicode e word-boundary; AutoHotkey usa hotstrings con end chars e reset al cambio della finestra attiva; Espanso ha `word: true` e separator config.
- **Proposta:** un tipo `TriggerRule` esplicito con `ActivationMode`, `DelimiterPolicy`, `WordBoundary`, `CasePolicy`, `ApplicationScope` e ordine di precedenza.
- **Test:** `;sig`, `dà`, `İ`, CJK, `word` dentro `sword`, Enter/Tab, metodi IME; collisioni con macOS system replacements.
- **Fonti:** https://manual.raycast.com/snippets ; https://www.autohotkey.com/docs/v2/Hotstrings.htm ; https://espanso.org/docs/matches/basics/.

## TP-02 — Multiple insertion backends

- **Problema:** un'app non accetta tasti simulati, oppure il paste rovina layout/clipboard.
- **Evidenza:** Espanso documenta keys/clipboard e `force_mode`; PhraseExpress usa VK_PACKET, Sendkey/clipboard; Keyboard Maestro distingue typing e paste.
- **Proposta:** `ITextInsertionStrategy`, capability per app/OS e fallback *solo* dopo diagnosi controllata; preferenze avanzate con motivo e azione di test.
- **Verifica:** test A/B su Notepad, browser, Office, Electron, terminale, VS Code, input Unicode, shortcut intervenuti.
- **Fonti:** https://espanso.org/docs/matches/basics/ ; https://www.phraseexpress.com/support/faq/ ; https://wiki.keyboardmaestro.com/manual/Text_Triggers.

## TP-03 — Clipboard lease, non transazione

- **Problema:** cancellare un'immagine o sovrascrivere una copia utente durante il ripristino.
- **Evidenza:** PhraseExpress descrive backup clipboard/focus/delay/restore; Alfred rende modificabili i timing del restore; i report su aText e Text Blaze documentano difficoltà analoghe.
- **Proposta:** un `ClipboardLease` che tenti di preservare i formati supportati, registri generazione/owner *se esposto*, ripristini soltanto quando è ragionevolmente sicuro; possibilità di disabilitare restore o incollare manualmente.
- **Limite:** i sistemi non offrono una transazione universale su appunti multi-formato e clipboard managers; condizioni di gara inevitabili.
- **Test:** immagine+HTML+testo, clipboard manager, user copies during paste, restore delay, app lenta e crash.
- **Fonti:** https://www.phraseexpress.com/support/faq/ ; https://www.alfredapp.com/help/features/snippets/advanced/.

## TP-04 — Target focus e privacy

- **Problema:** il picker apre una finestra e la sostituzione finisce in un'altra.
- **Evidenza:** FAQ PhraseExpress descrive esplicitamente questa failure e propone copia manuale; Alfred non espande in secure text fields; aText segnala Secure Input.
- **Proposta:** catturare identity del target, riaffermare focus solo nel rispetto OS, confrontare il target prima dell'azione irreversibile, abortire in caso di dubbio.
- **Fallback:** copia negli appunti e scelta manuale dell'utente; non aggirare campi protetti.
- **Fonti:** https://www.phraseexpress.com/support/faq/ ; https://www.alfredapp.com/help/features/snippets/auto-expansion/.

## TP-05 — Cursore dentro testo Unicode

- **Problema:** un emoji ZWJ è composto da più code point e la sequenza Backspace/Left può divergere fra editor.
- **Evidenza:** il codice Beeftext precedentemente studiato usa spostamenti sintetici stimati; Alfred, Raycast, TypeIt4Me documentano un cursor placeholder.
- **Proposta:** rappresentare il placeholder nel piano di espansione e calcolare offset logici/grapheme, ma applicare cursor positioning solo con metodo verificato su target; distinguere `inserito ma cursore non posizionato`.
- **Test:** flag, skin tone, famiglia ZWJ, combinazioni accentate e bidi.
- **Fonti:** https://www.alfredapp.com/help/workflows/advanced/placeholders/ ; https://manual.raycast.com/dynamic-placeholders ; https://learn.microsoft.com/it-it/dotnet/api/system.globalization.stringinfo?view=net-10.0.

## TP-06 — Form e variabili prima degli effetti esterni

- **Problema:** un prompt annullato lascia un trigger cancellato.
- **Evidenza:** Espanso e Text Blaze offrono form strutturati; i placeholder e riferimenti di Alfred hanno un ordine di valutazione e limiti sulla ricorsione.
- **Proposta:** `parse -> validate -> evaluate variables -> collect form -> build plan -> preflight target -> commit`; rilevare riferimenti ciclici e proibire script da import in automatico.
- **Test:** cancel, scelta multipla, variabili mancanti, riferimenti ciclici, payload locale italiano/francese.
- **Fonti:** https://espanso.org/docs/matches/forms/ ; https://blaze.today/guides/forms/ ; https://www.alfredapp.com/help/workflows/advanced/placeholders/.

## TP-07 — Gerarchie, tags, picker e ricerca

- **Problema:** le librerie grandi sono difficili da navigare.
- **Evidenza:** Beeftext ha gruppi, PhraseExpress gerarchie, Alfred collections, Raycast tags e pin, Typinator ricerca rapida.
- **Proposta:** gruppo singolo P0 per compatibilità Beeftext, tag/favoriti P1, ricerca sui nomi/trigger/contenuto e picker con anteprima a riga singola e shortcut.
- **Tradeoff:** non costruire gerarchie profonde e tag complessi prima di misurare casi d'uso.
- **Fonti:** https://manual.raycast.com/snippets ; https://www.alfredapp.com/help/features/snippets/.

## TP-08 — Diagnostica nativa e capability matrix

- **Problema:** su Wayland, Secure Input, VM, RDP e app elevate l'espansione può non funzionare.
- **Evidenza:** Espanso mantiene varianti X11/Wayland con avvisi; AutoKey dichiara X11 e avverte su Wayland; Karabiner EventViewer e Hammerspoon eventtap mostrano cosa succede a livello eventi.
- **Proposta:** pannello `Diagnostica`: OS/sessione, permessi, metodo, test output app, esclusioni e log senza contenuto digitato.
- **Limite:** supporto Wayland dipende da compositor, accesso API e permessi; non spingere accessi device senza threat model.
- **Fonti:** https://github.com/autokey/autokey ; https://espanso.org/install/ ; https://www.hammerspoon.org/docs/hs.eventtap.

## TP-09 — Migrazione e formati di scambio

- **Problema:** chi ha 500 snippet non vuole riscriverli.
- **Evidenza:** Raycast documenta import da JSON e da TextExpander/aText/Espanso/PhraseExpress; Beeftext usa file di libreria e CSV/JSON nelle funzioni storiche.
- **Proposta:** import adapters separati con preview, differenze, conflitti, versioning, rollback e quarantena dei comandi eseguibili.
- **Test:** input malformato, caratteri Unicode/RTL, collisioni e riferimenti ciclici.
- **Fonti:** https://manual.raycast.com/snippets ; https://github.com/xmichelo/Beeftext.

## TP-10 — Case, lingua UI e locale payload

- **Problema:** lingua del menu (it-IT) diversa da quella dei template (fr-FR) o dei trigger in turco.
- **Evidenza:** Espanso espone case propagation, PhraseExpress supporta frasi tradotte, Avalonia offre ResX+FlowDirection RTL e spiega il problema dei binding statici nel cambio runtime.
- **Proposta:** lingua UI indipendente dal content locale, fallback inglese, layout RTL verificato e mapping Unicode testato. Nessun cambio lingua altera snippet salvati.
- **Fonti:** https://espanso.org/docs/matches/basics/ ; https://docs.avaloniaui.net/docs/app-development/localizing.

## TP-11 — Conflitti e selezione deterministica

- **Problema:** trigger duplicati creano risultato imprevedibile; altri sistemi e applicazioni possono intercettare la stessa sequenza.
- **Evidenza:** il codice Beeftext esaminato nelle versioni precedenti seleziona casualmente in alcuni conflitti; PhraseExpress e Keyboard Maestro forniscono picker/selettore in scenari di ambiguità, Raycast descrive override system snippets come opzione sperimentale.
- **Proposta:** collisioni segnalate nell'editor; priorità scope app > gruppo attivo > globali solo se formalizzata; quando resta ambiguo, scegliere manualmente oppure non espandere. Mai random.
- **Test:** duplicati dopo import, case-fold/canonical equivalents, Mac built-in replacement, due expander attivi.
- **Fonti:** https://manual.raycast.com/snippets ; https://www.phraseexpress.com/doc/.

## TP-12 — Continuità dei dati

- **Problema:** import o update corrompe libreria personale.
- **Evidenza:** PhraseExpress ha workflow avanzati di gestione libreria/backup; il codice Beeftext esaminato storicamente ha metodi save/load che richiedono protezioni per operazioni atomiche nella nuova app.
- **Proposta:** export JSON versionato, backup automatico discreto, hash/valida/test restore, rollback prima di migrazione, cestino/undo.
- **Test:** failure injection, disco pieno, backup con file corrotti, migrazione schema.
- **Fonti:** https://www.phraseexpress.com/doc/ ; https://github.com/xmichelo/Beeftext.

## TP-13 — Limiti di complessità

- **Problema:** prendere tutte le feature della concorrenza rende ingestibile il prodotto.
- **Evidenza:** FastKeys/Keyboard Maestro/Hammerspoon sono suite di automazione; TextExpander/Text Blaze hanno funzioni avanzate con scope team e formule.
- **Proposta:** P0 motore+GUI semplice, P1 form/picker/variabili, P2 rich text/emoji/autocorrect, P3 scripting/cloud; nuova feature solo con utilità verificata e test stimati.
- **Test:** utenti reali riescono a creare/espandere uno snippet senza aprire impostazioni avanzate.

## TP-14 — Sicurezza di dati privati

- **Problema:** osservare tutti gli input aumenta il rischio privacy.
- **Evidenza:** Alfred dichiara di non ascoltare secure text fields; i produttori riconoscono interferenze fra hook e software bancari/antivirus. Il tool del sistema può non supportare sostituzioni in ogni app.
- **Proposta:** niente registrazione delle battute, riduzione permessi, campi protetti no expansion, app excluded, logging redatto e fallback manuale.
- **Fonti:** https://www.alfredapp.com/help/features/snippets/auto-expansion/ ; https://www.phraseexpress.com/support/faq/.

## Pattern v1.6 — approfondimenti mirati

- **TP-15 — Buffer per target, volatile:** suffix limitato, reset su cambio focus, caret/selection, navigazione, IME incerto. Lezione da [Keyboard Maestro](https://wiki.keyboardmaestro.com/trigger/Typed_String) e [AutoHotkey](https://doggy8088.github.io/AutoHotkeyDocs/docs/Hotstrings.htm); evitare keylogging.
- **TP-16 — Trigger Preview/Conflict UI:** esempio live di `keep/drop`, avvisi collisione anche dopo import; evidenza [TextExpander](https://textexpander.com/learn/using/snippets/conflicting-snippet-abbreviations) e [Raycast](https://manual.raycast.com/snippets).
- **TP-17 — Preflight irreversibilità:** riconoscere un match non autorizza backspace; per app/contesti ignoti nessuna cancellazione. Evidenza [Keyboard Maestro](https://wiki.keyboardmaestro.com/trigger/Typed_String), proposta nostra da verificare.
- **TP-18 — Case/Unicode policy esplicite:** differenziare insensitive/propagate/normalization e layout IME, come suggeriscono [Espanso](https://espanso.org/docs/matches/basics/) e [.NET](https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction).
- **TP-19 — Diagnostica senza payload:** aText e Alfred documentano interruzioni Secure Input e interferenze; strumento guidato privacy-safe per i nostri adapter. Fonti: [aText](https://www.trankynam.com/atext/doc/), [Alfred](https://www.alfredapp.com/help/troubleshooting/snippets/).

Approfondimenti e confronti: [TRIGGER_RECOGNITION_BENCHMARK.md](TRIGGER_RECOGNITION_BENCHMARK.md).
