# Verifiche effettive — Ristretto Alpha 20.24

## Stato di riferimento

- **Alpha 20.20:** compilazione e test .NET confermati dall'utente.
- **Alpha 20.22:** correzioni del picker e dell'editor ancora da verificare sul desktop.
- **Alpha 20.23–20.24:** nuovi componenti R5 sperimentali, test Windows ancora aperti.

| Verifica | Esito | Dettagli |
|---|---|---|
| Codice anteprima e comando opt-in | IMPLEMENTATO | Simulazione di `;firma ` con catalogo sintetico e risultati senza testo |
| Regressioni C# dell'anteprima | NOT_RUN | 15 nuovi test xUnit, richiedono SDK .NET 10 |
| Compilazione .NET completa | NOT_RUN | SDK .NET non installato nell'ambiente di preparazione |
| Laboratorio WinEvent su Windows | NOT_RUN | Richiede desktop interattivo e un Edit classico sintetico |
| Test Node dell'integrazione facoltativa | **PASS** | 32 su 32, eseguiti nella cartella dell'archivio |
| Struttura XML, XAML e risorse | **PASS** | 19 file verificati |
| Collegamenti Markdown locali | **PASS** | 740 collegamenti a file esistenti |
| Contratti statici dei nuovi file | **PASS** | Collegamento del comando, preview e protezione `CanAuthorizeExpansion=false` |
| Script Python nel progetto | **NESSUNO** | Strumenti di sviluppo in C#/.NET |
| GUI, picker, hotkey e SQLite | INVARIATI | Nessuna modifica ai file applicativi corrispondenti |
| ZIP completo e manifest SHA-256 | **PASS** | Verifica integrità sull’archivio estratto da zero |

Non reinterpretare `NOT_RUN` come `PASS`. Un possibile trigger osservato può anche provenire da incollamento o codice e **non autorizza espansioni esterne**.
