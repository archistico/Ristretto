# Piano di test dei trigger — P0 e differiti

**Stato:** design test, nessun test eseguito. I casi con ID `TRG-*` sono nuovi e non si sommano automaticamente ai precedenti 63/54 scenari storici. Il CSV `data/TRIGGER_SCENARIOS.csv` contiene input/aspettativa dettagliata, classificazione e stato; gli ID rimangono stabili.

## Distinguere tre oracoli

1. **Matcher:** produce l'esatto insieme e ordine dei candidati per una sequenza logica. Verifica in RAM, senza OS.
2. **Preflight:** autorizza o rifiuta l'operazione dato il contesto, senza inserimento o cancellazione.
3. **E2E:** verifica che il target realmente visualizzi i dati corretti, che il delimitatore sia conservato/scartato come previsto, che il caret rimanga al posto atteso e che gli appunti non siano sovrascritti. Non dedurre il terzo dal primo.

## Suite di fixture (struttura suggerita)

```text
fixture_id; locale; unicode_source; unicode_expected; trigger_rule; context;
input_sequence; expected_match; expected_selection; expected_output;
expected_caret; expected_clipboard; expected_result; adapter; result
```

- Confrontare byte/normalizzazione del testo *dove appropriato* e output visuale; screenshot solo di dati sintetici.
- Fixture accento: `é` e `e\u0301` come forme distinte; trigger canonico solo se configurato.
- Fixture emoji: `👍🏽`, `👨‍👩‍👧‍👦`, `🇮🇹`, `❤️`, con test di cancellazione del trigger *se supportata*.
- Fixture IME: giapponese, cinese e coreano con `CompositionStarted/Updated/Committed/Cancelled` e input source change.
- Fixture latino multi-lingua: `İ/ı/I/i`, `Σ/σ/ς`, `ß/SS`; non usare `.ToLower()` ingenuamente.
- Fixture target: plain text editor, rich editor, browser input, browser contenteditable, VS Code, office suite, terminale, password, Secure Input, RDP/VM.

## Gates d'ingresso per ogni adapter

**G0**: matcher puro deterministico, no payload persistente, esiti su Unicode e conflitti.  
**G1**: adapter rileva chiaramente i limiti e non tenta cancellazione su target ignoto.  
**G2**: focus-change / clipboard race / alt+tab / IME non producono cancellazione distruttiva in prove negative.  
**G3**: per ogni metodo dichiarato `Supported` esistono evidenze con OS/app/ver/install e controprove.  
**G4**: picker manuale resta operabile come percorso di recupero dove la piattaforma lo consente.

## Registrazione risultati

Per ogni caso da eseguire: ID, commit applicazione, OS build, desktop/compositor e protocollo, layout keyboard/IME, app destinataria e versione, permessi, backend, fixture, expected/actual, esito `PASS|FAIL|BLOCKED|NOT_RUN`, log redatti senza testo digitato. La pubblicazione compatibilità usa solo **PASS riproducibili**, non materiale della concorrenza.

## Criticità da non occultare

- Il test con `Unicode storage OK` non certifica `IME recognition OK` né `caret position OK`.
- Le API d'accessibilità non sempre identificano i campi password; una policy fail-closed potrà restringere le capacità, mai garantire rilevazione assoluta.
- Non esiste un'iniezione universale su Wayland; test per compositor/sessione e permission model.
- Un fallimento dopo la prima cancellazione è `PartialFailure`, non rollback completato.
