# Matcher Unicode — piano di verifica P0/P1 (v2.5)

**Stato: specifica e dataset di test, non test eseguiti.** Matrice machine-readable: [MATCHER_SCENARIOS_v2.5.csv](../data/MATCHER_SCENARIOS_v2.5.csv). Si integra, senza sostituirli, ai [test di dominio](DOMAIN_TEST_PLAN.md), [test trigger](../research/TRIGGER_TEST_DESIGN.md) e ai [test nativi](../data/NATIVE_SPIKE_MATRIX.csv).

## 1. Livelli di prova

| Livello | Verifica | Richiede UI / OS reale? | Prova attesa |
|---|---|---|---|
| Unit Core | NFC vs exact, case, Unicode valido, conflitti, suffix e delimitatori | No | risultato matcher, input invariato, nessuna side effect |
| Property-based | invarianti su testo Unicode, normalizzazioni, permutazioni ordine snippet, segmentation, equivalenze | No | seed riproducibile e modello di riferimento |
| Parser/Integration | import che crea trigger canonici; publish snapshot dopo commit; scope/gruppi | No | fixture e DB temporaneo |
| Native adapter | disponibilità di text commit, IME, focus e deletion verificabile | Sì | matrice OS e app, capability dichiarate |
| End-to-end | output esatto, trigger cancellato senza perdita dati, cursor marker, clipboard ripristinata condizionalmente | Sì | registrazione tecnica non contenente contenuti personali |
| GUI | preview regole, conflitti, messaggi e localizzazione chiara/scura | Sì | test UI, accessibilità, aggiornamento dinamico |

## 2. Oracoli e invarianti

- **NFC:** `normalize(NFC(x)) == normalize(NFC(normalize(NFC(x))))`, equivalenza canonica, nessuna fusione di emoji variation selector o skin tone. Confronti `Exact` e `NFC` non vanno confusi.
- **Case:** default `Sensitive`; `OrdinalIgnoreCase` solo candidate policy limitata, **non** equiparata a full folding. Test indipendenti per `İ/ı/I/i`, `Σ/σ/ς`, `ß/SS`.
- **Conflitti:** l'ordine di registrazione degli snippet non decide il vincitore; trigger equivalenti nello stesso scope => `Ambiguous` e niente scrittura esterna; scope più specifico non prevale senza decisione dichiarata.
- **Attivazione:** `AfterDelimiterKeep` preserva il *delimitatore effettivo* (anche punteggiatura), Drop lo consuma; Enter/Tab non sono equivalenti a uno spazio e richiedono supporto nativo specifico.
- **Boundary:** prima di chiamare un match `WholeWord` attendere evidenza del bordo destro; non anticipare e poi tentare Undo. CJK e thai/lo richiedono policy tailoring/documentata; P0 preferisce trigger espliciti o delimitatori.
- **IME:** stati di composizione non vengono memorizzati come sequenza committata; quando il provider non sa riconoscere commit, `Unsupported/ManualOnly`, non tentativi distruttivi.
- **Cancellazione:** risultati `MatchSpan` e `raw text` **non** significano che esiste un conteggio Backspace garantito; il preflight può negare l'espansione e offrire picker/Copia.
- **Security:** scope su finestre, nessun log del buffer o del contenuto dello snippet; stringhe bidirezionali e confusables visibili nell'editor senza correzioni automatiche.

## 3. Dataset ufficiali da aggiungere alla futura pipeline (non inclusi nel dossier)

- **Unicode NormalizationTest.txt** (UAX #15). Verificare conformità dell'algoritmo effettivamente scelto.
- **Unicode GraphemeBreakTest.txt / WordBreakTest.txt** (UAX #29). *Pin* alla versione Unicode del runtime/distributore, aggiornamento controllato.
- **Unicode emoji-test.txt** per repertorio e sequenze qualificate (la visualizzazione dipende dal font OS e non è garantita dalla stringa).
- **UTS #39 confusables.txt** per warning opzionali; non usarlo come normalizzatore.

**Versioning:** i dataset vengono referenziati mediante URL ufficiale, release Unicode e hash al momento dell'implementazione; non fare scaricamenti automatici a runtime. La versione Unicode dell'app è ancora da scegliere; quella pubblicata attualmente è 18.0, ma ciò non implica che .NET e tutti i sistemi dispongano degli stessi dati.

## 4. Scenario end-to-end minimo cross-app

Un solo trigger `;firma` con output `Caffè — Καλημέρα — مرحبا — 日本語 — 👩🏽‍💻 ❤️`: verificare input logico, composizione, tastiera/layout, clipboard, che non cambi il focus, comportamento dell'editor e outcome. Ripetere in editor semplici e browser, con focus cambiato e input non supportato, senza usare credenziali vere o registrare testo utente.

## 5. Criterio di uscita dal gate

- Nessun caso `P0` unit fallisce per NFC, case base, Unicode well-formed, emissione/ritardo delimitatore o ambiguità.
- Nessuna prova di output viene dichiarata `VerifiedComplete` senza osservazione del contenuto del target affidabile; altrimenti `SubmittedUnverified`.
- Zero regressioni di perdita testo o clipboard in piattaforme dichiarate supportate; dove l'API non garantisce questa proprietà, segnare la capability come **non supportata** e non attivare trigger automatici.
- I test nativi restano `NOT_RUN` finché non viene svolta la prova su Windows/macOS/X11/Wayland con relativa evidenza.
