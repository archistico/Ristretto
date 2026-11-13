# Alpha 9 — laboratorio a due processi (F3, incremento di prova)

**Stato:** codice e test aggiunti; build .NET 10 e prove interattive Windows **NON RUN** al momento della consegna. Ultima prova Windows storica conservata: alpha.7 (180 xUnit e 21 scenari nativi). GUI Preview 9, SQLite e picker invariati.

## Obiettivo e limiti

Questa fase verifica il confine tra due processi **del laboratorio**. Il comando `--cooperating-demo` avvia un secondo processo del tool, che crea e possiede il suo EDIT Win32, acquisisce soltanto i delta Unicode **committati nel proprio campo**, prepara la sostituzione e la applica localmente sul proprio thread UI tramite `OwnedEditTarget`. Il processo genitore **non** invia tasti, testo, piani di sostituzione né messaggi Windows al controllo figlio. Riceve solamente i contatori dell'esito dal suo stdout privato, con una sessione casuale di correlazione e il PID atteso.

**Non è un adapter per Word, Notepad, browser o editor arbitrari.** Non abilita l'osservazione globale, non prova una provenance affidabile su un processo terzo e non trasforma `SendInput` in una sostituzione atomica. Non rivendica isolamento contro codice malevolo con gli stessi permessi: PID e session token sono marcatori di correlazione, non credenziali.

## Cosa è cambiato

- `src/TextExpander.Application/Features/Output/CooperatingLabProtocol.cs`: protocollo di stato privo di contenuti, con parsing limitato e verifica esplicita di sessione/PID; il successo richiede `Verified > 0`, zero `Rejected`/`Uncertain` e uscita del processo figlio con codice zero.
- `tools/Ristretto.WindowsOutputLab/CooperatingProcessLab.cs`: avvio esplicito del processo figlio, verifica READY, raccolta risultato, timeout di avvio e cleanup del child; un report JSON facoltativo contiene soltanto metadati, esiti e contatori.
- `NativeReplacementLab.RunDemo`: due callback opzionali per ready e risultato, senza modificare il percorso di sostituzione esistente.
- `Program.cs`: `--cooperating-demo`, `--cooperating-demo --report <nuovo-file>`, `--cooperating-smoke` (prova automatica con fixture sintetica); modalità interna `--cooperating-child <session>`.
- `tests/TextExpander.Tests/CooperatingLabProtocolTests.cs`: parser non permissivo, correlazione, assenza di testo, contatori errati, condizioni di esito.

## Come provare su Windows

Eseguire dalla radice `Ristretto/`, **in una nuova cartella estratta**:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

Il comando `scripts\validate-native.cmd` ora esegue anche **uno smoke automatico in secondo processo**, usando solo la fixture sintetica `;firma` e verificando la sostituzione Unicode nel proprio EDIT. Il contatore degli scenari nativi precedenti (21) rimane distinto da questo smoke. È una prova di cooperazione tra processi, non di input fisico né di applicazione terza.

Per eseguire direttamente lo smoke:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --cooperating-smoke
```

Per il test manuale a due processi:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --cooperating-demo --report reports/alpha9-cooperating-prima-prova.json
```

Apparirà una finestra del processo figlio. Cliccare nel suo campo e digitare `;firma` seguito da uno spazio. Verificare visivamente che lo snippet Unicode sostituisca il trigger **preservando lo spazio**; chiudere la finestra. L'esito sul terminale e il report del genitore devono indicare `VerifiedInCooperatingChildOnly`, con almeno una sostituzione verificata, zero tentativi rifiutati/incerti e uscita corretta del figlio. Cambiare nome al report per prove successive: non si sovrascrivono evidenze precedenti. Non digitare dati reali o sensibili nella demo.

**Prove negative manuali:** cliccare fuori dal campo durante una digitazione parziale, rientrare e controllare che il vecchio prefisso non venga espanso; verificare che Ctrl/Alt/Shift blocchino tentativi durante l'invio; chiudere senza digitare e verificare `NotVerified`. Questi scenari non sono stati eseguiti in questa consegna.

## Privacy e semantica

Lo stdout del processo figlio trasporta READY e, a chiusura, un RESULT formato da sessione e tre interi; gli altri log di stato sono scartati. Il genitore non riceve il testo dell'EDIT. Il JSON non contiene trigger, snippet, clipboard o cronologia di tastiera. Il processo figlio continua a possedere ed eseguire tutte le operazioni sul proprio thread. `Ready` non equivale a espansione riuscita; un report mancante, errato, in ritardo o un exit code nonzero non può produrre successo. Nessun retry su esito incerto.

## Prossimo gate (non ancora implementato)

Per un primo **vero editor esterno** servono un adapter specifico per quel destinatario, una sorgente affidabile dei caratteri committati, capacità di leggere/rilevare il range, revisioni e caret e un'operazione di sostituzione condizionale con verifica. Solo dopo queste prove, ulteriori test Windows reali e attivazione esplicita si potrà collegare F3 al prodotto. In caso di assenza di capacità rimane il picker con copia manuale.

[Roadmap](FUNCTIONAL_ROADMAP.md) · [Stato](CURRENT_STATE.md) · [Build e prove](BUILD_AND_TEST.md).
