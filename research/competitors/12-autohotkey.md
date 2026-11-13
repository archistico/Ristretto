# AutoHotkey — scheda di ricerca

**Categoria:** Hotstrings programmabili e automazione Windows. **Piattaforma osservata:** Windows. **Licenza/modello:** Open source (controllare licenza repository).  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-12.

## 1. Funzionalità documentate

Hotstrings immediate o dopo endchar, case sensitive, word boundary, hotkeys, context-sensitive `#HotIf`, send modes.

## 2. Punto di forza

Riferimento molto preciso per semantica trigger, delimitatore, reset quando cambia finestra, testo letterale.

## 3. Limiti / compromessi

Richiede scripting per molte configurazioni e non è multipiattaforma; limiti di alcuni metodi di invio e trigger documentati.

## 4. Meccanismi da studiare / vantaggio per noi

Definire regole trigger come opzioni semplici GUI: immediate/delimiter, keep/drop delimiter, word boundary, scope.

## 5. Cosa testare più avanti

Validare end char, combinazioni Unicode, layout internazionali e editor con confronto semantico su VM Win.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://www.autohotkey.com/docs/v2/Hotstrings.htm
- https://doggy8088.github.io/AutoHotkeyDocs/docs/Hotstrings.htm

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Hotstrings: end chars, immediate, inside word, Backspace B0, reset di contesto e input sintetico non reinserito. Fonte dettagliata secondaria da ricontrollare su manuale primario.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://doggy8088.github.io/AutoHotkeyDocs/docs/Hotstrings.htm
