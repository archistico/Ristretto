# Text Blaze — scheda di ricerca

**Categoria:** Snippet dinamici, form e formule. **Piattaforma osservata:** Browser/desktop secondo offerta. **Licenza/modello:** Proprietario.  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-09.

## 1. Funzionalità documentate

Form text/paragraph/menu/toggle/date, formule, condizioni, dynamic commands, template; integrazione web.

## 2. Punto di forza

Modelli compilabili molto espressivi, preview e formule per dati ripetuti.

## 3. Limiti / compromessi

Molti comandi e dipendenze di piano; clipboard/rich text di alcuni target porta compromessi.

## 4. Meccanismi da studiare / vantaggio per noi

Form P1 non distruttivi e variabili previste dalla UI, non richiedere scripting nella base.

## 5. Cosa testare più avanti

Testare comandi condizionali e preview su Unicode, clipboard e app esterne.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://blaze.today/docs/
- https://blaze.today/guides/forms/
- https://blaze.today/guides/formulas/

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Tre modalità di confine da FAQ: Word Start, Start+End, Anywhere; usare selettore GUI semplice e prefissi per evitar conflitti.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://blaze.today/faq/
