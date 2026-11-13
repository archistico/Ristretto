# Audit v1.9 — preferenze, avvio, tray, i18n e sicurezza del ciclo di vita

**Metodo:** lettura statica `main.cpp`, `MainWindow.cpp`, `PreferencesManager.cpp`, `PreferencesDialog.cpp`, `I18nManager.cpp`, `AutoStart.cpp`, `UpdateManager.cpp`, `ProcessListManager.cpp`. Fonti: [registro](SOURCE_TRACE_V19.md).

## 1. Stati dell'applicazione e transizioni attese

| Evento | Beeftext osservato | Contratto proposto nella nuova app |
|---|---|---|
| Primo avvio | finestra visibile, domanda tutorial | onboarding in-app conciso e senza obbligo browser |
| Successivo avvio | background/tray possibile | stato chiaro e un'opzione 'mostra all'avvio' |
| Seconda istanza | segnala e apre l'istanza precedente | single instance con canale IPC minimo e autenticato per utente |
| Chiudi finestra | memorizza geometria, lascia il processo in tray | identico ma feedback esplicito per primo utilizzo |
| Pausa/Riprendi | alterna preference, icona e menu | fermare acquisizione e svuotare buffer; mostrare stato nell'interfaccia |
| Esci | chiude e termina loop | release hook, flush transazionale dati, timeout e log non sensibile |
| Sospensione/lock/logout | non completamente verificati nel sorgente visionato | reset del contesto e nessuna espansione mentre lock/secure input |
| Revoca permessi OS | non verificata end-to-end | degradazione in capability `RequiresPermission` e fallback manuale |
| Aggiornamento | worker Qt su timer con endpoint `latestVersionInfo.json` | verifica opt-in, firma/hash dove applicabile, nessun download eseguibile tacito |

## 2. I18n: funzionalità e bug da evitare

La lingua è un `QLocale`, a inizio vita `PreferencesManager` applica la locale scelta. `I18nManager` dispone di directory per lingua e paese (`de_DE`, `it_IT`, ecc.), accetta traduzioni integrate o utente, carica separatamente Qt, Beeftext e XMiLib, rigenera parte della UI su `LanguageChange`. Il flusso di traduzione dipende da `.qm`, supporto PowerShell per compilazione delle risorse e dalla presenza di file `xmilib_XX.qm`.

**Contratto i18n Avalonia:** inglese fallback, scelta lingua del sistema al primo avvio, override salvato, aggiornamento runtime con stringhe osservabili per ViewModel, dialog, tray, notifiche e validation errors, resource validation CI, plural rules e pseudolocalization; supportare layout RTL dove pertinente, senza modificare il testo originale degli snippet. Gli snippet possono essere in qualsiasi lingua indipendentemente da quella dell'interfaccia.

**Riscontro statico del sorgente:** i tre mapping file `.qm` polacchi in `migrateTranslationFilesToNewLayout()` destinano a `nl_NL`, apparente refuso di cartella; altri path presentano `.pm` vs `.qm`. Registrare come regressioni candidate, non come test eseguiti. Una `Locale` non disponibile deve produrre fallback e diagnostica, non una GUI parzialmente tradotta o crash.

## 3. Preferenze: modello da non replicare

| Categoria | Cosa emerge dal codice | Strategia futura |
|---|---|---|
| Setup locale | `QSettings`: installed vs portable `.ini` | profilo per utente in cartella OS, portable con scelta esplicita |
| Tema | cache + `applyThemePreferences()` | theme manager osservabile, colore e accessibilità coerenti |
| Shortcut | formati attuali e legacy, aggiornati a caldo | validare collisioni con OS e app prima del commit |
| Clipboard | tipo legacy/default aggiornabile da pref | per-app capability, metodo fallback visibile |
| Storage path | setter modifica preferenza e richiama save list | validare destino/swap prima di cambiare percorso |
| Import prefs | carica JSON e applica progressivamente | parse completo, validazione schema, preview diff, commit atomico dove possibile |
| Export prefs | serializza direttamente in file | formato JSON stabile, checksum opzionale, replace sicuro |
| Dati sensibili | alcuni percorsi sono memorizzati in preferenze | non esportare token o segreti per default; redazione log |

### Bug statici importanti da trasformare in test

- **PREF-001:** `UseCustomBackupLocation` esportato leggendo `UseCustomSound`.
- **PREF-002:** `MainWindowSplitterState` importato dentro `Geometry`.
- **PREF-003:** input JSON formalmente valido ma con campo errato può fallire dopo che altri campi sono stati scritti.
- **PREF-004:** un percorso personalizzato valido su Windows può essere invalido su Linux/macOS, e l'importazione non deve renderlo operativo senza conferma.
- **I18N-001:** mapping migrazione polacco con destinazione sbagliata; verificare su fixture se si decide import compatibile delle traduzioni.

## 4. Autostart e portable

`AutoStart.cpp` controlla `isInPortableMode()`; usa la chiave Windows `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. L'autostart portable è ignorato e la preferenza non è esposta. Per la nostra app: Windows startup per-utente non elevato, macOS login items secondo API consentite, Linux XDG autostart/sessione quando applicabile; comportamento disattivato per portable salvo scelta esplicita e possibile su quel sistema. Non chiedere privilegi admin per la sola autostart.

## 5. Schede dell'applicazione e privacy

Il programma originale conserva `sensitiveApps.json` ed `excludedApps.json`. L'elenco sensibile è associato a incompatibilità con Ctrl+V (esempio terminali), non si deve assumere che sia un filtro sicuro contro password o segreti. La nuova app deve avere **due concetti indipendenti**: (a) esclusioni di sicurezza/password/contesto incerto e (b) profili di compatibilità tecnica (es. paste non disponibile). Sono richiesti fail-safe, niente log di keystroke e opt-in ai permessi.

## 6. Test in backlog

L'elenco strutturato è in `data/ARCHITECTURE_AUDIT_SCENARIOS.csv`; tutti `NOT_RUN`. Nessuna funzionalità qui descritta è stata sviluppata nel nuovo progetto.
