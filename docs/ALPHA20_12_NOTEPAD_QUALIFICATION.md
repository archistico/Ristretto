# Alpha 20.12 — R2.05: qualificazione dell'inserimento in Notepad

**Data:** 14 novembre 2026. **Tipo:** prova manuale file-backed, senza hook o automazione dell'editor. Non è una prova di espansione automatica.

## Obiettivo

Verificare che il testo selezionato dal picker, dopo un inserimento **richiesto dall'utente** e un salvataggio in Notepad, corrisponda a una fixture multilinea Unicode. È una prova più forte del solo conteggio di eventi `SendInput`, perché controlla il **file effettivamente salvato**. Non può dimostrare l'origine esatta della modifica (per esempio Inserisci anziché incolla manuale), e la riuscita è locale a questo ambiente, file ed editor.

## Preparazione sicura

1. Estrai la versione corrente in una **cartella nuova** e avvia Ristretto con un **profilo di prova** (`--data-dir`), non con la tua libreria personale. Compilazione/test tradizionali: `scripts\validate.cmd`.
2. Crea nel profilo sintetico uno snippet, per esempio con trigger `;ristretto-test`, il cui corpo sia **esattamente** il contenuto di `data/PICKER_INSERT_SYNTHETIC_FIXTURE.txt`. Non aggiungere spazi, tab o righe finali.
3. Da `Ristretto\` esegui `scripts\validate-picker-notepad.cmd prepare --open`. Richiede Python 3.10+; `--open` richiede Windows e apre **solo** il documento sintetico creato in `.local-cache/notepad-qualification/<session>/notepad-synthetic.txt`. Puoi omettere `--open` e aprire manualmente il percorso stampato.
4. In Notepad posiziona il cursore **alla fine del file con Ctrl+End**, senza digitare altri caratteri. Attiva il picker dalla hotkey, seleziona lo snippet di prova e premi **Inserisci**. Non utilizzare Copia o Ctrl+V manuale per questa prova.
5. Verifica visivamente che l'editor sia rimasto quello previsto, poi premi **Ctrl+S** per salvare nel file sintetico.
6. Esegui `scripts\validate-picker-notepad.cmd verify --session ID_STAMPATO`, usando il token di 32 caratteri creato al punto 3.

Non aprire o salvare altri documenti per questa prova; non usare la funzione Inserisci con un destinatario non previsto. Il tool **non** invia `Ctrl+V`, non legge clipboard, non chiude Notepad e non apre documenti personali.

## Esiti e limiti

| Stato | Evidenza |
| --- | --- |
| `PASS` (exit 0) | File salvato contenente esattamente il prefisso sintetico seguito dalla fixture Unicode, tollerando soltanto conversione LF/CRLF |
| `FAIL` (exit 1) | File cambiato, ma risultato differente (anche normalizzazioni di accenti NFC/NFD, emoji alterate, tab/spazi aggiunti, testo incompleto) |
| `NOT_RUN` (exit 3) | File identico alla preparazione, assente, sessione invalida o non verificabile; **non** equivale a successo |

Per ogni verifica viene creato **un nuovo** `receipt-<token>.json` nella cartella della sessione. Include solo stato, motivo, versione e identificativo del test; niente snippet, appunti, nome di applicazioni personali, PID o titolo della finestra. `.local-cache/` è esclusa dallo ZIP. Puoi ripetere la verifica dopo un salvataggio corretto senza sovrascrivere prove precedenti. La prova non ripristina gli appunti che Ristretto può aver modificato.

**Privacy e attendibilità:** anche un `PASS` è una misura del contenuto persistito, non una prova crittografica dell'origine dell'input. Il test non fornisce supporto automatico per Notepad, Word o browser e non qualifica il riconoscimento del trigger digitato.

## Regressioni e validazione

`python -m unittest discover -s scripts -p "test_*.py" -v` comprende i test del qualificatore; `scripts\validate-source.cmd` lo richiama. Compilazione .NET e test desktop Windows vanno eseguiti separatamente. L'esecuzione del gate SOURCE non apre Notepad e non altera la libreria personale.

**Prossimo passo:** eseguire su Windows R2-I01a e la matrice di errori R2-I02+ (focus errato, editor chiuso, clipboard concorrente, finestre elevate); confrontare gli esiti e correggere le incompatibilità osservate. Prima di dichiarare R2 chiusa serviranno anche accettazione dell'interfaccia, prestazioni e profili isolati.
