# Alpha 14 — provenienza delle mutazioni nell’EDIT controllato

**Sorgenti:** Ristretto 3.9.0-alpha.14 · **GUI:** Preview 9 congelata · **Esecuzione .NET / nativa:** `NOT_RUN`.

## Problema risolto

Il verificatore del delta dell’Alpha 13 dimostra *che cosa* è stato inserito nel documento, ma non *quale azione* l’abbia generato. Un incollamento o un aggiornamento programmatico possono produrre la stessa differenza di stringhe. In Alpha 14 la sessione richiede **due verifiche indipendenti**:

1. `CommittedEditDeltaVerifier`: delta Unicode integro, target invariato, revisione consecutiva, caret corretto e contesto sicuro.
2. `OwnedEditProvenancePolicy`: metadati effimeri di provenienza **correlati alla stessa revisione ed epoch**, osservati dal ciclo dei messaggi del *nostro* EDIT oppure dalla fixture sintetica esplicita.

La seconda verifica precede il riconoscimento; in caso di diniego il buffer dei trigger viene **azzerato** senza tentare alcuna modifica. I campi protetti rimangono schermati dalla prima verifica.

## Sorgenti accettate e rifiutate

| Origine | Demo interattiva | Test sintetico | Motivazione |
|---|---|---|---|
| `OwnedCharacterMessage` durante dispatch dell’EDIT | Ammessa, se delta e focus sono validi | Ammessa | Messaggio locale e revisione correlati |
| `OwnedFixture` eseguita dal laboratorio | **Negata** | Ammessa solo con opt-in interno | Non è digitazione |
| `ClipboardOrPaste` | Negata | Negata | Provenienza incompatibile con la policy corrente |
| `ImeOrComposition` | Negata | Negata | IME disabilitato solo nel laboratorio; non qualificato |
| `ProgrammaticChange`, `Unknown`, evento senza correlazione | Negata | Negata | Origine non attestata |

Un `WM_CHAR` stampabile **non dimostra input fisico o umano**: può essere inviato sinteticamente. La demo non registra i caratteri né l’origine nel report; memorizza temporaneamente solo enum, epoch e numero di revisione. I messaggi di controllo (`Ctrl+V`, `Ctrl+X`, ecc.) non sono classificati come inserimenti di caratteri. Gli aggiornamenti arrivati da messaggi inviati sincronicamente fuori dal nostro ciclo di dispatch vengono classificati `Unknown` e bloccati.

L’eventuale origine di incollamento non viene sempre identificata con precisione da un controllo `EDIT` classico, per esempio se Windows gestisce comandi interni tramite `SendMessage`. In quei casi viene rifiutata come `Unknown`, non attribuita erroneamente alla digitazione. Questo è un limite di copertura, **non** un permesso di espansione.

### Restrizioni intenzionali

- Nessun hook globale, nessun listener della tastiera in altre applicazioni, nessun accesso agli appunti, nessun salvataggio dei testi o dei key code.
- La sonda di editor esterni Alpha 10–11 resta **read-only** e non può creare prove `OwnedEditMutationEvidence` autorizzate.
- La sostituzione qualificata continua a essere disponibile **solo** nel controllo Win32 posseduto dal laboratorio.
- La gestione IME generale, `SendInput`, browser, Word, RichEdit e widget personalizzati rimangono **non qualificati**.
- Il polling del laboratorio può aggregare più revisioni: in quel caso il validatore del delta interrompe il riconoscimento. Non è una garanzia di catturare ogni input.

## File modificati

- `src/TextExpander.Application/Features/Output/CommittedEditMutationProvenance.cs`: policy pura e motivi di diniego.
- `tools/Ristretto.WindowsOutputLab/OwnedEditTarget.cs`: origine osservata durante `DispatchMessageW`, revisioni stampate e fixture distinte; test nativo con `WM_CHAR` postato per **dimostrare che non è prova umana**.
- `tools/Ristretto.WindowsOutputLab/OwnedEditRecognitionSession.cs`: gate di provenienza dopo il delta, prima del trigger.
- `tests/TextExpander.Tests/OwnedEditProvenanceTests.cs`: nuovi test di ammissione, negazione, mismatch e privacy.
- `tools/Ristretto.WindowsOutputLab/NativeReplacementLab.cs`: due scenari nativi aggiuntivi; **25 previsti** (21 Alpha 7 + 2 Alpha 13 + 2 Alpha 14).

## Validazione e accettazione

Da una **nuova estrazione** dello ZIP su Windows:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

Per la dimostrazione con input reale:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab -- --replacement-demo
```

Digitare `;firma` + spazio nel campo del laboratorio, poi chiudere la finestra. Per controllare il comportamento di sicurezza, provare anche a incollare `;firma `: **non** deve espandersi. Se il sistema inoltra incollamenti come messaggi che non possiamo distinguere con sufficiente certezza, la demo va considerata *non qualificata* e la procedura deve essere ulteriormente isolata. Validare separatamente layout, accenti, emoji e composizione: i risultati storici non sostituiscono una nuova esecuzione.

**Non abbiamo eseguito build .NET, xUnit o prove Windows in questa consegna.** Restano aperti i gate delle Alpha 8–14; ultima baseline attestata Alpha 7 (180/180 xUnit, 21/21 nativi).

## Prossimo gate — Alpha 15

Test manuali Win32 della provenienza e del polling, qualificazione degli eventi nel controllo posseduto (inclusi incollamenti tramite menu/shortcut), e scelta di un percorso di input *committato* verificabile per il primo adapter esterno. Non introdurre un hook generale prima di ottenere una matrice applicazione/capacità reale.
