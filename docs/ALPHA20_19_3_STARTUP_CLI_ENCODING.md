# Alpha 20.19.3 — risoluzione del blocco `startup_cli` in Windows

## Cosa stiamo facendo

Rendiamo riproducibile il gate `CORE` senza confondere i messaggi diagnostici della console Windows con Unicode UTF-8 garantito. L'utente ha confermato che nella Alpha 20.19.2 `source_audit`, `python_regressions`, `static_checks`, `dotnet_restore`, `solution_build`, `xunit` e le quattro build dei tool sono **PASS**, ma `startup_cli` è **FAIL**; i passi successivi sono `NOT_RUN`.

## Problema e causa

`scripts/check_startup_profile.py` invocava `subprocess.run(... capture_output=True, encoding='utf-8')`. Sul PC Windows alcuni messaggi di **errore** del comando `--check-profile` erano codificati nella codepage della console, contenendo il byte `0x95`. Il thread di decodifica interno a `subprocess` ha sollevato `UnicodeDecodeError`; `stderr` è rimasto `None` e `.strip()` ha prodotto `AttributeError`. Il gate `CORE` non ha più eseguito il passo successivo. Il codice applicativo .NET ha invece già compilato e passato gli xUnit.

## Correzione implementata

- Invocazioni dei processi sempre in **modalità binaria** (`stdout=PIPE`, `stderr=PIPE`, nessun `encoding`/`text=True`), con gli stessi timeout e codici di uscita attesi.
- Per gli errori attesi (codice `2`): obbligo di `stderr` non vuoto, `stdout` vuoto; **non decodificare** il messaggio di errore localizzato.
- Per i successi (codice `0`): `stderr` vuoto, `stdout` decodificato in UTF-8 **strict**. Conservate le verifiche sui percorsi con `日本語 🌍`, sui dati JSON e sull'assenza di avvio GUI/database.
- Errori reali (codice differente, uscita sul canale sbagliato, stdout UTF-8 invalido) rimangono **FAIL** con diagnostica senza testo sensibile.
- Nove regressioni in `scripts/test_check_startup_profile.py`, tra cui sottoprocessi Python veri che emettono byte CP1252/CP850 non UTF-8.

## Verifiche e limiti

Gli esiti della nuova versione sono in [rapporto strutturato](../reports/ALPHA20_19_3_VALIDATION_STATUS.json). Non dedurre la compilazione .NET della nuova Alpha dai risultati della precedente. Nessun file C# e nessuna impostazione del picker o di SQLite è stata cambiata.

## Come verificare su Windows (passi in italiano)

1. Estrarre lo ZIP Alpha 20.19.3 nella propria cartella e aprire PowerShell dove si trova `Ristretto.sln`.
2. Facoltativamente, eseguire `python -m unittest discover -s scripts -p "test_check_startup_profile.py" -v` per controllare i nove nuovi test.
3. Eseguire `.\scripts\validate.cmd`; verificare che `startup_cli=PASS` e alla fine `GATE CORE: PASS`.
4. In `CORE`, `node_tests` e `export_contract` rimangono intenzionalmente `NOT_RUN`; i test del desktop e l'espansione globale restano fuori da questo gate.
5. Se `startup_cli` fallisse ancora, condividere l'**errore iniziale** e il riepilogo, senza inviare il contenuto di documenti personali o degli appunti.

## Obiettivo e prossimi lavori

**Obiettivo:** tornare a uno script di validazione attendibile in Windows e continuare verso l'espansione automatica Unicode senza modificare il picker già confermato funzionante. **Dopo:** validare l'esperimento facoltativo WinEvent e qualificare una sorgente affidabile di testo Unicode committato per R5–R6; R3 VS Code rimane opzionale.
