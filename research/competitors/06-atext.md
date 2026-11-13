# aText — scheda di ricerca

**Categoria:** Text expander general purpose. **Piattaforma osservata:** Windows, macOS. **Licenza/modello:** Proprietario.  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-06.

## 1. Funzionalità documentate

Gruppi, snippet, rich text, placeholder, script, backup/sync tramite percorsi; impostazioni di clipboard e compatibilità.

## 2. Punto di forza

Molte capacità in GUI relativamente familiare; FAQ affronta conflitti clipboard e Secure Input macOS.

## 3. Limiti / compromessi

Scripting e rich text aumentano combinazioni da testare; no Linux nativo dichiarato.

## 4. Meccanismi da studiare / vantaggio per noi

Fallback manuale quando Secure Input blocca; impostazioni avanzate separate da UX principale.

## 5. Cosa testare più avanti

Verificare race di ripristino clipboard e VM/desktop remoto con test.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://www.trankynam.com/atext/
- https://www.trankynam.com/atext/faq.html

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** FAQ conferma opzioni gruppo, Secure Input e problemi clipboard su testi lunghi. Serve una diagnosi che distingua detection e injection.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://www.trankynam.com/atext/doc/
