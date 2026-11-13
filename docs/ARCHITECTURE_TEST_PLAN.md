# Piano di verifica dei confini architetturali — v2.3

**Stato di ogni scenario:** `NOT_RUN`. Il presente documento descrive criteri di prova, non risultati. Non sono stati implementati/compilati né l'app C# né adapter nativi. Gli audit di codice originali sono statici.

## 1. Piramide di test

- **Core.Tests**: matching, parser, Unicode, collisioni, ordinamento/ranking, validazione piani; esecuzione identica Windows/macOS/Linux; nessun fake della tastiera come prova dell'OS reale.
- **Application.Tests**: use case reali con storage in-memory/fake e `TargetInspector`, `Inserter`, `Clipboard` di prova che iniettano cancellazioni e fallimenti; verificare ordine degli effetti.
- **Infrastructure.Tests**: SQLite/JSON, filesystem crash-safety con directory temporanee e fault injection, parsers Beeftext/Espanso senza esecuzione di comandi, correttezza backup/restore.
- **Desktop.Tests**: ViewModel, localizzazione runtime, navigazione, validazioni, picker keyboard-only, a11y; smoke test grafici sui target.
- **Native.Tests** (solo dopo adapter): unità e integrazione su VM/macchina **reale** per Windows/macOS/X11/Wayland e editor target. Automatizzare ove sicuro e documentare test manuali dove i permessi richiedono conferma.
- **Architecture checks**: controllo dipendenze `Core` non referenzia `Avalonia.*`, `SQLite`, `Windows.*`, `Infrastructure`; Application senza UI/Win32; Infrastructure senza ViewModels; nessun riferimento circolare.

## 2. Set di test per ogni confine

| ID | Ambito | Scenario | Accettazione attesa | Stato |
|---|---|---|---|---|
| ARC-001 | Dipendenze | `Core` non importa namespace UI/OS/persistence | zero referenze vietate | NOT_RUN |
| ARC-002 | Dipendenze | `Application` non referenzia `Desktop` | zero referenze | NOT_RUN |
| ARC-003 | Dipendenze | `Infrastructure` non referenzia `Desktop` | zero referenze | NOT_RUN |
| ARC-004 | Dipendenze | Controllo grafo pacchetti/progetti | nessun ciclo | NOT_RUN |
| ARC-005 | Core | Match univoco immediato | ID deterministico | NOT_RUN |
| ARC-006 | Core | Collisione stesso trigger e scope | collisione esplicita | NOT_RUN |
| ARC-007 | Core | Trigger prefisso di altro trigger | regola esplicita, no random | NOT_RUN |
| ARC-008 | Core | Boundary con caratteri Unicode | match conforme a policy | NOT_RUN |
| ARC-009 | Core | Input NFC vs NFD | confronto corretto, payload invariato | NOT_RUN |
| ARC-010 | Core | CJK, RTL, arabo, hindi | preservazione e confronto qualificato | NOT_RUN |
| ARC-011 | Core | Emoji ZWJ/flag/skin tone | nessuna troncatura dei dati | NOT_RUN |
| ARC-012 | Core | Parser di snippet con azioni non consentite | errore prima degli effetti | NOT_RUN |
| ARC-013 | Core | Ciclo tra riferimenti snippet | errore deterministico | NOT_RUN |
| ARC-014 | Core | Snapshot matcher aggiornato durante input | non usa item cancellato | NOT_RUN |
| ARC-015 | Application | Composizione IME in corso | nessun trigger | NOT_RUN |
| ARC-016 | Application | Evento con commit incerto | non autoespande | NOT_RUN |
| ARC-017 | Application | Cambio focus a metà trigger | reset buffer | NOT_RUN |
| ARC-018 | Application | Cambio target dopo `Prepare` | cancellazione vietata | NOT_RUN |
| ARC-019 | Application | Form annullato | testo destinazione intatto | NOT_RUN |
| ARC-020 | Application | Piano invalido | nessun native input | NOT_RUN |
| ARC-021 | Application | Clipboard non disponibile | esito senza effetti o fallback autorizzato | NOT_RUN |
| ARC-022 | Application | Due trigger contemporanei | no doppia inserzione | NOT_RUN |
| ARC-023 | Application | Input sintetico riecheggiato | non riattiva matcher | NOT_RUN |
| ARC-024 | Application | `SendInput` parziale simulato | `PartialInsert`, niente retry cieco | NOT_RUN |
| ARC-025 | Application | `Pause` nel mezzo della sequenza | stop sicuro + reset buffer | NOT_RUN |
| ARC-026 | Application | Permesso revocato durante sessione | stato capability degradato | NOT_RUN |
| ARC-027 | Application | Campo password/protetto | niente hook utile o espansione | NOT_RUN |
| ARC-028 | Application | Diagnostica evento sensibile | nessun payload nei log | NOT_RUN |
| ARC-029 | Application | Picker focus perso | no inserimento nel campo sbagliato | NOT_RUN |
| ARC-030 | Application | Picker copy-only su backend unsupported | copia solo per conferma utente | NOT_RUN |
| ARC-031 | Data | Import Beeftext JSON v10 | report completo campi/unknown | NOT_RUN |
| ARC-032 | Data | Import Espanso YAML con script | mai esecuzione shell/script | NOT_RUN |
| ARC-033 | Data | Import con collisioni | preview e decisione esplicite | NOT_RUN |
| ARC-034 | Data | Import che fallisce alla metà | vecchia libreria integra | NOT_RUN |
| ARC-035 | Data | Salvataggio interrotto | nessun file attivo parziale | NOT_RUN |
| ARC-036 | Data | Restore corrotto | backup precedente accessibile | NOT_RUN |
| ARC-037 | Data | Round-trip JSON emoji/RTL/NFD | contenuto byte-per-byte o semantica esatta secondo contratto | NOT_RUN |
| ARC-038 | UI | Switch italiano → inglese a runtime | menu/dialog/tray coerenti | NOT_RUN |
| ARC-039 | UI | Switch light/dark/automatico | contrasto e stato coerenti | NOT_RUN |
| ARC-040 | UI | Navigation keyboard-only | focus visibile e niente dead-end | NOT_RUN |
| ARC-041 | UI | Picker rapido con 10k snippets | prestazione misurata, baseline documentata | NOT_RUN |
| ARC-042 | Native Windows | `SendInput` UTF-16, key up/down | risultato su editor reale, errori riportati | NOT_RUN |
| ARC-043 | Native Windows | UIPI / elevated target | errore comprensibile, nessuna promessa falsa | NOT_RUN |
| ARC-044 | Native Windows | Copy concorrente durante paste | copia dell'utente non sovrascritta | NOT_RUN |
| ARC-045 | Native macOS | Permessi AX e revoca | capability on/off reali | NOT_RUN |
| ARC-046 | Native macOS | IME e Secure Input | nessuna espansione in composizione/protetto | NOT_RUN |
| ARC-047 | Native X11 | XKB/layout dead key/AltGr | testo committato non falsificato | NOT_RUN |
| ARC-048 | Native X11 | PRIMARY vs CLIPBOARD | nessuna distruzione inattesa | NOT_RUN |
| ARC-049 | Native Wayland | GNOME senza API osservazione | autoexpand Unsupported, GUI disponibile | NOT_RUN |
| ARC-050 | Native Wayland | KDE / wlroots con diverso backend | capacità per compositor, niente claim generale | NOT_RUN |
| ARC-051 | Native | Cambia lingua/layout mentre hook attivo | buffer reset/nessuna corruzione | NOT_RUN |
| ARC-052 | Native | Editor browser/IDE/terminal/electron | per-target report di compatibilità | NOT_RUN |
| ARC-053 | Packaging | Portable e installata coesistono | dati in percorsi distinti | NOT_RUN |
| ARC-054 | Packaging | Update/uninstall | dati utente preservati | NOT_RUN |
| ARC-055 | Licenza | SBOM e notices MIT / assenza copie GPL indesiderate | inventario verificato | NOT_RUN |

## 3. Matrice reale per gli adapter

La **qualifica** richiede: sistema operativo/versione, sessione/compositor, layout, IME, app target/versione, modalità output, stato permessi, testo di prova, risultato reale, issue. Non riassumere con un solo “Windows OK”. Sono necessarie prove su Notepad/TextEdit/Gedit-equivalenti, browser, editor Electron, IDE, terminale, RDP e campi protetti dove consentito.

Test manuali di intercettazione **solo su macchine proprie/autorizzate**, mai su password reali o dati di terzi. Test input Unicode con fixture sintetiche. Le clipboard contenenti dati reali non vanno allegate ai report.

## 4. Misure prestazionali da introdurre

- Tempo dal carattere committato alla decisione del matcher (p50, p95, max).
- Tempo dal `Prepare` all'output nella app target (con segmentazione per backend).
- Tempo di avvio a libreria piccola/media/grande e carico CPU/RAM idle.
- Tempo ricerca picker con 1k, 10k, 50k snippet; sessione IME senza keylogger buffer persistente.
- Conteggi errori `Unsupported`, `PartialInsert`, `TargetLost`, `ClipboardConflict` per backend, non contenuti.

Non fissare soglie impossibili prima di un baseline su hardware rappresentativo; definire obiettivi durante gli spike e trasformarli in test di regressione.

## 5. Gate di uscita della fase di studio

1. ADR architettura approvato o modificato; nessuna incertezza sui quattro progetti e sui confini di dipendenza.
2. Prima slice P0 scelta e test di accettazione scritti.
3. Scala Unicode e policy matching/cancellazione accettate.
4. Macchina reale o VM preparata per prime prove native; nessuna promessa di supporto su target non testati.
5. Licenza progetto e dipendenze approvate.

Leggere [architettura](ARCHITECTURE.md), [slice](VERTICAL_SLICES.md) e [porte native](PLATFORM_ADAPTER_CONTRACTS.md).
