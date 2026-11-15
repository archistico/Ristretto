# Verifiche Alpha 20.28

**Versione:** 3.9.0-alpha.20.28 · **Data:** 15 novembre 2026.

## Conferma utente

Dopo la consegna 20.27: «tramite hotkey funziona». Non sono stati riportati versione esatta, editor o combinazione. Questa conferma si aggiunge a quella precedente relativa a Blocco note; non chiude tutti i casi della matrice desktop.

## Registrazioni native

**21/21 PASS** su Windows 10, build 19045: worker del prodotto e chiamate Windows reali, collisioni anche fra processi, rilascio normale/terminazione del figlio, picker e coordinatore con pausa/ripristino. Nessuna callback osservata. Il caso finale esegue 20 cicli di Dispose immediato.

Il rapporto preesistente è stato rifiutato con codice 2 senza modificarne i byte; il processo holder ha rifiutato una combinazione fuori dalla fixture. Nessuna finestra, input sintetico, clipboard o profilo personale usato dal nuovo laboratorio.

I casi esercitano servizi/coordinatore e catalogo sintetico, non l'orchestrazione completa di Desktop.App. Tastiera fisica, DPI, notifiche pendenti dell'applicazione e inserimento negli editor esterni restano separati. FULL non eseguito: la prova nativa qui riguarda soltanto il nuovo comando.

## Gate e consegna

**GATE CI: PASS** nella cartella di lavoro e da ZIP estratto in cartella nuova: **870 xUnit, 32 Node, 23 regressioni degli strumenti C#, dieci CLI e contratto .NET→Node passati**. Solution, quattro laboratori e DevTools compilati senza warning. Nessun nuovo caso xUnit: i 21 casi nativi sono conteggiati separatamente.

Anche il laboratorio nativo ricompilato nell'estrazione passa **21/21**, con zero callback. Verificati **510 hash** del manifest e assenza iniziale di `bin`/`obj`. Sorgenti compilati identici: `3f97520d6487c4516351a68e6c16a13b6908d9ad7255334a13066e0be60a062f`.

Il pacchetto completo contiene **511 file**, incluso il manifest. Rapporti e documentazione sono completati dopo la verifica; l'archivio finale viene rigenerato e tutti gli hash ricontrollati. I sorgenti compilati rimangono quelli validati.

Restore da cache/feed NuGet locale già disponibile; audit vulnerabilità online non eseguito. Pipeline C#/.NET 10; Node soltanto per i controlli VS Code del gate CI.

[Rapporto strutturato](ALPHA20_28_VALIDATION.json) · [ricevute native](alpha20-28-native-hotkeys.json) · [log](alpha20-28-validation.log) · [comandi e limiti](../docs/ALPHA20_28_NATIVE_HOTKEY_REGISTRATIONS.md).
