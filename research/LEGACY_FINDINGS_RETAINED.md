# Risultati precedenti conservati senza cartelle storiche

Dalla v1.9 non esistono più cartelle di copie storiche nel pacchetto. Questa nota conserva le **conclusioni necessarie** degli studi v1.0–v1.4, senza replicare PDF, versioni datate o screenshot originali. I documenti canonici contengono i requisiti correnti; le ricerche sui singoli concorrenti restano in `research/competitors/`. La vecchia documentazione storica non è più inclusa nel dossier, come richiesto.

## Sintesi delle lezioni iniziali

1. **v1.0**: fattibilità C#/.NET/Avalonia positiva per GUI e modello; la cattura globale e l'output in app terze dipendono da OS. Wayland non garantito.
2. **v1.1**: catalogate 23 voci GUI (finestre, dialog e componenti): lista gruppi/snippet, editor, picker, tray, preferenze, import/export, backup. Priorità a leggibilità, temi chiaro/scuro, multilingua.
3. **v1.2**: identificati InputManager, ComboManager, ComboList, variabili, ClipboardManager, preferenze e rischio singleton/Qt nel core.
4. **v1.3**: studiato piano di espansione, conflitti casuali, clipboard dopo delay, riposizionamento cursore problematico con grafemi, script PowerShell facoltativi; necessaria una pipeline preflight e fallback esplicito.
5. **v1.4**: nove concorrenti analizzati in dettaglio e introdotti criteri Unicode, IME, NFD/NFC, ZWJ emoji, RTL, clipboard, librerie condivise e automazioni da differire.

## Test storici ancora utili

La vecchia matrice pipeline (63 scenari di progettazione) e la matrice Unicode (54 scenari) sono state convertite in `data/HISTORICAL_TEST_SCENARIOS.csv`, con ID conservati, fonte d'origine e stato `NOT_RUN`. Non sommare automaticamente questa matrice con le più recenti: possono esserci scenari duplicati o equivalenti. Le prove native e persistence aggiuntive restano nei CSV propri.

## Fonti di benchmark

Le 36 fonti primarie del primo benchmark sono consolidate in `research/BENCHMARK_SOURCE_REGISTER.md`, insieme alle note di affidabilità. Le schede individuali e l'indice dei concorrenti continuano a essere mantenuti.

## Interfaccia

L'atlante HTML e i concept superati sono stati rimossi dal progetto corrente nel riordino del 13 novembre 2026 e spostati nella cache locale di recupero, esclusa dal pacchetto. Le indicazioni utili sono consolidate nella [guida GUI corrente](../docs/GUI.md) e nel [design system](../docs/DESIGN_SYSTEM.md); le [anteprime definitive](../design/README.md) sono render Avalonia con dati sintetici, non screenshot dell'app originale.
