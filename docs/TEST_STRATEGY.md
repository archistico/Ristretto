# v2.8 — test storage SQLite

La scelta SQLite è APPROVATA; [piano DB](SQLITE_TEST_PLAN.md) e [matrice di scenari](../data/SQLITE_SCENARIOS_v2.8.csv) sono specifiche di test da implementare, **tutte NOT_RUN**. Coprire vincoli, revisioni, FK, migrazioni, WAL, backup sotto scrittura, restore, Unicode byte-equivalence, porta portable e carico 10k/50k. Nessun test di applicazione avviato con questa consegna.

---

# Nuovo piano output v2.6

[OUTPUT_TEST_PLAN.md](OUTPUT_TEST_PLAN.md) e [OUTPUT_SCENARIOS_v2.6.csv](../data/OUTPUT_SCENARIOS_v2.6.csv) aggiungono **108 scenari `NOT_RUN`** su preparazione/preflight/target/rimozione Unicode/output/clipboard/errori/UX/OS/interleavings. Fake adapter testano gli invarianti; soltanto prove con editor reali possono certificare output Unicode e clipboard. Non aggregare test progettati come test superati.

---

# Strategia qualità e test (NON eseguiti)

## Piramide di validazione

1. **Unit test puri**: trigger matcher, collision detection, parsing variabili, grapheme handling, plan generation, form validation, import validation.
2. **Integration**: SQLite, file atomici, import/backup, config/culture, error cases.
3. **Native integration**: hook, paste, clipboard, focus, sessione OS e app target; test manuali assistiti + harness dove possibile.
4. **GUI**: picker, key navigation, DPI, theme, RTL, screen reader, keyboard-only, resize, dialogs, input cancellation.
5. **Packaging/CI**: build pulita Windows/macOS/Linux, dependency audit, scansioni statica e license compliance.

## Qualità attesa / obiettivi ancora da misurare

- Gestione 10.000 snippet senza rallentamenti percepibili del picker; definire soglie misurate e macchine di riferimento prima di dichiarare prestazioni.
- Zero perdite di dati in injection cancellata **prima** della modifica al target; i casi *partial insert* devono essere registrati senza mentire su rollback garantito.
- Cross-platform Unicode matrix a esiti distinti per testo memorizzato, riconosciuto, inserito e cursor.
- I18n: chiavi mancanti, placeholders coerenti, UI layout italiano/tedesco con stringhe lunghe, RTL, locale di payload indipendente.
- Riservatezza: test che il logger rediga contenuto snippet, clipboard, input utente.

## Test gates e artefatti

| Gate | Deliverable | Dimostrazione |
|---|---|---|
| Q0 | Core matcher e parser | Unit fixtures Unicode, collisioni, fuzz/property based dove utile |
| Q1 | Storage | Atomicità simulando crash/disco pieno/schema non valido |
| Q2 | Adapter Windows | Report app/OS/backend e test perdite di focus/clipboard |
| Q3 | Adapter macOS | Permessi, Secure Input, tastiere non US, macOS app matrix |
| Q4 | Linux X11 | X layout, device, clipboard manager e remote desktop |
| Q5 | Linux Wayland | Report per compositor; unsupported espliciti; permessi e fallbacks |
| Q6 | GUI/i18n | Flussi keyboard-only, tema/lingua live, RTL, contrasto, DPI |
| Q7 | Package | Install/update/uninstall, dati preservati, checksums, CI |

**Test proposti storici:** 117 scenari pipeline/Unicode raccolti nel [CSV storico](../data/HISTORICAL_TEST_SCENARIOS.csv), tutti `NOT_RUN`; deduplicare rispetto a TRG, NAT, DAT e ARCH.

**Nuovo backlog esperimenti**: `research/VALIDATION_BACKLOG.md`.

## Nuovo pacchetto di scenari v1.6 (NON eseguiti)

- **79** scenari `TRG-001…TRG-079` nel [CSV canonico](../data/TRIGGER_SCENARIOS.csv), con tipo unit/native/E2E e output atteso. Vedere [piano trigger](../research/TRIGGER_TEST_DESIGN.md).
- Distinguere ogni test in `MatcherPass`, `PreflightPass`, `OutputPass`, `DeletionPass`, `CaretPass`, `ClipboardPass`: non utilizzare un PASS globale quando sono state provate solo parti della pipeline.
- Scenario più rischioso: target modificato o non editabile dopo match. Deve fermarsi **prima** della prima azione irreversibile quando rilevabile, senza promettere transazioni cross-process.
- Le prove native devono registrare OS, compositor, keyboard layout, IME, app/versione e provider; non confondere gli scenari progettati con test implementati.


## Nuova matrice prove native v1.7

- [Piano riproducibile per OS](../research/NATIVE_SPIKE_PLAN.md).
- [72 scenari NAT proposti](../data/NATIVE_SPIKE_MATRIX.csv) (12 per ciascuno di Windows, macOS, X11, GNOME Wayland, KDE Wayland, wlroots Wayland): **tutti `NOT_RUN`**.
- Nessuna finzione di test OS tramite mock: mock solo per matcher/coordinatore; vere prove con permessi e software reale obbligatorie per capability dichiarate.
- Registrarne l'esito per piattaforma/compositor/editor/versione/locale/IME/focus/clipboard e mantenere test regressivi deterministici.

## v1.8 — Test di persistenza e migrazione

Aggiunti scenari DAT (vedere [matrice](../data/PERSISTENCE_AUDIT_SCENARIOS.csv)), tutti `NOT_RUN`. Classi: schema legacy, validità dei gruppi, import CSV e YAML, Unicode, snapshot/restore, file system failure, size limits e dati sensibili. I fixture in [research/fixtures](../research/fixtures/README.md) sono sintetici, non test di compatibilità. Gate per importatore: zero side-effect su preview/abort; transazione completa; niente esecuzione script; report di conversione.

## Aggiornamento v1.9 — audit build/lifecycle/impostazioni

Nuova matrice [ARCH](../data/ARCHITECTURE_AUDIT_SCENARIOS.csv): **45 scenari progettati**, tutti `NOT_RUN`, comprendono single instance, tray, pause, import preferenze atomicità e mapping, i18n runtime, packaging e privacy. I vecchi casi pipeline/Unicode (in totale **117** righe conservate) sono ora in [matrice storica unificata](../data/HISTORICAL_TEST_SCENARIOS.csv), sempre `NOT_RUN`, non sommare senza deduplicazione a TRG/NAT/DAT.

## Scenari aggiunti v2.1

Aggiunti scenari `AS-001...` in `data/LOCAL_SOURCE_AUDIT_SCENARIOS_v2.1.csv`: comportamento degli originali e requisiti di test per il porting. La baseline statica dei file ZIP non significa test runtime. Separare `STATIC_OBSERVED` (constatazione da file) da `NOT_RUN` (esperimento mancante) e `PASS` soltanto dopo esecuzione reale con log. Rimane fondamentale distinguere *evento gestito* da *testo correttamente inserito*.

## v2.2 — Scenari submodule

Sono stati aggiunti [36 scenari](../data/SUBMODULE_AUDIT_SCENARIOS_v2.2.csv) con `status=NOT_RUN`, comprendendo XMiLib Windows input, atom/hotkey, clipboard busy, single-instance e catalogo emojilib. La verifica Python della struttura JSON e dei quattro ZIP è **validazione statica**, non un test delle applicazioni o della GUI. Attestazione commit submodule e build Qt restano mancanti. Ricordare che un `SendInput` riuscito **non dimostra** l'output corretto nel destinatario.

## v2.3 — architettura e prova di confine

L'elenco canonico dei **55 nuovi scenari**, tutti `NOT_RUN`, è in [ARCHITECTURE_TEST_PLAN.md](ARCHITECTURE_TEST_PLAN.md). Aggiunto controllo statico del grafo dipendenze della futura solution. I test puri del matcher/planner non certificano input nativo o compatibilità dei campi testo. Prima di dichiarare supporto servono prove per OS, compositor, versione, app e IME.

## v2.4 — contratto di dominio

Aggiunti **90 scenari `DM-001..DM-090`**, tutti `NOT_RUN`, in [DOMINIO test CSV](../data/DOMAIN_TEST_SCENARIOS.csv). Pianificazione [qui](DOMAIN_TEST_PLAN.md). Non sommare questi casi alle vecchie matrici come test univoci finché non viene deduplicata la copertura: le nuove righe tracciano invarianti e casi boundary, anche quando uno scenario sovrappone una precedente suite. Le prove native sono ancora indispensabili; i test puri non dimostrano che un editor terzo abbia ricevuto il testo Unicode.

## Gate v2.5 — regressioni del matcher

Per il matching servono test puri separati da test OS: equivalenza NFC, grapheme/word boundary, conflitti, ordine indipendente, input IME committato, delimitatore Keep/Drop e buffer volatile; poi E2E solo sui backend realmente accessibili. [Matrice](../data/MATCHER_SCENARIOS_v2.5.csv) · [Piano](MATCHER_TEST_PLAN.md). **Stato iniziale di tutte le prove: `NOT_RUN`.**

## Addendum v2.7 — Benchmark riproducibili e leak test

[Piano performance](PERFORMANCE_TEST_PLAN.md), [budget proposti](../data/PERFORMANCE_BUDGETS_v2.7.csv), [scenari NOT_RUN](../data/PERFORMANCE_SCENARIOS_v2.7.csv). Distinguere benchmark del matcher puro, hook OS, picker UI, tempi di inserimento, startup, tray idle e soak (24h). Non usare risultati di media al posto di P95/P99. Priorità correttezza/fail-closed: se l'ingresso perde eventi, non si produce un'espansione. Gli obiettivi non sono misurati né approvati.

## Nuovi test predisposti v3.1

Aggiunti `GroupManagementTests.cs` e `PortableLibraryTests.cs` per vincoli gruppi, collisioni dei nomi, Unicode, formato, integrità e divieto di overwrite; tutti **non eseguiti** in assenza di SDK. [Guida alle prove](BUILD_AND_TEST.md).
