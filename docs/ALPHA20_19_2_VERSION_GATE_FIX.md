# Alpha 20.19.2 — correzione del gate Windows su versioni prerelease

## Problema osservato

L'utente ha eseguito con successo **64 test Python** nella versione 3.9.0-alpha.20.19.1. Il comando `scripts\validate.cmd`, invece, terminava subito con:

```text
ERRORE VALIDAZIONE RISTRETTO: Versione MSBuild non valida.
Estrai lo ZIP integrale della versione corrente in una cartella nuova; non unire le cartelle di versioni differenti.
```

L'archivio esaminato contiene `Directory.Build.props` con la versione corretta `3.9.0-alpha.20.19.1` e `VERSION.txt` coerente. Il problema **non** era causato dall'estrazione né da MSBuild.

## Causa verificata nel sorgente

La condizione dello script PowerShell usava una espressione regolare che richiedeva esattamente due gruppi numerici dopo `-alpha`, per esempio `-alpha.20.19`. La versione valida `-alpha.20.19.1`, invece, ne contiene tre. L'interruzione si verificava **prima dell'avvio del gate Python o della compilazione .NET**.

## Correzione

- `scripts/verify-release.ps1`: il controllo ora accetta `MAJOR.MINOR.PATCH-alpha.<numero>[.<numero>...]`, con uno o più segmenti numerici. Continua a respingere formati senza identificativi, componenti vuote e suffissi alfanumerici imprevisti.
- È mantenuto il controllo di **uguaglianza esatta** tra `VERSION.txt` e `Directory.Build.props`.
- È corretto il messaggio diagnostico: non si suggerisce più automaticamente una nuova estrazione dello ZIP.
- `scripts/test_release_version_guard.py`: quattro test controllano la regola realmente presente nel file PowerShell, l'accettazione di versioni con più segmenti, il rifiuto dei casi malformati e la diagnostica.
- `VERSION.txt`, `Directory.Build.props` e documentazione allineati alla versione **3.9.0-alpha.20.19.2**.

## Verifiche ed evidenze

Risultati della preparazione della hotfix in ambiente Linux: **68/68 test Python PASS**, **32/32 test Node PASS**, audit e verifiche statiche PASS. Sono risultati dei test del gate e degli strumenti, non una prova della compilazione del programma.

Non sono stati eseguiti qui PowerShell, .NET 10, xUnit o test Windows nativi. L'utente aveva già confermato i 64 test Python superati nella precedente Alpha 20.19.1. [Report strutturato](../reports/ALPHA20_19_2_VALIDATION_STATUS.json).

## Come confermare su Windows

1. Estrarre la Alpha 20.19.2 in una cartella separata e aprire PowerShell dove si trova `Ristretto.sln`.
2. Eseguire `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-release.ps1`: attendersi `SORGENTI RISTRETTO 3.9.0-alpha.20.19.2 VERIFICATI`.
3. Eseguire `.\scripts\validate.cmd`: prima il gate preliminare, poi audit, Python, statici, compilazione .NET e xUnit.
4. In caso di errore riportare il **primo** messaggio, senza considerare `NOT_RUN` come un test fallito o superato.

## Stato e prossimo passo

Resta prioritario R5: la barriera di focus è implementata ma non ancora verificata nativamente. Il picker/Notepad Unicode ha una precedente conferma manuale positiva e non è stato modificato. Dopo il ripristino del gate Windows, proseguire con la sorgente attendibile di testo Unicode committato e l'automatismo inizialmente esplicito.
