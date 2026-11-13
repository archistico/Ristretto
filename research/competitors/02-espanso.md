# Espanso — scheda di ricerca

**Categoria:** Text expander open source e riferimento multipiattaforma. **Piattaforma osservata:** Windows, macOS, Linux X11, Wayland sperimentale. **Licenza/modello:** GPL-3.0 (verificare versione corrente).  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-02.

## 1. Funzionalità documentate

YAML, matches exact/regex, word boundary, variabili, forms, app-specific config, Espanso Hub; clipboard/keyboard backend.

## 2. Punto di forza

Backend e ambiti di applicazione espliciti; separazione tra WHAT (matches) e HOW (config), package hub.

## 3. Limiti / compromessi

YAML porta frizione UX per non esperti; Wayland richiede hardware input permissions e ha limitazioni ufficialmente dichiarate.

## 4. Meccanismi da studiare / vantaggio per noi

GUI semplice su motore configurabile; strategie injection per app; form e profili dopo core affidabile.

## 5. Cosa testare più avanti

Provare regex, Unicode, layout internazionali, path Wayland e behavior di clipboard con editor reali.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://espanso.org/docs/configuration/basics/
- https://espanso.org/docs/matches/basics/
- https://espanso.org/docs/matches/forms/
- https://espanso.org/docs/install/linux/

**Fonti e conclusioni del benchmark iniziale:** [registro fonti](../BENCHMARK_SOURCE_REGISTER.md) e [sintesi](../LEGACY_FINDINGS_RETAINED.md).

## Approfondimento mirato v1.6 — trigger e affidabilità

**Evidenza da guida pubblica (D), verificata documentalmente il 12-11-2026:** Supporto `word: true`, `left_word`, `right_word`, `word_separators` e `propagate_case`. Separazione dei backend `keys`/`clipboard`; proposta di mapping GUI semplificato, test Unicode+IME necessari.

**Da incorporare:** vedi [benchmark dei trigger](../TRIGGER_RECOGNITION_BENCHMARK.md) e [contratto del motore](../../docs/TRIGGER_ENGINE.md). Non sono test diretti; compatibilità reale da dimostrare per OS/app.

**Fonte:** https://espanso.org/docs/matches/basics/

## Riscontri diretti dello snapshot utente v2.1

`espanso-dev.zip` contiene 15 crate Rust con matcher rolling e regex, backend OS, clipboard e pipeline di dispatch; `Auto` preferisce clipboard su Linux per testo non ASCII. Codice GPLv3: studiare la progettazione, non riutilizzare sorgenti senza valutazione compatibilità. [Audit](../audit/ESPANSO_DIRECT_SOURCE_AUDIT_v2.1.md).
