# Dati e scenari di riferimento

Le matrici conservano ID, aspettative e stati della fase di studio. Una riga `NOT_RUN` non è automaticamente coperta dai successivi test xUnit: servono collegamenti espliciti tra scenario e prova. Lo [stato corrente](../docs/CURRENT_STATE.md) e i [rapporti](../reports/README.md) descrivono le verifiche effettive.

Il [CSV dei contrasti Preview 9](UI_PREVIEW9_CONTRAST.csv) riguarda il tema corrente; la vecchia tabella di colori candidati è stata spostata nella cache di recupero. Il [benchmark Core](../docs/CORE_BENCHMARK.md) misura parte dei budget proposti, senza certificare l'intero piano performance.

## Scenari SQLite — v2.8 (matrice di studio)

[Matrice SQLite](SQLITE_SCENARIOS_v2.8.csv): **78** scenari progettati, tutti `NOT_RUN`. Aree: schema, transazioni, WAL, backup, migrazioni, Unicode, performance, sicurezza. Gli esiti previsti indicano **aspettative**, non prove eseguite. [Specifica](../docs/SQLITE_STORAGE.md) · [Piano](../docs/SQLITE_TEST_PLAN.md).

---

# Matrici v2.7

- [Scenari performance e lifecycle (88)](PERFORMANCE_SCENARIOS_v2.7.csv) — tutti `NOT_RUN`, non duplicare meccanicamente i test precedenti in fase di sviluppo.
- [12 budget candidati](PERFORMANCE_BUDGETS_v2.7.csv) — piano complessivo ancora da qualificare; prime misure del solo Core nel benchmark citato sopra. Protocollo: [piano](../docs/PERFORMANCE_TEST_PLAN.md).

---

# Dataset v2.6 — Output

[OUTPUT_SCENARIOS_v2.6.csv](OUTPUT_SCENARIOS_v2.6.csv): **108** nuovi scenari suddivisi in nove aree (PRE/TGT/DEL/OUT/CLP/ERR/UX/OS/INT), tutti `NOT_RUN`. La colonna `ExpectedOutcome` esprime la **policy desiderata**, non un comportamento osservato. UTF-8 BOM per Excel; referenze in [piano](../docs/OUTPUT_TEST_PLAN.md).

---

# Matrice e backlog tabellare

- `COMPETITOR_MATRIX_v1.5.csv`: 17 righe = 16 concorrenti/riferimenti + baseline Beeftext; colonne documentali, **non** prove. **D** documentato, **PAR** disponibile limitatamente/ambito ridotto, **NO** non disponibile per lo scenario indicato, **ND** non documentato sufficientemente. Alcuni `NO` si riferiscono alla piattaforma, non al prodotto come categoria.
- `FEATURE_BACKLOG_v1.5.csv`: backlog iniziale con priorità e provenienza; per pianificare lo sviluppo usare la [roadmap corrente](../docs/FUNCTIONAL_ROADMAP.md).
- Le etichette per piattaforma sono snapshot documentali, non affermazioni di supporto permanente; consultare sempre `docs/PLATFORMS.md` e fonti.

- `ARCHITECTURE_AUDIT_SCENARIOS.csv`: **45** nuovi scenari di progettazione startup/tray/preferences/i18n/licenses, tutti `NOT_RUN`.
- `HISTORICAL_TEST_SCENARIOS.csv`: **117** casi storici pipeline + Unicode, ID originali preservati, tutti `NOT_RUN`; sono potenzialmente sovrapposti a TRG/NAT/DAT.

- `DOMAIN_TEST_SCENARIOS.csv`: **90** scenari aggiunti in v2.4 (`DM-001..DM-090`), tutti `NOT_RUN`, su invarianti di dominio, Unicode, trigger, preflight, clipboard, import e transazioni. Le righe descrivono test **da implementare**, non prove svolte. Vedi [piano canonico](../docs/DOMAIN_TEST_PLAN.md).

## Dataset v2.5 — Matcher Unicode

[MATCHER_SCENARIOS_v2.5.csv](MATCHER_SCENARIOS_v2.5.csv) contiene 114 scenari documentali con identificatore, classe, trigger, sequenza input, policy, outcome previsto, priorità e `status=NOT_RUN`. Alcune aspettative `ReviewCulture`, `PendingOrTailored` e `RequiresNativeCapability` sono intenzionalmente **condizionate**: non rappresentano test con esito determinato. Il file è CSV UTF-8 con BOM per compatibilità con Excel, con campi quotati correttamente.
