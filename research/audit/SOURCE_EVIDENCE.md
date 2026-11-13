# Registro fonti — audit v1.8

**Legenda:** `D` = sorgente originale o documentazione ufficiale letta; `I` = deduzione tecnica da validare; `N` = non ancora riprodotto/testato.

| ID | Livello | Fonte | Punto di verifica / risultato |
|---|---|---|---|
| PERS-S01 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/ComboList.cpp | 21–25/336–449: schema root, clear-before-parse, write diretto |
| PERS-S02 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/ComboList.h | 32–35: nome file e versione 10 |
| PERS-S03 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/Combo.cpp | 30–46/96–139/439–453: campi e legacy mapping |
| PERS-S04 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Group/Group.cpp | 12–18/36–51/113–121: gruppi |
| PERS-S05 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/MatchingMode.h | enum match |
| PERS-S06 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/CaseSensitivity.h | enum case |
| PERS-S07 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/ComboManager.cpp | 90–142: load, upgrade, archive e restore |
| PERS-S08 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Backup/BackupManager.cpp | 13–16/110–137: retention, rename backup |
| PERS-S09 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/ComboImportDialog.cpp | CSV, conflitti, UUID, modifica parziale |
| PERS-S10 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/LastUse/ComboLastUseFile.cpp | ultimo uso separato |
| PERS-S11 | D | https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/BeeftextConstants.h | Qt::ISODateWithMs |
| PERS-S12 | D | https://github.com/xmichelo/Beeftext/wiki/Import-and-export | semantica export/import e backup |
| PERS-S13 | D | https://espanso.org/docs/configuration/basics/ | split config/match |
| PERS-S14 | D | https://espanso.org/docs/matches/organizing-matches/ | include, file `_`, path esterni |
| PERS-S15 | D | https://espanso.org/docs/matches/regex-triggers/ | regex e gruppi nominati Rust |
| PERS-S16 | D | https://espanso.org/docs/matches/forms/ | form e fields |
| PERS-S17 | D | https://espanso.org/docs/matches/variables/ | variabili e dependency injection |
| PERS-S18 | D | https://espanso.org/docs/matches/extensions/ | shell, data, scelte |
| PERS-S19 | D | https://espanso.org/docs/configuration/app-specific-configurations/ | filtri e limiti Wayland |
| PERS-S20 | D | https://www.sqlite.org/backup.html | Backup API vs copia file |
| PERS-S21 | D | https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace | semantics replace file |
| PERS-S22 | D | https://owasp.org/www-community/attacks/CSV_Injection | CSV formula injection |

**Note di provenienza:** non sono stati salvati i file C++ di terzi dentro la distribuzione. Il `master` analizzato non è bloccato a uno SHA; per prove riproducibili acquisire il commit e un tarball originale, quindi integrare l'inventario delle dipendenze. Il contenuto delle schede prodotto e delle API ufficiali è documentale, non test runtime.
