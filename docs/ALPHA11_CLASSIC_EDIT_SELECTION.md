# Alpha 11 — selezione/cursore di un Win32 Edit esterno (solo lettura)

**Stato:** codice e xUnit aggiunti; build .NET 10 e Windows nativo **NON ESEGUITI** in questa consegna. GUI Preview 9 invariata. Alpha.8–10 sono ancora da validare. Ultima evidenza Windows conservata: alpha.7, 180/180 xUnit e 21/21 test nativi (NON sono test di questa versione).

## Obiettivo della milestone

Rispondere a una domanda concreta: *un controllo Win32 `Edit` esterno, attualmente in focus, risponde a una richiesta limitata di informazioni sulla selezione?*

La sonda Alpha 10 accerta soltanto metadati del controllo. Alpha 11 aggiunge **un unico messaggio Win32 in sola lettura**, `EM_GETSEL`, inviato manualmente e soltanto quando la policy precedente classifica un controllo `Edit` come candidato di ricerca. `SendMessageTimeoutW` usa `SMTO_ABORTIFHUNG` e un timeout massimo di 150 ms; `wParam` e `lParam` sono entrambi zero, senza passare puntatori nel processo esterno. Se UIPI, un controllo non standard, un timeout o un problema di focus impediscono l'osservazione, il risultato è **inconcludente**.

Il risultato non è una coppia di offset. La risposta nativa viene immediatamente ridotta alle sole categorie **cursore collassato**, **intervallo selezionato**, **intervallo non rappresentabile**, **intervallo non valido**, oppure **risposta mancante**. Il valore `0xFFFFFFFF` è una sentinella per range non rappresentabili: non significa caret a 65.535. Il risultato non contiene HWND, PID, nome processo, testo, clipboard, titolo, classe grezza, offset o lunghezza della selezione.

## Comando volontario

Su **Windows in sessione desktop interattiva**, dalla radice `Ristretto/`:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --inspect-classic-edit
```

Quando il programma lo chiede, portare entro **cinque secondi** in foreground un'applicazione di prova con un campo Win32 `Edit` *non sensibile*; posizionare il caret o selezionare del testo sintetico. L'output mostra soltanto categoria del preflight e stato del tentativo. Non viene inviata alcuna sostituzione. Un campo di un browser, RichEdit, WinUI o un Notepad moderno potrebbe non essere un controllo `Edit` classico e sarà rifiutato; questo è previsto.

Report facoltativo **solo su nuovo file**, mai sovrascritto:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --inspect-classic-edit --report reports/alpha11-prova-locale.json
```

Il JSON contiene esito, stabilità del focus, successo della query e indicazioni di capability **tutte false** per la sostituzione esterna. Non registra contenuti o offset. La prova è **manuale** e non viene chiamata automaticamente da `validate.cmd` o `validate-native.cmd`.

## Matrice delle garanzie

| Capacità | Alpha 11 su editor esterno | Per l'espansione F3 |
|---|---|---|
| Classe `Edit`, foreground/processo/focus e stato stile | Preflight read-only (Alpha 10) | Ricontrollo al commit necessario |
| Disponibilità di un range di selezione | Query read-only con timeout, **non atomica** | Offset affidabili e binding al contenuto necessari |
| Testo Unicode **committato** | **Non acquisito** | Origine affidabile necessaria |
| Revisioni del documento, caret e range raw coerenti | **Non verificati** | Necessari |
| Stato IME e modificate combinazioni di tasti | **Non verificati** | Necessari |
| Sostituzione indivisibile e verifica esito | **Non disponibile** | Necessaria per scrittura sicura |
| Scrittura o lettura del testo di terze parti | **Sempre negata** | Nessuna autorizzazione conferita |

### Limiti e sicurezza

- La query è solo un **indizio tecnico**. Anche se foreground e focus coincidono prima/dopo, **non** è una transazione atomica né una prova contro ABA (cambi temporanei e ritorno allo stesso target).
- `EM_GETSEL` codifica soltanto due valori a 16 bit; non è adatto a stabilire il range completo in documenti lunghi o nei casi non rappresentabili.
- Leggere selezione/caret **non** dimostra input Unicode committato, contenuto, revisione, caret stabile o compatibilità col processo destinatario; non abilita `ExpansionAdmissionPolicy` in `ExternalApplication`.
- Non usare questa opzione su password, contenuti privati o applicazioni di terzi senza consenso. Esiti protetti o sconosciuti vengono rifiutati.
- Nessun hook globale, key logging, interrogazione del testo, chiamata `WM_GETTEXT`, output o clipboard; il codice è solo in Infrastructure e nel laboratorio separato. La GUI resta congelata.

## Verifiche e prossimo gate

Test puri: `WindowsClassicEditSelectionProbeTests` verifica decodifica del risultato packed, overflow, selezione inversa, timeout, cambio focus, preflight password/readonly e impossibilità di autorizzare output in qualsiasi stato. Per eseguire gli xUnit e compilare il laboratorio su Windows: `scripts\validate.cmd`; per la suite nativa **preesistente**: `scripts\validate-native.cmd`.

**Prossimo gate (Alpha 12):** scegliere un editor specifico che esponga un contratto verificabile per testo Unicode committato, revisione e sostituzione condizionale. Se non è possibile, niente espansione automatica in quell'applicazione; resta disponibile il picker/copia manuale. La presente milestone **non qualifica alcun editor esterno**.
