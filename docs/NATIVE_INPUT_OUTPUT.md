# Nota output v2.6: ack dei backend e proprietà clipboard

Nessun `SendInput`/paste/event tap/XTest produce in modo universale una verifica *end-to-end* del testo nell'editor esterno. I metodi di inserimento devono restituire fase e livello di evidenza, non solo success/fail. Per la clipboard le verifiche change sequence / change count / selection owner supportano un ripristino **condizionato**; non provano atomicità né preservazione di ogni formato. [Contratto coordinatore](OUTPUT_COORDINATION.md) · [piano prove](OUTPUT_TEST_PLAN.md).

---

# Integrazioni native e inserimento del testo — contratto tecnico candidato v1.7

**12 novembre 2026 · Studio documentale, NON test eseguiti · Stato: PROPOSTA, non ADR ratificato.**  
Fonti primarie e limiti: [NATIVE_SOURCE_EVIDENCE.md](../research/NATIVE_SOURCE_EVIDENCE.md).  
Piano di verifica: [NATIVE_SPIKE_PLAN.md](../research/NATIVE_SPIKE_PLAN.md).

## 1. Problema che dobbiamo risolvere

Un text expander non controlla l'applicazione nella quale scrive. Il suo lavoro è osservare un input, riconoscere una sequenza, capire *se* sia sicuro intervenire, sostituirla e conservare l'integrità di focus, clipboard, layout, testo Unicode e selezione. **Non esiste una transazione atomica universale fra applicazioni.**

Separare almeno cinque capacità indipendenti:
1. `GlobalHotkey`: aprire picker/pausare il motore.
2. `InputObservation`: conoscere eventi nativi (non necessariamente testo confermato).
3. `CommittedTextObservation`: conoscere testo *effettivamente* confermato dal sistema di input, se disponibile.
4. `TargetVerification`: identificare finestra/campo e stato di sicurezza in modo attendibile.
5. `TextInsertion`: inserire Unicode o eseguire paste senza effetti indesiderati.

Il supporto a una capacità **non implica** le altre. Non usare la disponibilità di una GUI, una tray o una scorciatoia globale per dichiarare espansione automatica funzionante.

## 2. Tabella delle strategie di inserimento

| Strategia | Quando considerarla | Rischi | Stato |
|---|---|---|---|
| Testo Unicode tramite API native/eventi di testo | Payload breve, target compatibile | Interpretazione del target, privilegi, IME, surrogati | Da provare per app |
| Tasti fisici/scancode sintetici | Shortcut, backspace, sequenze speciali | Layout, dead keys, tasti modificatori, perdita caratteri | Da provare per app |
| Clipboard `text/plain` + paste | Payload lunghi, emoji, CJK, multilinea | Concorrenza, formati multipli, clipboard manager, focus | Candidato preferibile su target compatibili, NON universale |
| Inserimento tramite accessibility/selection API | Campo identificato e controllabile | APIs variabili fra controlli, permessi, incompletezza | Ricerca separata, non presupporre |
| Picker con `Copia` esplicita | Auto-espansione indisponibile | L'utente deve incollare; non modificare il focus impropriamente | Fallback progettuale |

**Non adottare una regola statica del tipo “oltre 100 caratteri usa clipboard” senza misure reali**. Espanso documenta un limite configurabile e un backend `auto`, utile come pattern, non come soglia da copiare.

## 3. Windows — elementi API verificati documentalmente

- **Osservazione:** `SetWindowsHookEx(WH_KEYBOARD_LL)` / `LowLevelKeyboardProc`: richiede thread con message loop; callback rapido e senza I/O/UI. Un timeout può causare rimozione silenziosa dell'hook. Le istruzioni Microsoft raccomandano lavorazione fuori callback; per monitoraggio puro suggeriscono anche Raw Input, che tuttavia non risolve automaticamente il problema del testo confermato e dell'IME.
- **Segnalazione input sintetico:** `KBDLLHOOKSTRUCT.flags` espone `LLKHF_INJECTED` e `LLKHF_LOWER_IL_INJECTED`. Filtrare la provenienza degli eventi non è equivalente a riconoscere testo composto.
- **Emissione:** `SendInput` è soggetta a UIPI (un processo ordinario può non poter raggiungere una finestra elevata). `KEYEVENTF_UNICODE` usa eventi `VK_PACKET` e passa unità UTF-16; verifica obbligatoria in editor nativi, browser ed Electron, specialmente per coppie surrogate e IME.
- **Target:** processo/finestra foreground + UI Automation quando disponibile; `UIA_IsPasswordPropertyId` segnala campi protetti. L'assenza del segnale **non dimostra** che un campo sia sicuro.
- **Clipboard:** `GetClipboardSequenceNumber()` segnala cambi di contenuto in molti casi, ma non è un lock e la delayed rendering ne modifica la semantica. Windows permette clipboard con vari formati e ownership; non promettere ripristino byte-per-byte per oggetti opachi.
- **Limiti:** sessione UAC/secure desktop, applicazioni elevate, RDP, antivirus/hook di terzi, layout con dead keys, IME e password manager sono da testare separatamente.

**Spike Windows:** prima misurare eventi fisici, input sintetico e focus con logger che registra SOLO categoria di evento e timestamp; poi fixture Unicode note in Notepad, VS Code e browser, infine Office e terminale. Mai raccogliere testo reale digitato.

Fonti: Microsoft LowLevelKeyboardProc, KBDLLHOOKSTRUCT, SendInput, KEYBDINPUT, GetClipboardSequenceNumber, Clipboard Operations e UIA (URL nel registro fonti).

## 4. macOS — elementi API verificati documentalmente

- **Osservazione:** Quartz `CGEventTapCreate` può osservare/filtrare eventi in un run loop; i permessi dipendono dal tipo di monitoraggio/interazione. `AXIsProcessTrustedWithOptions` permette di verificare la fiducia Accessibility, non garantisce che il target consenta la sostituzione.
- **Secure Event Input:** quando attivo, non tentare bypass. In presenza di campo sensibile non osservabile, sospendere matching; aggiungere un messaggio di stato privo di contenuti.
- **Inserimento:** valutare CGEvent di testo, paste tramite Cmd+V, accessibilità dove consentito e fallback manuale; verificare di nuovo l'app destinataria quando chiude il picker.
- **Clipboard:** `NSPasteboard` supporta item e molteplici tipi; `changeCount` varia al cambio di ownership. È utile come indicatore di conflitto ma non garantisce un ripristino perfetto di oggetti, provider lazy o clipboard condivisa fra dispositivi.
- **Distribuzione:** va verificato comportamento dei permessi per applicazione firmata/non firmata, aggiornamenti e identificativo bundle. Non assumere che i permessi rimangano validi se cambia l'identità dell'app.

**Spike macOS:** testare grant/deny/revoke per Accessibility e Input Monitoring dove richiesto, Secure Input in campi protetti, cambio finestra durante picker, contenuto con emoji ZWJ, note app native, editor browser ed Electron, copia concorrente.

Fonti: Apple Quartz Event Services, `CGEventTapCreate`, `AXIsProcessTrustedWithOptions`, `NSPasteboard`, `NSPasteboard.changeCount`.

## 5. Linux X11 — elementi documentati e rischi

- **Osservazione:** eventi X11/XInput e layout XKB; un keycode non equivale a un carattere e non prova la conclusione della composizione IME.
- **Emissione:** XTest simula eventi tasto; XSendEvent non è universalmente rispettato dalle app. Applicare tasti fisici al testo Unicode internazionale è fragile. Per testo Unicode preferire sperimentazioni di clipboard UTF-8 dove disponibile, senza presupporre equivalenza con app XWayland.
- **Clipboard:** X11 distingue `PRIMARY` (selezione) e `CLIPBOARD` (copia esplicita). Il protocollo delle selezioni è mediato dall'owner, può essere lazy e richiedere un manager per la persistenza. Evitare di alterare PRIMARY quando si deve solo incollare su CLIPBOARD.
- **Sessioni:** distinguere X11 vero da applicazione XWayland su sessione Wayland; i risultati non si trasferiscono automaticamente.
- **Sicurezza:** osservare globalmente la tastiera su X11 è possibile ma invasivo: non memorizzare eventi, disabilitare per target esclusi, non dichiarare scoperti tutti i campi protetti.

**Spike X11:** almeno un ambiente KDE/GNOME/XFCE su X11 disponibile; registrare server/WM/versione, layout IT/US/dead keys, applicazioni X11 native e XWayland, clipboard multi-MIME e IME ibus/fcitx.

Fonte: freedesktop Clipboard Spec e documentazione XTest; repository/documentazione AutoKey come confronto.

## 6. Linux Wayland — limiti strutturali, non bug della GUI

Distinguere chiaramente i protocolli:

| Meccanismo | Offre | **Non offre automaticamente** |
|---|---|---|
| `org.freedesktop.portal.GlobalShortcuts` | Azione globale autorizzata | Stream del testo digitato in tutte le finestre |
| `org.freedesktop.portal.RemoteDesktop` con libei/Notify | Emissione di eventi in sessione autorizzata | Monitoraggio passivo e continuo di testo altrui |
| `org.freedesktop.portal.InputCapture` | Cattura input attivata dal compositor, con controllo di sessione | Keylogger generale in background; cattura immediata a richiesta |
| `zwp_text_input_v3` | Gestione composizione del campo di testo dell'app collegata | Accesso ai campi di altre applicazioni |
| `zwp_virtual_keyboard_v1` | Tastiera virtuale **se** compositor autorizza/protocollo esposto | Supporto universale, lettura testo, disponibilità su GNOME/KWin |
| `ext-data-control-v1` | Controllo clipboard privilegiato in compositor che lo offre | Clipboard globale disponibile a qualunque client senza policy |
| `evdev` + `uinput` (come Espanso Wayland) | Accesso dispositivo e simulazione a livello kernel se permessi e ambiente adeguati | Sicurezza/portabilità garantite; deploy senza privilegi; gestione IME universalmente corretta |

**Importante:** usare RemoteDesktop soltanto per aggirare i permessi di Wayland non è un obiettivo; valutarlo solo come integrazione autorizzata con UX adeguata. Il protocollo `text-input-v3` è rivolto al controllo che riceve input, non a una terza app che cerca di intercettare ogni parola.

**Proposta di capability tier** (non sono dichiarazioni di supporto):
- `T0`: GUI, libreria, editor, export, picker interno e copia esplicita.
- `T1`: apertura picker tramite scorciatoia globale via portal, ove implementato.
- `T2`: inserimento in app esterna con autorizzazioni e backend verificati.
- `T3`: auto-espansione in target terzo, SOLO se osservazione, focus, sicurezza e inserimento sono testati e supportati nel compositor/versione.

**Non pubblicare “Linux Wayland supportato” senza riportare almeno compositor + release, permessi, metodo, applicazioni testate e capacità disponibili.**

Fonti: XDG Desktop Portal `GlobalShortcuts`, `RemoteDesktop`, `InputCapture`; Wayland `text-input-v3`, `virtual-keyboard-v1`, `ext-data-control-v1`; schema di configurazione Espanso e issue di compatibilità del repository.

## 7. Contratto P0 proposto per i componenti nativi

La lingua del codice seguente è **pseudointerfaccia concettuale**; non è codice implementato e non richiede quattro progetti vuoti per ciascun OS.

```text
PlatformCapabilities:
  CanOpenGlobalPicker: Available | RequiresPermission | Unsupported | Unknown
  CanObservePhysicalKeys: ...
  CanObserveCommittedText: ...
  CanIdentifyTextTarget: ...
  CanIdentifyProtectedField: ...
  CanInsertUnicode: ...
  CanPastePlainText: ...
  CanRestoreClipboardConditionally: ...
  CanGuaranteeAtomicReplacement: false

InputEnvelope:
  sourceKind, synthetic, timestamp, targetGeneration, compositionState
  committedText?  // solo se realmente prodotto dal sistema, non inventato

ExpansionPreflight:
  targetIdentity, targetGeneration, insertionMethod, permissionState,
  triggerDeletionConfidence, clipboardLeaseCapability, userConsent

ExpansionOutcome:
  Completed | NotMatched | Cancelled | Unsupported | TargetLost |
  PermissionDenied | ProtectedField | InjectionRejected | PartialInsert |
  ClipboardConflict | CompositionUnknown
```

Gli adapter devono poter rispondere `Unknown` o `Unsupported`. Non convertire l'incertezza in `true`. I success/failure vengono mappati in messaggi localizzabili, log privi di payload e assistenza contestuale.

## 8. Algoritmo di preflight e inserimento — proposta

1. **Capture:** ricevi solo il suffisso volatile strettamente necessario; niente registrazione di tasti o password.
2. **Recognize:** usa il matcher puro e valuta collisioni/scope/configurazione.
3. **Prepare:** espandi variabili sicure e contenuto in memoria; calcola piano e posizione del cursore senza modificare ancora il target.
4. **Preflight:** conferma capacità del backend, permessi, identità del target e segnale non-protetto **con sufficiente confidenza**. Se non verificabile, annulla o richiedi azione manuale.
5. **Lease/Insertion:** blocca **solo le espansioni della nostra app**, non il sistema; se usa clipboard conserva un snapshot best-effort dei soli formati supportati; marca il proprio contenuto, incolla e verifica condizioni osservabili. Non usare sleep fisso come prova di paste riuscito.
6. **Cleanup:** ripristina clipboard **solo se è ancora il nostro contenuto**, altrimenti preserva la nuova copia effettuata dall'utente e segnala conflitto. Il conteggio seriale è un indizio, non lock assoluto.
7. **Finalize:** ripristino focus/posizionamento solo con capacità dimostrata, messaggio locale corretto e log senza testo. Se si è verificato `PartialInsert`, non promettere undo transazionale universale.

**Cancellazione trigger:** non dedurre il numero di Backspace dal numero di `char` UTF-16, `Rune` o perfino grapheme in modo cieco. La semantica del backspace dipende dall'editor/IME. Prevedere fallback `NoAutomaticDeletion` per target non qualificati.

## 9. Privacy e permessi (non negoziabili)

- Nessun keylogging persistente, telemetria del digitato, acquisizione del contenuto di campi protetti o bypass Secure Input/UAC/portali.
- Allowlist/denylist di applicazioni e stato esplicito del motore; modalità pausa effettiva; revoca permessi gestita senza crash.
- Su Wayland privilegi `input`/`uinput` non diventano un requisito invisibile: solo percorso opzionale, documentato, con consenso e rischio esplicitato.
- Log: `platform`, `capability`, `editorClass` non sensibile, `resultCode`, timestamp, versione; NON snippet espanso, trigger reale, clipboard o titolo sensibile della finestra.
- Non creare daemon privilegiato di sistema durante l'MVP senza un ADR approvato e un modello di minaccia.

## 10. UX moderna e chiara per capacità diverse

`Preferenze > Compatibilità` (proposta) visualizza: OS/sessione, permessi e funzioni disponibili, metodo di inserimento selezionato (automatico o per app) e breve spiegazione. Non mostra stack trace, raw hook o testo catturato. L'utente può lanciare un **test guidato su un campo interno dell'app**, ma questo NON equivale alla compatibilità con altre app.

Messaggi tipici (da localizzare):
- `Il motore è in pausa`.
- `Questa applicazione non consente l'inserimento automatico. Usa Copia.`
- `Autorizzazione di accessibilità mancante: apri le impostazioni di sistema.`
- `Gli appunti sono cambiati durante l'inserimento: il nuovo contenuto è stato conservato.`

## 11. Criteri per dichiarare una capacità come verificata

- Test eseguito su **OS+release+sessione/compositor+editor/versione** reali.
- Fixture Unicode con confronto esatto di code point e bytes, non solo controllo visivo.
- Test pass/fail su focus switch, clipboard concorrente, IME non terminato e campo protetto.
- Ripetizioni e tempi misurati (p50/p95), separando latenza e percentuale di successi; soglie da definire dopo baseline.
- Log di esito privacy-safe e report con data, commit e permessi.
- Fallimenti note e capability documentate nell'help; niente badge universali.

Vedere [PLATFORMS.md](PLATFORMS.md), [UNICODE_IME.md](UNICODE_IME.md), [TEST_STRATEGY.md](TEST_STRATEGY.md), [SECURITY_PRIVACY.md](SECURITY_PRIVACY.md).
