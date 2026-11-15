# Alpha 20.28 — Registrazioni hotkey Windows

Il richiamo tramite hotkey è stato nuovamente confermato dall'utente dopo la consegna 20.27. La nuova release aggiunge una prova ripetibile del ciclo di registrazione Windows, senza cambiare GUI, picker, schema dei dati o inserimento.

## Prova disponibile

```powershell
.\scripts\validate-snippet-hotkeys.cmd
```

Il comando compila il laboratorio e produce un nuovo JSON in `.local-cache/gates/`. Per scegliere il percorso:

```powershell
.\scripts\validate-snippet-hotkeys.cmd .local-cache\gates\mia-prova-hotkey.json
```

Il file deve essere nuovo e la cartella deve esistere. Un rapporto preesistente viene rifiutato prima di registrare combinazioni. Esiti: `PASS` (uscita 0), `FAIL` (1), percorso/argomenti non validi (2), prerequisiti insufficienti `NOT_RUN` (3).

Il laboratorio sceglie tre combinazioni disponibili fra **Ctrl+Alt+Shift+F6…F11**. Le registra temporaneamente usando i worker del prodotto; al termine le rilascia. Non forza combinazioni già occupate. Non apre finestre, non invia tasti, non usa appunti o profili personali. Evitare di premere quelle combinazioni durante il breve test: eventuali callback inattesi rendono la prova non riuscita.

## Cosa controllano i 21 casi

| Area | Evidenza |
| --- | --- |
| Catalogo e worker | Due assegnazioni, conflitto isolato, rimozione, disabilitazione snippet/gruppo, riabilitazione, cambio hotkey, assegnazione vuota ed eliminazione |
| Rilascio | Una seconda registrazione riesce dopo rimozione/Dispose; le altre combinazioni restano occupate dal proprietario previsto |
| Secondo processo | Un figlio possiede una combinazione: il padre riceve un rifiuto su quella e registra l'altra; dopo rilascio normale o terminazione del solo figlio, la riacquisisce |
| Picker | Collisione con uno snippet, modifica fallita che conserva la precedente, annullamento della preparazione, commit e Dispose |
| Cattura | Coordinatore reale con worker reali: durante la pausa le combinazioni sono libere; dopo annullamento, conferma o eccezione tornano occupate dal catalogo salvato |
| Avvio/uscita | 20 cicli di Dispose immediato, richieste successive rifiutate e combinazioni disponibili |

Ogni API attesa ha un timeout. Il processo figlio riceve soltanto una combinazione sintetica ammessa, usa input/output standard e ha un limite di permanenza di 15 secondi. La prova di terminazione riguarda soltanto il figlio appena creato dal laboratorio.

Il comando è incluso anche in `validate-native.cmd` e nel gate FULL. CORE e CI continuano a non registrare hotkey; il solo PASS CI non comprende questa prova. Si può eseguire il nuovo laboratorio da solo senza avviare gli altri laboratori nativi.

## Perimetro e limiti

Le ricevute provengono dai servizi `WindowsSnippetHotkeyService` e `WindowsPickerHotkeyService` e dal coordinatore del prodotto. Il catalogo usa dati sintetici in memoria. Non si apre `Desktop.App`: la gestione reale di save/import, notifiche in coda, cambio focus, appunti e durata dell'intera applicazione resta da qualificare. Il dialogo resta coperto dai [test UI headless 20.27](ALPHA20_27_CAPTURE_INTEGRATION.md).

R2.09 avanza sulle registrazioni native; la [matrice desktop](WINDOWS_QUALIFICATION_PLAN.md) mantiene i suoi casi non ancora eseguiti. Il test non dimostra digitazione fisica, espansione automatica o compatibilità di un editor esterno.

Le API associano la registrazione al thread e prevedono un rilascio esplicito: [RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey), [UnregisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unregisterhotkey). I risultati effettivi, separati dalle aspettative delle API, sono nel [rapporto della release](../reports/ALPHA20_28_CHECKS.md).
