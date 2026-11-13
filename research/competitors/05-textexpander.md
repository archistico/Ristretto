# TextExpander — scheda di ricerca

**Categoria:** Prodotto snippet e collaborazione orientato al team. **Piattaforma osservata:** Windows, macOS, Chrome/iOS secondo edizione. **Licenza/modello:** Proprietario.  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-05.

## 1. Funzionalità documentate

Snippet, Fill-ins, librerie/team, sincronizzazione, funzioni contestuali, form e conditional sections in release recenti.

## 2. Punto di forza

UX orientata alla creazione e condivisione; form ricchi, struttura dati adatta a template di team.

## 3. Limiti / compromessi

Dipendenza da ecosistema/account e governance del cloud non coerenti con core local-first; differenze fra piattaforme.

## 4. Meccanismi da studiare / vantaggio per noi

Editor con form semplici; collaborazione solo opzionale in futuro e offline-first per default.

## 5. Cosa testare più avanti

Testare form, gestione conflitti sync e unicode trigger/output su piattaforme diverse.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://textexpander.com/
- https://textexpander.com/release/mac

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Conflitti visibili con indicatori colore, azioni di correzione e scope applicativi per gruppo; `Inline Search` vicino al cursore. Trasferire il modello di prevenzione dei conflitti.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://textexpander.com/learn/using/snippets/conflicting-snippet-abbreviations
