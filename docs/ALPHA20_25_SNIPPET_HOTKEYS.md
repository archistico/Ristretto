# Alpha 20.25 — Hotkey per snippet e correzione CS0246

**15 novembre 2026 · 3.9.0-alpha.20.25.**

## Correzione della 20.24

Riprodotti i due errori `CS0246` alle righe 23 e 164 di `ExternalClassicEditTriggerPreviewSessionTests.cs`. Il tipo esisteva già in Application: mancava `using TextExpander.Application.Features.Input;` nei test. Dopo la correzione il gate CORE della baseline ha superato build, **779 xUnit**, strumenti e dieci controlli CLI. L'anteprima R5 continua a non autorizzare scritture.

## Uso della nuova funzione

1. Apri o crea uno snippet nella libreria. Il nuovo campo **Hotkey dello snippet (facoltativa)** si trova sotto titolo/trigger.
2. Scrivi, ad esempio, `Ctrl+Alt+F` e premi **Salva**. Il messaggio sotto il campo distingue combinazione attiva, bozza da salvare, snippet/gruppo disabilitato e registrazione non disponibile.
3. Lascia Ristretto in esecuzione, anche nella tray. Nell'applicazione Windows in cui stai scrivendo, premi e rilascia la combinazione: viene richiesto l'incolla del testo salvato **senza aprire il picker**.

Sono ammesse combinazioni con almeno Ctrl o Alt e una lettera A–Z, una cifra, Space oppure F1–F11; Shift e Win sono modificatori aggiuntivi. F12 è escluso. Campo vuoto o `None` rimuove l'associazione dopo Salva. La hotkey vale nell'intera sessione Windows mentre Ristretto è aperto; non va configurata nell'editor esterno.

Ogni combinazione può essere assegnata a un solo snippet ed è riservata anche quando quello snippet è disabilitato. Per riutilizzarla, rimuovila dal precedente snippet o eliminalo. Le combinazioni del picker e degli snippet non possono coincidere. Se un'altra applicazione o un altro profilo Ristretto possiede già la combinazione, lo snippet viene salvato ma la hotkey risulta **non attiva**: scegli una combinazione diversa o riprova salvando dopo aver liberato quella precedente.

Snippet disabilitati e gruppi disabilitati non registrano hotkey. Salvataggio, eliminazione, importazione, ripristino e cambi dello stato del gruppo aggiornano le registrazioni. Tutte le nuove istruzioni sono disponibili nelle cinque lingue. Il layout del picker e le sue regole di selezione/Invio della 20.22 restano invariati; nell'editor viene aggiunto solo il campo necessario alla nuova funzione.

## Inserimento diretto e limiti

La notifica `WM_HOTKEY` acquisisce il destinatario esterno prima del passaggio al dispatcher UI. Non vengono installati hook della tastiera. Un singolo thread gestisce tutte le hotkey degli snippet; quello preesistente del picker resta separato.

L'inserimento aspetta il rilascio della combinazione per un massimo di due secondi. Durante l'attesa ricontrolla finestra/campo, modificatori, stato del controllo e sequenza degli appunti. Non riattiva un'altra finestra se il focus è cambiato. Le notifiche obsolete, le operazioni concorrenti e i tasti tenuti premuti non accodano incollamenti. Apertura della libreria, aggiornamento del catalogo e uscita annullano le richieste ancora annullabili.

Il testo passa attraverso gli appunti, con rilettura e controllo della sequenza prima di inviare `Ctrl+V`; **rimane negli appunti**. Una richiesta è monouso e non ritenta dopo un effetto incerto. Il servizio ha gli stessi limiti di compatibilità del picker: un controllo non identificabile, protetto o non accessibile può essere rifiutato. `SendInput` accettato non prova che l'editor abbia applicato l'incolla. Gli esiti compaiono nella barra di stato della libreria, senza riportarla in primo piano.

Questa è un'azione manuale esplicita. Non riconosce automaticamente `;firma` durante la digitazione, non elimina un trigger già scritto e non cambia i requisiti R5/R6 sulla provenienza dell'input.

## Persistenza e compatibilità

- SQLite passa a **schema 2**: colonna `snippets.hotkey`, vuota per gli snippet esistenti, con indice univoco sulle assegnazioni non vuote. Migrazione transazionale e idempotente da schema 1; testo, revisioni e impostazioni restano invariati. Chiudere l'istanza precedente prima di usare la nuova versione. Le versioni precedenti non possono aprire lo schema 2.
- Backup/ripristino conserva le hotkey. Un backup schema 1 viene migrato **nella copia di staging**, senza modificare il file scelto; il database precedente resta nel file di recupero.
- Il JSON v1 riceve il campo facoltativo `hotkey`. I vecchi export senza il campo continuano a essere importabili; i nuovi export conservano l'associazione. Un conflitto nell'importazione annulla l'intera transazione, senza perdere o sovrascrivere assegnazioni esistenti. L'estensione VS Code ignora questo campo facoltativo.

## Verifiche

**820 xUnit passati**, inclusi **41 nuovi casi** su normalizzazione, conflitti, cataloghi obsoleti, registrazioni simulate, rilascio dei tasti, timeout reale, focus/appunti concorrenti, cancellazione, effetto parziale, migrazione, backup, JSON e stato dell'editor. La baseline R5 della 20.24 è inclusa nella suite completa.

**GATE CI: PASS**, sia nei sorgenti di lavoro sia da una nuova estrazione dello ZIP priva di `bin`/`obj`: solution, quattro laboratori e DevTools compilati, 820 xUnit, 23 regressioni degli strumenti, dieci controlli CLI, **32 Node** e contratto .NET→Node passati. Manifest verificato su 483 file; hash dei sorgenti, XAML e traduzioni uguali nelle due esecuzioni. Restore dalla cache NuGet locale; audit vulnerabilità online non eseguito. Nessun warning o errore nella build finale. Gli aggiornamenti conclusivi al rapporto e alla documentazione non cambiano i sorgenti compilati.

Le prove di registrazione e inserimento usano API simulate; quelle SQLite usano cartelle e librerie sintetiche. Nessuna finestra, hotkey globale reale, clipboard reale, digitazione o editor esterno è stato usato per questi test. La qualificazione desktop delle nuove hotkey e delle modifiche 20.22 rimane aperta. Il report distingue i gate eseguiti da quelli `NOT_RUN`.

[Rapporto](../reports/ALPHA20_25_CHECKS.md) · [JSON](../reports/ALPHA20_25_VALIDATION.json) · [Roadmap](ROADMAP.md).
