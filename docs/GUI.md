# GUI corrente — Preview 9

Interfaccia introdotta nella alpha.6, aggiornata nella **3.9.0-alpha.20.22** con area testo reattiva e Invio sul risultato selezionato nel picker. Tema chiaro caldo, scuro grafite, gerarchia centrata sul contenuto e dimensioni compatte. Restano le [verifiche manuali](GUI_VISUAL_ACCEPTANCE.md).

## Superfici e comportamento

- **Alpha 20.25:** campo facoltativo hotkey nell'editor, con stato di registrazione e istruzioni in cinque lingue. Il testo rimane nell'area flessibile. Picker invariato; rendering del nuovo campo ancora da qualificare. [Uso](ALPHA20_25_SNIPPET_HOTKEYS.md).
- **Finestra principale:** gruppi, ricerca/elenco snippet ed editor. Titolo dello snippet in primo piano, trigger su chip sotto il nome, stato contenuto nella larghezza disponibile. Gestisce bozze non salvate, import/export, backup/ripristino, lingua e tema.
- **Picker:** ricerca locale con [indice riutilizzabile e confini Unicode](ALPHA20_3_PICKER_SEARCH.md), selezione da tastiera, [Invio per inserire il risultato selezionato](ALPHA20_22_PICKER_EDITOR_AND_CARET.md) ed Esc per chiudere. Copia è sul relativo pulsante; Inserisci usa la selezione. Hotkey Windows configurabile; prova positiva dell’inserimento manuale in Notepad sintetico, altri editor ancora da qualificare.
- **Caricamento picker:** messaggio localizzato durante l'attesa; Copia e Inserisci disabilitati fino ai risultati correnti. Indice costruito su worker e letture superate annullate; [verifiche del ViewModel](ALPHA20_3_PICKER_LOADING.md) senza avvio grafico.
- **Comandi appunti picker:** una sola operazione Copia/Inserisci alla volta, con entrambi i pulsanti disabilitati durante l'attesa. Testo fissato al clic e messaggi associati alla sessione/selezione corretta; [verifiche](ALPHA20_3_PICKER_CLIPBOARD.md) senza appunti reali.
- **Import review e dialoghi:** selezione dei singoli elementi, avvisi, conferme e gestione delle bozze. Mantengono metriche proprie e condividono i token cromatici.
- **Tray:** accesso a finestra, picker e uscita; il prodotto resta privo di cattura globale dell'input.

Dimensioni in DIP: principale **980×640**, minimo **800×510**; picker **600×390**, minimo **500×365**. La sidebar principale parte da 226 DIP; l'editor mantiene lo spazio flessibile. [Token e metriche](DESIGN_SYSTEM.md).

## Anteprime definitive

Unico riferimento grafico: [design/current](../design/current/). Le immagini sono state spostate senza rigenerarle; vecchi screenshot, concept HTML e iterazioni intermedie sono stati rimossi.

I render conservati precedono il pulsante Inserisci e gli incrementi di ricerca/ciclo di vita del picker; non sono stati rigenerati per queste modifiche.

| Vista | Chiaro | Scuro |
|---|---|---|
| Libreria | [Main](../design/current/main-light-it.png) | [Main](../design/current/main-dark-it.png) |
| Picker | [Picker](../design/current/picker-light-it.png) | [Picker](../design/current/picker-dark-it.png) |
| Minimo, tedesco | [Main](../design/current/main-min-light-de.png) | [Main](../design/current/main-min-dark-de.png) |
| Stati controlli | [Controlli](../design/current/controls-light.png) | [Controlli](../design/current/controls-dark.png) |

`Ristretto.UiReview` carica gli XAML reali con Avalonia/Skia e backend desktop. Usa esclusivamente fixture sintetiche in memoria; non apre la libreria personale, il tray o la clipboard. Produce **32 PNG**: main/picker IT/EN/FR/DE/ES in due temi, minimi IT/DE, picker vuoto e tavole controlli. Controlla i limiti dei pulsanti applicativi visibili. [Registro originale](../design/current/review.json).

Sono render dei client a **96 DPI**, senza cornice nativa. Le pseudo-classi nelle tavole illustrano hover/pressed/focus; non dimostrano input reale. I contrasti dei token hanno **72 coppie passate**, minimo testuale **4,664:1** nel [CSV](../data/UI_PREVIEW9_CONTRAST.csv); compositing e accessibilità completa richiedono prove ulteriori.

## Riprodurre una revisione

Da una sessione desktop, nella radice del progetto:

```powershell
dotnet run -c Release --project tools/Ristretto.UiReview/Ristretto.UiReview.csproj -- --output .local-cache/ui-review-nuova
```

Usare una cartella nuova. Il tool apre brevemente finestre proprie e termina. I render di lavoro restano nella cache esclusa dai pacchetti; sostituire il riferimento corrente dopo la revisione. Lo script di validazione compila il tool senza eseguirlo.

## Evoluzione

Le prime preview hanno sperimentato palette e geometrie poi superate. La Preview 6 ha centralizzato token e template; le Preview 7–8 hanno reso compatte le finestre; la Preview 9 ha aggiornato palette, tipografia, chip dei trigger, focus e stati dei campi. La cronologia resta nel [changelog](CHANGELOG.md). Gli audit delle singole preview sono stati consolidati in questa guida, nel design system e nella checklist visuale.

Il riordino non cambia XAML, ViewModel, code-behind, traduzioni o comportamento applicativo. Per la validazione funzionale corrente consultare [reports](../reports/README.md).
