# Alpha 13 — F3: verifica delle modifiche di testo realmente presenti nell'EDIT posseduto

**Sorgenti:** Ristretto 3.9.0-alpha.13 · **Ambito:** solo laboratorio Win32 proprietario · **Test nativi e xUnit Windows:** `NOT_RUN` in questa consegna.

## Perché serve

Gli eventi fisici di tastiera (`WM_KEYDOWN`, scan code, layout, dead key) non identificano in modo affidabile il carattere Unicode effettivamente inserito. Incollamento, accenti composti, emoji ZWJ e composizione IME rendono errata ogni ricostruzione basata sui tasti.

L'Alpha 7 disponeva di una verifica di differenza tra snapshot, privata della classe `OwnedEditRecognitionSession`. L'Alpha 13 rende questa verifica un contratto *puro*, testabile e riutilizzabile nel modulo Application: `CommittedEditDeltaVerifier.Verify`.

## Contratto del verificatore

L'adapter proprietario, sul **proprio thread UI**, passa le snapshot *precedente* e *corrente*, più informazioni sul contesto. Il verificatore accetta un inserimento soltanto se:

1. Il chiamante dichiara in modo verificabile nel proprio adapter di possedere il controllo e il focus; il flag DTO **non costituisce una capacità di sicurezza OS**, né può essere imposto per un editor arbitrario.
2. La composizione IME è nota e inattiva; nessun modificatore è premuto.
3. Token e epoch del target coincidono e sono validi; la revisione del testo avanza di **esattamente una** unità.
4. Il controllo è stato verificato come modificabile, non protetto, e con capacità di sostituzione condizionale del solo laboratorio.
5. Le due snapshot sono Unicode ben formato, con selezione collassata e offset UTF-16 validi.
6. L'intera modifica è esattamente un **inserimento contiguo al caret precedente**, senza alterare prefisso o suffisso preesistenti; il caret finale segue l'inserimento.
7. L'inserimento non supera 1.024 unità UTF-16, non contiene caratteri di controllo/separatori di riga e non spezza un confine grafemico del documento precedente o del caret finale.

Il risultato positivo è `VerifiedCommittedEditInsertion`, contenente un'osservazione Unicode **esatta e non normalizzata** e l'eventuale prova del confine iniziale della parola. Solo quest'ultima consente di riconoscere trigger alfabetici come `brb` dopo uno spazio. Dopo una lettera, un numero, un connettore `_` o un segno combinante il confine resta sconosciuto.

Un esito `NoChange` non resetta la sessione; ogni altro rifiuto resetta la storia di digitazione. In caso di snapshot diverse ma revisione non avanzata, salto di revisione, cambio focus/epoch, selezione, cancellazione, incollamento non qualificato o altro comportamento non riconosciuto: **nessuna attivazione**. Il testo di ingresso non viene registrato o esportato nei report. I metodi `ToString()` degli oggetti che racchiudono testo temporaneo lo oscurano.

## Cosa il risultato NON dimostra

Una differenza verificata dimostra un *inserimento nel documento posseduto*, **non** che sia stato digitato da una persona: anche un incollamento o una chiamata programmatica al controllo possono essere osservati come un inserimento. La demo usa esplicitamente un campo controllato con IME disassociato; non garantisce input committato affidabile in applicazioni terze o con IME generali. Eventuali adapter esterni richiedono provenienza attendibile dell'evento, policy propria di sicurezza, lettura/versionamento del documento e un replace atomico verificabile. `ExpansionAdmissionPolicy` resta limitata a `OwnedCooperatingLaboratory`.

In Alpha 13 non sono stati aggiunti hook, invii globali, acquisizione del testo di applicazioni esterne o automatismi su Word/browser. Il picker e la sua hotkey globale Windows (Alpha 12) restano invariati.

## Percorsi modificati

- `src/TextExpander.Application/Features/Output/CommittedEditDeltaVerifier.cs`: nuovo validatore senza side effect.
- `tools/Ristretto.WindowsOutputLab/OwnedEditRecognitionSession.cs`: sostituita l'implementazione privata con l'uso del contratto; inoltrata la prova del word boundary.
- `src/TextExpander.Core/Matching/CommittedInputSession.cs`: diagnostica `ToString()` redatta.
- `tests/TextExpander.Tests/CommittedEditDeltaVerifierTests.cs`: test con input Unicode, revisione/focus, IME, modificatori, selezioni, cambi non contigui, sicurezza, boundary e assenza di payload nei log.

Sono aggiunti anche **2 scenari nativi** alla suite del laboratorio (`brb` dopo spazio e `brb` dentro un’altra parola). Quindi il totale atteso dei casi nativi è **23** rispetto ai 21 storicamente verificati in Alpha 7.

## Validazione richiesta

In **una cartella nuova** estratta dallo ZIP:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

Il primo comando esegue solution, xUnit e compilazione laboratori; il secondo esegue suite Win32 e smoke cooperante, da una sessione desktop Windows. La digitazione umana rimane un gate separato:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab -- --cooperating-demo
```

Digitare `;firma` seguito da spazio nel campo della prova, chiudere la finestra e verificare l'esito. Nessuna validazione reale Windows/.NET viene dichiarata superata dal presente rilascio.

## Prossima milestone

**Alpha 14 — sorgente degli eventi e provenance:** distinguere eventi di edit realmente osservati da aggiornamenti synthetic/paste/IME, mantenendo il reset fail-closed e delimitando precisamente gli adapter ammissibili. Solo dopo è sensato preparare un adapter esterno con lettura del documento, revisione e sostituzione atomica, senza presumere che `EM_GETSEL` sia sufficiente.
