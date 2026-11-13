# Registro delle evidenze audit v2.0 — 13/11/2026

Ogni fonte è pubblica. `master` è mobile e non fissato a SHA: **evidenze statiche, non test runtime**. Le citazioni a GitHub sono link ai file leggibili e al relativo repository. Non sono presenti copie dei sorgenti di terzi nel dossier.

| ID | Tipo | File / sezione | Fatto verificato staticamente | URL |
|---|---|---|---|---|
| E20-01 | GitHub file | `.gitmodules` | XMiLib, emojilib come submodule | https://github.com/xmichelo/Beeftext/blob/master/.gitmodules |
| E20-02 | GitHub file | `CMakeLists.txt` | root aggiunge subdirectory XMiLib e Beeftext | https://github.com/xmichelo/Beeftext/blob/master/CMakeLists.txt |
| E20-03 | GitHub file | `Beeftext/CMakeLists.txt` | Qt6 Core/Gui/Widgets/Network, XMiLib, Winmm | https://github.com/xmichelo/Beeftext/blob/master/Beeftext/CMakeLists.txt |
| E20-04 | GitHub file | `BeeftextSolution.pro` | qmake subdirs e dipendenza da XMiLib | https://github.com/xmichelo/Beeftext/blob/master/BeeftextSolution.pro |
| E20-05 | GitHub file | `Installer/installer.nsi` | compile via devenv, richiesta permessi, file e registro, uninstall directory ricorsiva | https://github.com/xmichelo/Beeftext/blob/master/Installer/installer.nsi |
| E20-06 | GitHub file | `Update/UpdateManager.cpp` | controllo su avvio e ogni 24h se attivo | https://github.com/xmichelo/Beeftext/blob/master/Beeftext/Update/UpdateManager.cpp |
| E20-07 | GitHub file | `Update/UpdateDialog.cpp` | SHA-256 confrontato prima dell'esecuzione; portable apre release | https://github.com/xmichelo/Beeftext/blob/master/Beeftext/Update/UpdateDialog.cpp |
| E20-08 | GitHub file | `Website/latestVersionInfo.json` | versione 16.0, URL, SHA256, note HTML; title v9.1 nei releaseNotes | https://github.com/xmichelo/Beeftext/blob/master/Website/latestVersionInfo.json |
| E20-09 | GitHub doc | `README.md` | licenze, toolchain e modalità manutenzione | https://github.com/xmichelo/Beeftext/blob/master/README.md |
| E20-10 | GitHub pagina | Releases | v16.0 release indicata come latest, abbreviazione SHA 1f24a1c | https://github.com/xmichelo/Beeftext/releases |
| E20-11 | GitHub website | `Website/index.html` | link distribuzione installer / portable edizioni | https://github.com/xmichelo/Beeftext/blob/master/Website/index.html |

## Limiti

1. `git ls-remote` fallisce per `Could not resolve host: github.com` in questo ambiente: **SHA completo non accertato**.
2. Le pagine `tree/master/...` non sono interrogabili tramite lo strumento Web in questa sessione: **inventario integrale, CI e Vendor non verificati**.
3. Nessuna compilazione, avvio, installazione, disinstallazione, aggiornamento o simulazione test su Windows è stata eseguita.
4. Non attribuire etichette PASS ai casi descritti in `data/RELEASE_AUDIT_SCENARIOS.csv`.
