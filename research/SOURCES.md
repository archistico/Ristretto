# Catalogo fonti consultate / riferimento — v1.5

**Data ricognizione:** 12 novembre 2026. Le URL primarie descrivono le caratteristiche pubblicate dai produttori, **non** costituiscono test di prestazione o compatibilità. Una URL può cambiare: il documento di progetto non deve dipendere da testi copiati da siti esterni.

| ID | Fonte | Utilizzo | Tipo | URL |
|---|---|---|---|---|
| SRC-001 | Beeftext repo | Originale + licenza; audit storico | Repo | https://github.com/xmichelo/Beeftext |
| SRC-002 | Beeftext wiki | Funzioni originale | Doc | https://github.com/xmichelo/Beeftext/wiki |
| SRC-003 | Espanso config | config app e profiles | Doc | https://espanso.org/docs/configuration/basics/ |
| SRC-004 | Espanso match | word boundary, unicode, backend | Doc | https://espanso.org/docs/matches/basics/ |
| SRC-005 | Espanso forms | forms/control types | Doc | https://espanso.org/docs/matches/forms/ |
| SRC-006 | Espanso install | Piattaforme X11/Wayland | Doc | https://espanso.org/install/ |
| SRC-007 | PhraseExpress manual | funzionalità per edizione | Doc | https://www.phraseexpress.com/doc/ |
| SRC-008 | PhraseExpress FAQ | focus, keyboard hook, clipboard | FAQ | https://www.phraseexpress.com/support/faq/ |
| SRC-009 | FastKeys | Text expansion e macro | Vendor | https://www.fastkeysautomation.com/ |
| SRC-010 | TextExpander | Editor/form/condivisione | Vendor | https://textexpander.com/ |
| SRC-011 | TextExpander release | feature 2026; versioned | Release | https://textexpander.com/release/mac |
| SRC-012 | aText | features | Vendor | https://www.trankynam.com/atext/ |
| SRC-013 | Karabiner | input remapper | Vendor/OSS | https://karabiner-elements.pqrs.org/ |
| SRC-014 | Karabiner repository | sorgente | Repo | https://github.com/pqrs-org/Karabiner-Elements |
| SRC-015 | Keyboard Maestro | automation | Vendor | https://www.keyboardmaestro.com/main/ |
| SRC-016 | KM text triggers | testo trigger / palette | Doc | https://wiki.keyboardmaestro.com/manual/Text_Triggers |
| SRC-017 | Text Blaze docs | snippets/automation | Doc | https://blaze.today/docs/ |
| SRC-018 | Text Blaze forms | form fields | Doc | https://blaze.today/guides/forms/ |
| SRC-019 | Text Blaze formulas | formule | Doc | https://blaze.today/guides/formulas/ |
| SRC-020 | Typinator | feature mac/W beta | Vendor | https://ergonis.com/en/typinator/ |
| SRC-021 | AutoKey docs | X11, script, layout caveat | Doc | https://autokey.github.io/intro.html |
| SRC-022 | AutoKey repo | X11/Wayland limitation | Repo | https://github.com/autokey/autokey |
| SRC-023 | AutoHotkey docs | hotstrings/endchars/case | Doc | https://www.autohotkey.com/docs/v2/Hotstrings.htm |
| SRC-024 | AHK docs mirror | Mirror consultabile: confermare primaria | Mirror | https://doggy8088.github.io/AutoHotkeyDocs/docs/Hotstrings.htm |
| SRC-025 | Alfred snippets | viewer, collection, security | Doc | https://www.alfredapp.com/help/features/snippets/ |
| SRC-026 | Alfred advanced | delays, clipboard | Doc | https://www.alfredapp.com/help/features/snippets/advanced/ |
| SRC-027 | Alfred placeholders | cursor, nested snippets | Doc | https://www.alfredapp.com/help/workflows/advanced/placeholders/ |
| SRC-028 | Raycast snippets | modes, tags, unicode keywords, import | Doc | https://manual.raycast.com/snippets |
| SRC-029 | Raycast placeholders | cursor, date, refs | Doc | https://manual.raycast.com/dynamic-placeholders |
| SRC-030 | TypeIt4Me | features | Vendor | https://www.ettoresoftware.com/mac-apps/typeit4me/ |
| SRC-031 | TypeIt4Me help | forms/vars/cursor | Doc | https://ettoresoftware.store/mac-apps/typeit4me/help/ |
| SRC-032 | Hammerspoon eventtap | input observation/keyStrokes | API | https://www.hammerspoon.org/docs/hs.eventtap |
| SRC-033 | Apple Replacements | built-in replacements, support not all apps | OS doc | https://support.apple.com/guide/mac-help/replace-text-punctuation-documents-mac-mh35735/mac |
| SRC-034 | Avalonia localization | ResX, runtime change, RTL | API | https://docs.avaloniaui.net/docs/app-development/localizing |
| SRC-035 | Avalonia tray | tray support caveat Linux | API | https://docs.avaloniaui.net/controls/navigation/trayicon/ |
| SRC-036 | .NET Rune | Rune/scalar UTF16 | API | https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction |
| SRC-037 | .NET StringInfo | text elements/graphemes | API | https://learn.microsoft.com/it-it/dotnet/api/system.globalization.stringinfo?view=net-10.0 |

I 36 riferimenti originari e la loro attribuzione sono nel [registro delle fonti benchmark](BENCHMARK_SOURCE_REGISTER.md). Non confondere disponibilità documentata e compatibilità testata.

## Fonti primarie aggiunte/riutilizzate in v1.6 — trigger e affidabilità (12-11-2026)

| Tema | Fonte | Livello |
|---|---|---|
| Trigger PhraseExpress, duplicati e boundary | https://www.phraseexpress.com/doc/edit/trigger/ | D (ufficiale) |
| PhraseExpress expert backend/focus | https://www.phraseexpress.com/doc/settings/expert/ | D (ufficiale) |
| Keyboard Maestro typed strings, longest e cancellazione | https://wiki.keyboardmaestro.com/trigger/Typed_String | D (ufficiale) |
| TextExpander conflitti | https://textexpander.com/learn/using/snippets/conflicting-snippet-abbreviations | D (ufficiale) |
| TextExpander policy case/app | https://textexpander.com/learn/using/preferences/expansion-settings | D (ufficiale) |
| FastKeys advanced trigger | https://www.fastkeysautomation.com/documentation6/TextExpander.html | D (ufficiale) |
| Espanso word/case/backend | https://espanso.org/docs/matches/basics/ | D (ufficiale) |
| Raycast trigger modes | https://manual.raycast.com/snippets | D (ufficiale) |
| Text Blaze word rules | https://blaze.today/faq/ | D (ufficiale) |
| aText secure input/clipboard | https://www.trankynam.com/atext/doc/ | D (ufficiale) |
| Alfred troubleshooting | https://www.alfredapp.com/help/troubleshooting/snippets/ | D (ufficiale) |
| Typinator features | https://ergonis.com/en/typinator/features/ | D (ufficiale) |
| Karabiner Unicode tools | https://karabiner-elements.pqrs.org/docs/getting-started/features/ | D (ufficiale) |
| .NET Unicode/graphemes | https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction | D (ufficiale) |
| AutoHotkey hotstrings | https://doggy8088.github.io/AutoHotkeyDocs/docs/Hotstrings.htm | D (mirror, validare versione primaria) |

**Metodologia:** ogni `D` conferma che la documentazione ne descrive il comportamento; **non** certifica un test reale o prestazioni. `I` (inferenza) e `V` (da validare) sono segnalati nei rapporti.


## Fonti aggiunte nella ricognizione v1.7

La fonte di dettaglio ufficiale, con ID `NAT-S01`–`NAT-S24`, grado di evidenza e limiti, è [NATIVE_SOURCE_EVIDENCE.md](NATIVE_SOURCE_EVIDENCE.md). Include Microsoft (LowLevelKeyboardProc, SendInput, KEYBDINPUT, KBDLLHOOKSTRUCT, Clipboard), Apple (Quartz, Accessibility, NSPasteboard), freedesktop/X11, portali XDG/Wayland, Avalonia ed Espanso. `D` significa *documentato*, non *testato*.
