# Verifiche — Ristretto Alpha 20.21

La baseline **Alpha 20.20** ha compilato e superato i test sul computer Windows dell'utente (conferma ricevuta in chat).

La nuova Alpha contiene il confronto Unicode e una sonda sperimentale Win32 esterna avviabile su richiesta. `CandidateInsertion` non autorizza espansioni né dimostra la provenienza umana di una modifica.

| Verifica | Esito | Evidenza |
|---|---|---|
| Test Node dell'adapter facoltativo | **PASS** | 32/32 eseguiti in questo ambiente |
| Parsing XML/XAML / MSBuild | **PASS** | 14 file esaminati staticamente |
| Link Markdown | **PASS** | 531 link locali controllati, nessuno mancante |
| Confronto file critici picker/SQLite con Alpha 20.20 | **PASS** | 5 file invariati, confronto byte per byte |
| Versione / riferimenti e nuovi file | **PASS statico** | Versione 20.21 allineata; 7 nuovi file, nessuno eliminato |
| Compilazione .NET Alpha 20.21 | **NOT_RUN** | SDK .NET 10 non disponibile qui |
| Nuovi test xUnit | **NOT_RUN** | `ExternalClassicEditTextDeltaTests` ancora da eseguire |
| Prova Win32 esterna | **NOT_RUN** | Necessaria una sessione desktop Windows e un `Edit` classico compatibile |
| Espansione globale | **NON IMPLEMENTATA** | Nessun hook o scrittura esterna |

Il test manuale Windows NON deve essere eseguito su documenti personali: il lettore legge temporaneamente in RAM fino a 4096 unità UTF-16 del campo selezionato. Non esistono log del contenuto o report del lettore.
