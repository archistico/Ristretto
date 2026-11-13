# Piano riproducibilità, submodule, licenze e SBOM — v2.0

**Stato:** piano di verifica, **non eseguito**. Lo scopo è fissare un percorso ripetibile senza copiare in dossier file sorgente o binari di terzi.

## Gate G1 — Identificare esattamente la sorgente

Sul PC con Git e rete disponibili:

```bash
git clone --recursive https://github.com/xmichelo/Beeftext.git
cd Beeftext
git fetch --tags --prune
git checkout --detach v16.0
git rev-parse HEAD
git status --porcelain=v1
git submodule status --recursive
git ls-files > audit-tracked-files.txt
git log -1 --format='%H %cI %s'
```

**Atteso:** commit SHA completo, stato pulito, hash dei due submodule e inventory. Annotare sistema operativo, Git version e toolchain; non depositare `audit-tracked-files.txt` come elenco completo verificato prima dell'esecuzione. L'esame corrente di `master` NON è pinned, quindi i dati possono divergere dalla v16.0.

## Gate G2 — Inventario terze parti

- Enumerare `.gitmodules`, cartelle `Vendor`, `Installer/Resources`, Qt DLL/plugin, `emojilib`, XMiLib, OpenSSL, icone, font e asset della GUI.
- Separare **codice sorgente**, **dipendenza build-time**, **binary runtime**, **asset grafico**, **strumento di packaging**.
- Per ciascun componente registrare `nome`, `versione`, `source`, `commit`/digest, `licenza`, `redistribution`, `file NOTICE`, `usage`, `risk`.
- Generare SBOM reale *dopo* il checkout/build usando strumenti adatti; produrre SPDX o CycloneDX e confrontarlo con gli artefatti distribuiti.
- Non presumere che licenza MIT del repository copra automaticamente tutte le risorse di terzi; l'uso di Qt LGPLv3 comporta obblighi specifici per la distribuzione dei binari. Richiedere verifica legale prima di distribuire artefatti derivati.

## Gate G3 — Build Windows originale riproducibile

- Fissare versione Windows, VS2022 Build Tools/Community, MSVC, Qt 6 e Qt Visual Studio Tools.
- Confermare `CMakeLists.txt`, `Beeftext.pro`, `.sln`, `BeeftextSolution.pro` e scelta della pipeline autorevole.
- Verificare editor di risorse, traduzioni e copia file `emojis.json`.
- Compilare Debug/Release, registrare warning/errori e hash degli output; testare single-instance e aperture GUI senza dati utente reali.
- Separare successo della **compilazione** da successo del **pacchetto installer**.

## Gate G4 — Installer e aggiornamento

- Test Windows in VM pulita: primo setup, percorso custom, upgrade, downgrade, sessione utente senza admin, UAC, disinstallazione, cleanup e dati utente conservati.
- Test ZIP portable su unità rimovibile, cartella sola lettura, spostamento cartella e presenza installazione normale in parallelo.
- Test updater: offline, timeout, SHA errato, URL sbagliato, redirect, cancel, skip-version, install fallita, verifica dell'eseguibile dopo scaricamento.
- Nessun esperimento sull'installazione reale dell'utente: utilizzare VM/sandbox.

## Gate G5 — Nuova applicazione cross-platform

- Pin dell'SDK .NET via `global.json` e central package management solo se utile; aggiornamenti dipendenze monitorati.
- Builds separate Windows/macOS/Linux con CI; test unit indipendenti dai sistemi OS, test native solo sulle piattaforme idonee.
- Verificare firmatari e pipeline di release, checksum, SBOM, test installazione/disinstallazione e allegati release.
- Mantenere l'updater automatico **non obbligatorio** e subordinato a un ADR di sicurezza.

## Criteri di uscita

Una fase è chiusa soltanto se produce risultato eseguito, ambiente, versioni, output/log, PASS/FAIL/BLOCKED motivato e link al documento canonico. Senza prove, segnare **NOT_RUN**; il piano non equivale a compatibilità dimostrata.
