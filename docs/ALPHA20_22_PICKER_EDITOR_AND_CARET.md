# Rapporto tecnico — Ristretto Alpha 20.22

## Correzione del test segnalato

Il test `ExternalClassicEditTextDeltaTests.InsertionInMiddleAtCaretIsOnlyCandidate` della versione 20.21 costruiva `before = "ab", caret=1` e `after = "a☕b", caret=3`. `☕` occupa **1 unità UTF-16**, perciò un vero inserimento senza spostamento del cursore deve terminare a **2**. Il risultato di produzione `NotInsertionAtCaret` era corretto. La fixture ora usa `caret=2`. Un test distinto verifica che `caret=3` rimanga rifiutato; un secondo controlla il caso di un'emoji di **2 unità UTF-16** con caret finale a 3. Nessun indebolimento della politica di ammissione.

## Picker e layout

Il `ContentEditor` è ora in una riga `*` di `Grid`, con allineamenti `Stretch`, e senza il vecchio `Height=225` né `MaxWidth=830`. La barra inferiore rimane separata. La selezione del picker segue `MoveSelection` (frecce su/giù) e `InsertForEnterAsync` (riga selezionata o prima se assente). Il tasto Invio continua a essere gestito al rilascio, con la protezione contro repeat e il percorso monouso `InsertSelectedAsync`.

## Test da eseguire

- Correzione offset caret nel centro del documento, rifiuto caret spostato, emoji surrogate (xUnit).
- Frecce su/giù, bordi dell'elenco, primo fallback, selezione durante copia e inserimento selezionato (xUnit).
- Audit statici per layout reattivo e presenza della gestione frecce.
- Verifica Windows reale: ridimensionamento e inserimento del secondo risultato con **Freccia Giù + Invio**.

## Limiti

I controlli disponibili hanno verificato 19 file XML/XAML/risorse validi, 5 lingue con 120 chiavi, 731 collegamenti locali e 32 test Node superati. Possono verificare riferimenti testuali e struttura dell'archivio, **non** sostituiscono la compilazione .NET né l'esecuzione degli xUnit o il rendering Avalonia. La 20.20 è l'ultima baseline con build e test confermati. La 20.21 ha 742/743 test superati; i test della 20.22 sono ancora da eseguire su Windows.
