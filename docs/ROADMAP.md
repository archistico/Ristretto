# Roadmap unica — Ristretto

**Versione di riferimento: 3.9.0-alpha.20.20.** Obiettivo finale: text expander multipiattaforma con trigger Unicode, espansione automatica sicura nelle applicazioni compatibili e picker manuale di riserva.

| Fase | Risultato | Stato attuale |
|---|---|---|
| R1 — Fondamenta | SQLite, libreria, portabilità JSON, test, validazione e CI | Strumenti C# validati dall'utente con compilazione e test PASS nella 20.19.5 |
| R2 — Picker Windows | Hotkey configurabile, Invio/Inserisci, Unicode multilinea | Inserimento Notepad confermato; altri editor da qualificare |
| R3 — Adapter VS Code | Integrazione opzionale | Sospesa, non prioritaria |
| R4 — Distribuzione | Installer e release Windows | Da completare |
| R5 — Input automatico | Contesto focus e testo Unicode committato | In sviluppo: bridge input committato Alpha 20.20 integrato nel laboratorio, nessuna intercettazione globale produttiva |
| R6 — Espansione automatica | Sostituzione del trigger senza picker | Da sviluppare |
| R7 — Snippet avanzati | Template, variabili, formulari | Successivamente |
| R8 — Multipiattaforma | Linux e macOS, packaging | Successivamente |

## Attività immediate

1. **P0:** compilare e verificare la Alpha 20.20, inclusi i nuovi test del bridge Unicode e la regressione del laboratorio Win32.
2. **P0:** ripetere i test nativi del controllo EDIT posseduto, per assicurare che il nuovo bridge non cambi l'inserimento del laboratorio né il picker funzionante.
3. **P1:** proseguire R5 distinguendo testo confermato e eventi tastiera; gestire IME, focus e campi protetti.
4. **P1:** implementare e qualificare il primo percorso R6 di espansione automatica in un editor Windows supportato.

I dettagli cronologici delle Alpha precedenti sono disponibili nei rapporti tecnici storici. Questa è l'unica roadmap operativa aggiornata.
