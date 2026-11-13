# Primo backend Windows Unicode — v3.4 (spike isolato)

**Stato:** codice presente nei progetti `Application` e `Infrastructure`, **non compilato né eseguito**. Nessun hook globale e nessuna funzione di input esterno abilitata nella GUI. Il motore automatico rimane assente.

## Obiettivo e confini

Definire una prima implementazione verificabile dell'invio di testo Unicode a un'altra finestra Windows, senza trasferire nella GUI il comportamento nativo di `SendInput` e senza promettere garanzie che Windows non fornisce. Lo spike serve come base per i futuri esperimenti controllati; **non** è ancora la funzionalità text-expander.

- `src/TextExpander.Application/Features/Output/UnicodeOutputPlan.cs`: piano semplice e non mutabile, stato degli esiti. Non usa Windows o Avalonia.
- `src/TextExpander.Infrastructure/Platform/Windows/WindowsUnicodeTextOutput.cs`: acquisizione di un riferimento a una finestra esterna, rivalidazione immediata del destinatario e invio Win32 tramite API `SendInput`.
- `tests/TextExpander.Tests/WindowsOutputContractTests.cs`: mock delle chiamate native e prove senza alcun input reale; *non eseguite in questo ambiente*.

## Regole del primo spike

1. Su macOS e Linux il backend ritorna `Rejected/not_windows`, senza chiamare Win32.
2. Rifiuta destinazioni nulle, appartenenti al processo dell'applicazione o diverse dalla finestra in foreground all'istante dell'invio.
3. Rifiuta l'operazione quando Shift/Ctrl/Alt/Win risultano premuti. Non forza il focus e non usa `SetForegroundWindow`.
4. Accetta **solo** testo Unicode ben formato, non vuoto, con un massimo di **1.024 unità UTF-16**. Non normalizza l'output: preserva accenti, emoji composte, modificatori e caratteri CJK/RTL.
5. Rifiuta caratteri di controllo, separatori Unicode U+2028/U+2029, `\r`, `\n` e `\t` perché la loro semantica con `SendInput` non è ancora qualificata; servono metodi dedicati per snippet multilinea.
6. Ogni unità UTF-16 è inviata con evento di pressione `KEYEVENTF_UNICODE` ed evento di rilascio `KEYEVENTF_UNICODE | KEYEVENTF_KEYUP`. L'intera sequenza viene sottoposta in **un'unica chiamata `SendInput`**.
7. Il ritorno da `SendInput` è solo **numero di eventi accettati nello stream**, **NON** conferma che l'editor mostri il contenuto corretto. Risultati: `Rejected`, `SubmittedUnverified`, `PartialOrUnknown`.
8. Nessuna ripetizione automatica, nessun rollback automatico, nessuna modifica degli appunti, nessuna cancellazione del trigger e nessun logging del testo utente.

## Limitazioni che impediscono di abilitarlo in produzione

- Il controllo della finestra foreground non identifica il campo interno, né dimostra che non sia una password, un campo protetto o una composizione IME attiva.
- Le restrizioni UIPI e gli editor che ignorano `VK_PACKET` possono impedire l'input.
- Un cambio di focus dopo il controllo e prima/durante la chiamata non è eliminabile con questo meccanismo.
- Non sono ancora disponibili: la cattura globale dei trigger, la gestione sicura della rimozione Unicode, l'inserimento di `Tab`/newline, il fallback tramite clipboard, l'attivazione per applicazione e il recupero dell'esito effettivo.
- Non tentare di far coincidere `string.Length` con il numero di Backspace da inviare; questa slice non effettua cancellazioni.

## Prove automatiche predisposte

I test C# coprono testo Unicode inalterato (emoji ZWJ, skin tone, accenti composti, CJK, RTL), rifiuto di coppie surrogate non valide, controllo dei limiti, focus alterato, self-target, modificatori premuti, input parziale e sistemi non Windows. Tutte le prove utilizzano fake `IWindowsTextApi`: **non sono prove di compatibilità con applicazioni esterne**.

## Gate prima di una futura integrazione GUI

1. `dotnet restore`, `dotnet build -c Release` e `dotnet test` completi su Windows con .NET 10.
2. Verificare dimensione/marshalling di `INPUT`, callback e `SendInput` in build Windows x64 e ARM64; correggere eventuali warning.
3. Eseguire smoke test manuali in Notepad, VS Code, browser e editor GTK/Qt quando applicabile, con layout italiani/internazionali, emoji e CJK.
4. Verificare focus, campo password, IME, UIPI, sessioni remote e comportamento con modifier premuti.
5. Solo dopo, progettare uno **specifico gesto esplicito** o una scorciatoia con cattura sicura del destinatario. La cancellazione del trigger e l'espansione automatica sono una slice successiva, non parte di questo spike.

## Fonti di API

- [Microsoft SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)
- [Microsoft GetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getforegroundwindow)

**Decisione:** spike architetturale predisposto nel codice, ma non autorizzato ad agire nel desktop finché non supera il gate. Le librerie SQLite, gli importatori e il picker locale restano invariati.
