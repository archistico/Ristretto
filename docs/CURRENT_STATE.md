# Stato corrente — Ristretto 3.9.0-alpha.20.28

**Aggiornamento:** 15 novembre 2026. Alpha 20.28 aggiunge la verifica delle registrazioni Windows reali e conserva il comportamento della GUI 20.27.

## Cosa si può usare

| Funzione | Stato |
| --- | --- |
| Libreria, gruppi, ricerca, SQLite | Disponibili; dati locali |
| Import/export e backup/ripristino | Disponibili; migrazione schema 1→2 e recupero coperti da test sintetici |
| Picker | Ricerca, selezione con frecce, Invio/Inserisci e Copia; flusso base in Blocco note confermato dall'utente |
| Hotkey per snippet Windows | Inserimento diretto senza picker; richiamo confermato nuovamente dall'utente dopo la 20.27; 21 casi nativi sulle registrazioni passati |
| Registra hotkey | Display dei tasti, conferma, annullamento e rimozione; flusso UI/SQLite provato automaticamente in 20.27 |
| Temi e lingue | Due temi e cinque lingue; nuove superfici ancora da controllare su DPI e layout reali |
| Profili separati | `--data-dir` e controllo senza GUI disponibili |
| Espansione automatica nelle altre app | **Da sviluppare e qualificare** |
| Hotkey/inserimento nativi Linux e macOS | **Da sviluppare e qualificare** |
| Installer e aggiornamento automatico | **Non disponibili**; la consegna attuale è lo ZIP sorgenti |

L'inserimento manuale usa gli appunti, che rimangono contenenti lo snippet. Il destinatario deve essere identificabile e compatibile; un tentativo inviato non prova da solo che l'editor abbia applicato il testo.

## Evidenze della baseline Alpha 20.28

- Gate CI locale e da ZIP nuovo: solution, quattro laboratori e DevTools compilati senza warning; 510 hash verificati, assenza iniziale di `bin`/`obj` e sorgenti compilati identici. Anche il laboratorio nativo passa 21/21 dall'estrazione.
- **870 xUnit**, **32 Node**, **23 regressioni degli strumenti C#**, **10 controlli CLI** e contratto export .NET→Node passati.
- Restore da cache/feed locale già disponibile. Audit vulnerabilità online non eseguito.
- Conferma dell'utente dopo la 20.27: «tramite hotkey funziona», senza editor/versione/combinazione specificati. Conservata anche la precedente conferma relativa a Blocco note.
- **21 casi nativi PASS** su Windows 10 build 19045: worker e coordinatore del prodotto, catalogo sintetico e collisioni fra processi. Nessuna callback osservata. FULL non eseguito; il rapporto nativo riguarda il nuovo laboratorio.
- Registra…: ai 26 casi di stato/coordinamento della 20.26 si aggiungono **24 casi** nella 20.27: 22 UI headless e due di stato. Verificati dialogo, navigazione, azioni, libreria, SQLite e disposizione in cinque lingue/due temi; nessuna registrazione nativa.
- Corretti la perdita della combinazione con Shift+Tab e lo spazio insufficiente per le azioni in francese/tedesco.

[Risultati Alpha 20.28](../reports/ALPHA20_28_CHECKS.md) · [rapporto strutturato](../reports/ALPHA20_28_VALIDATION.json) · [registrazioni native](ALPHA20_28_NATIVE_HOTKEY_REGISTRATIONS.md) · [perimetro UI headless](ALPHA20_27_CAPTURE_INTEGRATION.md).

## Verifiche ancora aperte

1. Form sul desktop: tastiera fisica, focus del sistema operativo, resa/DPI e orchestrazione di Desktop.App; le prove headless e dei worker non chiudono questi casi.
2. Flussi completi di conflitto e modifica tramite UI/save/import, notifiche pendenti, profili, sessioni prolungate e appunti/focus concorrenti. Le ricevute native dei worker sono già coperte.
3. Novità UI della 20.22: area testo ridimensionabile e scelta del risultato tramite frecce+Invio su una matrice desktop ripetibile.
4. Aggiornamento completo, recupero dopo interruzioni, accessibilità e nuove immagini del prodotto.
5. Audit delle dipendenze, licenza, pacchetto binario e pubblicazione qualificata.

Le anteprime in `design/current` sono della **GUI Preview 9, Alpha 7**. Non documentano i pulsanti e le scorciatoie introdotti successivamente.

## R5 e obiettivo automatico

Sono implementati confronti Unicode, lettura di un controllo Edit esterno, osservazione WinEvent e anteprima di trigger sintetici. La suite compila e i test passano. Le prove live dei nuovi osservatori restano separate e non eseguite in questa baseline.

Un `PotentialTrigger` non dimostra digitazione: lo stesso testo può provenire da paste, IME o codice. L'anteprima non autorizza scritture. R5 deve decidere quali garanzie offre un target; R6 potrà introdurre una sostituzione automatica soltanto dopo un esito positivo.

L'adapter VS Code resta un esperimento manuale da export JSON. I test Node e del contratto sono passati; il vero host rimane non qualificato e il ramo R3 è sospeso.

## Prossimo lavoro

**Prima priorità: R2.01, R2.08 e R2.09**, con correzioni R2.07 se emergono difetti: qualificare la form e le hotkey attuali. In parallelo possono avanzare dati/distribuzione R4 e gli esperimenti di provenienza R5.

[Roadmap dettagliata](ROADMAP.md) · [Installazione e aggiornamento](INSTALLATION_AND_UPDATE.md) · [Build e test](BUILD_AND_TEST.md) · [Handoff](HANDOFF.md).
