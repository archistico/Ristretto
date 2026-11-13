# ADR-0004 — Matching Unicode, NFC e confini delle parole

- **Stato:** PROPOSTO (studio v2.5; **non ratificato**)
- **Decisione richiesta:** politica di equivalenza sui trigger e regole di attivazione P0
- **Ambito:** `Core` e contratti di `Application`; non definisce backend OS

## Contesto

I sorgenti originali non garantiscono una semantica uniforme fra normalizzazione, grafemi, separatori e cancellazione. Il requisito del nuovo prodotto include accenti, emoji composte, CJK, RTL e IME.

## Proposta

1. Salvare trigger e output **esattamente come forniti**, validando stringhe Unicode ben formate; creare separatamente una **MatchKey NFC**, mai un dato da incollare o cancellare.
2. Confronto P0 `Sensitive` con chiavi NFC, `Ordinal`; modalità `InvariantInsensitive` opzionale con `OrdinalIgnoreCase` da specificare come confronto limitato (non full case folding). Non eliminare accenti, skin tone, VS16, ZWJ o diacritici.
3. `Anywhere`, `PrecedingBoundary` e `WholeWord` descrivono confini del trigger, non una garanzia di editabilità del target. `WholeWord` attende evidenza del bordo destro e non si abbina indiscriminatamente a `Immediate`.
4. `Immediate` e `AfterDelimiterKeep/Drop` sono politiche **indipendenti** dal confine Unicode; i delimitatori ammessi in P0 sono testo committato esplicitamente configurato (spazio, punteggiatura); Enter/Tab sono riservati a capability native specifiche.
5. Trigger con stesso scope, stessa modalità e stessa MatchKey efficace generano **conflitto visibile**. Non si sceglie casualmente, non si espande automaticamente finché la collisione è irrisolta. Overlap `;a`/`;ab`: non anticipare `;a` se configurato per attendere delimitatore, né promuovere implicitamente un match già azionato.
6. `MatchResult` conserva `RawMatchedText` (non normalizzato), evidenze di segmentazione e intervallo eventi committati; **nessun valore di questo modello implica numero universale di Backspace**. Il provider nativo valuta la rimozione realmente supportata.
7. Durante IME non confermato, cambio focus, caret sconosciuto o evento incompleto: non espandere, esplicitare stato e fallback manuale.

## Decisioni rinviate / alternative

- `Exact` predefinito vs `NFC` predefinito: si propone **NFC**, ma richiede approvazione, perché v2.4 lasciava `Exact` come impostazione conservativa.
- Full Unicode case folding con trasformazioni di lunghezza, locale Turkish/Greek e `PropagateCase`: **post-MVP** salvo necessità; richiedere algoritmo e Unicode version pin.
- Implementazione completa UAX #29 vs profilo ridotto: non usare `Regex(\b)` come sostituto implicito; in P0 priorità a trigger espliciti e delimitatori; conformance UAX #29 dove implementata.
- Criterio deterministico per scope più specifico e per i trigger sovrapposti: decisione UX separata, niente precedenze invisibili.

## Conseguenze

**Vantaggi:** equivalenza canonica e output fedele, collisioni diagnosticabili, niente assunzioni errate su IME e Backspace, Core testabile. **Costi:** chiavi/indici separati, conformance Unicode, adattamento per lingua, GUI che spieghi conflitti. **No assunzioni:** NFC non risolve la compatibilità di inserimento OS o la resa grafica dei font.

Riferimenti: [specifica trigger](../TRIGGER_ENGINE.md), [Unicode IME](../UNICODE_IME.md), [test](../MATCHER_TEST_PLAN.md), [audit sorgenti](../../research/audit/FOUR_SOURCE_UNICODE_MATCHER_AUDIT_v2.5.md).
