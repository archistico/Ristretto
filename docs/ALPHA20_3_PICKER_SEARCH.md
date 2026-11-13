# Alpha 20.3 — ricerca picker: indice e confini Unicode

## Problemi riprodotti

Il prefisso del contenuto era tagliato con `[..4096]`. Un'emoji che attraversava quel confine diventava un surrogate incompleto e `Normalize` sollevava `ArgumentException`, interrompendo la ricerca. Anche una query con un surrogate isolato poteva provocare la stessa eccezione. Il taglio prima di un accento combinante produceva invece un risultato parziale non corrispondente al carattere completo.

Prima della correzione sono falliti quattro nuovi casi: coppia surrogate al confine, accento combinante al confine, query con surrogate alto e query con surrogate basso. Tutti ora passano.

## Comportamento e indice

- Il prefisso indicizzato comprende **solo grafemi completi**, fino a 4.096 unità UTF-16 del testo originale. Un'emoji composta, una bandiera o un carattere con accenti combinanti che attraversa il limite viene escluso interamente. Se un unico grafema supera il limite, il prefisso è vuoto e gli altri campi restano ricercabili.
- Una query Unicode incompleta restituisce zero risultati; una query successiva valida riutilizza lo stesso indice. Il carattere sostitutivo Unicode `�` rimane valido.
- Titolo, trigger, descrizione e prefisso sono normalizzati una volta alla creazione di `PickerSearchIndex`. Il ViewModel conserva l'indice e lo ricrea quando ricarica la libreria; non normalizza nuovamente tutti i campi a ogni query.
- Un campo indicizzato non normalizzabile esclude quello snippet dall'indice, senza interrompere la ricerca degli altri snippet. Non si riparano o modificano i dati salvati; il suffisso del contenuto oltre il prefisso non viene indicizzato né validato da questo controllo.
- I risultati mantengono **testi originali** per visualizzazione, Copia e Inserisci. Normalizzare la ricerca non riscrive accenti, emoji o contenuto nella libreria.

Le priorità restano: trigger esatto, prefisso trigger, prefisso titolo, testo nel titolo, testo nel trigger, descrizione, prefisso contenuto. L'ordine a parità di priorità rimane titolo senza distinzione maiuscole/minuscole, trigger ordinale, ID. Restano esclusi snippet disabilitati, gruppi disabilitati e snippet senza gruppo.

L'indice è una proiezione immutabile della snapshot, con la relativa revisione. Ordina i criteri secondari una volta; ogni ricerca conserva al massimo `limit` risultati per ciascuna delle sette priorità, anziché ordinare nuovamente tutti i risultati. Il limite pubblico resta 1–100, default 50. `PickerSearch.Find` rimane disponibile per chiamate singole e costruisce un indice per quella chiamata; il picker usa direttamente l'indice conservato.

## Verifiche eseguite

**506/506 xUnit** passati, inclusi **30 casi di ricerca** (22 nuovi rispetto alla baseline di 484 test). Coprono ranking e limiti, ordine stabile, proiezione indipendente dalle liste originarie, NFC/NFD senza riscrittura dei risultati, campi malformati, emoji ZWJ e bandiere al confine, accenti al limite esatto, grafema molto lungo e gruppi disabilitati.

Solution e quattro strumenti compilati in Release; restore dalla cache locale NuGet. Audit sorgenti e statici, più sette test Python, superati. Resta il warning preesistente `xUnit2000` nella suite capacità adapter. Nessuna modifica a Core, SQLite, formato export, percorso nativo o adapter VS Code.

Build di solution/quattro tool e 506 xUnit ripetuti con esito positivo da una nuova estrazione dello ZIP priva di `bin`/`obj`, dopo la verifica del manifest SHA-256. I sorgenti compilati corrispondono alla consegna.

Non sono stati avviati programmi grafici, usati appunti reali o inviati tasti. Test nativi/Node precedenti non ripetuti per questo incremento. La latenza percepita, i tempi di costruzione dell'indice e la memoria su librerie grandi **non sono stati misurati**: il cambiamento riduce il lavoro ripetuto per query, senza attestare obiettivi prestazionali della GUI.

[Riepilogo JSON](../reports/PICKER_SEARCH_VALIDATION.json) · [log build e test](../reports/picker-search-validation.log) · [incremento precedente Inserisci](ALPHA20_3_PICKER_SESSION.md).

## Prove sul desktop ancora aperte

Verificare ricerca durante composizione IME e con librerie grandi, selezione mantenuta dopo ricaricamento e integrazione del picker con gli appunti. La [matrice Inserisci](ALPHA20_3_PICKER_INSERT.md) e il [gate host VS Code](ALPHA20_3_VSCODE_HOST_TESTS.md) rimangono separati.
