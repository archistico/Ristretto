# Verifiche Alpha 20.27

**Versione:** 3.9.0-alpha.20.27 · **Data:** 15 novembre 2026.

- **GATE CI: PASS** nella cartella di lavoro: solution, quattro laboratori e DevTools compilati senza warning; 870 xUnit, 32 Node, 23 regressioni degli strumenti C#, dieci CLI e contratto export .NET→Node passati.
- **24 nuovi casi:** 22 di integrazione Avalonia headless e due sullo stato. Il filtro della cattura passa 50/50.
- Riprodotti prima della correzione: perdita della combinazione con Shift+Tab (un FAIL su tre casi iniziali) e pulsanti fuori dal bordo in francese/tedesco (due FAIL sui 44 casi della suite intermedia). Evidenze incluse nel log.
- Test UI con risorse del prodotto e SQLite sintetico; nessuna finestra del sistema operativo, hotkey globale o accesso al profilo personale. Registrazioni native e tastiera fisica: **NOT_RUN**.

## Restore e ambiente

Windows, .NET SDK 10.0.201, runtime 10.0.5, Node 24.20.0. Restore .NET online bloccato da NU1301/TLS del sistema. I due pacchetti headless 11.3.22 mancanti sono stati scaricati dall'endpoint ufficiale NuGet con HTTPS e verifica normale del certificato tramite Node; restore da feed locale e cache con verifica NuGet delle firme invariata. Nessuna disattivazione TLS o delle firme. Node non diventa un requisito del gate CORE.

Audit vulnerabilità online, host VS Code, DPI/desktop e input nativo restano **NOT_RUN**. Il PASS headless non sostituisce la matrice manuale Windows. La conferma utente del richiamo in Blocco note della 20.25 resta un'evidenza distinta.

## Consegna

**GATE CI: PASS anche dallo ZIP estratto in cartella nuova**, inizialmente priva di `bin`/`obj`. Ripetuti 870 xUnit, 32 Node, 23 regressioni C#, dieci CLI e contratto .NET→Node; build senza warning. Verificati tutti i **503 hash** del manifest. Digest dei sorgenti compilati identico fra cartella di lavoro ed estrazione: `304d9c5f25e2585df587c2d282b5698a9b4cafb0d3ab39a2de7bd7097f3bbff8`.

L'archivio completo contiene **504 file**, incluso il manifest. Rapporti e documentazione sono stati completati dopo il rebuild e l'archivio rigenerato con verifica di tutti gli hash; i sorgenti compilati rimangono invariati. Nessun pacchetto NuGet/cache o output di compilazione è incluso.

[Esiti strutturati e hash](ALPHA20_27_VALIDATION.json) · [log](alpha20-27-validation.log) · [correzioni e perimetro dei test](../docs/ALPHA20_27_CAPTURE_INTEGRATION.md).
