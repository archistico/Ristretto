# Pulizia degli output .NET (bin / obj)

Dalla cartella radice `Ristretto` in Windows:

```bat
scripts\clean-build.cmd -Preview
scripts\clean-build.cmd
```

Il primo comando mostra le cartelle; il secondo richiede di digitare `ELIMINA` prima di rimuoverle. Per esecuzione senza conferma: `scripts\clean-build.cmd -Force`.

La pulizia riguarda solo le directory `bin` e `obj` poste accanto a un file di progetto `.csproj`, compresi i progetti in `src`, `tests` e `tools`. Non tocca codice, file `.sln`, SQLite, `node_modules`, `.git` o altre cartelle. Per sicurezza ignora le directory che sono junction/link simbolici o che ne contengono al proprio interno.

Chiudere prima Ristretto, Visual Studio e i programmi che usano i file compilati. Le cartelle `bin` e `obj` saranno ricreate da `dotnet build` o dagli script `validate*.cmd`. Questo intervento non cambia la versione dell'applicazione.
