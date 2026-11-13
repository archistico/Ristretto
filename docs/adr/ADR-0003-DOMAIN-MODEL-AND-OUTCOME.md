# ADR-0003 — Modello di dominio minimale e risultati di inserimento veritieri

**Stato:** PROPOSTO, **da ratificare** prima dello sviluppo. **Data:** 13 novembre 2026. **Contesto:** [architettura v2.3](../ARCHITECTURE.md) e [studio dominio v2.4](../DOMAIN_MODEL.md).

## Contesto

Beeftext combina modello, parsing, UI e input esterno in alcuni componenti C++; Espanso separa matcher e renderer ma usa un'architettura più estesa di quella necessaria al nostro prodotto. XMiLib mostra che un invio di eventi Win32 può fallire o essere incompleto; emojilib è dataset, non logica del motore. L'utente richiede Unicode/emoji/IME corretti, UI moderna e semplicità architetturale.

## Proposta

1. Modello `Snippet`, `Group`, `TriggerDefinition`, `MatchResult`, `ExpansionPlan` in Core puro; `InputSession`, `TargetContext` e `InsertionOutcome` ai confini applicativi; OS e clipboard esclusivamente negli adapter.
2. Template preservato come valore Unicode; modalità letterale P0; nessun conteggio di cancellazioni ricavato da unità UTF-16/grafemi.
3. Identificatori stabili e revisioni ottimistiche; snapshot matcher pubblicate solo dopo commit.
4. `Prepare` completo e `Preflight` prima del primo effetto; esiti `SubmittedUnverified`, `PartialFailure`, `UnknownAfterEffects` distinti dal successo verificato.
5. Nessun retry automatico una volta che l'editor esterno potrebbe essere stato modificato.

## Alternative scartate per questa fase

- `Combo` come entità che salva, notifica UI e scrive nel target: difficile da testare e non multipiattaforma.
- Modello a 15 progetti/microservizi: overhead non giustificato per un text expander locale.
- `bool Expand()` come unico risultato: nasconde output parziale e stato di verifica.
- `string.Length` o solo conteggio grafemi per decidere i Backspace: non equivalenti alle semantiche del target.
- Un database o uno schema YAML direttamente come modello runtime: accoppiamento forte alla fonte dati.

## Conseguenze

Pro: nucleo verificabile senza OS, gestione errori onesta, separazione per slice, facile introdurre adapter. Costi: qualche DTO/conversione di confine e necessità di prove native reali. Rischi aperti: disponibilità committed text/IME, policy trigger prefisso/delimitatore, supporto cursor anchor, storage finale.

**Gate:** concordare i punti aperti e creare test sui contratti. Solo allora si passa all'implementazione delle prime slice.
