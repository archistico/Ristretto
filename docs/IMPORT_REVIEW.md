# Revisione degli import esterni — Vertical Slice v3.3

**Stato:** codice scritto, **non compilato** e **test xUnit non eseguiti** in questo ambiente (SDK .NET assente). La review v3.3 è la prima versione selettiva; non è un convertitore semantico completo.

## Obiettivo

Ridurre importazioni accidentali, visibilità parziale degli avvisi e conflitti quando l'utente migra da Beeftext, Espanso oppure dal JSON portabile interno. La libreria resta in SQLite, con backup nativo del DB; import/export JSON sono strumenti di interoperabilità.

## Modello della preview

`ExternalImportPreview` contiene il documento validato, la revisione SQLite osservata e una `ImportCandidate` per ogni snippet convertibile. Ogni candidata espone ID interno della preview, titolo, trigger, gruppo, stato di abilitazione dopo import e bitmask `ImportItemRisk`:

| Flag | Significato | Preselezionato? |
| --- | --- | --- |
| `None` | Nessun problema identificato da questa versione | Sì |
| `TriggerCollision` | Trigger NFC già nel DB oppure duplicato nel file | No |
| `DynamicContent` | Variabili, form, placeholder o contenuto storico speciale | No |
| `UnsupportedOptions` | Comportamento originale non rappresentato dal Core attuale | No |

Le flags possono combinarsi. **Un elemento non segnalato non è necessariamente semanticamente equivalente all'originale**; servono test reali prima dell'abilitazione. Trigger dalla stessa origine che collidono sono contrassegnati **tutti**, non solo il secondo. Non vengono registrati log con il contenuto degli snippet.

## GUI

- Dialog ridimensionabile con filtro per titolo, trigger e gruppo.
- 60 righe per pagina, così la GUI non crea migliaia di controlli contemporaneamente; selezione mantenuta attraverso pagine/filtri.
- Selezione iniziale prudente; pulsanti `Selezione sicura`, `Seleziona pagina`, `Deseleziona pagina`, navigazione precedente/successiva.
- Per ogni riga: titolo, trigger, gruppo e motivi degli avvisi. Il contenuto non viene eseguito né mostrato nel riepilogo.
- Azione di conferma disabilitata per selezione vuota. La scelta di includere un elemento rischioso è esplicita.
- Nuove risorse localizzate nelle cinque lingue dell'interfaccia; il contenuto importato non viene mai tradotto.

## Commit e atomicità

1. All'ingresso vengono validate selezione (ID univoci, appartenenti alla preview) e documento portabile.
2. Viene creato un nuovo documento contenente **solo** gli snippet selezionati e i loro gruppi di riferimento.
3. `SqliteLibraryStore.ImportPortableAsync` riceve `expectedLibraryRevision`; **all'interno della transazione** verifica che `app_meta.library_revision` corrisponda. In caso contrario solleva `ConcurrencyException` e non inserisce dati.
4. Inserimento additivo con GUID nuovi e suffissi per nomi di gruppo già presenti, come nella v3.1.
5. Commit e incremento revisione in un'unica transazione; confermare due volte la stessa preview non è permesso.
6. L'esportazione/importazione nativa precedente continua a funzionare senza obbligo di revisione, quando chiamata dal servizio portabile dedicato. L'API di preview richiede la revisione.

## Limiti residui

- Nessun diff campo-per-campo né modifica dei valori prima dell'import; la risoluzione delle collisioni avviene **escludendo** o **includendo consapevolmente** uno snippet, non riscrivendo automaticamente il suo trigger.
- La selezione della preview è in memoria; non è persistita tra sessioni.
- La revisione è globale: una modifica *non correlata* alla libreria impone comunque una nuova preview, per evitare corse difficili da diagnosticare.
- La gestione di segnali di testo sensibile, la conversione delle variabili e l'interpretazione dei form rimangono fuori ambito. Le istruzioni native esterne sono importate come testo disattivato.
- La sicurezza del parser YAML e la responsività su librerie molto grandi necessitano di prove runtime e fuzz test.
- Niente adattatore globale Windows/macOS/Linux ancora attivo.

## Verifiche da svolgere

Le prove predisposte in `tests/TextExpander.Tests/ImportReviewTests.cs` verificano: collisioni intra-file e col DB con NFC; scelta parziale e import dei soli gruppi riferiti; selezioni vuote o non valide; preview obsoleta; seconda conferma della stessa preview; contenuti dinamici Espanso. Eseguire xUnit e controllare manualmente la GUI in tutte le lingue prima di considerare concluso il gate di qualità.
