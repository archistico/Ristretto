# Alpha 12 — Scorciatoia globale configurabile del picker

**Stato:** implementata nei sorgenti, **NON ancora compilata né provata su Windows**. Non usare questa consegna come evidenza di test superati. La GUI Preview 9 è invariata salvo la nuova sezione scorciatoia nel menu Impostazioni.

## Funzione

Su **Windows**, all'avvio viene tentata la registrazione di `Ctrl+Shift+Space`. Premendo la combinazione, anche se la finestra principale è nascosta nella tray, Ristretto richiama **la stessa istanza del picker** e porta il focus sulla casella di ricerca. I risultati rimangono copia manuale negli appunti: non è attivata l'espansione automatica.

Per cambiare: aprire la finestra di Ristretto, selezionare **Impostazioni (ingranaggio)**, digitare la combinazione nel campo **Scorciatoia globale del picker** e premere **Applica scorciatoia**. Esempi: `Ctrl+Alt+K`, `Ctrl+Shift+P`, `Alt+F9`, `Ctrl+Shift+Space`. La sintassi è insensibile a maiuscole e ordine dei modificatori: si salva in forma canonica. Digitare **`None`** per disattivare la hotkey globale. Non sono permesse combinazioni senza Ctrl oppure Alt, tasti modificatori duplicati, tasti non previsti o più di un tasto non modificatore. Sono supportati lettere latine A–Z, numeri 0–9, Space, F1–F11; Ctrl, Alt, Shift e Win come modificatori. Non esistono registrazioni di sequenze di tasti o combinazioni del testo.

Se un'altra applicazione possiede la combinazione, o Windows la rifiuta, **la nuova combinazione non viene salvata** e quella precedente resta attiva. Anche in caso di errore di salvataggio SQLite viene annullata la nuova registrazione. Il valore viene memorizzato nella stessa tabella `settings` esistente (`picker.hotkey`), senza migrazioni dello schema. Il servizio è dismesso all'uscita; mentre Ristretto è nella tray resta attivo. L'istanza unica evita registrazioni duplicate durante un secondo avvio.

La scorciatoia storica **Ctrl+K nella sola finestra principale** resta disponibile; `None` disabilita esclusivamente il nuovo shortcut globale. Su macOS e Linux questa release non installa global hotkey: il controllo segnala la piattaforma non supportata; il picker continua a funzionare dal menu e dalla GUI. Serviranno adapter nativi specifici per macOS e X11/Wayland, senza promettere permessi che le piattaforme non offrono.

## Architettura e privacy

- `PickerHotkey.TryParse`: contratto puro e testabile, non legge input del sistema.
- `WindowsPickerHotkeyService`: thread dedicato con message queue Win32, `RegisterHotKey` / `UnregisterHotKey`, `MOD_NOREPEAT` e solo evento `WM_HOTKEY`. Non usa `WH_KEYBOARD_LL`, clipboard, testo dei processi o tasti digitati.
- Cambio in due fasi: **prepare** della nuova registrazione (senza togliere la vecchia), salvataggio della configurazione, **commit**. Se il prepare non riesce, resta la vecchia; se fallisce la persistenza, **cancel** della nuova. La disattivazione segue lo stesso percorso.
- Il callback viene inoltrato sul thread UI Avalonia e riusa il picker. Nessun accesso a documenti esterni o al motore di sostituzione.

## Test Windows da effettuare

1. Estrarre il nuovo ZIP in una cartella pulita ed eseguire `scripts\validate.cmd` e `scripts\validate-native.cmd`.
2. Lanciare Ristretto; premere Ctrl+Shift+Space in un'altra applicazione; verificare che si apra il picker e la ricerca abbia il focus, senza digitare nel documento in primo piano.
3. Chiudere il picker con Esc e la finestra principale verso la tray; ripetere la hotkey; verificare una sola apertura anche tenendo premuti i tasti.
4. Modificare la hotkey in `Ctrl+Alt+K`; verificare che la vecchia non sia più attiva e che la nuova funzioni. Riavviare e verificare la persistenza.
5. Configurare una combinazione già registrata da un'altra applicazione; controllare il messaggio di conflitto e il mantenimento del vecchio shortcut e del valore salvato.
6. Impostare `None`, riavviare e verificare che non sia registrata alcuna scorciatoia globale; Ctrl+K nella sola finestra principale resta valido.
7. Verificare Windows con layout italiano/internazionale, cambio lingua dell'app, focus, applicazioni a privilegi elevati, più sessioni utente e chiusura esplicita dalla tray.

Gli ultimi punti necessitano **prove reali**, non sono deducibili dai test puri del parser. La nuova hotkey non aumenta la qualificazione di applicazioni esterne ai fini della sostituzione del testo.

**Nota Win32:** F12 è riservato dal sistema per i debugger e pertanto non viene proposto come hotkey ([RegisterHotKey, Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)).
