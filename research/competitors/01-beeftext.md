# Beeftext — scheda di ricerca

**Categoria:** Riferimento originario. **Piattaforma osservata:** Windows. **Licenza/modello:** MIT (repo).  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-01.

## 1. Funzionalità documentate

Gestione visuale di snippet, gruppi, picker, tray, trigger Strict/Loose, variabili, esportazione e backup.

## 2. Punto di forza

Familiarità e rapidità nelle funzioni base; utile per migrazione dati e mappatura GUI.

## 3. Limiti / compromessi

Limitazioni da Win-only e da gestione clipboard/focus; scelta casuale delle collisioni osservata nell’audit storico.

## 4. Meccanismi da studiare / vantaggio per noi

Separare input/matcher/plan da GUI, importatore Beeftext e compatibilità semantica documentata.

## 5. Cosa testare più avanti

Confrontare file import/export reali e completare audit del checkout/commit, inclusi settaggi avanzati.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://github.com/xmichelo/Beeftext
- https://github.com/xmichelo/Beeftext/wiki

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Riscontri diretti dello snapshot utente v2.1

Il sorgente in `Beeftext-master.zip` (269 file) conferma hook Windows, matcher con scelta casuale tra conflitti, frammenti di snippet, picker Qt, clipboard doppio backend, i18n e dipendenze dei submodule mancanti. [Audit con linee](../audit/BEEFTEXT_DIRECT_SOURCE_AUDIT_v2.1.md).
