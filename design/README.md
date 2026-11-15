# Anteprime dell'interfaccia

## Form hotkey — Alpha 20.29

[Tema chiaro](hotkey-capture-20-29/light-it.png) · [tema scuro](hotkey-capture-20-29/dark-it.png). Render aggiornati dei controlli reali con Avalonia/Skia offscreen, font del prodotto e dati sintetici; nessuna finestra nativa o prova manuale. Il comando `UiReview --output <cartella-nuova> --hotkey-only` verifica 80 configurazioni. [Metriche, comando completo e limiti](../docs/ALPHA20_29_HOTKEY_STYLE.md).

## Anteprime storiche — GUI Preview 9

La cartella [current](current/) conserva il set di riferimento della **GUI Preview 9, Alpha 7**: **32 PNG** e il [registro dei render](current/review.json). Il nome della cartella è mantenuto per i collegamenti esistenti; le immagini precedono Inserisci, le hotkey per snippet e Registra… della versione attuale.

Sono render Avalonia/Skia degli XAML di quella baseline a 96 DPI, prodotti con dati sintetici in memoria. Coprono main/picker in cinque lingue, due temi, minimi IT/DE, picker vuoto e stati dei controlli. Non sono screenshot del desktop né prove manuali di tastiera o DPI. La nuova acquisizione è pianificata in R2.06 della [roadmap](../docs/ROADMAP.md).

Vecchi screenshot, concept HTML e iterazioni di revisione sono stati rimossi dal progetto corrente e spostati in `.local-cache/cleanup-recovery`, esclusa dalla distribuzione. La cronologia è nel [changelog](../docs/CHANGELOG.md); comportamento, generazione e limiti nella [guida GUI](../docs/GUI.md). I nuovi render di lavoro vanno in `.local-cache`.
