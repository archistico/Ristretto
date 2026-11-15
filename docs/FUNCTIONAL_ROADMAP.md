# Quadro funzionale F0–F6

Questo documento è una mappa delle aree tecniche. Priorità, attività e criteri di chiusura sono soltanto nella [roadmap R1–R8](ROADMAP.md). Baseline: **3.9.0-alpha.20.28**; [stato effettivo](CURRENT_STATE.md).

| Area | Contenuto | Stato e lavoro collegato |
| --- | --- | --- |
| F0 | Libreria, SQLite, import/export, backup, picker, tray e hotkey per snippet | Disponibili; base manuale in Blocco note confermata. Nuova form Registra… e matrice desktop in R2; dati e distribuzione in R4 |
| F1 | Riconoscimento e attivazione Unicode | Motore e contratti coperti da test; non equivalgono a un adapter automatico esterno |
| F2 | Preparazione e sostituzione condizionale nei controlli posseduti | Laboratori separati dal prodotto; evidenze storiche native, non supporto generale agli editor |
| F3 | Adapter e orchestrazione esterna | Diagnostica Windows e anteprima senza scrittura; provenienza in R5, prima espansione condizionata in R6. VS Code sospeso in R3 |
| F4 | Variabili, template e form degli snippet | R7; gli snippet esistenti restano letterali. La form di acquisizione della hotkey è già un'altra funzione |
| F5 | Input/output e hotkey macOS/Linux | R8, con qualificazione distinta per sistema e sessione |
| F6 | Stabilità, sicurezza, installazione e rilascio | R1 per strumenti/provenienza/licenze, R2 per flussi manuali, R4 per prodotto Windows, R8 per altri OS |

La baseline automatica corrente è di **870 xUnit e 32 Node**, con integrazione UI headless della form Registra…. La 20.28 aggiunge **21 casi nativi** di registrazione Windows, separati da CI e dal flusso completo Desktop.App. I singoli perimetri e le prove desktop ancora aperte sono nei [rapporti](../reports/README.md).
