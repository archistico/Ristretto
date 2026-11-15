# Handoff — Ristretto 3.9.0-alpha.20.19.3

**Sorgenti canonici:** nuovo ZIP completo con radice `Ristretto/`. Non sovrapporre due Alpha. **Stato:** [CURRENT_STATE.md](CURRENT_STATE.md). **Unica roadmap:** [ROADMAP.md](ROADMAP.md).

## Cosa è cambiato nella Alpha 20.19.3

- Corretto `scripts/check_startup_profile.py`: in Windows la lettura `encoding='utf-8'` causava `UnicodeDecodeError` nel reader thread quando un comando CLI con uscita 2 emetteva un messaggio di errore in codifica locale. Il conseguente `stderr=None` causava `AttributeError`. Ora si acquisiscono i byte in modo binario con timeout e codice di uscita invariati; solo stdout di successo viene decodificato rigorosamente come UTF-8.
- Nove test Python in `scripts/test_check_startup_profile.py` riproducono messaggi CP1252/CP850 di processi veri, Unicode UTF-8 valido, output non UTF-8 sul canale di successo e errori di contratto.
- Evidenza utente Alpha 20.19.2: gate `source_audit`, Python, statici, restore, solution, xUnit e quattro tool **PASS**; `startup_cli` **FAIL** per la decodifica; step seguenti `NOT_RUN`. Non scambiare questa evidenza con una validazione Alpha 20.19.3.
- Nessuna modifica a sorgenti C#, GUI, picker, hotkey, SQLite e logica R5. [Rapporto](ALPHA20_19_3_STARTUP_CLI_ENCODING.md).

## Funzionalità già presenti dalla Alpha 20.19

- `ExternalFocusBoundarySession` in Application azzera i suffissi sia di `TriggerActivationSession` sia di `CommittedInputSession` quando arriva un segnale di cambio finestra/focus o quando l'osservazione non è affidabile.
- Il laboratorio Windows offre **solo** `--observe-focus 15`: `SetWinEventHook` out-of-context riceve eventi di finestra/focus, senza tastiera, testo o clipboard. È disattivato per impostazione predefinita e termina dopo 3–30 secondi.
- Nuovi xUnit per reset, evento duplicato, segnale sconosciuto e divieto di trattare eventi non qualificati come testo committato.
- Nessun file GUI/XAML, gestione picker/Invio, hotkey, SQLite o output Windows di produzione modificato.

## Evidenze

- **Conferma utente precedente:** Alpha 20.15 compilata/testata, Inserisci/Invio ripristinato. Alpha 20.18 ha ottenuto `PASS` su documento sintetico Notepad Unicode con uso del picker confermato.
- **Questa Alpha:** [report verifiche](../reports/ALPHA20_19_3_VALIDATION_STATUS.json); Python, Node e controlli statici eseguibili qui. **Build .NET, nuovi xUnit e WinEvent reali = NOT_RUN**.
- Nessuna implementazione di text expander automatico per editor esterni: WinEvent segnala solo perdita di continuità; non certifica caratteri, revisione o caret.

## Istruzioni per riprendere

1. Estrarre il nuovo ZIP in una directory nuova. Chiudere Ristretto prima dei test che utilizzano la hotkey.
2. Dalla cartella con `Ristretto.sln` eseguire ` .\scripts\validate.cmd `: prima verificare che compaia **SORGENTI RISTRETTO 3.9.0-alpha.20.19.3 VERIFICATI**, poi controllare `python_regressions=PASS`, compilazione e xUnit. I nuovi xUnit si chiamano `ExternalFocusBoundarySessionTests`.
3. Solo **facoltativamente**, su Windows con desktop interattivo, avviare ` .\scripts\observe-focus.cmd 15 `, passare tra finestre/campi sintetici e leggere i conteggi. La procedura non chiede di digitare testo e **non** va usata per provare l'espansione automatica.
4. Se una verifica fallisce, riportare il messaggio e la versione stampata. Non modificare i sorgenti del picker per indagare sulla sonda focus.

## Obiettivo e lavoro successivo

**Ora:** qualificare il confine di focus R5. **Obiettivo:** espansione automatica Unicode in un target controllabile. **Da fare:** prove WinEvent e scelta di un canale affidabile di testo committato; focus/IME, target, revisioni e sostituzione verificabile; attivazione opt-in solo dopo evidenze. VS Code resta facoltativo, R2/R4 proseguono in parallelo.

Ogni consegna richiede ZIP completo, documenti correnti in italiano e risultati `PASS`/`FAIL`/`NOT_RUN` separati. Conservare `clean-build.cmd` e `clean-build.ps1`.
