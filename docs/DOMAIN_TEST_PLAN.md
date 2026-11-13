# Piano di test del modello di dominio — v2.4

> Solo **specifica dei test**, nessuna prova implementata o eseguita. Gli scenari granulari sono in [DOMAIN_TEST_SCENARIOS.csv](../data/DOMAIN_TEST_SCENARIOS.csv), stato iniziale `NOT_RUN`. Il riferimento per tutte le altre prove rimane [TEST_STRATEGY.md](TEST_STRATEGY.md).

## 1. Piramide pragmatica

| Livello | Oggetto | Tecnica | Requisito di rilascio |
|---|---|---|---|
| Core unit | invarianti, matching, grapheme, parser, conflict resolver | xUnit, fixture isolate, property tests mirati | P0 completo, deterministico |
| Application unit | handler e coordinatore con port in memoria | fake controllati e fault injection | nessun effetto prima del preflight |
| Infrastructure integration | storage, commit, snapshot, import/export | DB/file temporaneo, crash simulation | recovery, no dati persi |
| Native E2E | focus, IME, clipboard, output/Backspace, permessi | OS reali, editor target, osservazione indipendente | certificazione per piattaforma e target |
| Desktop UI | editor, dialog, picker e traduzioni | test ViewModel + accessibility manuale | errori chiari; cambio lingua runtime |

Le certificazioni native non possono essere sostituite da test unit pure. Un `SendInput` che restituisce successo non equivale al testo visibile in un'app di terzi.

## 2. Suites P0

1. **Identity & revision:** creazione, spostamento, duplicazione, import con UUID uguali, salvataggi concorrenti, gruppo predefinito, disabilitazione collettiva.
2. **Trigger:** immediate/delimiter, prefissi sovrapposti, collisioni, case policy, Unicode non valido, target epoch e snapshot revision.
3. **Unicode:** NFC/NFD distinti nel contenuto, emoji ZWJ e VS16, combining marks, tedesco/turco/greco, CJK, RTL, terminatori di riga, null/controlli non consentiti.
4. **Template:** literal mode, marcatori speciali, cursor anchor validata a confine grapheme, variabile mancante, ricorsione, input form annullato.
5. **Lifecycle:** focus cambia, protected/unknown, IME composing, output parziale, doppia attivazione, clipboard concorrente, retry proibito.
6. **Persistence & import:** storage atomico, recovery, nessuna esecuzione di script dal file esterno, migrazione non lossy, aggiornamento snapshot solo dopo commit.

## 3. Analisi statica delle dipendenze

- `Core` non referenzia `Application`, `Infrastructure`, `Desktop`, Avalonia, Win32/Quartz/X11, SQLite.
- `Application` non referenzia `Infrastructure` o Avalonia; `Infrastructure` non referenzia Desktop.
- `Infrastructure.Platform.Windows` non deve essere inizializzata sui sistemi diversi da Windows; analogamente macOS/X11/Wayland.
- Nessun `P/Invoke` nel matcher o nelle entità.
- Per ogni test native: annotare backend, versione OS, desktop/compositor, layout tastiera, editor, permessi, metodo output e prova visiva/logica indipendente.

## 4. Oracoli di test Unicode

Confrontare **scalari Unicode**, ordine dei joiner, variation selector e terminatori di riga. `string.Length`, `Rune` count e grapheme count sono misure differenti da registrare ma non devono generare il numero di Backspace. La serializzazione JSON può usare escape diversi a parità di contenuto; testare il round trip semantico, non l'identità del file byte per byte, a meno che non si stia verificando un backup raw.

Fixture obbligatorie: `caffè`, `e\u0301`, `İ ı I i`, `Σ σ ς`, `👨‍👩‍👧‍👦`, `👍🏽`, `🇮🇹`, `❤️`, `こんにちは`, `العربية עברית`, tab, CRLF e LF, stringhe SQL, payload con surrogate orfano (input deliberatamente invalido), testi lunghi e mixed script.

## 5. Fault injection da non dimenticare

- `Remove` restituisce errore dopo un numero parziale di eventi OS: non attribuire esito `NoChange` per inerzia.
- La chiamata `Insert` riporta correttamente eventi inviati, ma il target li ignora: se manca la prova di ricezione → `SubmittedUnverified`.
- L'utente copia un'immagine mentre il programma sta inserendo tramite clipboard: ripristino condizionale, mai sovrascrivere una nuova copia.
- DB commit riuscito ma evento di aggiornamento matcher non ancora pubblicato: nuova snapshot pubblicata una sola volta e coerentemente, mai anticipata.
- UI di variabili aperta; ritorno focus su un'altra app: preflight fallisce e nessun trigger viene eliminato.
- IME emette composizione e commit separati: nessuna espansione prematura.

## 6. Criteri di maturità

**P0 Core pronto** quando invarianti DM-01..DM-15 sono coperte da test automatizzati appropriati, i conflitti hanno policy deterministica e nessuna API OS è referenziata. **P0 native pronto** solo per target e piattaforme qualificati con matrice riproducibile; Wayland non diventa supportato per la sola disponibilità della GUI.

Nessuna metrica prestazionale target è ancora ratificata: misurare CPU idle, latenza osservazione→candidate, memoria con 100/1.000/10.000 snippet, comportamento del picker e regressioni prima di fissare soglie. La metodologia va documentata e replicata per le release.

## Estensione v2.5 — Matcher Unicode

Il piano del dominio v2.4 rimane valido. Le nuove prove mirate su NFC, boundary, case, CJK, RTL, emoji, IME e collisioni sono in [MATCHER_TEST_PLAN.md](MATCHER_TEST_PLAN.md) e [CSV](../data/MATCHER_SCENARIOS_v2.5.csv); `NOT_RUN`, non sommare i loro esiti ai test eseguiti. `MatchResult` deve preservare gli intervalli del testo committato originale, non quelli della sola chiave normalizzata.
