# Alpha 10 — ricognizione Win32 di un editor esterno (sola lettura)

**Stato:** implementata nei sorgenti; build e nuovi xUnit **NOT_RUN** in questa consegna. Nessun adapter di sostituzione per applicazioni esterne è stato qualificato. Baseline Windows storica: alpha.7, 180 xUnit e 21 scenari nativi superati. Alpha.8/9 rimangono da validare.

## Perché questa fase

Il laboratorio della alpha.9 ha permesso una prova cooperante in un **secondo processo** che possiede un proprio `EDIT`. Questo non significa che gli editor di terze parti espongano le stesse garanzie di contenuto, cursore, revisione o commit. L'obiettivo qui è identificare **solo metadati del controllo attivo**, in modo esplicito e transitorio, per progettare e scegliere il primo adapter nativo da studiare. La GUI Preview 9 rimane identica.

## Comando manuale, non automatico

Aprire un terminale e, dalla radice `Ristretto/`, lanciare:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --probe-external
```

Entro cinque secondi portare in primo piano un'applicazione esterna con un editor aperto (per esempio un campo di testo di un programma di prova, senza contenuti sensibili). Il programma osserva **una sola volta** il focus del thread in primo piano. Non installa hook, non riceve tasti e non modifica l'editor. Può essere eseguito più volte per osservare singoli tipi di controllo, avviandolo volontariamente ogni volta. Se focus o metadati sono assenti, il risultato è inconcludente.

Rapporto facoltativo, solo verso un **nuovo** nome di file, mai sovrascritto:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --probe-external --report reports/alpha10-probe-personale.json
```

Il rapporto registra famiglia del controllo (`ClassicEdit`, `RichEdit`, `Other`, `Unknown`), classificazione, data e limiti. **Non** include testo, nome del processo, PID, HWND, titolo, classe grezza, selezione, clipboard o tasti. È possibile evitarne del tutto la creazione omettendo `--report`.

## Contratto verificabile

Codice puro: `WindowsExternalEditorProbePolicy`; snapshot minimale `ExternalEditorProbeObservation`; valutazione `ExternalEditorProbeAssessment`. Sonda nativa esplicita: `WindowsExternalEditorProbeReader` in Infrastructure. Comando separato: `ExternalEditorProbeCommand` nel laboratorio, **non** nell'applicazione desktop.

1. Il thread del foreground viene individuato mediante Win32 e il focus è letto da `GetGUIThreadInfo(threadId, …)`; non si presume che il focus del terminale del laboratorio coincida con quello dell'editor.
2. Devono coincidere processo del controllo e processo del foreground, e il focus deve appartenere alla stessa finestra radice. Risultati non confermati falliscono chiusi.
3. Il codice legge soltanto metadati della finestra: visibilità, abilitazione, famiglia della classe del controllo e bit di stile `ES_PASSWORD`/`ES_READONLY` **solo** per la classe esatta `Edit`.
4. Password, sola lettura, assenza di focus e dati sconosciuti non possono essere candidati. RichEdit, browser e altri controlli non vengono qualificati sulla base dell'apparenza.
5. Persino `ClassicEditCandidateNotQualified` significa **soltanto ricerca futura**. `CanReplaceText=false`, `CanReadCommittedText=false` e `ReplacementCapability.Unsupported` per ogni esito.
6. Il gate `ExpansionAdmissionPolicy` continua a rifiutare `ExternalApplication`: non è stato aggiunto nessun ponte di output.

### Limiti noti

- È uno snapshot non atomico: durante la raccolta può cambiare il foreground. Non deve essere riutilizzato come token di autorizzazione.
- Un controllo `Edit` potrebbe contenere dati privati, essere gestito in modo personalizzato, avere un IME in composizione o appartenere a un processo con privilegi diversi. La sonda **non** prova queste capacità né legge il testo per verificarle.
- Un browser o un editor moderno può usare UI Automation, text services o controlli compositi; un HWND non identifica necessariamente il campo testuale concreto.
- Il test non certifica input committato Unicode, caret, revisione, possibilità di compare-and-replace, privilegi, protezione contro eventi sintetici o compatibilità con altre tecnologie.
- Non eseguire il probe su finestre di password, contesti sensibili o elevati. La procedura è volontaria e isolata, ma il report non implica una certificazione di sicurezza dell'applicazione osservata.

## Test e gate

I nuovi `WindowsExternalEditorProbeTests` sono test **puri**, eseguibili insieme alla suite xUnit su qualsiasi OS: verificano classe di controllo, focus, processo/radice, stati protetti e che **ogni** esito vieti lettura dei commit e sostituzione. Non aprono finestre e non fanno chiamate Win32.

Su Windows, quando disponibile:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

Il secondo script verifica solo le prove nativo/cooperanti preesistenti, **non** invoca automaticamente la sonda esterna perché richiede interazione e scelta dell'operatore. L'esperimento è da eseguire a parte e non deve alterare il conteggio degli scenari nativi storici.

## Prossima fase

Selezionare un'applicazione di prova esterna *specifica* e creare una matrice di capacità: sorgente di testo Unicode committato, identità/range/caret, rilettura snapshot con revisione, confronto e sostituzione indivisibili, protezione IME/readonly/password e test in caso di cambio focus. Solo dopo un vero adapter capace di soddisfare quel contratto si potrà prendere in considerazione un opt-in di espansione. Se non soddisfatto, solo picker/copia manuale.
