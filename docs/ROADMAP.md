# Roadmap unica — Ristretto

**Versione di riferimento: 3.9.0-alpha.20.25.** Architettura in C#/.NET 10 e Avalonia; strumenti di validazione C# (nessuna dipendenza Python).

**Obiettivo finale:** text expander multipiattaforma con trigger Unicode ed espansione automatica nelle applicazioni compatibili, più picker manuale di riserva.

| Fase | Risultato | Stato attuale |
|---|---|---|
| R1 — Fondamenta | SQLite, JSON, test C#, CI | CS0246 corretto; 820 xUnit passati, migrazione schema 2 verificata |
| R2 — Picker Windows | Hotkey, ricerca, Copia/Inserisci, frecce+Invio e hotkey per snippet | Unicode Notepad confermato; novità UI 20.22 e hotkey 20.25 da qualificare sul desktop |
| R3 — VS Code | Integrazione facoltativa | Sospesa |
| R4 — Distribuzione | Installer e rilascio Windows | Da completare |
| R5 — Input Unicode | Bridge posseduto, snapshot esterni, WinEvent e anteprima trigger opt-in | Alpha 20.24 sperimentale, nessuna prova della provenienza |
| R6 — Espansione automatica | Trigger e sostituzione qualificata | Da sviluppare |
| R7 — Snippet avanzati | Variabili, formulari e template | Successivamente |
| R8 — Multipiattaforma | Linux/macOS, adapter input | Successivamente |

## Attività immediate

1. **P0 — Qualificazione manuale:** hotkey per snippet 20.25, conflitti con altre app/profili, disabilitazione del gruppo, Unicode e focus/appunti concorrenti; poi editor ridimensionabile e secondo risultato nel picker (20.22). Usare un profilo sintetico con `--data-dir`. Build e 820 xUnit superati; [dettagli](ALPHA20_25_SNIPPET_HOTKEYS.md).
2. **P0 — Esperimento R5:** prova WinEvent e anteprima facoltativa del trigger sintetico `;firma ` in un controllo classico `Edit`. `NOT_RUN` è previsto su editor non compatibili, nessuna sostituzione.
3. **P1 — Provenienza e continuità:** dimostrare testo effettivamente committato con focus, revisione, IME e operazioni concorrenti; non fidarsi dei soli eventi tastiera o WinEvent.
4. **P1 — Primo R6 opt-in:** qualificare un singolo destinatario e una sostituzione verificabile; mai sostituire in un editor sconosciuto.
5. **P2 — Rilascio e altre piattaforme:** packaging, test di durata, Linux/macOS e funzionalità avanzate.

L'unica roadmap autorevole è questo documento. I rapporti Alpha sono storico tecnico.
