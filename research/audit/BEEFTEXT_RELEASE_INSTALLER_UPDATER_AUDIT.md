# Audit v2.0 — Installer, aggiornamenti, distribuzione e dipendenze Beeftext

**Data:** 13 novembre 2026 · **Metodo:** lettura statica di file specifici su GitHub; senza checkout locale, build o esecuzione. **Oggetto:** Beeftext repository `xmichelo/Beeftext`, branch `master` consultato il 13/11/2026. **Attenzione:** `master` è un riferimento mobile. La release v16.0 è indicata dalla pagina Releases con abbreviazione di commit `1f24a1c`, NON risolta in SHA completo qui. Una precedente indicizzazione DeepWiki è ferma al commit `767614fe...` del maggio 2025; le evidenze estratte da quell'indice non devono essere spacciate per codice corrente.

## 1. Ambito, verifica e classificazione

- **Osservato nel codice:** fatti visibili direttamente nei file pubblici elencati nel registro fonti.
- **Osservato nella documentazione:** dichiarazioni README, wiki, Releases, sito.
- **Rischio ipotetico:** conseguenza possibile, ma non difetto dimostrato.
- **Non verificato:** risultato che richiede checkout pinned, submodule, build e comportamento runtime.

Non è stato possibile eseguire `git ls-remote`: il container non risolve il DNS `github.com`. Le directory di `GitHub/tree` non sono accessibili in questa sessione, mentre alcuni singoli file in `GitHub/blob` sono consultabili. Questo è un **audit parziale del percorso di distribuzione**, non una verifica dell'intero repository.

## 2. Struttura di build e dipendenze

| Evidenza | Osservazione | Implicazione per il nuovo prodotto |
|---|---|---|
| `.gitmodules` | Due submodule: `Submodules/XMiLib` e `Submodules/emojilib` | Non riprendere dipendenze C++ per inerzia; mantenere attribuzioni se si riutilizzano contenuti |
| `CMakeLists.txt` root | Aggiunge `Submodules/XMiLib/XMiLib` e `Beeftext` | La build è composta, non un unico eseguibile isolato |
| `Beeftext/CMakeLists.txt` | Collega Qt6 Core, Gui, Widgets, Network, XMiLib e Winmm; elenca file della GUI, input, snippet e update | È Windows/Qt-centrica. Avalonia non sostituisce gli adapter nativi di tastiera |
| `BeeftextSolution.pro` | Soluzione qmake con dipendenza Beeftext da XMiLib | Esistono più descrizioni di build; non presumere equivalenza senza confronto effettivo |
| README | Richiede VS2022, Qt Visual Studio Tools e NSIS per installer; non offre istruzioni dettagliate di build | La nuova codebase deve includere istruzioni replicabili e CI testata |

Il README dichiara Beeftext sotto MIT, Qt sotto LGPLv3, emojilib sotto MIT e OpenSSL con licenze OpenSSL/SSLeay. Queste dichiarazioni **non sostituiscono** l'inventario di licenze e dei file binari inclusi nella release: resta un gate SBOM e third-party-notices.

## 3. Installer NSIS — fatti osservabili

File `Installer/installer.nsi` (294 linee del sorgente), configurazione Windows:

1. Richiama Visual Studio `devenv.com` per compilare Release x64 usando un percorso locale hard-coded verso Visual Studio 2022 Community; il file dell'installer e la cartella di output sono definiti nello script. **Non è una pipeline riproducibile multipiattaforma.**
2. Dichiara plugin NSIS `ShellExecAsUser` e `NsProcess`; usa l'esecuzione utente standard a conclusione dell'installazione.
3. Il percorso iniziale è sotto `Program Files`; la cartella precedentemente scelta viene letta da una chiave di registro HKCU.
4. Crea valori HKCU relativi a posizione dell'eseguibile/installazione e valori HKLM per la registrazione di disinstallazione.
5. Crea collegamenti Start Menu in contesto `all`.
6. In disinstallazione tenta la chiusura dell'istanza ed esegue `RMDir /r "$INSTDIR"`, oltre a rimuovere alcune chiavi e scorciatoie.

**Rischio da non replicare:** rimozione ricorsiva dell'intera cartella può cancellare file non gestiti dall'installer qualora qualcuno li salvi lì. Non è provato che Beeftext cancelli i dati utente per questo motivo, perché i percorsi dei dati richiedono verifica; per il nostro prodotto imporre install-dir e user-data distinti e disinstallazione con elenco dei file installati. In modalità portabile le regole cambiano, e un'eventuale rimozione deve chiedere esplicitamente cosa conservare.

**Ulteriore rischio potenziale:** il comportamento dipende da permessi e combinazione di chiavi HKCU/HKLM. Non chiamarlo difetto confermato senza test install/uninstall con utenti diversi, upgrade e UAC.

## 4. UpdateManager — comportamento osservato

- `UpdateManager.cpp` imposta un controllo dopo il lancio e un intervallo di 24 ore se l'opzione è abilitata.
- Utilizza worker su thread e segnali Qt; il controllo può essere eseguito manualmente o automaticamente.
- Il manifesto disponibile in `Website/latestVersionInfo.json` per v16.0 contiene `versionMajor`, `versionMinor`, `downloadUrl`, `releaseUrl`, `sha256Sum`, `releaseNotes`.
- In `UpdateDialog.cpp`, la build installata scarica un EXE temporaneo, calcola un hash incrementale e **confronta** il digest con lo SHA-256 del manifesto prima di aprire l'eseguibile. In modalità portable apre invece la pagina di release, senza aggiornare automaticamente i propri file.
- L'utente può rimandare, saltare una specifica versione o annullare l'operazione.

**Correzione rispetto a un'ipotesi di audit preliminare:** Beeftext non si limita a esporre un hash nel manifesto; il file `UpdateDialog.cpp` contiene il controllo esplicito `hashCalculator_.result() != latestVersionInfo_->sha256Hash()`. Non attribuire all'originale l'assenza di una verifica SHA-256.

### 4.1 Miglioramenti progettuali che proponiamo

1. Separare `CheckForUpdate` da `DownloadPackage`, `VerifyPackage` e `LaunchInstaller`.
2. Garantire verifica di **autenticità**, oltre all'integrità: il digest da solo non autentica il produttore se proviene da un manifesto compromesso.
3. Utilizzare HTTPS, policy redirect, URL consentiti, limiti di dimensione, timeout, firma dei pacchetti dove applicabile e downgrade protection.
4. Non aggiornare silenziosamente mentre l'utente sta modificando dati; copia di sicurezza e ripristino se una migrazione schema fallisce.
5. Per la prima release possiamo **non avere alcun updater automatico**: pagina ufficiale download, versione attuale visibile, controlli manuali opzionali e package firmati se disponibili.
6. Non fare telemetria né upload di frammenti digitati; logging privacy-safe e locale.

### 4.2 Incongruenza staticamente osservata nel manifesto

Il file v16.0 usa `versionMajor=16` e `versionMinor=0`, ma l'HTML contenuto in `releaseNotes` dichiara nel tag `<title>` la stringa `Beeftext v9.1 Release Notes`. Probabile residuo redazionale; **non sappiamo se il titolo HTML sia visibile in GUI**. Registrare come difetto editoriale potenziale, non problema di sicurezza.

## 5. Scelte per Ristretto (proposte, non tutte ratificate)

- **Build**: `dotnet restore`, `dotnet test`, `dotnet publish` con versioni .NET e NuGet pin compatibilmente con gli SDK; nessun VS richiesto agli utenti finali.
- **OS**: Windows x64/arm64 secondo supporto effettivo; macOS arm64/x64; Linux x64/arm64 dove adapter e runtime saranno provati. Non promettere automaticamente tutte le combinazioni.
- **Artefatti**: Win ZIP portable + installer (MSI/MSIX/altro da decidere); macOS app bundle e DMG/PKG con firma/notarizzazione quando si distribuisce pubblicamente; Linux tar.gz/AppImage e in seguito pacchetti DEB/RPM secondo sostenibilità. Le opzioni sono candidate, non pipeline implementate.
- **Dati**: profilo utente separato, cartella portable autosufficiente, esportazione e backup espliciti. Migrazioni atomiche e fallback.
- **Licenze**: licenza della nuova codebase da scegliere, licenze delle dipendenze documentate, file `THIRD_PARTY_NOTICES`, BOM SPDX/CycloneDX e controlli vulnerabilità in CI.
- **Distribuzione**: firma codice e release reproducibility goals, attestazione della provenienza, checksum pubblicati; automatic updater fuori dal primo gate funzionale.
- **i18n**: pacchetti traduzioni versionati insieme al programma, nessun download remoto delle lingue obbligatorio.

## 6. Limiti e azioni successive

**Non verificato**: contenuto integrale di `Vendor/`, risorse NSIS, Action workflows, submodule SHA pinned, licenze di ogni asset, build originale eseguibile, comportamento di cleanup durante rollback e notarizzazione. Non inferire l'assenza di CI dal fatto che la cartella `.github` non è consultabile.

La **prossima attività ad alto valore** è verificare una sorgente pinned alla release `v16.0` con `git submodule status --recursive` e `git ls-files` quando una rete Git funzionante sarà disponibile; poi produrre manifest e SBOM effettivi e confrontare `master` con il tag tramite `git diff --stat v16.0..master`.

## Fonti ufficiali

- [Root repository](https://github.com/xmichelo/Beeftext)
- [README](https://github.com/xmichelo/Beeftext/blob/master/README.md)
- [.gitmodules](https://github.com/xmichelo/Beeftext/blob/master/.gitmodules)
- [CMake root](https://github.com/xmichelo/Beeftext/blob/master/CMakeLists.txt)
- [CMake progetto](https://github.com/xmichelo/Beeftext/blob/master/Beeftext/CMakeLists.txt)
- [qmake solution](https://github.com/xmichelo/Beeftext/blob/master/BeeftextSolution.pro)
- [NSIS installer](https://github.com/xmichelo/Beeftext/blob/master/Installer/installer.nsi)
- [UpdateManager](https://github.com/xmichelo/Beeftext/blob/master/Beeftext/Update/UpdateManager.cpp)
- [UpdateCheckWorker](https://github.com/xmichelo/Beeftext/blob/master/Beeftext/Update/UpdateCheckWorker.cpp)
- [UpdateDialog](https://github.com/xmichelo/Beeftext/blob/master/Beeftext/Update/UpdateDialog.cpp)
- [Manifest update](https://github.com/xmichelo/Beeftext/blob/master/Website/latestVersionInfo.json)
- [Releases](https://github.com/xmichelo/Beeftext/releases)
