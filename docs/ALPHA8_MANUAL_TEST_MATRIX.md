# Alpha 8 — Matrice di prova manuale Windows

**Non eseguita in questa consegna.** Annotare l'esito senza incollare qui dati personali o testo privato. Il campo della demo contiene solo dati sintetici.

## Preparazione

1. Estrarre lo ZIP in una **cartella nuova**.
2. Eseguire `scripts\validate.cmd`, poi `scripts\validate-native.cmd` da una sessione Windows interattiva.
3. Avviare:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --replacement-demo --report reports/alpha8-demo-propria.json
```

Il percorso del report deve essere **nuovo**: il tool usa `CreateNew`, quindi non sovrascrive evidenze esistenti. Nessuna schermata del prodotto principale viene modificata.

## Passaggi

| Scenario | Azione | Risultato atteso | Esito da annotare |
|---|---|---|---|
| Digitazione | Clic nel campo del laboratorio; digitare `;firma` e uno spazio | Testo sintetico multilingue al posto del trigger; spazio preservato; `VerifiedComplete` | PASS/FAIL |
| Focus perso | Clic su un'altra finestra **prima** dell'ultimo carattere; digitare altrove | Nessuna scrittura/sostituzione nella finestra estranea | PASS/FAIL |
| Recupero focus | Tornare nel laboratorio, digitare un nuovo trigger completo dall'inizio | Sostituzione solo se osservazione affidabile e focus confermato | PASS/FAIL |
| Editing intermedio | Digitare parte del trigger e spostare il caret con mouse/frecce | Nessuna cancellazione retroattiva del testo non verificato | PASS/FAIL |
| Unicode | Verificare il risultato della firma sintetica | Accenti, emoji, CJK, arabo e newline integri | PASS/FAIL |
| Chiusura | Chiudere la finestra senza digitare nulla, in un secondo tentativo senza `--report` | Esito `NON VERIFICATA`, exit code non zero | PASS/FAIL |

La demo non usa registrazione globale. Non provare campi password o editor di terzi come se fossero supportati: il laboratorio non li modifica.

## Diagnostica senza dati sensibili

Annotare Windows/build, impostazione della lingua/layout della tastiera, presenza di IME, DPI e contatori `Verified/Rejected/Uncertain` dal JSON. **Non** allegare il contenuto del campo, snippet personali o log dei tasti. La prova con IME reale resta fuori scope perché il laboratorio lo disassocia.

**Gate:** un risultato PASS della demo non autorizza l'espansione globale; il prossimo step resta una singola integrazione esterna scelta e qualificata.
