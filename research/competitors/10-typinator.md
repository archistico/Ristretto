# Typinator — scheda di ricerca

**Categoria:** Text expander orientato a velocità e autocorrezione. **Piattaforma osservata:** macOS; Windows da verificare per maturità. **Licenza/modello:** Proprietario.  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-10.

## 1. Funzionalità documentate

Snippet, autocorrect, ricerca, espansione dinamica, emoji, data; preset secondo versione.

## 2. Punto di forza

Interfaccia contenuta e minimalista; esempio di autocorrezione e scorciatoie.

## 3. Limiti / compromessi

Parità multipiattaforma non dimostrata; il nostro scope richiede Linux e GUI crossplatform.

## 4. Meccanismi da studiare / vantaggio per noi

Autocomplete/autocorrezione opt-in dopo il motore; semplicità visiva del picker.

## 5. Cosa testare più avanti

Acquisire GUI/documentazione specifica per versione e confronto sulle prestazioni con test.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://ergonis.com/en/typinator/

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Quick Search, autocorrezione multilingue, variabili e cursore, ma interni del matcher non documentati: non attribuire algoritmi o latenza misurata.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://ergonis.com/en/typinator/features/
