# Alpha 20.16 — R2.05: profilo SQLite sintetico per qualificare il picker in Notepad

**14 novembre 2026.** Incremento funzionale limitato al laboratorio e ai suoi test. La **Alpha 20.15 è stata confermata dall'utente**: compilazione, test e gesto **picker → Invio → Notepad** funzionanti. Nessuna modifica a GUI Preview 9, hotkey, servizio Inserisci o schema di produzione.

## Cosa stiamo facendo

Automatizziamo la preparazione degli snippet sintetici per la prova già disponibile in `scripts/qualify_notepad.py`. In precedenza, per verificare Unicode e multilinea nel documento Notepad si doveva copiare manualmente una fixture lunga dentro un profilo di prova. Un errore di copiatura poteva produrre un FAIL non attribuibile al picker.

Il nuovo comando `--seed-picker-profile TOKEN` nel laboratorio `Ristretto.WindowsOutputLab` **riutilizza `SqliteLibraryStore`, `SnippetDraft`, `SaveSnippetAsync` e `ReadAsync` di produzione** per creare **un solo** gruppo e snippet `;ristretto-test` contenente esattamente la fixture UTF-8 del progetto. Il nuovo script `scripts/seed-picker-notepad.cmd` richiama il comando. Non sono stati creati nuovi formati di database o percorsi alternativi di salvataggio.

## Procedura Windows

Da `Ristretto/`, con Python 3.10+, .NET SDK 10 e Notepad disponibili:

```bat
scripts\validate-picker-notepad.cmd prepare --open
```

Il comando crea una sessione nuova e mostra il suo token a 32 caratteri. Sostituire `TOKEN` con quello stampato:

```bat
scripts\seed-picker-notepad.cmd TOKEN
```

Il comando stampa il percorso **esatto** della nuova cartella `profile`. Avviare Ristretto da una seconda console specificando quel percorso (attenzione alle virgolette se ci sono spazi):

```bat
dotnet run -c Release --project src\TextExpander.Desktop\TextExpander.Desktop.csproj -- --data-dir ".local-cache\notepad-qualification\TOKEN\profile"
```

Controllare che Ristretto sia in esecuzione con **il nuovo profilo di prova**, non con quello personale. Se un'istanza diversa intercetta il secondo avvio, chiuderla prima di proseguire. Nella finestra Notepad del documento sintetico usare **Ctrl+End**, richiamare il picker dalla hotkey e cercare `;ristretto-test`. Premere **Invio** oppure il pulsante **Inserisci**. Salvare **solo** quel documento sintetico con **Ctrl+S**; poi eseguire:

```bat
scripts\validate-picker-notepad.cmd verify --session TOKEN
```

`PASS` significa che il **file salvato** corrisponde a prefisso e fixture Unicode, senza normalizzare accenti NFC/NFD; `FAIL` significa differenza, `NOT_RUN` significa file invariato, mancante o prova non verificabile. La prova non dimostra la provenienza fisica del testo né abilita l'espansione automatica globale. Il contenuto del file è sintetico e non viene inserito nei report JSON.

## Sicurezza e regressioni

Il seeder accetta soltanto un token esadecimale minuscolo di 32 caratteri con sessione già generata. Verifica schema JSON, token, versione corrente, SHA-256 della fixture e **baseline del documento ancora invariata**. Rifiuta collegamenti simbolici sui percorsi di sessione, input assenti e cartelle `profile` già presenti: non sovrascrive alcun database. Non legge né scrive il profilo predefinito. Non invia tasti, non apre Notepad e non legge gli appunti. La prova richiede sempre un'azione manuale.

Sei nuovi test xUnit verificano salvataggio/readback SQLite e Unicode, divieto di overwrite, token non valido, fixture cambiata, documento modificato e sessione inesistente. Sono inclusi in `scripts\validate-picker.cmd` e nel normale `scripts\validate.cmd`. Il seeder viene compilato anche dentro il progetto test con sorgente collegato, evitando un riferimento a un eseguibile del laboratorio.

**Validazione:** i test .NET e il desktop Windows di Alpha 20.16 rimangono `NOT_RUN` finché non vengono eseguiti realmente. I risultati Python/Node e statici sono registrati in `reports/ALPHA20_16_VALIDATION_STATUS.json`.

## Obiettivo finale e cosa manca

**Obiettivo:** misurare con dati riproducibili che Invio/Inserisci dal picker trasferisca correttamente Unicode multilinea a Notepad, prima di qualificare altri editor e passare al primo adapter di espansione automatica.

**Resta:** build/xUnit della nuova Alpha, una prova `prepare → seed → editor → verify` nel desktop reale, scenari di focus/clipboard concorrenti, qualificazione di altri target, host VS Code e decisione R5 sul primo automatismo. Conservare il fallback tramite picker per editor non qualificati.
