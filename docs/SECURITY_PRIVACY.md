# v2.8 — privacy dello storage locale

SQLite, JSON esportato e copie di backup **non sono cifrati automaticamente**. Salvare in directory utente a permessi restrittivi; non memorizzare buffer di digitazione, focus/target o clipboard; non registrare body snippet in log. La password nella connection string `Microsoft.Data.Sqlite` non conferisce cifratura senza provider nativo compatibile. Backup in cartelle cloud solo come archivio consistente chiuso e con avvertenza sulla riservatezza. Cifratura futura soggetta a threat model/ADR separati. [Dettagli](SQLITE_STORAGE.md) · [ADR approvato](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md).

---

# Policy output v2.6: privacy durante errori e clipboard

Nessuna cattura o persistenza di input da campi protetti; dati clipboard letti solo se il backend ne ha bisogno e con limiti best-effort; identificativi del target opachi/effimeri; log senza testo utente. Un sistema di restore incondizionato potrebbe sovrascrivere una copia appena effettuata dall'utente: preferire `RestoreSkippedConcurrentChange` a una perdita dati. Niente auto-replay dopo crash o errore parziale. [Contratto](OUTPUT_COORDINATION.md).

---

# Sicurezza, privacy e resilienza

## Dati trattati

Un text expander globale osserva eventi di tastiera per distinguere trigger: è un privilegio sensibile. Il progetto NON deve somigliare a un keylogger, né conservare flussi di input globali, né inviarli in rete.

## Regole di base

1. Buffer volatile limitato, azzerabile; niente tasti/testo reale nei log diagnostici. Stato e codice errore bastano.
2. Non intervenire su campi password/Secure Input quando identificabili; ove non identificabili dichiarare il limite e offrire esclusioni.
3. Allow/denylist app con precedenza chiara e messaggi di diagnostica; non cercare di aggirare l'OS.
4. Script/command importati sempre disabilitati e descritti come non attendibili finché l'utente non li controlla; non nell'MVP.
5. No network runtime richiesta per il motore, nessun account; update check solo opt-in e con policy trasparente.
6. Validare tutte le risorse locali di traduzione, import e backup, proteggendo da path traversal e dimensioni eccessive.
7. Clipboard è condivisa, asincrona e multi-formato. Ripristino *best effort* e condizionato se rilevabile; non scrivere sopra una copia utente intercorsa.
8. Restrizioni Wayland e permessi speciali documentati, non richiesti senza scopo spiegato.
9. Crash non deve lasciare attivo un hook che intercetta senza app o dati corrotti.
10. Monitorare licenze terze e vulnerabilità dipendenze; firmare distribuzioni dove fattibile, fornire checksum e build riproducibili come obiettivo.

## Threat model prima di implementare adapter

Attori e failure: estensioni/script maliziosi, trigger in chat/campi protetti, altre app che rubano focus, clipboard manager, hook concorrenti, API native che falliscono, sessioni remote, permessi elevati, dato importato gigantesco, libreria sincronizzata parzialmente. Per ogni minaccia indicare mitigazione, livello di test e limite residuo.

## Non-obiettivi di sicurezza

Non promettere che la clipboard torni sempre allo stato esatto, né isolare un processo esterno da tutte le tecniche di keylogging; limitarsi a controlli che possiamo verificare.

## Addendum v1.6 — anti-keylogging e fallimento chiuso

Il matcher deve mantenere **soltanto un suffisso volatile minimo**, mai memorizzare flussi di tasti per audit/analytics/crash report. Su secure input non tentare elusione. La possibilità di identificare correttamente il campo protetto/di testo dipende dalle API OS: in dubbio non cancellare né iniettare. Non affermare mai "rilevamento universale dei campi password". La diagnostica visualizza soltanto categorie. In contesti non editabili, pulsanti e scorciatoie di cancellazione sono potenzialmente distruttivi: la modalità sicura è nessuna azione automatica e, se utile, copia esplicita.


## Affinamento v1.7 — permessi e endpoint desktop

- Trattare hook/event taps/accessi input device come **facoltà sensibili**; chiedere solo permessi strettamente necessari e consentire revoca/pausa effettiva.
- Non aggirare UAC, campi password, Secure Input o policy del compositor; `Unknown` sul carattere protetto di un target implica modalità prudente.
- Per Wayland `evdev/uinput` non è una dipendenza implicita, non aggiungere l'utente a gruppi privilegiati senza spiegazione e consenso; valutare prima il threat model.
- La clipboard non è transazionale: mai cancellare il contenuto nuovo scritto dall'utente durante l'espansione; supporto multi-MIME best effort esplicitato.
- Eliminare dati di input dalla RAM appena inutili; log solo codici di errore e versione/ambiente, non trigger o testo. Vietati screenshot di campi sensibili nei test automatici.

Vedi [NATIVE_INPUT_OUTPUT.md](NATIVE_INPUT_OUTPUT.md) e [PLATFORM_CAPABILITY_POLICY.md](PLATFORM_CAPABILITY_POLICY.md).

## v1.8 — Formati importati e backup

- Importatore di file terzi è un parser non esecutivo con limiti su byte, nesting, YAML alias, ZIP, ricorsione, path e symlink. `shell/script/PowerShell` e regex importate non vengono eseguite né attivate.
- File di backup possono contenere dati personali e snippet confidenziali: niente telemetria o upload automatici; report redatti senza body, percorso sensibile o screenshot non autorizzati.
- Export CSV per Excel/LibreOffice ha rischio formula injection: proporre **modalità report sicura distinta** dall'export lossless; il quoting da solo non è una difesa universale. Fonte https://owasp.org/www-community/attacks/CSV_Injection.
- Snapshot coerente SQLite via Backup API o equivalente: mai dedurre consistenza copiando solo `.db` in presenza di WAL.

## Aggiornamento v1.9 — distinzione profili nativi e privacy

Beeftext `ProcessListManager` gestisce due liste distinte: `excludedApps` (non espandere) e `sensitiveApps` (contesti in cui Ctrl+V non opera correttamente, come alcuni terminali). Queste ultime **non** sono una protezione dei campi password. Per la nuova app separare `InputPrivacyPolicy` (campi/finestre sensibili, no logging keystroke), `AppExclusionRules` (scelte utente) e `OutputCompatibilityProfile` (paste/typing/fallback). Nessun bypass dei permessi di sistema. L'importazione preferenze è soggetta a validazione completa e rollback: può contenere percorsi, shortcut e configurazioni operative, non solo colori della GUI.

## Licenze e codice sorgente concorrenza (v2.1)

Il contenuto di `espanso-dev.zip` è GPLv3; `Beeftext-master.zip` è MIT. La nuova applicazione è prevista open source ma la licenza non è ancora deliberata. Adottare confronto concettuale e implementazione indipendente per non introdurre accidentalmente codice copyleft incompatibile. Gli importatori non devono eseguire script; ogni adapter di input deve dichiarare permessi e limitazioni. Baseline verificata in `research/audit/LOCAL_SOURCE_BASELINE_v2.1.md`.

## Requisiti v2.5 — testo sensibile e confusables

Evitare buffer persistenti, dump dei tasti, logging del testo utente, ricerca dei campi password attraverso bypass e ripristino clipboard non condizionale. Supportare warning opzionali su trigger visivamente confondibili (UTS #39), **senza convertirli** o vietare alfabeti legittimi. Quando IME commit, focus, caret o cancellazione sono incerti, non effettuare espansione automatica; offrire picker/copia manuale. [Audit](../research/audit/FOUR_SOURCE_UNICODE_MATCHER_AUDIT_v2.5.md).

## Addendum v2.7 — Stabilità e riservatezza delle metriche

Il monitoraggio locale di CPU, tempo, allocazioni e outcome può usare metriche aggregate e counters. Nessun log di tasti, trigger digitati, payload clipboard, query picker o corpo snippet. Coda di input **bounded** con reset conservativo in overload; non degradare la sicurezza per rientrare nel budget prestazionale. Ogni cache contenente contenuto utente ha limiti e scadenza; gli snapshot vecchi devono poter essere rilasciati.
