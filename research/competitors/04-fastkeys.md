# FastKeys — scheda di ricerca

**Categoria:** Text expansion più automazione generale. **Piattaforma osservata:** Windows. **Licenza/modello:** Proprietario.  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-04.

## 1. Funzionalità documentate

Espansione testo, auto-complete, menu, form e automazione UI/keyboard, applicazioni e trigger contestuali.

## 2. Punto di forza

Buono come riferimento a autocomplete e attivazione per contesto, GUI funzionale per power user.

## 3. Limiti / compromessi

Scope molto più vasto del nostro; assenza Linux/macOS come target dichiarato del prodotto.

## 4. Meccanismi da studiare / vantaggio per noi

Autocomplete opt-in, menu picker e contesti specifici; NON copiare la suite macro.

## 5. Cosa testare più avanti

Distinguere modalità testo/automation e API impiegate per unicode.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://www.fastkeysautomation.com/
- https://www.fastkeysautomation.com/text-expander/

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Supporto documentato per immediate/ending char, keep/drop, inside-word, manual confirmation, raw text e clipboard. Ridurre in una UX essenziale con esempi.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://www.fastkeysautomation.com/documentation6/TextExpander.html
