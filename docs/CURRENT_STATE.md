# Stato corrente — Ristretto 3.9.0-alpha.20.20

## Dove siamo

- **R1 — Fondamenta:** C#/.NET 10, Avalonia, SQLite, portabilità JSON, strumenti `Ristretto.DevTools` interamente in C# e gate SOURCE/CORE/CI. L'utente ha confermato che **Alpha 20.19.5 compila e supera tutti i test**.
- **R2 — Picker Windows:** ricerca Unicode, hotkey configurabile, Copia e Inserisci/Invio. La prova del documento sintetico Notepad è stata completata con `PASS` e inserimento confermato dall'utente; restano altri editor e casi di errore.
- **R3 — VS Code:** integrazione facoltativa, in pausa. Non è necessaria per il motore generale di Ristretto.
- **R5 — Input automatico:** l'Alpha 20.19 aveva aggiunto il reset su cambio finestra/focus. La **Alpha 20.20** introduce un coordinatore applicativo `OwnedCommittedInputBridge`, riutilizzato dal laboratorio Win32: verifica delta Unicode, provenienza legata alla revisione e attivazione del trigger in successione. Non aggiunge monitoraggio globale o privilegi sui controlli esterni.
- **R6 — Espansione automatica:** non ancora disponibile nelle applicazioni esterne; l'esecuzione è consentita solo nel proprio editor Win32 qualificato.

## Stato delle verifiche

| Versione | Evidenza | Stato |
|---|---|---|
| **20.19.5** | Compilazione e test confermati dall'utente | **PASS** |
| **20.20** | Nuovo bridge e nuovi test xUnit preparati, laboratorio integrato | **Da compilare/eseguire su Windows** |
| Picker Notepad sintetico | Documento salvato Unicode corretto e azione Inserisci confermata | **PASS** per questa specifica prova |
| Espansione globale in editor esterni | Non implementata | **NOT_RUN** |

I controlli statici dell'archivio non equivalgono alla compilazione del codice C#. Non conservare dati digitati nei rapporti diagnostici.

## Obiettivo finale

Digitare un trigger Unicode come `;firma` in un'applicazione compatibile e ottenere automaticamente lo snippet, senza aprire il picker. Il picker deve restare disponibile come fallback manuale.

## Cosa resta da fare

1. Eseguire `scripts\validate-source.cmd` e `scripts\validate.cmd` sulla 20.20. Confermare i nuovi test del bridge e l'assenza di regressioni nel laboratorio.
2. Eseguire `scripts\validate-native.cmd` da un desktop Windows interattivo per verificare che il laboratorio continui ad espandere correttamente i propri trigger.
3. R5: progettare un adapter esterno che possa **dimostrare** la provenienza e il testo Unicode committato, distinguendo IME, incollamenti, focus e campi protetti.
4. R6: introdurre l'espansione globale in forma opt-in solo dopo una qualificazione del destinatario e test di sicurezza. Non usare eventi di tastiera grezzi come prova del testo inserito.

Vedi [ROADMAP](ROADMAP.md), [BUILD_AND_TEST](BUILD_AND_TEST.md) e il [rapporto tecnico 20.20](ALPHA20_20_OWNED_INPUT_BRIDGE.md).
