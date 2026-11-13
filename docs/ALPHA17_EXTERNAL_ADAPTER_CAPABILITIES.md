# Alpha 17 — contratto delle capacità degli editor Windows esterni

**Versione:** `3.9.0-alpha.17` (13 novembre 2026). **Non validata con build/xUnit/Windows in questo ambiente.** GUI Preview 9, hotkey del picker, SQLite, motore F1 e laboratorio di espansione posseduto invariati.

## Obiettivo

Non confondere le *possibilità di osservazione* di un controllo Windows con il diritto e la capacità di modificarne il contenuto. Questa milestone **non implementa un adapter di sostituzione esterna**: consolida le evidenze di Alpha 10/11 e rende verificabili i requisiti che un futuro adapter dovrà soddisfare.

## Codice introdotto

- `WindowsExternalAdapterCapabilityPolicy` (Infrastructure): composizione *pura*, deterministica e senza I/O dei risultati delle sonde precedenti; distinzione dei diversi esiti del preflight, focus, timeout e range Win32 non rappresentabili.
- `ExternalAdapterObservedEvidence`: soltanto due categorie osservabili: metadati di un controllo `Edit` candidato e categoria della selezione (caret/range). Nessun testo, offset, titolo, PID o HWND è contenuto nel risultato.
- `ExternalAdapterMissingProof`: otto garanzie **tutte ancora mancanti** in Alpha 17. Nessuna delle evidenze precedenti le soddisfa.
- `ExternalAdapterCapabilityCommand` nel laboratorio separato: comando one-shot `--assess-external-adapter`, opzionalmente `--report <nuovo.json>`. Usa la sonda esistente che verifica foreground/focus prima e dopo `EM_GETSEL` con timeout 150 ms. Scrive solo classificazioni e garanzie mancanti; il report non sovrascrive un file preesistente.
- `WindowsExternalAdapterCapabilityTests`: nuovi test puri per fallimento conservativo, mismatch di focus, timeout, valori non rappresentabili, campi protetti, classi sconosciute e obblighi non dimostrati.

## Requisiti per un adapter futuro

| Garanzia | Perché è necessaria | Alpha 17 |
|---|---|---|
| Sorgente credibile di testo Unicode committato | eventi raw e `WM_CHAR` sintetico non sono prova sufficiente | Assente |
| Identità stabile del target e revisione del documento | il testo potrebbe cambiare tra verifica ed esecuzione | Assente |
| Focus e stato protetto verificati al punto di modifica | non scrivere in campo sbagliato, protetto, read-only | Assente |
| Stato IME e modificatori | evitare alterazioni durante composizione/shortcut | Assente |
| Range esatto sul confine di grapheme | evitare cancellazioni parziali di emoji/Unicode | Assente |
| Compare-and-replace atomico | il confronto non può precedere separatamente un invio `SendInput` | Assente |
| Esito osservato dopo la modifica | incertezza non equivale a successo; nessun retry automatico | Assente |
| Consenso esplicito, pausa, esclusioni | controllo utente e contesti esclusi | Assente |

Anche il segnale `SelectionObservedReadOnly` **non abilita** `ReplacementCapability.VerifiedConditionalReplace`. La `ExpansionAdmissionPolicy` continua a rifiutare `ExternalApplication` senza eccezioni. Il nuovo risultato non è un token di accesso e non va collegato all'esecutore.

## Esecuzione facoltativa su Windows

Estrarre il progetto in una **cartella nuova**. Verificare prima:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

In una sessione interattiva, lanciare:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab -- --assess-external-adapter
```

Entro cinque secondi dare il focus a un controllo Win32 `Edit` di un'altra applicazione. Il comando è **read-only**, non registra caratteri e non installa hook. Selezionare un campo semplice, non contenente dati sensibili; evitare finestre elevate o non affidabili. È normale che molti editor riportino `PreflightRejected` o `SelectionQueryUnavailable`.

Per salvare un rapporto privo di testo e identificativi:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab -- --assess-external-adapter --report .\alpha17-editor-probe.json
```

**Exit code:** `0` soltanto quando è stata osservata la categoria della selezione in un candidato per ulteriori studi; `3` per risultati non qualificabili; l'uscita `0` NON autorizza una sostituzione.

## Limiti e prossima milestone

`EM_GETSEL` non dimostra input committato, revisione stabile, atomicità o verifica dell'esito; anche il controllo del focus prima/dopo la query è solo un'istantanea. In Alpha 18 la scelta del primo editor target dovrà essere basata su un adapter specifico e un protocollo verificabile, **non** sulla mera classificazione della finestra. Prima del collegamento all'app servono build/xUnit e prove Windows delle Alpha 8–17, incluse la hotkey e la demo interattiva.
