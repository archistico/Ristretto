# Risultato delle verifiche — Alpha 20.20

## Baseline utente

L'utente ha confermato **Alpha 20.19.5: compilazione e test passati**. Questa conferma riguarda la versione precedente, non i nuovi sorgenti 20.20.

## Controlli effettuati sulla nuova Alpha 20.20

| Controllo | Esito |
|---|---|
| Test Node adapter VS Code (modulo facoltativo) | **32/32 PASS** |
| Analisi struttura XML/XAML/RESX/PROPS | **19/19 validi** |
| Collegamenti documentali interni | **721 verificati, nessuno mancante** prima della relazione (conteggio finale aggiornabile) |
| Confronto SHA-256 di GUI, picker, SQLite e servizio Inserisci con 20.19.5 | **File identici** |
| Script Python presenti | **Nessuno** |
| Nuova suite `OwnedCommittedInputBridgeTests` | **17 casi preparati — NOT_RUN** |
| Build .NET 10 e xUnit nuova Alpha 20.20 | **NOT_RUN**: SDK non disponibile nell'ambiente di preparazione |
| Test nativi Windows del laboratorio aggiornato | **NOT_RUN**: desktop Windows non disponibile |

Il controllo dei sorgenti mediante ricerca o parsing non è una compilazione C#: nessuna prova .NET è dichiarata superata per la Alpha 20.20.

## Comandi Windows per validare

Da PowerShell, nella cartella contenente `Ristretto.sln`:

```powershell
.\scripts\validate-source.cmd
.\scripts\validate.cmd
.\scripts\validate-native.cmd
```

Il terzo comando richiede una sessione desktop Windows interattiva. Non sono necessarie prove con documenti personali.

Per la motivazione tecnica e le limitazioni leggere [il rapporto R5](../docs/ALPHA20_20_OWNED_INPUT_BRIDGE.md).
