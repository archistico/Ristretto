# Raycast Snippets — scheda di ricerca

**Categoria:** Launcher/snippet manager di nuova generazione. **Piattaforma osservata:** macOS; disponibilità multipiattaforma da verificare. **Licenza/modello:** Proprietario.  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-14.

## 1. Funzionalità documentate

Snippets con tag, search, pin, import JSON, dynamic placeholders; expander modes immediately/after delimiter keep/drop e app excluded.

## 2. Punto di forza

Molto chiaro nel distinguere trigger, nome, tag e modalità di espansione; importa da più tool e documenta limiti dei keyword Unicode.

## 3. Limiti / compromessi

Parte di suite più grande; integrazioni e shared snippets di team non sono necessari per MVP.

## 4. Meccanismi da studiare / vantaggio per noi

Adottare una policy chiarissima su delimitatori e confini, picker con tag, preview collisioni/import.

## 5. Cosa testare più avanti

Testare dettagli di word boundary e caratteri Unicode nel trigger; attenzione a conflitti con sostituzioni di sistema.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://manual.raycast.com/snippets
- https://manual.raycast.com/dynamic-placeholders

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Immediate/AfterDelimiterKeep/Drop, ExpandWithinWords, delimitatori non usabili nei keyword, override system snippets sperimentale; utile un controllo GUI esplicito.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://manual.raycast.com/snippets
