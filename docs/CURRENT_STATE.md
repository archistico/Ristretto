# Stato corrente — Ristretto 3.9.0-alpha.20.26

**Aggiornamento documentale:** 15 novembre 2026. La versione del codice resta Alpha 20.26.

## Cosa si può usare

| Funzione | Stato |
| --- | --- |
| Libreria, gruppi, ricerca, SQLite | Disponibili; dati locali |
| Import/export e backup/ripristino | Disponibili; migrazione schema 1→2 e recupero coperti da test sintetici |
| Picker | Ricerca, selezione con frecce, Invio/Inserisci e Copia; flusso base in Blocco note confermato dall'utente |
| Hotkey per snippet Windows | Inserimento diretto senza picker; richiamo base in Blocco note confermato dall'utente dopo Alpha 20.25 |
| Registra hotkey | Implementata in Alpha 20.26; form con display dei tasti, conferma, annullamento e rimozione |
| Temi e lingue | Due temi e cinque lingue; nuove superfici ancora da controllare su DPI e layout reali |
| Profili separati | `--data-dir` e controllo senza GUI disponibili |
| Espansione automatica nelle altre app | **Da sviluppare e qualificare** |
| Hotkey/inserimento nativi Linux e macOS | **Da sviluppare e qualificare** |
| Installer e aggiornamento automatico | **Non disponibili**; la consegna attuale è lo ZIP sorgenti |

L'inserimento manuale usa gli appunti, che rimangono contenenti lo snippet. Il destinatario deve essere identificabile e compatibile; un tentativo inviato non prova da solo che l'editor abbia applicato il testo.

## Evidenze della baseline Alpha 20.26

- Gate CI locale e da ZIP estratto in cartella nuova: solution, quattro laboratori e DevTools compilati senza warning.
- **846 xUnit**, **32 Node**, **23 regressioni degli strumenti C#**, **10 controlli CLI** e contratto export .NET→Node passati.
- Restore dalla cache NuGet locale. Audit vulnerabilità online non eseguito.
- Conferma dell'utente: richiamo tramite hotkey funzionante in Blocco note Windows. Versione esatta dell'editor e matrice completa dei casi non fornite.
- Nuova form Registra…: **26 nuovi casi** sullo stato di acquisizione e sul coordinatore, senza aprire il backend grafico o registrare hotkey reali.

[Risultati Alpha 20.26](../reports/ALPHA20_26_CHECKS.md) · [rapporto strutturato](../reports/ALPHA20_26_VALIDATION.json).

La presente revisione modifica i documenti: non attribuisce nuove esecuzioni a questi test. [Verifica documentale](../reports/DOCS_ROADMAP_REVIEW.md).

## Verifiche ancora aperte

1. Nuova form sul desktop: focus, tasti mostrati, conferma/rimozione, annullamento e sospensione/ripristino delle registrazioni.
2. Conflitti fra snippet, picker, altre app e profili; disabilitazione, uscita, sessioni prolungate e appunti/focus concorrenti.
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
