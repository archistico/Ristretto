# Verifiche Alpha 20.26

**Versione:** 3.9.0-alpha.20.26 · **Data:** 15 novembre 2026.

- Conferma utente: richiamo tramite hotkey funzionante in Blocco note Windows dopo la consegna 20.25; nessuna nuova matrice completa di compatibilità dichiarata.
- Suite completa: **846 xUnit passati**, inclusi **26 nuovi casi** di acquisizione e ciclo di sospensione/ripristino.
- Due regressioni nuove riprodotte prima della correzione: display dei soli modificatori con una vecchia combinazione ancora confermabile; correzione di un tasto non supportato mentre Ctrl rimane premuto.
- Nuova form non eseguita sul desktop: test di stato/coordinamento senza backend Avalonia, registrazioni, appunti o input nativo.

**GATE CI: PASS** nella cartella di lavoro e nello ZIP estratto in una cartella nuova senza `bin`/`obj`: 846 xUnit, 23 regressioni C#, dieci CLI, 32 Node e contratto .NET→Node passati. Solution e cinque strumenti compilati senza warning. Manifest di 492 hash verificato; hash dei sorgenti uguali. Restore locale; audit vulnerabilità online `NOT_RUN`. I rapporti aggiornati dopo l'estrazione mantengono invariati i sorgenti compilati.

[Esiti dei gate e hash](ALPHA20_26_VALIDATION.json) · [log](alpha20-26-validation.log) · [Uso e limiti](../docs/ALPHA20_26_HOTKEY_CAPTURE.md).
