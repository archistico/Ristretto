# Tracciabilità delle evidenze — audit architetturale v1.9

**Data:** 2026-11-12. Le URL `raw/master` sono sorgenti originali ma il riferimento `master` è mobile, **non pinned**. `D` = file sorgente/README esaminato, `R` = release metadata, `I` = deduzione o politica proposta, `N` = non verificato con test runtime.

| ID | Tipo | Percorso e righe significative | Osservazione |
|---|---|---|---|
| ARC-01 | D | [main.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/main.cpp) 54–118, 150–168, 199–224 | single instance, lifecycle, portable, run UI, picker |
| ARC-02 | D | [MainWindow.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/MainWindow.cpp) 27–46, 70–86, 94–102, 108–179, 212–264 | widget→manager, DnD, tray e locale |
| ARC-03 | D | [PreferencesManager.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Preferences/PreferencesManager.cpp) 27–90, 314–378 | chiavi, installed/portable, reset |
| ARC-04 | D | [PreferencesManager.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Preferences/PreferencesManager.cpp) 385–435, 470–571 | import/export, problemi di mappatura e side effect |
| ARC-05 | D | [PreferencesDialog.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Preferences/PreferencesDialog.cpp) 24–63, 70–116 | 5 tab; azioni import/export/default |
| ARC-06 | D | [I18nManager.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/I18nManager.cpp) 24–65, 98–138, 213–265 | traduzioni per lingua/regione, mapping polacco |
| ARC-07 | D | [AutoStart.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/AutoStart.cpp) 18–70 | HKCU Run, portable exception |
| ARC-08 | D | [InputManager.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/InputManager.cpp) 21–39, 337–422 | layout Win32 e hook keyboard/mouse |
| ARC-09 | D | [ProcessListManager.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/ProcessListManager.cpp) 27–65 | process rules con wildcard |
| ARC-10 | D | [BeeftextGlobals.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/BeeftextGlobals.cpp) 30–102 | sensitive apps come profilo clipboard, excluded apps distinti |
| ARC-11 | D | [UpdateManager.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Update/UpdateManager.cpp) 17–65 | timer aggiornamenti |
| ARC-12 | D | [UpdateCheckWorker.cpp](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Update/UpdateCheckWorker.cpp) 14–85 | endpoint JSON e worker Qt |
| ARC-13 | D | [.gitmodules](https://github.com/xmichelo/Beeftext/blob/master/.gitmodules) 200–210 | due submodules XMiLib/emojilib |
| ARC-14 | D | [root CMakeLists.txt](https://github.com/xmichelo/Beeftext/blob/master/CMakeLists.txt) 179–185 | subdir root |
| ARC-15 | D | [Beeftext/CMakeLists.txt](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/CMakeLists.txt) 0–37, 39–202 | component inventory, Qt6 C++20 PowerShell Winmm |
| ARC-16 | D | [Beeftext.pro](https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Beeftext.pro) 4–22 | qmake/Qt e C++14 |
| ARC-17 | D | [Beeftext LICENSE](https://raw.githubusercontent.com/xmichelo/Beeftext/master/LICENSE) | MIT |
| ARC-18 | D | [XMiLib LICENSE.md](https://raw.githubusercontent.com/xmichelo/XMiLib/master/LICENSE.md) | MIT |
| ARC-19 | D | [emojilib LICENSE](https://raw.githubusercontent.com/muan/emojilib/main/LICENSE) | MIT |
| ARC-20 | R | [Releases](https://github.com/xmichelo/Beeftext/releases) | v16.0 e breve SHA indicato in pagina; nessuna relazione master/tag attestata |
| ARC-21 | D | [README Beeftext](https://github.com/xmichelo/Beeftext) | build Visual Studio 2022, Qt, NSIS e licenze dichiarate |
| ARC-22 | N | `Vendor/`, `Installer/`, `.github/` completi | non inventariati: richiedono clone e scansione |

**Non affermare:** checkout integrale, test statici automatici eseguiti su codice C++, regressioni riprodotte, correzioni dell'originale, compatibilità cross-platform del motore Qt originale. I difetti osservati sono quelli espressi nel sorgente letto alla data della ricognizione.
