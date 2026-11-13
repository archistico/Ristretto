# Piano riproducibile degli spike nativi — v1.7

**Solo specifica delle prove. Nessun adapter o test OS è stato compilato/eseguito.**  
Scenari macchina: [NATIVE_SPIKE_MATRIX.csv](../data/NATIVE_SPIKE_MATRIX.csv).

## Gate A6 — Windows

1. Ambiente: Windows 11 non elevato, lingua IT, tastiera IT e US-intl; registrare versione, architettura, eventuale RDP.
2. Prototipo isolato (non prodotto): verifica `WH_KEYBOARD_LL`, thread dedicato, ripristino dopo errore, flag sintetici; **solo conteggi, mai keylog**.
3. Confrontare `SendInput` `KEYEVENTF_UNICODE`, pressioni simulate e paste su Notepad, VS Code, Chromium/Firefox, campo multilinea, finestra elevata e terminale; raccogliere solo fixture sintetiche.
4. Indagare `UIA_IsPasswordPropertyId`, campo non editabile, focus che cambia, desktop protetto e UIPI; aspettativa fail-closed.
5. Clipboard concorrenziale: testo, HTML, immagine, mixed MIME, delayed rendering e altro processo che copia durante espansione. Verificare nessuna perdita della nuova copia.
6. Test passaggio sospensione/session lock e chiusura forzata: nessun hook zombie o clipboard lasciata in stato inatteso.

## Gate A7 — macOS

1. macOS versioni supportate da definire; registrare architettura Intel/Apple Silicon e versione del bundle.
2. Permessi Accessibility/Input Monitoring dove richiesti: grant/deny/revoke, aggiornamento dell'app e riavvio; misurare i segnali reali disponibili.
3. Quartz event taps + classificazione IME e dead keys senza acquisire testo privato; verifica dei contesti Secure Input.
4. Metodi inserimento: evento Unicode, paste e copia manuale in TextEdit, browser, IDE, app Electron, terminale; comparare grapheme e caret.
5. NSPasteboard: snapshot multi-type supportato, `changeCount`, altra app che modifica clipboard dopo il paste e concorrenza durante l'operazione.
6. Picker, cambio app, salvataggio della destinazione, revoca permessi a runtime; nessun testo inserito in una finestra diversa.

## Gate A8 — Linux X11

1. Registrare distro, desktop, sessione **X11** vs XWayland e layout; il nome della distro da solo non basta.
2. XTest vs clipboard, XKB layout, dead keys, ibus/fcitx; misurare errori e casi ambigui.
3. Separare PRIMARY e CLIPBOARD, clipboard manager e target con multi-MIME; test comportamenti dopo la chiusura dell'owner.
4. Browser/GTK/Qt/Electron/terminale come target; virtual machine e accessibilità solo dove rappresentativi.
5. Assenza di rilevazione campo protetto → fail-closed senza keylogging persistente.

## Gate A8W — Linux Wayland, almeno tre compositor

1. GNU/Linux su sessioni **GNOME/Mutter**, **KDE/KWin**, **Sway o altra wlroots**. Registrare versione compositor e versione xdg-desktop-portal/backend.
2. Capability probing per `GlobalShortcuts`: sessione, conferma utente, revoca, disponibilità; test apertura picker indipendente dall'espansione.
3. Verifica accesso `RemoteDesktop` autorizzato e `InputCapture` **solo come fattibilità controllata**, senza sostenere che siano meccanismi generici di text expansion.
4. Verifica protocolli opzionali di virtual keyboard e data control (senza assumerne l'esposizione a processi normali).
5. Per `evdev/uinput`, redigere prima threat model e istruzioni di autorizzazione/revoca; nessun avvio implicito come root, nessun add al gruppo `input` non spiegato.
6. Valutare tier `T0` GUI+copio, `T1` hotkey+picker, `T2` emissione autorizzata, `T3` auto-match provato per editor; certificare **separatamente** per compositor.

## Fixture sintetiche (non usarne di reali/personali)

| ID | Stringa/fattore | Cosa verificare |
|---|---|---|
| F-01 | `caffè déjà vu mañana Łódź` | Accenti/precomposti |
| F-02 | `e\u0301` (NFD) | Preservazione sequenza/normalizzazione |
| F-03 | `👩‍💻 👨‍👩‍👧‍👦 👍🏽 🇮🇹 ❤️` | ZWJ, flag, variation selector |
| F-04 | `日本語 한국어 中文` | CJK e IME |
| F-05 | `العربية עברית ABC 123` | RTL con contenuto LTR |
| F-06 | `SELECT N'😀';\n\t-- naïve` | codice, newline, tab |
| F-07 | testo da 1, 100, 1.000, 10.000 scalari | misure latenza/backend |
| F-08 | copiare testo B mentre si incolla testo A | race clipboard: non perdere B |
| F-09 | picker in primo piano, target cambia | focus: nessun inserimento nella finestra errata |
| F-10 | campo password/secure input | nessuna espansione / nessun log sensibile |

## Formato del report esperimento

`scenario_id`, `date`, `commit`, `os_release`, `session`, `compositor`, `portal_backend`, `target_app_version`, `input_layout`, `ime`, `permission_state`, `method`, `expected`, `actual`, `status`, `latency_ms`, `notes_redacted`.

Risultati ammessi: `NOT_RUN`, `PASS`, `FAIL`, `BLOCKED`, `NOT_APPLICABLE`. `BLOCKED` non è `PASS` e la verifica di un campo interno Avalonia non certifica il supporto esterno.

## Criteri di uscita (prima di dichiarare supporto)

- **Zero** inserimenti in campo protetto nei test noti; scenari non osservabili restano `UNKNOWN` e disabilitano espansione automatica.
- Filtro eventi sintetici che non genera loop; timeout/pausa senza deadlock.
- Non sovrascrivere una clipboard cambiata dall'utente; dove non possibile, limitazione indicata e opt-in.
- Almeno due famiglie di app testate per backend, e suite Unicode + focus + clipboard senza regressioni; definire in seguito soglie quantitative di affidabilità.
- Unità di documentazione distribuzione: platform capability table visibile anche agli utenti.

## Registrazione dei blocchi

Se non sono disponibili host macOS o Wayland, non simulare il loro comportamento attraverso mock dichiarando il gate passato. Conservare `BLOCKED: host unavailable` e continuare su motore puro/documentazione.
