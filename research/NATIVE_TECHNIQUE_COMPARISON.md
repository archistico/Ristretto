# Confronto tecnico dei concorrenti — input/output nativo v1.7

**Ricerca documentale del 12-11-2026.** Il materiale seguente descrive *pattern progettuali* e limiti attestati dalle fonti, non un benchmark con installazioni eseguite.

## Criteri uniformi di confronto

Per ogni prodotto osservare: `detection`, `commit/IME`, `insertion method`, `clipboard`, `focus/permissions`, `scoped configuration`, `fallback`, `evidence` e `risk`.

| Prodotto / componente | Soluzione documentata o identificata | Beneficio per noi | Rischio/limite | Fonte principale |
|---|---|---|---|---|
| Beeftext | C++/Qt e API Windows; clipboard e simulazione di tasti; manager globali | Conoscere il flusso storico e importare librerie | Focus, timeout, ripristino clipboard e cursor offset fragili | https://github.com/xmichelo/Beeftext |
| Espanso | `backend: auto/clipboard/inject`, `clipboard_threshold`, override per app; evdev/uinput su Wayland | Adattatore selezionabile per target, fallback non nascosto | Permessi e compositor; soglia non universale | https://github.com/espanso/espanso/blob/dev/schemas/config.schema.json |
| PhraseExpress | Metodi di emissione alternativi, diagnostica e regole per applicazione | Compatibilità configurabile ma guidata | Complessità dell'interfaccia e differenze fra edition/OS | https://www.phraseexpress.com/doc/settings/expert/ |
| AutoHotkey | Hotstrings, delimitatori, confini e opzioni di invio | Comportamento prevedibile del matcher | Windows-only; layout e metodo invio da verificare | https://www.autohotkey.com/docs/v2/Hotstrings.htm |
| Keyboard Maestro | Text triggers e scelta fra inserimento/typing; avvisi cancellazione | Non assumere che la cancellazione del trigger sia sempre lecita | macOS-only; macro engine fuori scope | https://wiki.keyboardmaestro.com/trigger/Typed_String |
| Karabiner-Elements | Remapping di eventi a livello dispositivo/macOS | Studiare tasti fisici vs caratteri | Non è un motore di input Unicode arbitrario; permessi nativi | https://karabiner-elements.pqrs.org/ |
| AutoKey | Sostituzione e scripting su Linux X11 | Separazione X11/Wayland, test layout | Wayland non equivalente a X11 | https://github.com/autokey/autokey |
| Alfred | Picker/snippets, workaround e ritardi documentati | Fallback manuale e compatibilità senza schermate complesse | Mac-only, incolla/focus non sempre controllabili | https://www.alfredapp.com/help/features/snippets/advanced/ |
| Raycast | Modalità trigger/delimitatore e picker | UX chiara su trigger e conferma | Mac e piattaforme a supporto differenziato; non risolve universalmente IME | https://manual.raycast.com/snippets |
| Typinator | Autocorrect e snippet con UX essenziale | Pipeline invisibile quando funziona, diagnostica se fallisce | Qualità per editor/OS non deducibile dal marketing | https://ergonis.com/en/typinator/features/ |

### Lezione 1 — backend multipli senza menu tecnici invasivi

Pattern proveniente da Espanso: scelta automatica con override per target; l'utente normalmente non deve sapere cosa siano `VK_PACKET`, XTest o il pasteboard. La GUI presenta `Automatico`, `Compatibilità elevata`, `Digitazione simulata` solo se gli spike ne confermano significato per quella piattaforma. Debug esplicito separato.

### Lezione 2 — la distinzione evento / testo confermato è fondamentale

Da API Windows e Wayland text-input: evento tasto non equivale sempre a Unicode confermato. Dead keys, layout, ligature, dictation, autocorrect e IME possono trasformare l'input. **Non presentare l'osservazione fisica come osservazione generale di testo committed.** I concorrenti sono fonte di ipotesi: non disponiamo di prove che risolvano tutti gli IME.

### Lezione 3 — clipboard lease best-effort

Beeftext e strumenti analoghi si affidano all'incolla; Windows e Apple espongono seriali/changeCount, ma non sono primitive transazionali. Servono snapshot selettivi, restore condizionale e fallback; un utente che copia durante l'espansione non deve perdere la sua nuova clipboard.

### Lezione 4 — Wayland non è un singolo backend

Espanso rende visibile il costo pratico del percorso evdev/uinput; i portal GlobalShortcuts/RemoteDesktop/InputCapture hanno finalità diverse e non equivalenti. Una GUI Avalonia può funzionare perfettamente senza che l'espansione automatica sia disponibile: progettare la *capability discovery* e un prodotto comunque utile con picker/copia.

### Lezione 5 — evitare overengineering

Non copiare interi framework macro da Keyboard Maestro, PhraseExpress e FastKeys. Scomporre solo le responsabilità che hanno input/output differenti; un matching engine puro e adapter nativi con contratti minimi sono sufficienti alla prima fase.

## Cose che NON possiamo affermare ora

- Che uno specifico concorrente inserisca correttamente **tutti** gli emoji/IME su **ogni** OS/editor.
- Che `KEYEVENTF_UNICODE` produca lo stesso risultato di un paste in ogni applicazione.
- Che l'accessibilità permetta una cancellazione atomica in tutti i campi editabili.
- Che il portale RemoteDesktop costituisca un percorso universale e invisibile per i text expander.
- Che qualsiasi app XWayland si comporti esattamente come la stessa app su X11 nativo.

## Opportunità da conservare per il futuro

1. Diagnostica di compatibilità con suggerimento backend per app (P1).
2. Profili specifici per editor e preview dei conflitti (P1).
3. Catalogo compatibilità condivisibile localmente, senza inviare testo privato (P2).
4. Motore form e inserimento in campi multipli (P2, attenzione focus).
5. Rich text e multi-MIME solo dopo corretta gestione ownership e collisioni clipboard (P2).
6. Undo automatico soltanto su editor qualificati (P2, non promettere rollback universale).

*Le fonti tecniche verificabili sono indicizzate in [NATIVE_SOURCE_EVIDENCE.md](NATIVE_SOURCE_EVIDENCE.md).*
