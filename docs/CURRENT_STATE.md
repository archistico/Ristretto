# Stato corrente — Ristretto 3.9.0-alpha.20.18

**Data:** 14 novembre 2026 · **Piano autorevole:** [ROADMAP.md](ROADMAP.md) · **Dettagli della consegna:** [Alpha 20.18](ALPHA20_18_NOTEPAD_DIAGNOSTICS.md).

## Cosa stiamo facendo

**R2.05 — qualificazione del picker in Notepad** con file e profilo SQLite **sintetici**. La Alpha 20.18 migliora esclusivamente il qualificatore Python e le sue diagnosi, senza modificare i sorgenti C# dell'applicazione, la GUI Preview 9, l'hotkey o i dati.

## Funzionalità e livello di evidenza

| Area | Stato |
| --- | --- |
| Libreria snippet, gruppi, SQLite, import/export e backup | Implementati, con evidenze storiche di test |
| Picker, ricerca Unicode, Copia, Inserisci, gesto Invio | Implementati; **Alpha 20.15 confermata** dall'utente su Windows (build, test e Invio in Notepad) |
| Hotkey configurabile Windows | Implementata con `RegisterHotKey`; matrice completa tray/conflitti ancora aperta |
| Inserimento manuale in altri editor | Richiede qualificazione desktop per target, focus e clipboard; `SendInput` non prova la ricezione |
| Test Notepad su profilo sintetico | Disponibile; `prepare/seed/status/launch/verify`, ora con diagnosi in italiano |
| Adapter VS Code | Comando esplicito, 32 test Node storici; prove host reali aperte |
| Espansione automatica dei trigger | **NON implementata globalmente** |
| macOS/Linux hotkey e output | Adapter nativi ancora da implementare |

## Verifiche e limiti

- **Baseline confermata dall'utente:** Alpha 20.15, compilazione e test superati; Invio dal picker inserisce in Notepad.
- **Questa Alpha 20.18:** Python (inclusi nuovi casi del verificatore), Node e audit statici eseguiti nella consegna e documentati in [rapporto](ALPHA20_18_NOTEPAD_DIAGNOSTICS.md). Compilazione .NET/xUnit e test interattivi Windows **NOT_RUN** in questa sessione.
- Il risultato `PASS` del verificatore prova **solo** che il file sintetico salvato corrisponde al testo atteso; non dimostra l'origine dell'inserimento. `NOT_RUN` su file invariato **non è** un fallimento del picker.
- Nessun editor esterno deve essere modificato automaticamente basandosi soltanto su eventi tastiera o su un controllo Win32 read-only.

## Obiettivo e prossimi lavori

**Obiettivo:** completare l'affidabilità del picker manuale Windows, quindi arrivare alla prima espansione automatica Unicode in un target con API/garanzie dimostrate, preservando il fallback manuale per gli altri editor.

**Da fare:** prove desktop R2.02–R2.07, validazione VS Code nell'host reale R3, consolidamento rilascio Windows R4, spike e adapter automatico R5–R6, funzioni avanzate R7 e altri OS R8.

I dettagli delle versioni precedenti sono disponibili nel [CHANGELOG](CHANGELOG.md) e nell'[indice storico](INDEX.md), senza riprodurre qui tutti i rapporti Alpha.
