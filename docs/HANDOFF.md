# Handoff — Ristretto Alpha 20.25

## Cosa stiamo facendo

Correzione dei due CS0246 della 20.24 e **hotkey opzionale per ogni snippet**, per l'inserimento manuale senza picker. [Dettagli 20.25](ALPHA20_25_SNIPPET_HOTKEYS.md). Il percorso automatico R5/R6 resta separato e senza permessi aggiuntivi.

R5 prepara l'espansione automatica Windows. La 20.24 introduce una **anteprima opt-in non attendibile** del riconoscimento di `;firma ` in un Edit Win32 esterno. Le notifiche WinEvent portano a un confronto Unicode e a una simulazione privata del matcher, ma non provano la provenienza dell'input. Nessun evento può autorizzare la scrittura.

## Baseline e verifica

- **Alpha 20.20:** l'utente ha confermato compilazione e test superati.
- **Alpha 20.22:** editor ridimensionabile e navigazione picker con frecce+Invio ancora da verificare sul desktop.
- **Alpha 20.24 corretta:** import Application aggiunto al file test; gate CORE superato con 779 xUnit. Errore iniziale riprodotto prima del fix.
- **Alpha 20.25:** 820 xUnit, inclusi 41 nuovi casi, passati; schema SQLite 2, migrazione da 1 e backup legacy provati su fixture. Nuove hotkey, clipboard, incolla e rendering desktop ancora da qualificare.

## Modifiche Alpha 20.25

- Campo facoltativo `Hotkey` in snippet, draft e JSON v1. SQLite schema 2, migrazione transazionale da 1, restore legacy migrato nello staging.
- `SnippetHotkeyCatalog` filtra snippet/gruppi disabilitati e rifiuta notifiche obsolete. `WindowsSnippetHotkeyService` usa un unico thread WM_HOTKEY per tutti gli snippet.
- `WindowsSnippetInsertRequest` attende il rilascio entro due secondi e riusa le verifiche del picker senza riattivare finestre. Richiesta monouso, nessun retry, appunti verificati e lasciati contenenti lo snippet.
- Campo nell'editor con cinque lingue; registrazioni aggiornate al reload. Duplicati snippet/picker rifiutati; conflitti OS salvati come configurazione inattiva.
- Hash dei gate esteso a XAML e risorse. [Esiti](../reports/ALPHA20_25_CHECKS.md).

## Modifiche Alpha 20.24

- `ExternalClassicEditTriggerPreviewSession`: simulazione con `TriggerActivationSession` privata, resettata dopo cambi di focus e anomalie; risultato senza testo/snippet, `CanAuthorizeExpansion=false`.
- `ExternalClassicEditLiveCommand`: aggiunta modalità preview con catalogo sintetico locale, contatori anonimi e nessuna scrittura.
- `scripts/observe-classic-edit-preview.cmd`: avvio manuale limitato nel tempo.
- Nuovi test xUnit e guardie `Ristretto.DevTools`. GUI, picker, hotkey, SQLite e VS Code rimangono invariati.

## Come riprendere

1. Estrai lo ZIP completo in una cartella nuova, apri PowerShell nella directory di `Ristretto.sln`.
2. Per modifiche successive esegui i gate pertinenti: `.\scripts\validate.cmd` per CORE, `gate ci` per Node e contratto export. Conservare gli esiti e validare lo ZIP estratto in una cartella nuova. Nessuna dipendenza Python reintrodotta.
3. Prova facoltativa Win32 (solo testo di prova): `.\scripts\observe-classic-edit-preview.cmd 15`. Digita `;firma ` nel campo dopo il messaggio di avvio. Nessuna espansione.
4. Qualifica prima le nuove hotkey su dati sintetici e profilo separato. Continua R5 sulla provenienza e R6 solo per destinatari qualificati. Nessun hook globale: la hotkey è un comando manuale.

## Obiettivo finale

Espansione automatica Unicode senza aprire il picker, mantenendo il picker come fallback.

[Stato](CURRENT_STATE.md) · [Roadmap unica](ROADMAP.md) · [Guida test](BUILD_AND_TEST.md) · [Alpha 20.24](ALPHA20_24_TRIGGER_PREVIEW.md).
