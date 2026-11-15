# Verifiche Alpha 20.29

**Versione:** 3.9.0-alpha.20.29 · **Data:** 15 novembre 2026.

La form Registra hotkey condivide la densità compatta di libreria e picker: testo 13 DIP, titolo 16, note 11, pulsanti 32, campo 34, colori e focus dal tema. Indicazioni accorciate nelle cinque lingue e duplicazione iniziale rimossa. Nessuna modifica al riconoscimento o all'inserimento delle hotkey.

## Prove visuali

Generati **80 render Skia offscreen**, usando controlli, risorse e font del prodotto: cinque lingue, due temi, due larghezze e quattro stati. Azioni contenute nell'area client in tutti i render; selezione di anteprime chiaro/scuro e traduzioni lunghe ispezionata. Il client italiano normale misura 500×194 DIP; display e testo base sono entrambi da 13 DIP. Anteprime prima/dopo prodotte durante la lavorazione; i due render italiani pronti della versione finale sono inclusi nel pacchetto.

Nessuna finestra del sistema operativo o accesso a database, appunti e registrazioni native durante il rendering. La nuova modalità `--hotkey-only` è separata dal renderer storico delle altre finestre. Resa desktop, tastiera fisica e DPI: **NOT_RUN**.

## Test e consegna

**GATE CI: PASS** nella cartella di lavoro e da ZIP estratto in cartella nuova: **870 xUnit, 32 Node, 23 regressioni C#, dieci CLI e contratto .NET→Node passati**. Inclusi i 50 casi esistenti della cattura; nessun nuovo caso xUnit. Solution, quattro laboratori e DevTools compilati senza warning.

Ripetuti anche gli **80 render** con il renderer ricompilato dall'estrazione. Le due anteprime incluse coincidono byte per byte con quelle rigenerate. Verificati tutti i **518 hash** del manifest e assenza iniziale di `bin`/`obj`. Sorgenti compilati identici fra le due cartelle: `039cafe3fa39d1846d0d4e045c49a14404ce23b1634a749edf548c247d26de5d`.

Il pacchetto completo contiene **519 file**, incluso il manifest. Rapporti e documentazione sono completati dopo la prova e l'archivio rigenerato verificando tutti gli hash; i sorgenti compilati rimangono invariati.

I 21 casi nativi della 20.28 sono evidenza storica e non vengono conteggiati come rieseguiti per questa modifica visuale. Non sono stati aggiunti test unitari per replicare i valori di stile; si riutilizzano i test di comportamento e disposizione già esistenti. Restore da cache/feed locale; audit vulnerabilità online non eseguito.

[Rapporto strutturato](ALPHA20_29_VALIDATION.json) · [misure degli 80 render](alpha20-29-ui-review.json) · [log](alpha20-29-validation.log) · [anteprime e metriche](../docs/ALPHA20_29_HOTKEY_STYLE.md).
