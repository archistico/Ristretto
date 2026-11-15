# Installazione, aggiornamento e dati — Windows

**Versione di riferimento:** 3.9.0-alpha.20.26. Questa guida riguarda lo ZIP dei sorgenti. Un installer e un pacchetto eseguibile qualificato sono attività di R4 nella [roadmap](ROADMAP.md).

## Prima installazione

1. Installa l'[SDK .NET 10 dalla pagina ufficiale Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). Per compilare serve l'SDK, non soltanto il runtime.
2. Estrai lo ZIP in una cartella nuova, per esempio una cartella dedicata a questa versione.
3. Apri PowerShell nella cartella contenente `Ristretto.sln` e controlla l'SDK:

   ```powershell
   dotnet --version
   ```

4. Compila ed esegui le verifiche ordinarie:

   ```powershell
   .\scripts\validate.cmd
   ```

   Attendi **GATE CORE: PASS**. Il primo restore richiede accesso ai pacchetti NuGet; una cache locale completa può essere usata in un ambiente già predisposto. Node.js e Python non sono necessari a questo gate.

5. Avvia:

   ```powershell
   dotnet run --project src/TextExpander.Desktop/TextExpander.Desktop.csproj -c Release
   ```

Con l'SDK già installato puoi anche avviare `Ristretto.exe` dalla cartella `src/TextExpander.Desktop/bin/Release/net10.0/` dopo la build. Conserva insieme tutti i file generati: il solo EXE non è un pacchetto distribuibile completo.

## Primo utilizzo

Crea un gruppo e uno snippet, compila nome, trigger e testo, poi salva. Per una hotkey premi **Registra…**, premi e rilascia la combinazione, scegli **Usa combinazione** e salva di nuovo.

Prova il richiamo in un documento vuoto di Blocco note. Per usare il picker, lascia il cursore nel documento e premi **Ctrl+Shift+Space**; cerca lo snippet e premi Invio sul risultato selezionato. L'inserimento usa gli appunti. [Funzionamento](../README.md#come-funziona) · [Guida alla registrazione](ALPHA20_26_HOTKEY_CAPTURE.md).

## Dove sono i dati

Il profilo predefinito Windows è `%LOCALAPPDATA%\TextExpanderStudio`:

| Elemento | Contenuto |
| --- | --- |
| `library.db` | Snippet, gruppi, hotkey, impostazioni e metadati |
| `Backups/` | Backup SQLite creati dall'applicazione |
| `.ristretto.profile.lock` | File di coordinamento della singola istanza; non eliminarlo mentre il profilo è in uso |

La cartella dei sorgenti può cambiare a ogni versione: il percorso predefinito dei dati resta lo stesso. Su Windows la chiusura della finestra può lasciare Ristretto nella tray; per terminare il processo usa **Esci**.

### Profilo separato

Per prove o una seconda libreria scegli una directory assoluta fuori dalla cartella della versione:

```powershell
dotnet run --project src/TextExpander.Desktop/TextExpander.Desktop.csproj -c Release -- --data-dir "$env:LOCALAPPDATA\Ristretto-Prova"
```

Usa lo stesso percorso a ogni avvio. Un percorso relativo viene risolto rispetto alla directory di lavoro: spostarsi in una nuova cartella dei sorgenti può quindi aprire una libreria diversa. Il profilo separa i dati; le hotkey restano condivise con le altre applicazioni della sessione Windows e possono risultare occupate.

## Aggiornare Ristretto

1. Nella versione in uso scegli **⋯ → Backup**. Il percorso del file viene mostrato nella barra di stato; conservalo anche fuori dalla cartella che intendi sostituire.
2. Annota l'eventuale percorso personalizzato passato a `--data-dir`.
3. Esci dall'applicazione tramite la tray e chiudi le altre istanze che usano quel profilo.
4. Estrai il nuovo ZIP in una cartella nuova. Non unire `bin` e `obj` di versioni diverse.
5. Esegui `.\scripts\validate.cmd`, quindi avvia la nuova versione sullo stesso profilo: senza opzioni per il profilo normale, con lo stesso percorso assoluto per un profilo personalizzato.
6. Controlla gruppi, snippet e impostazioni. Esegui un richiamo su un documento di prova prima di riprendere il lavoro normale.

La nuova versione non richiede di importare nuovamente la libreria predefinita. L'importazione è additiva e genera nuovi ID: non è il metodo per aggiornare il programma.

## Backup, ripristino e JSON

**Backup SQLite** conserva libreria e impostazioni, comprese le hotkey. **Export JSON** serve a trasferire snippet e gruppi; non sostituisce il backup delle impostazioni. Il JSON v1 attuale include il campo facoltativo `hotkey`; gli export precedenti senza quel campo restano leggibili.

Per recuperare un backup, apri il profilo di destinazione e scegli **⋯ → Ripristina**, seleziona il file e conferma. L'operazione sostituisce la libreria del profilo: non aggiunge semplicemente gli elementi. Il database precedente viene conservato in un file di recupero accanto a quello attivo. Un errore o un conflitto deve essere risolto prima di considerare il ripristino riuscito.

Per copiare manualmente un intero profilo, esci prima dall'applicazione. Per il normale backup usa il comando della GUI, che produce una copia consistente anche quando SQLite usa i file WAL.

### Tornare a una versione precedente

Dalla Alpha 20.25 è in uso lo **schema SQLite 2**. Gli archivi schema 1 vengono migrati dalla nuova versione; le versioni che conoscono soltanto lo schema 1 rifiutano il database migrato.

Per tornare a una di queste versioni conserva prima un backup schema 1 compatibile. Avvia la vecchia versione con un profilo separato e ripristina quel backup tramite la sua procedura; mantieni intatto il profilo aggiornato finché il recupero non è stato verificato. Non riaprire il database schema 2 con un programma che supporta solo schema 1.

Il ripristino di un backup schema 1 nella versione attuale migra soltanto la copia di staging e lascia invariato il backup scelto. Le prove automatiche coprono questi passaggi su dati sintetici; l'aggiornamento completo sul desktop è ancora una voce di qualificazione R4.03.

## Se qualcosa non funziona

| Sintomo | Controllo da fare |
| --- | --- |
| `dotnet` non trovato | Installare l'SDK .NET 10 e riaprire PowerShell |
| Errore di restore | Controllare accesso a NuGet e il primo errore; usare una cache solo se contiene le dipendenze necessarie |
| Libreria apparentemente vuota dopo l'aggiornamento | Confrontare il profilo predefinito e il percorso assoluto usato con `--data-dir`; evitare una reimportazione finché il percorso non è stato chiarito |
| Hotkey salvata ma non attiva | Verificare conflitti con picker, altri snippet, altre istanze o altre applicazioni; scegliere una combinazione diversa |
| Inserisci non disponibile | Richiamare il picker dal campo esterno; alcuni controlli non sono identificabili o accessibili |
| Profilo non disponibile | Controllare directory, permessi e istanze aperte; non viene aperto automaticamente un profilo alternativo |

Dopo una build puoi verificare la scrivibilità del profilo senza aprire GUI o database:

```powershell
dotnet src/TextExpander.Desktop/bin/Release/net10.0/Ristretto.dll --check-profile --data-dir "$env:LOCALAPPDATA\Ristretto-Prova"
```

Il risultato `profile_writable` riguarda la directory, non l'integrità del database o la disponibilità di una hotkey. Per una segnalazione conserva versione e primo errore completo, senza includere il contenuto degli snippet. [Verifiche tecniche](BUILD_AND_TEST.md).
