# F2a — laboratorio d'inserimento Unicode Windows (modalità --self-test)

**Aggiornamento alpha.7:** è disponibile una prova separata `--replacement-test` con [sostituzione e rilettura in un EDIT posseduto](F2B_WINDOWS_OWNED_EDIT.md), 21/21 scenari nativi passati. Usa messaggi al proprio controllo sul thread UI; non verifica SendInput. Questa pagina resta il manuale della modalità F2a `--self-test`, senza prova di consegna superata.

**Stato corrente:** tool compilato nell'ultima [validazione alpha.7](../reports/WINDOWS_ALPHA7_VALIDATION.json), test del gate puro inclusi nei 180 xUnit passati. Un tentativo interattivo nella alpha.3 è stato bloccato con `target_changed`, uscita 3, prima di SendInput; la consegna resta non qualificata. Il risultato è conservato nel [changelog](CHANGELOG.md), senza mantenere log duplicati delle vecchie build.

Non si avvia con Ristretto e non abilita hook globali o autoespansione.

## Perché un laboratorio autonomo

`WindowsUnicodeTextOutput` esisteva già come SPIKE: `SendInput` può accettare gli eventi senza che il campo destinatario li riceva. Inoltre controllare solo la finestra in primo piano non identifica la casella effettiva e non consente di distinguere password, controllo in sola lettura e IME. In F2a **non** si integra perciò l'adapter nell'app, né si invia testo a editor arbitrari.

Il laboratorio `tools/Ristretto.WindowsOutputLab` crea **un secondo processo figlio** con una piccola finestra Win32 e un controllo `EDIT` standard, dove poter confrontare in memoria il risultato effettivo con la fixture inviata. Non condivide lo schema SQLite, la GUI Avalonia o la libreria di snippet.

## Esecuzione Windows, solo su scelta esplicita

Da una **nuova** estrazione della release:

1. Lanciare `scripts\validate.cmd` per verificare build, test xUnit e compilazione anche del laboratorio. Il comando **non avvia** il laboratorio e non invia tasti.
2. Aprire un terminale PowerShell nella cartella `Ristretto` e lanciare:

   ```powershell
   dotnet run --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --self-test
   ```

3. Si apre una piccola finestra `Ristretto F2a Test Target...`. **Cliccare il campo bianco**; dopo cinque secondi il laboratorio prova ad inviare una fixture fissa (accenti, emoji, combinanti, CJK, arabo). Se il campo non è quello previsto, il test non chiama `SendInput`.
4. Il terminale riporta `TEST NON ESEGUITO`, `PASS`, `FAIL` o `ESITO NON VERIFICABILE`; la finestra del figlio viene chiusa al termine. Il laboratorio non stampa mai il testo acquisito dal campo.

**NOTA:** è un test nativo sperimentale su un editor **controllato dal laboratorio**, non una dichiarazione di compatibilità con Notepad, Word, browser, IDE o campi password. Può fallire per focus, UIPI, IME, SendInput o restrizioni della sessione. Non ripete automaticamente inserimenti parziali. L'eventuale cambio di focus fra preflight e stream resta una condizione di race: NON usare questo protocollo come autorizzazione per produrre un text expander in applicazioni terze.

## Cosa è implementato

- `WindowsExplicitTestTargetGate`: componente puro che riceve una snapshot osservata e rifiuta ID/PID invalidi, finestre/focus diversi, controlli non `EDIT`, non visibili, disabilitati, password, readonly e composizione/contesto sconosciuto. Testabile senza Windows.
- Il programma laboratorio genera un nonce per il processo figlio, legge gli handle del suo `EDIT`, ricontrolla handle, processo, focus e stile del campo, quindi usa l'adapter già presente solo per quel target. `Submit` ricontrolla finestra, PID e modificatori prima di inviare.
- Un processo figlio restituisce *solo al processo padre* il contenuto del proprio campo tramite pipe; il padre confronta byte UTF-16 (stringhe .NET esatte, inclusi i combinanti) e comunica soltanto l'esito. Nessun contenuto salvato su file.
- Eventi Win32 in una singola chiamata `SendInput`, niente clipboard, niente trigger cancellati, niente riconoscimento globale.
- I test xUnit `WindowsExplicitTestTargetGateTests` verificano le condizioni fail-closed senza P/Invoke.

## Limiti e prossimi gate

1. **Gate build/test:** il laboratorio è compilato nella validazione ordinaria; i test del gate puro non dimostrano la consegna Win32.
2. **Gate prova nativa F2a:** lanciare esplicitamente `--self-test` su Windows desktop, annotando versione OS, layout e risultato. Non usare una sessione con IME in composizione: il laboratorio possiede la casella vuota e presume, solo per questo esperimento deliberato, che non si stia componendo testo.
3. **Matrice applicazioni reali:** servono nuovi adapter di ispezione/accessibilità, consenso e classificazione del campo, un modello serio di IME, prove sui privilegi e fallback. Il presente gate `EDIT` non generalizza a WebView/Chromium/Electron/RichEdit.
4. **F2b:** [sostituzione condizionale](F2B_CONDITIONAL_REPLACEMENT.md) verificata nella simulazione e nel proprio EDIT; adapter esterno ancora da qualificare. **F3:** sorgente di eventi committati e attivazione globale opt-in restano da sviluppare.

## Proprietà NON garantite

- `SendInput` che restituisce tutti gli eventi non significa testo scritto. Solo il laboratorio controlla il contenuto del proprio EDIT.
- Non c'è una transazione atomica tra verifica del foreground, focus, invio e lettura; in ambienti reali servono ulteriori garanzie e/o fallback.
- Questo esperimento **non prova** la gestione universale di IME, assistive technologies, sessioni elevate, RDP e campi protetti. Il laboratorio non prova a bypassare le protezioni.

Riferimenti: [output Windows esistente](WINDOWS_OUTPUT_SPIKE.md), [roadmap funzionale](FUNCTIONAL_ROADMAP.md), [stato](CURRENT_STATE.md).
