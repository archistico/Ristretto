# ADR-0006 — Resource budgets, snapshot e ciclo di vita

**Stato:** PROPOSTO, NON RATIFICATO · 2026-11-13 · SOLO STUDIO

## Contesto

La nuova applicazione resta in background, osserva eventi sensibili a latenza e deve gestire librerie grandi senza consumare risorse eccessive. I quattro sorgenti consultati dimostrano pattern diversi: hook e Qt model di Beeftext; matcher incrementale, canali interni e componenti di rendering/output di Espanso; helper native XMiLib; catalogo dati emojilib. Nessun benchmark comparativo è stato eseguito.

## Decisione proposta

1. Un solo processo per l'MVP, quattro assembly Core/Application/Infrastructure/Desktop; niente daemon extra.
2. Callback nativo minimale; ingress **bounded** con invalidazione della sessione al sovraccarico. Mai continuare a matchare dopo perdita d'ordine degli eventi.
3. Matcher serializzato per sessione, snapshot compilata fuori dal path caldo e pubblicazione atomica/versionata dopo commit valido della libreria.
4. Un solo tentativo di output per contesto, nessun retry silenzioso dopo effetti incerti.
5. Picker con indice/paginazione indipendenti dal matcher e query latest-only, dispatcher Avalonia soltanto per UI.
6. Avvio e terminazione espliciti con stati `Starting/Ready/Paused/Degraded/Suspended/Stopping/Stopped`, tray compatibile e cleanup risorse.
7. Budget PB-01..PB-12 come **target candidati** da validare e ricalibrare; la sicurezza prevale sui percentili.
8. Diagnostica locale aggregata senza tasti, clipboard o contenuto snippet.

## Alternative scartate per l'MVP

- Processo daemon separato, IPC o architettura microservizi: overhead, packaging e debug non giustificati.
- Repository generico/mediator obbligatorio/actor framework: eccesso di astrazioni per casi d'uso noti.
- Scansione completa della libreria nel callback native: variabilità e accoppiamento a storage/UI.
- Buffering illimitato o drop silenzioso di caratteri: rischio di false espansioni.
- Thread/task per evento: overhead, ordering e race evitabili.
- Caching illimitato dei corpi snippet: aumento RSS e possibile retention di contenuti sensibili.

## Conseguenze e limiti

Migliore prevedibilità e sicurezza, al costo di una politica di revisione degli indici e di casi `NeedsResync` quando la pipeline è sovraccarica. Le capacità native e il time-to-type delle app terze restano limitazioni del sistema operativo. Misurare e rivedere i budget dopo la prima prova, non promuoverli a garanzie pubbliche senza esecuzione.

## Condizioni per ratifica

- Approvazione delle regole di overflow/snapshot e della semantica `Paused/Degraded`.
- Baseline documentata per D10 e un editor esterno di prova su almeno una piattaforma, con percentili e metrica di correttezza.
- Verifica della policy di chiusura con tray assente e delle autorizzazioni OS.
- Coerenza con [ADR-0004](ADR-0004-UNICODE-MATCHING-AND-WORD-BOUNDARIES.md) e [ADR-0005](ADR-0005-OUTPUT-COMMIT-AND-CLIPBOARD.md).

[Riferimento canonico](../PERFORMANCE_RESOURCE_LIFECYCLE.md) · [Piano prove](../PERFORMANCE_TEST_PLAN.md).
