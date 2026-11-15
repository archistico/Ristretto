# Roadmap unica — Ristretto

**Versione di riferimento: 3.9.0-alpha.20.19.5.** Obiettivo finale: text expander multipiattaforma con trigger Unicode, espansione automatica sicura nelle applicazioni compatibili e picker manuale di riserva.

| Fase | Risultato | Stato attuale |
|---|---|---|
| R1 — Fondamenta | SQLite, libreria, portabilità JSON, test, validazione e CI | Avanzata; tool migrati a C# in 20.19.4; CS9174 corretto in 20.19.5, validazione Windows aperta |
| R2 — Picker Windows | Hotkey configurabile, Invio/Inserisci, Unicode multilinea | Inserimento Notepad confermato; altri editor da qualificare |
| R3 — Adapter VS Code | Integrazione opzionale | Sospesa, non prioritaria |
| R4 — Distribuzione | Installer e release Windows | Da completare |
| R5 — Input automatico | Contesto focus e testo Unicode committato | In sviluppo nel laboratorio, nessuna intercettazione globale produttiva |
| R6 — Espansione automatica | Sostituzione del trigger senza picker | Da sviluppare |
| R7 — Snippet avanzati | Template, variabili, formulari | Successivamente |
| R8 — Multipiattaforma | Linux e macOS, packaging | Successivamente |

## Attività immediate

1. **P0:** compilare e validare l’hotfix C# Alpha 20.19.5; confermare la risoluzione dei due errori CS9174 e verificare eventuali errori successivi senza introdurre Python.
2. **P0:** mantenere il picker Notepad funzionante, confermare i gate e le prove sintetiche con i nuovi strumenti.
3. **P1:** proseguire R5 distinguendo testo confermato e eventi tastiera; gestire IME, focus e campi protetti.
4. **P1:** implementare e qualificare il primo percorso R6 di espansione automatica in un editor Windows supportato.

I dettagli cronologici delle Alpha precedenti sono disponibili nei rapporti tecnici storici. Questa è l'unica roadmap operativa aggiornata.
