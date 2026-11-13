# ADR-0001 — Confini Vertical Slice e adapters nativi

**Stato:** *proposto per approvazione*, consolidato a livello di studio. Data: 2026-11-13. Decisioni attive nel dossier ma modificabili dopo spike.

## Contesto

Beeftext è Windows C++/Qt con manager globali; Espanso separa matcher/render/backend ma usa numerosi crate Rust GPLv3. XMiLib contiene utility Win32/Qt; emojilib è un dataset emoji. Il nuovo programma deve essere C#/.NET Avalonia, open source, compatto e semplice, Unicode-aware su piattaforme diverse.

## Decisione proposta

- **Quattro assembly logici** iniziali: `Core`, `Application`, `Infrastructure`, `Desktop` e progetti di test; le vertical slice sono cartelle nei progetti Application/Desktop.
- **Core puro** per modelli/algoritmi; use-case verticali in Application; porta di infrastruttura al confine; Desktop come composition root e presentazione.
- **Backend nativi in Infrastructure/Platform/**; assembly OS separati solo se fattori reali di build/permessi lo impongono.
- **Preparazione prima degli effetti** e outcome parziali espliciti; no pretesa di rollback universale.
- **Baseline proposta .NET 10 LTS** e Avalonia versione da fissare dopo verifica package/license; no mediator obbligatorio, event bus e general repositories.

## Alternative scartate ora

1. Porting 1:1 dei singleton Beeftext: accoppiamento a Qt/Win32, difficile testability.
2. Clonare workspace Espanso: complessità, dipendenze GPL e target del prodotto differenti.
3. Clean Architecture a 8–12 layer + progetto per slice: boilerplate e navigazione complessa.
4. Motore dentro i ViewModel Avalonia: impedisce test/headless e portabilità del runtime.
5. Processo background separato in partenza: costo IPC/performance e packaging senza motivo dimostrato.

## Conseguenze e trade-off

- Leggibilità e test puri migliori, ma i contratti OS richiedono spike e saranno affinati.
- Una sola GUI multipiattaforma ma capacità automatiche diverse per sessione/compositor.
- Meno librerie e meno service code; coordinatore `ExpandText` rimane il componente critico da testare.

## Criteri di rivalutazione

Rivedere se: adapter platform richiede linkage nativo isolato, interfacce assumono capacità inesistenti, eventi input perdono ordering, plugin nativi portano privilegi non sicuri o processo separato dimostra benefici chiari e misurati.

**Fonti:** [audit diretto](../../research/audit/FOUR_SOURCE_ARCHITECTURE_AUDIT_v2.3.md) e [specifica](../ARCHITECTURE.md).
