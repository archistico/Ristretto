# Stato corrente — Ristretto 3.9.0-alpha.20.25

## Cosa stiamo facendo

**Alpha 20.25:** corretti i due CS0246 nei test della 20.24. Aggiunta la hotkey facoltativa per ogni snippet, con inserimento manuale diretto nell'applicazione Windows attiva. **820 xUnit passati**, inclusi 41 nuovi casi; nuove hotkey sul desktop ancora da qualificare. [Uso e verifiche](ALPHA20_25_SNIPPET_HOTKEYS.md).

**R5 — osservazione Unicode e anteprima trigger su editor esterno:** sperimentiamo gli eventi `EVENT_OBJECT_VALUECHANGE` per controlli Win32 `Edit`, con un comando esplicito e limitato nel tempo. Confrontiamo osservazioni temporanee, invalidando la sessione ai cambi di focus. In Alpha 20.24 viene anche simulato il riconoscimento di `;firma ` usando soltanto un catalogo sintetico. I risultati mostrano soltanto contatori; non intercettiamo la tastiera e **non abilitiamo l'espansione automatica**.

## Stato verificato e ancora aperto

| Area | Stato |
|---|---|
| R1 — Fondamenta, strumenti C#, SQLite e xUnit | Baseline 20.24 corretta: CORE e 779 xUnit passati; suite 20.25 a 820 xUnit, schema 2 e migrazione provati su dati sintetici |
| R2 — Picker, Invio, Inserisci e hotkey | Inserimento Unicode in Notepad confermato; novità GUI e frecce+Invio 20.22 ancora da validare |
| R2 — Hotkey per snippet | Implementate in 20.25; catalogo, registrazione e inserimento testati con API simulate, prova desktop aperta |
| R5 — Bridge Unicode nel controllo posseduto | Alpha 20.20 confermata |
| R5 — Osservazione esterna in due snapshot | Compilazione e xUnit inclusi nella suite passata; prova esterna aperta |
| R5 — Osservazione WinEvent e anteprima trigger | Compilazione e xUnit passati dopo fix CS0246; prova nativa **NOT_RUN**, provenienza non qualificata |
| R6 — Espansione automatica editor esterni | Da sviluppare |
| R3 VS Code e R7–R8 | Facoltativo sospeso; nuove funzionalità e Linux/macOS successivamente |

## Obiettivo finale

Digitare un trigger Unicode come `;firma` in un editor compatibile e ottenere automaticamente lo snippet, senza aprire il picker.

## Cosa resta da fare

1. **P0:** qualificare le hotkey per snippet sul desktop con un profilo isolato, insieme a editor ridimensionabile e frecce+Invio della 20.22. Compilazione e xUnit sono passati.
2. **P0:** eseguire, solo se utile, la prova WinEvent e l'anteprima del trigger sintetico in un controllo `Edit` classico, senza dati personali.
3. **P1:** qualificare provenienza e sequenza dell'input committato: digitazione, IME, clipboard e modifiche sintetiche.
4. **P1:** sviluppare un adapter esterno con sostituzione condizionale e verifica dell'esito, opt-in e con fallback al picker.

[Roadmap](ROADMAP.md) · [Test](BUILD_AND_TEST.md) · [Alpha 20.24](ALPHA20_24_TRIGGER_PREVIEW.md) · [Handoff](HANDOFF.md).
