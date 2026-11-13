# Keyboard Maestro — scheda di ricerca

**Categoria:** Automazione general purpose con trigger testuali. **Piattaforma osservata:** macOS. **Licenza/modello:** Proprietario.  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-08.

## 1. Funzionalità documentate

Text triggers, palette, macros, typed/paste insertion, clipboard managers e azioni complesse.

## 2. Punto di forza

Gestione esplicita di scelte trigger e metodi di inserimento; diagnostica di macro e variabili.

## 3. Limiti / compromessi

Troppa ampiezza e macro coding fuori dallo scopo; rischio sostituzione distruttiva in campi con comportamento speciale.

## 4. Meccanismi da studiare / vantaggio per noi

Rendere l’espansione una sequenza pianificata e fallibile; conflitti → risoluzione deterministica.

## 5. Cosa testare più avanti

Confrontare target focus e paste vs typing per editor e stringhe Unicode.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://www.keyboardmaestro.com/main/
- https://wiki.keyboardmaestro.com/manual/Text_Triggers

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Typed String: preferenza trigger più lungo, chooser a pari lunghezza, reset sequenza e avvertimento esplicito sulla cancellazione distruttiva fuori dai campi testuali.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://wiki.keyboardmaestro.com/trigger/Typed_String
