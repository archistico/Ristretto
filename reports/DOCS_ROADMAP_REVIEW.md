# Revisione README, documentazione e roadmap

**Data:** 15 novembre 2026 · **Codice di riferimento:** 3.9.0-alpha.20.26.

Revisione dei soli documenti: nessuna modifica alla versione del codice, alla GUI o ai test applicativi. Il README segue l'ordine richiesto: titolo, screenshot, descrizione, funzionamento, installazione, aggiornamento e riferimenti. Aggiunta la guida ai dati e al recupero; roadmap R1–R8 con **48 attività numerate**, dipendenze e criteri di chiusura. Predisposti **27 casi manuali**, tutti NOT_RUN, per la qualificazione Windows.

Le immagini sono anteprime Preview 9 della Alpha 7, viste e identificate prima dell'inserimento nel README. Sono indicate come storiche; non è stata generata né dichiarata una nuova acquisizione del desktop.

**Baseline conservata:** [validazione Alpha 20.26](ALPHA20_26_VALIDATION.json), con 846 xUnit, 32 Node e CI da ZIP estratto. Questi test applicativi non vengono presentati come rieseguiti nella revisione documentale. Conferma utente del richiamo base in Blocco note separata dalla nuova form ancora da qualificare.

**Verifica della revisione: GATE SOURCE PASS.** Audit C#, 23 regressioni degli strumenti, 19 XML, cinque localizzazioni, link Markdown e DDL SQLite passati. Verificati ordine delle sezioni del README e unicità degli ID della roadmap. Hash dei sorgenti funzionali uguale alla baseline: `a155c81350f7fd12c944da79464a98dbab6ad4f59d9573937307a21da13385a5`.

[Rapporto del gate](DOCS_ROADMAP_GATE.json) · [log](docs-roadmap-review.log). Nessuna nuova build della solution o esecuzione della suite xUnit applicativa, di Node, della GUI o delle prove native/di espansione automatica. Il collegamento per il download SDK è stato verificato sulla pagina ufficiale .NET 10.

**Pacchetto documentale:** manifest di 497 hash verificato prima della compilazione dello strumento in una cartella nuova, inizialmente senza `bin`/`obj`. GATE SOURCE passato anche dall'estrazione, con hash funzionale identico alla baseline. La compilazione dello strumento usa la cache NuGet locale; le prove applicative restano quelle già conservate. Gli aggiornamenti conclusivi al rapporto non cambiano il codice validato.
