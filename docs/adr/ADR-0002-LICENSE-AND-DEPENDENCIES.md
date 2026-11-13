# ADR-0002 — Licenze e politica dipendenze

**Stato:** politica proposta, **licenza del nuovo software non ancora scelta**. Data: 2026-11-13.

## Decisioni per la fase di studio

- Progetto destinato ad essere open source; la licenza nuova (MIT, Apache-2.0, GPL, altro) richiede scelta esplicita prima dell'inizio del codice pubblico.
- **Beeftext, XMiLib, emojilib MIT:** studio consentito; se si incorpora codice/dataset effettivo, preservare attribuzioni e notices e verificare dipendenze del dataset.
- **Espanso GPLv3:** studiare documentazione e tecniche; **non trasferire sorgente, porzioni riscritte meccanicamente o librerie copyleft incompatibili** in un progetto destinato a licenza permissiva senza ulteriore analisi/licenza appropriata.
- Ogni nuova dipendenza: licenza, manutenzione, sicurezza, supporto Windows/macOS/Linux, transitive deps, aggiornamenti e SBOM.
- Avalonia `TreeDataGrid` risulta nella documentazione v12 come controllo Pro: niente dipendenza obbligatoria a pagamento per il progetto open source senza decisione consapevole. Valutare alternative community.
- Nessun runtime Node richiesto per il catalogo emoji, né Qt per le utility XMiLib; preferire servizi .NET semplici.

## Gate

Definire `LICENSE`, `NOTICE` ove necessario, policy contributi/CLA se utile, matrice licenze pacchetti, pipeline SBOM/SCA e test di packaging **prima delle release**. Nessun attestato licenze finale è stato prodotto in questa fase di studio.

**Fonti:** quattro ZIP dell'utente, [audit](../../research/audit/FOUR_SOURCE_ARCHITECTURE_AUDIT_v2.3.md), https://docs.avaloniaui.net/controls/data-display/structured-data/treedatagrid.
