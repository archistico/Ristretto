# Accettazione visuale — GUI Preview 9

Riferimenti: [guida GUI](GUI.md), [design system](DESIGN_SYSTEM.md), [32 render definitivi](../design/current/review.json). Gli audit di preview sono consolidati qui; una build o un render non promuovono una prova manuale a PASS.

## Evidenze disponibili

| Verifica | Esito | Scope |
|---|---|---|
| Render main/picker, cinque lingue, due temi | PASS | Client Avalonia a 96 DPI, dati sintetici |
| Minimi IT/DE, picker vuoto, tavole controlli | PASS | Inclusi nei 32 render |
| Limiti dei pulsanti applicativi visibili | PASS | Controllo geometrico del tool |
| Contrasti token | PASS | 72 coppie; minimo testuale 4,664:1; non certificazione WCAG dell'app |
| Build e regressione alpha.7 | PASS | 180 xUnit; [rapporti](../reports/README.md) |

## Checklist manuale ancora aperta

Usare dati sintetici nell'app reale. Annotare OS, versione, tema, lingua, risoluzione/DPI ed esito `PASS`, `FAIL`, `BLOCKED` o `NOT_RUN`.

| Area | Prova | Stato |
|---|---|---|
| Temi | Chiaro/scuro/sistema, cambio a runtime, testi e icone leggibili | NOT_RUN |
| Tastiera | Tab/Shift+Tab, focus visibile, menu e dialoghi raggiungibili | NOT_RUN |
| Stati | Hover/pressed/disabled, selezione lista e focus dei campi | NOT_RUN |
| Geometria | Resize continuo e minimi; stato lungo senza sovrapposizioni | NOT_RUN |
| DPI | 100/125/150%, anche cambiando monitor | NOT_RUN |
| Lingue | IT/EN/FR/DE/ES, etichette lunghe e assenza di clipping | NOT_RUN |
| Picker | Focus ricerca, frecce, Invio=copia esatta, Esc=chiudi, lista vuota | NOT_RUN |
| Editor | Salva/elimina, bozze, cambi selezione, chiusura/nascondimento in tray | NOT_RUN |
| Dialoghi | Import review, avvisi, conferme, backup/ripristino e menu | NOT_RUN |
| Unicode | Accenti combinanti, emoji ZWJ, CJK, RTL e testo multiriga | NOT_RUN |
| Accessibilità | Windows high contrast, focus, indicatori non affidati al solo colore | NOT_RUN |
| Piattaforme | Resa e input su macOS/Linux | NOT_RUN |

Rigenerare le anteprime in una nuova cartella in `.local-cache`, come indicato in [GUI.md](GUI.md). Il tool apre finestre proprie; la validazione ordinaria lo compila senza avviarlo.
