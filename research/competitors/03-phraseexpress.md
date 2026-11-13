# PhraseExpress — scheda di ricerca

**Categoria:** Suite professionale espansione, template, team. **Piattaforma osservata:** Windows, macOS e piattaforme mobile dichiarate. **Licenza/modello:** Proprietario.  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-03.

## 1. Funzionalità documentate

Gerarchie, phrase menu, macro/form, versioni/cestino, app scope, output formattato; modalità di inserimento multiple incl. VK_PACKET e clipboard.

## 2. Punto di forza

Robusta amministrazione di grandi librerie, controllo ambiguities, documentazione diagnostica focus e keyboard-hook conflicts.

## 3. Limiti / compromessi

Complessità enterprise e differenze funzionali fra edizioni; output HTML/RTF dipende dal target che sceglie il formato.

## 4. Meccanismi da studiare / vantaggio per noi

Import sicuro, cestino, preview, collision policy, output mode per applicazione, log diagnostici redatti.

## 5. Cosa testare più avanti

Testare stesso testo in editor web/Office e condizioni focus/clipboard; non assumere parità Windows/macOS.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://www.phraseexpress.com/doc/
- https://www.phraseexpress.com/support/faq/

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Regole start/end/middle/whole word, prefissi/suffissi custom e menu per trigger duplicati. `Expert Options` permette differenziare metodo di paste e delay: ispirare chooser e diagnosi, non copiare complessità della UI.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://www.phraseexpress.com/doc/edit/trigger/
