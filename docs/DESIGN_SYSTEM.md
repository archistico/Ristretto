# Design system implementato — Preview 9

La fonte dei token e degli stati è [App.axaml](../src/TextExpander.Desktop/App.axaml). Questa guida descrive il tema corrente; le precedenti tabelle di colori proposti sono state eliminate.

## Token principali

| Risorsa | Chiaro | Scuro | Uso |
|---|---|---|---|
| `RCanvas` | `#F6F5F1` | `#171819` | Sfondo |
| `RSidebar` | `#F0EFEB` | `#1C1D1E` | Navigazione |
| `RSurface` | `#FFFFFF` | `#242527` | Superfici |
| `RInput` | `#FFFFFF` | `#2A2B2D` | Campi |
| `REditorInset` | `#FAF9F6` | `#202123` | Contenuto editor |
| `RSubtle` | `#E7E5DF` | `#353638` | Chip e controlli secondari |
| `RHover` | `#E2E2DB` | `#3C3F3D` | Hover |
| `RSelection` | `#E3E7E0` | `#363D37` | Selezione |
| `RSelectionMarker` | `#646F65` | `#B8C1B6` | Indicatore laterale |
| `RBorder` | `#D2D2C9` | `#494B48` | Bordo |
| `RSoftBorder` | `#DEDFD7` | `#3B3D3A` | Separatore leggero |
| `RText` | `#272A28` | `#F0F0EB` | Testo principale |
| `RMuted` | `#5C615C` | `#C1C5BC` | Testo secondario |
| `RFaint` | `#61665F` | `#ABAFA6` | Istruzioni e placeholder |
| `RPrimary` | `#2D302F` | `#ECEBE6` | Azione primaria |
| `RPrimaryHover` | `#414541` | `#FFFFFF` | Hover primario |
| `RPrimaryPressed` | `#212422` | `#D6D4CD` | Pressed primario |
| `RPrimaryForeground` | `#FFFFFF` | `#242624` | Testo primario |
| `RFocus` | `#647164` | `#C0CABD` | Focus |
| `RSuccess` | `#3F765C` | `#8ABAA2` | Successo |
| `RWarning` | `#8A572B` | `#E4BB8E` | Avviso |
| `RDanger` | `#A13F3D` | `#F0A29B` | Errore/azione distruttiva |

Gli alias `RHero*` e `RAccent*` mantengono coerenti i dialoghi esistenti.

## Metriche e gerarchia

Main e picker usano `Window.compact`: testo base e pulsanti 13 DIP, label 12 DIP, contenuto editor 14 DIP. Pulsanti alti almeno 32 DIP con angoli 6; campi almeno 34 DIP; righe lista almeno 28 DIP con indicatore laterale da 2 DIP. I dialoghi mantengono metriche proprie, con pulsanti base da almeno 36 DIP salvo override.

Il nome dello snippet precede i metadati; il trigger è un chip su riga distinta. Le dimensioni delle finestre sono nella [guida GUI](GUI.md). Evitare bordi doppi e decorazioni che competono con il contenuto.

## Stati dei controlli

- `Button` e `ListBoxItem` hanno template posseduti che seguono background, foreground e bordo. I testi annidati devono ereditare il foreground, inclusi hover/pressed/disabled.
- Selezione e focus hanno indicatori distinti; la selezione resta visibile durante hover. Il bordo fornisce il focus nei template posseduti.
- I `TextBox` definiscono testo reale, caret, placeholder normale/focus/hover e disabled dentro il template Fluent. Il solo foreground del controllo non basta.
- I controlli disabilitati devono restare riconoscibili; evitare etichette o icone con colori fissi che ignorano lo stato.
- I pulsanti hanno transizioni cromatiche da 120 ms senza spostare il layout. Non aggiungere animazioni alla digitazione o al cambio di selezione.

## Verifica

[Render e limiti](GUI.md), [checklist manuale](GUI_VISUAL_ACCEPTANCE.md), [contrasti dei token](../data/UI_PREVIEW9_CONTRAST.csv). Il CSV riguarda colori pieni; non certifica l'accessibilità di ogni controllo o stato inattivo. Dopo cambi a risorse/template verificare entrambi i temi, focus e minimi in cinque lingue.
