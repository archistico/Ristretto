# Karabiner-Elements — scheda di ricerca

**Categoria:** Riferimento architettura input, non concorrente diretto. **Piattaforma osservata:** macOS. **Licenza/modello:** Open source (licenza repo da riesaminare).  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-07.

## 1. Funzionalità documentate

Remapping fisico/virtual keyboard, profili e rules, gestione layout, EventViewer.

## 2. Punto di forza

Esempio concreto di acquisizione e reinvio di eventi tastiera a basso livello, profili per contesto.

## 3. Limiti / compromessi

Non nasce come text expander; complessità e permessi della virtual keyboard non sono proporzionati all’MVP.

## 4. Meccanismi da studiare / vantaggio per noi

Osservare separazione input/rewrite/output e strumento diagnostico, evitando un motore key-remapper generale.

## 5. Cosa testare più avanti

Studiare incompatibilità Secure Input/IME con fonti ufficiali e test macOS.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://karabiner-elements.pqrs.org/
- https://github.com/pqrs-org/Karabiner-Elements

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Unicode input via clipboard o Unicode Hex Input; EventViewer per analisi eventi nativi. Non è equivalenza a un expander.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://karabiner-elements.pqrs.org/docs/getting-started/features/
