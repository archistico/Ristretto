# Specifiche backend v2.6 — accettazione vs consegna

- Adapter Win32: `SendInput` restituisce numero eventi inseriti nello stream, soggetto a UIPI; **non** è attestazione che un editor abbia scritto il testo. Valutare clipboard sequence number come indicatore, non lock atomico.
- macOS: Event Tap e Accessibility richiedono permessi; `NSPasteboard.changeCount` segnala ownership, non un restore universale.
- X11: clipboard selection e owner cambiano; XTest/SendEvent non garantiscono gestione uniforme del testo esterno.
- Wayland: la disponibilità di una sessione Remote Desktop per inviare input non autorizza l'osservazione globale del testo: dichiarare capability separate.
- In tutti i backend il preflight precede il primo effetto, ma non può prevenire **tutte** le race di focus; `SubmittedUnverified` o `PartialOrUnknown` restano esiti legittimi.

Vedere [dettagli e matrix](OUTPUT_COORDINATION.md) e [fonti ufficiali](../research/OUTPUT_SOURCE_REGISTER_v2.6.md).

---

# Confini nativi e contratti dei port — proposta v2.3

> **Stato:** caratteristiche proposte, non API finali e non funzionalità implementate. Le capacità devono essere rilevate sul dispositivo reale; l'API nativa che permette di inviare input **non** certifica l'osservazione Unicode/IME o la sicurezza del focus.

## 1. Porte applicative minime

Creare interfacce **solo quando servono agli spike**, organizzate nel progetto Application e implementate in Infrastructure:

| Porta candidata | Domanda a cui risponde | Perché esiste |
|---|---|---|
| `IInputObserver` | Posso osservare eventi e, quando disponibile, testo committato con contesto? | Hook nativo e eventi platform |
| `ITargetInspector` | Quale app/finestra/campo è il destinatario, è sicuro? | Focus/permissions/protected fields |
| `ITextInserter` | Quali modalità di inserimento sono disponibili e con quale esito? | SendInput/Quartz/XTest/compositor |
| `IClipboardLease` / `IClipboardBroker` | Posso usare temporaneamente la clipboard senza sovrascrivere una copia concorrente? | Clipboard con ownership best effort |
| `IGlobalHotkeyRegistrar` | Quali scorciatoie posso registrare? | GUI/picker/pause, distinto dall'hook |
| `IPlatformCapabilities` | Cosa è `Available`, `RequiresPermission`, `Unsupported`, `Unknown` e perché? | UX/diagnostica/guardrail |
| `IAppInstanceCoordinator` (se serve) | Posso attivare la prima istanza e proteggere il profilo? | Startup OS-specific / IPC locale |

Non creare un'unica interfaccia `IPlatformService` con 30 metodi, né 7 nuove class library. Portare le porte ad API esatte dopo almeno uno spike reale per la funzionalità. Le porte dati (`ILibraryStore` o un accesso concreto a SQLite) sono *separate* dalle porte OS.

## 2. Dati scambiati ai confini

- **`InputObservation`** = sessione/target token, evento, timestamp monotono opzionale, `Committed|Composing|Unknown`, caratteri committati *solo se disponibile in modo affidabile*, origine fisica/sintetica ove rilevabile.
- **`TargetContext`** = token opaco e/o identificativo finestra/profilo, generazione focus, app/processo se lecito, stato `TextEditable/Protected/Unknown` (mai contenuto del campo).
- **`Capability`** = status + scope/OS/compositor/versione + ragione + eventuale azione consentita per l'utente; aggiornabile durante il ciclo di vita.
- **`ExpansionPlan`** = payload Unicode immutato + requisiti + azioni consentite. Nessun puntatore nativo e nessun DTO clipboard.
- **`InsertionOutcome`** = `Success|Unsupported|Denied|TargetLost|Partial|Failed`, stadio, reason sicura, contatori di eventi inviati se disponibili; non dichiarare che il target abbia accettato testo solo perché una API ha accettato eventi.
- **`ClipboardLeaseResult`** = snapshot supportata? ownership verificabile? conflitto? restore condizionale? specifiche app/mac/Wayland diverse.

Il `targetToken` va invalidato al cambio di window/focus/selection o generazione identificabile. Dato che i sistemi non garantiscono gli stessi segnali, l'adapter può restituire `Unknown` e impedire l'inserimento automatico.

## 3. Windows

- Candidati: `WH_KEYBOARD_LL`, `GetForegroundWindow`, UI Automation (es. `IsPassword`), `SendInput`, `RegisterHotKey`, Win32 clipboard (`GetClipboardSequenceNumber`), autorun policy.
- Unicode: `KEYEVENTF_UNICODE` su keydown **e keyup** se si usa il percorso Unicode, verificando il valore ritornato da `SendInput` e gli errori possibili. `SendInput` non aggira UIPI e privilegi elevati; fallimento esplicito.
- Hook a callback rapida, non chiamare SQLite o Avalonia dentro la callback; distinguere `LLKHF_INJECTED` quando utile, evitando loop.
- IME: un hook di tasti fisici non è automaticamente un osservatore affidabile del testo confermato in tutte le app. I contesti non qualificati si gestiscono conservativamente.
- Clipboard: snapshot del set di formati richiesti senza presumere completo supporto a tutti; cambio del sequence number e *ownership* sono indizi, non transazione garantita. Se l'utente copia nel frattempo non sovrascrivere.
- Interop .NET: preferire `LibraryImport` generato quando supportato dalla firma/SDK, marshalling esplicito e cleanup `SafeHandle`/`IDisposable` dove utile. Fonte: https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation.

## 4. macOS

- Candidati: Quartz Event Taps/CGEvent, Accessibility (AX), NSPasteboard e meccanismi di shortcut autorizzati.
- Permessi di accessibilità e/o monitoraggio input devono avere una spiegazione semplice nella GUI; gestione revoca e cambio lingua/layout.
- Secure Input/field protetto implica sospendere automaticamente l'osservazione; l'assenza di osservabilità non è un permesso implicito.
- Unicode UTF-16, emoji ZWJ/skin tone, composition IME, testo incollato e cursore richiedono prove sui target reali. Non spezzare arbitrariamente sequenze multicodepoint in blocchi con effetti visibili.
- Picker/focus: verificare che il campo di destinazione sia ancora quello originario prima dell'inserimento. Nessuna scelta silenziosa dopo cambio finestra.

## 5. Linux X11

- Candidati: XKB per layout, XTest per input sintetico, X11 selections (`CLIPBOARD` / `PRIMARY`), proprietà finestra/WM_CLASS. `PRIMARY` e `CLIPBOARD` non sono intercambiabili.
- Osservazione e conversione dei key events con layout e input method: non interpretare codice fisico come carattere Unicode committato se non dimostrato.
- Contesti terminali/Wayland/XWayland: limitazioni diverse per app; certificare per sessione e target.
- Semplificare dipendenze: non introdurre helper esterni obbligatori finché non validati. Eventuali librerie native richiedono SBOM, packaging e test aggiornati.

## 6. Linux Wayland

- **Non esiste una API universale** per leggere e sostituire tutto il testo digitato nelle applicazioni degli altri processi. La disponibilità dipende da compositor, desktop, sessione, device permissions e backend.
- `org.freedesktop.portal.GlobalShortcuts` consente scorciatoie globali, **non** trascrizione dei caratteri (https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.GlobalShortcuts.html).
- `org.freedesktop.portal.RemoteDesktop` permette input in una sessione autorizzata, **non** equivale a monitorare tutti i campi di testo; UI di consenso e sessione sono parte del contratto (https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.RemoteDesktop.html).
- Percorsi `evdev/uinput`, libei/EIS, protocolli specifici del compositor o helper privilegiati **solo dopo spike, threat model e strategia distributiva**. Nessun bypass non spiegato dei permessi utente.
- **Degrado dichiarato:** GUI/gestione libreria sempre possibili dove Avalonia gira; picker se hotkey disponibile; copia manuale se clipboard disponibile; auto-expand `Unsupported` quando mancano le capacità.

## 7. Capability matrix iniziale — non risultato di test

| Funzione | Windows | macOS | X11 | Wayland |
|---|---|---|---|---|
| GUI Avalonia | Da validare | Da validare | Da validare | Da validare |
| Registrare hotkey globale | Candidato API OS | Richiede permessi/OS | Candidato X11 | Solo se portal/compositor disponibile |
| Acquisire trigger globali | Candidato hook | Candidato Event Tap | Da qualificare | Non universale |
| Unicode sintetico | Candidato `SendInput` | Da qualificare Quartz/AX | Da qualificare | Non universale |
| Clipboard lease e restore condizionale | Da qualificare | Da qualificare | Da qualificare | Per backend, non garantito |
| Rilevazione campo protetto | Dipende da target | Dipende da AX/Secure Input | Limitata | Limitata/non universale |
| Autofill IME CJK nei target esterni | Non provato | Non provato | Non provato | Non provato |

Questa tabella è **un backlog di compatibilità**. Nessuna cella “candidato” autorizza la dicitura “funziona”.

## 8. Contratti di errore e fail-safe

- Preflight deve distinguere permission denied, output unsupported, target changed, secure input, clipboard unavailable.
- Non cancellare il trigger se il piano dipende da una finestra modale ancora aperta o se non si può verificare il destinatario.
- Dopo cancellazione/iniezione, riportare partial failure; non ripetere automaticamente la sostituzione perché può duplicare il testo.
- In caso di crash/stop, liberare hook/hotkey e ridurre al minimo stato temporaneo. Clipboard concorrente dell'utente ha priorità su un restore cosmetico.
- In diagnostica: ID tecnica del backend, OS/compositor, categoria errore, non testo digitato/snippet, processi solo quando necessari e configurati.

**Gate:** un adapter con test di laboratorio e capability reali è prerequisito per scegliere l'esatta API delle porte. Vedere [piano test](ARCHITECTURE_TEST_PLAN.md) e [proposta generale](ARCHITECTURE.md).

## Raffinamento v2.4 — esito applicativo e no handle OS

Contratti complementari: [DOMAIN_CONTRACTS.md](DOMAIN_CONTRACTS.md), [EXPANSION_LIFECYCLE.md](EXPANSION_LIFECYCLE.md). `TargetContext` è un token di confronto effimero senza handle/PID nel Core; l'adapter restituisce capacità separate per committed text, protezione, rimozione, unicode insert e cursor. `ITextOutput` deve differenziare richiesta inviata (`SubmittedUnverified`) da risultato realmente verificato (`VerifiedComplete`); ogni stato dopo effetti possibili vieta retry automatico. `ClipboardLease` resta esclusivamente nativo. Non fissare firme definitive senza spike.

## Integrazione v2.5 — cancellazione Unicode

L'adapter distingue eventi fisici, testo committato, testo di composizione e separatori azione. Il contratto di rimozione deve conoscere il *raw span* digitato e la prova che il focus/caret non sia cambiato; **né UTF-16 Length né numero di Rune/grafemi è universalmente un numero di Backspace sicuro**. In mancanza di prova, capability `Unsupported`, nessuna modifica e fallback picker/copia. Confermare per target con E2E (IME, emoji ZWJ, combinazioni accentate).

[ADR-0004](adr/ADR-0004-UNICODE-MATCHING-AND-WORD-BOUNDARIES.md) · [Matrice](../data/MATCHER_SCENARIOS_v2.5.csv).
