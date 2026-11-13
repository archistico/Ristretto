# F1a — Catalogo dei trigger e sessione di testo committato

**Implementato nei sorgenti 3.9.0-alpha.1, da compilare e testare su .NET 10.** Non è collegato al desktop, non intercetta tastiera, non invia input a Windows e non modifica SQLite.

## Componenti

- `Core/Matching/CompiledTriggerCatalog.cs`: compila una snapshot già letta da SQLite in una tabella di trigger NFC case-sensitive e prefissi. Ignora snippet/gruppi disabilitati e trigger non ammissibili. Il matching cerca solo lunghezze distinte e suffissi, invece di scorrere ogni snippet per tasto; il caricamento della snapshot NON avviene nel callback input.
- `Core/Matching/CommittedInputSession.cs`: memorizza **soltanto** un suffisso di digitazione temporaneo per un `TargetEpoch` opaco. Il massimo è 512 unità UTF-16, e ogni osservazione accettata ha massimo 1024 unità. Il taglio dei suffissi avviene su confini grapheme; lo snapshot viene sostituito con `PublishSnapshot()` che cancella il contesto precedente.
- `tests/TextExpander.Tests/CommittedInputSessionTests.cs`: casi xUnit di canonical equivalence, emoji ZWJ, giapponese e arabo, ambiguità, prefissi, cambi focus, pausa, sicurezza, Unicode malformato e 10k trigger sintetici. Sono **test aggiunti**, non risultati confermati in questo ambiente.

## Contratto delle osservazioni

Un adapter futuro dovrà fornire `CommittedInputObservation(TargetEpoch, Text, Kind, FieldSafety)` da un canale serializzato. `VerifiedCommitted` è utilizzabile **solo** quando il backend attesta davvero il testo già committato; `VerifiedNormalEditable` non può essere assunto da un semplice evento di tasto. Nessuna API corrente del desktop fornisce ancora questa garanzia. In particolare, la policy conservativa è:

- `Composing`, `Unknown`, `Synthetic`, navigazione/selezione → cancellare il suffisso, `Suppressed`.
- Campo `Protected`/`Unknown`, target epoch non valido, input nullo o Unicode malformato, controlli (anche invio/tab), chunk eccessivi → cancellare il suffisso, `Suppressed`.
- Cambio di `TargetEpoch` → cancellare il contesto precedente e utilizzare **solo il nuovo** evento affidabile, senza ricongiungere testo di due finestre.
- `SetPaused(true|false)` o pubblicazione snapshot → azzeramento del suffisso.
- Match doppi canonicalmente equivalenti → `Ambiguous`. Prefisso di un trigger più lungo → `WaitingForLongerTrigger` anche se un trigger più corto combacia. Nessuna decisione retroattiva su prefisso interrotto (futura policy da definire in F1b).

`TriggerRecognition` è **informativo**, non un `ExpansionPlan`. Non contiene il numero di Backspace né autorizza modifiche al target. Anche quando ritorna `Matched`, l'output automatico rimane **inattivo**.

## Limiti e prosecuzione (F1b ora aggiunta in alpha.2)

1. La policy attuale è conservativa per prefissi sovrapposti: ad esempio, con `;sig` e `;signature`, `;sig` attende. Se il testo prosegue con `x`, il match corto non viene eseguito retroattivamente. In F1b è stata introdotta una sessione separata con regole `Immediate`/`OnDelimiter`, confini di parola e test. Vedere [F1b](F1B_ACTIVATION_POLICY.md); il comportamento F1a è volutamente invariato.
2. La composizione IME può essere osservata correttamente solo da un adapter nativo con evidenza reale; la classe non deduce testo da `KeyDown`.
3. La sessione è **single-consumer**, senza lock globali: un futuro wrapper deve serializzare eventi, transizioni di focus e pubblicazioni snapshot.
4. Le stringhe .NET sono immutabili: il reset elimina i riferimenti gestiti ma non promette azzeramento crittografico della memoria. Nessun buffer viene serializzato.
5. Questo codice non misura ancora latenze o RSS. Per 10k trigger è presente una fixture di correttezza, NON un benchmark prestazionale.
6. Win32 `SendInput` isolato è ancora presente come spike storico e non viene richiamato da nessuna finestra.

## Test richiesti

Su Windows, da nuova estrazione dello ZIP:

```
scripts\validate.cmd
```

Verificare che i test nuovi compaiano e passino, che il picker continui a copiare negli appunti e che nessuna nuova azione di inserimento o hook sia disponibile. Su Linux/macOS, dopo installazione SDK, `bash scripts/validate.sh`. Non considerare build, xUnit o prove native conclusi finché l'esecuzione effettiva non è documentata.
