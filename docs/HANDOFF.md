# Handoff — Ristretto Alpha 20.26

**15 novembre 2026.** Questa revisione riguarda documentazione e pianificazione; il codice rimane 3.9.0-alpha.20.26.

## Obiettivo e punto di ripresa

L'ultimo incremento funzionale aggiunge **Registra…** nella libreria: una form acquisisce i tasti, mostra per esempio `ALT + CTRL + SHIFT + B` e aggiorna la bozza dopo conferma. La persistenza usa ancora Salva. L'utente ha confermato che il richiamo via hotkey della 20.25 funziona in Blocco note Windows.

Il prossimo passo è **qualificare la form sul desktop**, insieme a conflitti e ripristino delle registrazioni: R2.01 → R2.08 → R2.09, poi eventuali correzioni R2.07. Preparare un profilo sintetico, risultati attesi e rapporto per caso. Non trasferire il PASS di Blocco note a tutti gli editor o al nuovo dialogo.

La [roadmap](ROADMAP.md) è l'unico piano autorevole; include dipendenze e criteri di chiusura di R1–R8. [Stato corrente](CURRENT_STATE.md) e [rapporti](../reports/README.md) distinguono implementazione, esecuzione e conferme dell'utente.

## Baseline da conservare

- Alpha 20.26: gate CI anche da ZIP nuovo; **846 xUnit**, **32 Node**, 23 regressioni dei tool, 10 CLI e contratto .NET→Node passati; build senza warning.
- Restore NuGet locale; audit vulnerabilità online non eseguito.
- Schema SQLite 2 dalla 20.25, migrazione da 1, backup legacy migrato solo nello staging, hotkey nel JSON v1 facoltativo.
- Registra…: 26 nuove regressioni sullo stato e sul coordinatore. Nuova form, routing dei tasti e sospensione/ripristino nativi ancora da provare sul desktop.
- R5: motore/bridge e diagnostica disponibili; provenienza esterna non qualificata. Nessun collegamento dall'anteprima all'espansione di prodotto.
- R3: Node e contratto export validati; vero host non qualificato. Crash precedente `Code.exe / 0x80000003` senza causa confermata, nessun riavvio automatico.

[Evidenze della baseline](../reports/ALPHA20_26_CHECKS.md) · [revisione documentale](../reports/DOCS_ROADMAP_REVIEW.md).

## Dettagli utili della 20.26

- `HotkeyCaptureState` gestisce display, ripetizioni, tasti non supportati, rilascio e correzione del gesto.
- `HotkeyCaptureWindow` offre Usa combinazione, Annulla e Rimuovi hotkey; Esc/chiusura annullano.
- `HotkeyCaptureCoordinator` mantiene sospese le notifiche durante pausa, dialogo e ripristino.
- `App.HotkeyCapture` ferma/ricrea i worker senza impostazioni temporanee nel database; usa il picker effettivamente attivo e lo snapshot più recente.
- Le notifiche obsolete del picker e le richieste di inserimento pendenti sono invalidate. Il worker picker gestisce anche la disposizione prima del completamento dell'avvio.
- Schema, JSON e contenuti degli snippet restano quelli della baseline; non ridisegnare il picker durante una correzione del dialogo.

## Lavoro indipendente dalla prova desktop

Se l'ambiente non permette la qualificazione, avanzare su R1 residui, recupero dati/distribuzione R4 e studio R5 in sola lettura. Registrare ciò che manca, senza dichiarare completa R2.

R5 deve confrontare digitazione, paste, IME, programmatiche e contesto con evidenze; WinEvent o un hook da soli non sono un'autorizzazione. R6 dipende da una decisione positiva per un target concreto.

## Regole della documentazione

**Preferenza dell'utente da mantenere:** README con titolo, screenshot, descrizione del progetto, funzionamento, installazione, aggiornamento e riferimenti. Linguaggio chiaro e diretto; storico delle versioni nel changelog, procedure tecniche nei documenti dedicati.

Le immagini attuali sono anteprime Preview 9 della Alpha 7: indicarne la provenienza finché non vengono sostituite con acquisizioni reali del prodotto corrente. Nessun render o mockup va presentato come prova manuale.

Aggiornare insieme README, stato, roadmap, changelog e indice quando cambia il comportamento. Le specifiche e i rapporti delle vecchie Alpha restano storici. I documenti di studio non dimostrano implementazione o qualificazione attuale.

## Verifica e consegna

Per modifiche funzionali: nuova versione coerente, gate pertinenti, rapporto e archivio verificato da cartella nuova. Per soli documenti: mantenere la versione del codice, eseguire gate SOURCE/audit documentali e registrare che i test applicativi non sono stati rieseguiti.

La pipeline di progetto è C#/.NET 10. Python non è richiesto; Node serve a CI/FULL per il ramo VS Code. Non disabilitare TLS o verifiche delle firme per risolvere problemi di restore.

Conservare dati e backup fuori dalle cartelle delle versioni. Un `--data-dir` relativo cambia significato se cambia la directory di lavoro; per riutilizzare un profilo usare un percorso assoluto.

[Installazione e aggiornamento](INSTALLATION_AND_UPDATE.md) · [comandi e perimetri dei gate](BUILD_AND_TEST.md).
