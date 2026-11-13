# Alfred — scheda di ricerca

**Categoria:** Launcher macOS con snippet e picker. **Piattaforma osservata:** macOS. **Licenza/modello:** Proprietario (Powerpack per snippet).  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-13.

## 1. Funzionalità documentate

Collezioni, snippets viewer, auto expansion, placeholder per data/time/clipboard/cursor, app exclusions e tuning delays.

## 2. Punto di forza

Ottima UX di ricerca e shortcut; indicazioni esplicite sul non ascoltare campi password e sulla scelta keyword sicure.

## 3. Limiti / compromessi

Solo macOS e funzionalità non disponibili in edizione base; placeholder ricorsivi limitati esplicitamente.

## 4. Meccanismi da studiare / vantaggio per noi

Picker keyboard-first, prefissi di raccolta, place-holder cursor, copy fallback, esclusioni e configurazione ritardi di compatibilità.

## 5. Cosa testare più avanti

Testare creazione da history, metadata, cursore e preservazione clipboard in app non-native.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://www.alfredapp.com/help/features/snippets/
- https://www.alfredapp.com/help/features/snippets/advanced/
- https://www.alfredapp.com/help/workflows/advanced/placeholders/

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Consigli su prefissi, esclusioni applicazione, slowing key events e interferenze con Google Docs/clipboard managers; priorità workflow documentata.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://www.alfredapp.com/help/troubleshooting/snippets/
