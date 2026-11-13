# Backlog di verifiche: ricerca documentale vs prove reali

**Nessuna verifica elencata è stata eseguita nella fase v1.5.**

| ID | Esperimento | Piattaforme | Cosa documentare | Priorità |
|---|---|---|---|---|
| VAL-001 | Trigger/paste/plain Unicode | Windows, macOS, X11, Wayland | esito per app e metodo | P0 |
| VAL-002 | Emoji ZWJ/flag/skin-tone + cursor | tutte | byte identici, grafemi, cursor | P0 |
| VAL-003 | Diacritici decomposed/NFC e Turkish I | tutte | matching, case, collisioni | P0 |
| VAL-004 | IME composizione JPN/CHN/KOR | tutte | no espansione pre-commit | P0 |
| VAL-005 | Clipboard immagine+RTF+HTML e copy concorrente | tutte | restore o conflitto rilevato | P0 |
| VAL-006 | Focus switch fra picker e target | tutte | rifiuto sicuro, fallback | P0 |
| VAL-007 | Hook simultaneo con altri expander | Windows/macOS/Linux | double expansion, lost keys | P0 |
| VAL-008 | Password manager / secure fields | Win/macOS | no capture/expansion | P0 |
| VAL-009 | Tray e global hotkey per desktop env | tutte | capability/permessi | P0 |
| VAL-010 | App native/Chromium/Electron/Office/terminal | tutte | output mode e workarounds | P0 |
| VAL-011 | Raycast delimiter modes e system collisions | macOS (reference) | confronto comportamentale | P1 |
| VAL-012 | AutoHotkey endchar/word scope | Windows (reference) | equivalenza opzionale GUI | P1 |
| VAL-013 | Espanso keys/clipboard/app profiles | Win/macOS/X11/Wayland | backend behavior, privileges | P0 |
| VAL-014 | PhraseExpress focus and rich text output | Windows (reference) | target, clipboard formats | P1 |
| VAL-015 | AutoKey X11/Wayland boundary | Linux (reference) | DE/session, permissions | P0 |
| VAL-016 | 10k snippets search + GUI list virtualization | tutte | tempi, RAM, input latency | P1 |
| VAL-017 | Backup recovery after process kill/disco pieno | tutte | dati verificabili | P0 |
| VAL-018 | Import Beeftext reale e concorrenti JSON/YAML | tutte | diff/import only, no script | P1 |
| VAL-019 | i18n hot switch + RTL/UI scaling | tutte | strings, focus, layout, tests | P0 |
| VAL-020 | Linux Wayland capability with compositors | GNOME/KDE/Sway (se disponibili) | esito per release/compositor | P0 |

**Artefatto previsto per ogni test:** `environment`, commit, product version, OS+session+app, input fixture, expected, actual, result, steps, source URL, screenshot se autorizzata, test date, redacted logs. Nessun uso di screenshot sensibili come prova pubblica.

Vedere anche la [matrice storica consolidata](../data/HISTORICAL_TEST_SCENARIOS.csv) (63 pipeline + 54 Unicode, `NOT_RUN`).

## Nuovi esperimenti v1.6 (non eseguiti)

| ID | Verifica | Prodotto/target | Priorità |
|---|---|---|---|
| VAL-021 | Keep/drop e Enter/Tab, output effettivo | Raycast + editor target | P0 |
| VAL-022 | Collisioni identical/overlapping e chooser | KM/PhraseExpress/TextExpander | P0 |
| VAL-023 | Backspace grapheme trigger con ZWJ/NFD | WM/IME su Win/macOS/X11/Wayland | P0 |
| VAL-024 | Buffer dopo cambio app/caret/selection | Adapter nativi, originali come confronto | P0 |
| VAL-025 | Trigger IME: provvisorio/commit/cancel | JPN/CHN/KOR su app target | P0 |
| VAL-026 | Interferenze Text Blaze/Office autocorrect | browser/Office | P1 |
| VAL-027 | Diagnostica senza payload sensibili | nostra futura app | P0 |
| VAL-028 | Manual picker copy-only on unknown target | nostra futura app | P1 |

Matrice scenari atomici di dettaglio: [data/TRIGGER_SCENARIOS.csv](../data/TRIGGER_SCENARIOS.csv).


## Gate v1.7 — test di piattaforma ancora da eseguire

Aggiungere il piano [NATIVE_SPIKE_PLAN.md](NATIVE_SPIKE_PLAN.md) come riferimento per A6/A7/A8 e la [matrice di 72 scenari NAT](../data/NATIVE_SPIKE_MATRIX.csv), tutti `NOT_RUN`. In particolare isolare `GlobalShortcuts` vs `InputCapture` e `RemoteDesktop`, `KEYEVENTF_UNICODE` vs clipboard, input fisico vs IME committed, applicazione protetta e clipboard con altro owner.
