# ADR-0005 — Preflight, output non transazionale e clipboard condizionale

- **Data:** 2026-11-13
- **Stato:** PROPOSTO; non formalmente ratificato
- **Ambito:** coordinatore delle espansioni, bridge clipboard, contratto adapter OS

**Aggiornamento alpha.5:** nell'ambito delle successive richieste di sviluppo è stata implementata una [tranche managed con target simulato](../F2B_CONDITIONAL_REPLACEMENT.md): planner, tentativo unico ed esiti verificati in memoria. Questo non ratifica la proposta per output OS o clipboard e non autorizza ad attribuire capacità atomiche a un editor esterno.

**Aggiornamento alpha.7:** [proof-of-concept nativo in EDIT cooperante](../F2B_WINDOWS_OWNED_EDIT.md) verificato con 21 scenari. La garanzia deriva da controllo e thread UI posseduti, non da una transazione OS universale. La qualifica di editor esterni, IME, clipboard e altre piattaforme resta aperta.

## Contesto

Beeftext usa `keyword_.size()` Backspace e ripristino/clear clipboard dopo 1.000 ms. Espanso seleziona backend e implementa undo Backspace, ma gli esecutori di iniezione usano un `bool` per segnalare evento trattato, non un livello di verifica del target. Il sistema operativo può accettare input sintetico mentre l'editor non lo incorpora. Non esiste transazione generale di modifica dell'editor esterno.

## Decisione proposta

1. `Prepare` completo e preflight sul target prima di qualsiasi effetto.
2. Output seriale con punti di failure tipizzati; registrare in modo effimero `LastReachedStage` e `MayHaveModifiedTarget`.
3. `ITextOutput` è un port applicativo; `IClipboardBridge` viene introdotto **solo** quando un backend clipboard lo richiede. Non creare infrastrutture di orchestrazione/event bus generiche.
4. Cancellazione automatica consentita solo su target e metodi qualificati; niente count Backspace da `string.Length`.
5. `SubmittedUnverified`, `PartialOrUnknownAfterEffects`, `VerifiedComplete` sono esiti distinti; nessun retry/undo automatico se il target può essere stato alterato.
6. Ripristino clipboard *best effort* solo quando la nostra proprietà provvisoria è verificabile; non sovrascrivere una nuova copia dell'utente, non svuotare la clipboard per timeout.
7. Picker/copia manuale come fallback quando capacità target sono ignote o insufficienti.

## Alternative considerate

- **Replica Beeftext:** semplice ma timout rigido e cancellazione non sicura con caratteri composti — scartata.
- **Replica Espanso con undo immediato:** UX interessante, ma semantica `.chars().count()` insufficiente per tutti gli editor e processi — valutazione post-MVP.
- **Solo clipboard, su ogni OS:** migliora Unicode lungo, ma altera appunti e non risolve rimozione/focus — scartata come universal rule.
- **Solo simulazione tasti:** evita clipboard, ma performance e supporto Unicode/IME/secure input non uniformi — scartata come universal rule.
- **Target `replaceRange` universale:** non disponibile in tutte le app, può richiedere permessi invasivi — usare solo dove dimostrato.

## Conseguenze

- Più stati di errore da gestire, ma niente nuova assembly, mediator o servizi singleton.
- UX meno “magica” nei campi non verificabili: preferibile alla perdita di testo.
- Necessità di spike OS/editor e dati sperimentali; tempi non fissati per deduzione.

## Condizioni di ratifica

Test architetturali e proof-of-concept OS almeno su Windows; valutazione permessi macOS, Linux X11 e Wayland; criteri UX per `SubmittedUnverified`; consenso sul comportamento predefinito della clipboard. Il vincolo originario di sola fase di studio riguardava la proposta v2.6. La tranche simulata alpha.5 è distinta dalla ratifica dell'output nativo e della policy clipboard, ancora aperta.

[Riferimento tecnico](../OUTPUT_COORDINATION.md) · [Test](../OUTPUT_TEST_PLAN.md) · [Audit](../../research/audit/FOUR_SOURCE_OUTPUT_AUDIT_v2.6.md).
