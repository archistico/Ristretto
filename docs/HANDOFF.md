# Handoff — Ristretto 3.9.0-alpha.20.18

**Sorgenti canonici:** archivio ZIP integrale Alpha 20.18, estratto in una cartella nuova. **Stato corrente:** [CURRENT_STATE.md](CURRENT_STATE.md). **Roadmap unica:** [ROADMAP.md](ROADMAP.md).

## Cosa è cambiato

- Diagnostica del file sintetico in **italiano** e più precisa (`snippet_inserted_twice`, posizione, intestazione, NFC/NFD e riga finale).
- `status` e `verify` condividono la classificazione, ma solo `verify` crea una ricevuta immutabile priva di contenuti.
- `prepare --open` conserva la sessione anche quando l'avvio di Notepad fallisce; nuovi test simulano Windows e altri sistemi senza aprire editor.
- File C# di produzione, picker, gestione focus, hotkey, GUI Preview 9, database e import/export **invariati** rispetto alla Alpha 20.17.

## Evidenze

- Baseline utente: **Alpha 20.15 compilata e testata, Invio in Notepad funzionante**.
- Alpha 20.18: controlli statici, Python e Node eseguibili qui; risultati nel report della consegna.
- Build .NET, nuovi xUnit, prove native e inserimento manuale Windows: **NOT_RUN** finché non verificati sul desktop.

## Come riprendere

1. In nuova estrazione, verificare **`VERSION.txt` e `Directory.Build.props`**; evitare di mescolare cartelle di due Alpha.
2. Eseguire `scripts\validate.cmd` e `scripts\validate-picker.cmd` in PowerShell usando ` .\scripts\...` (vedi [istruzioni](BUILD_AND_TEST.md)).
3. Per Notepad eseguire i passi guidati `prepare --open`, `seed`, `status`, `launch`, inserimento **manuale** e `verify` descritti in [Alpha 20.18](ALPHA20_18_NOTEPAD_DIAGNOSTICS.md). Se `FAIL`, non ripetere Inserisci sullo stesso file già modificato: creare una nuova sessione.
4. Confermare su Windows contenuto Unicode multilinea e casi negativi della [matrice R2](R2_PICKER_WINDOWS_ACCEPTANCE.md). Non dedurre compatibilità generale da un solo `PASS`.
5. Proseguire R3/VS Code reale senza introdurre hook globali e senza modificare il percorso Invio che l'utente ha già confermato funzionante.

## Obiettivo e prossimi lavori

**Adesso:** terminare R2 mediante prove desktop.

**Obiettivo finale:** espansione automatica Unicode affidabile per editor qualificati, quindi altre piattaforme e funzionalità avanzate.

**Ancora aperto:** CI reale, R2 desktop, R3 host VS Code, R4 packaging e prestazioni, R5–R6 primo automatismo, R7 template e R8 OS.

Ad ogni incremento consegnare **ZIP completo**, audit, risultati `PASS/FAIL/NOT_RUN`, documentazione aggiornata e istruzioni passo per passo in italiano. Gli script `clean-build` sono da conservare.
