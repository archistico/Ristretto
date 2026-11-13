# Integrazione submodule e riproducibilità — v2.2

## Stato delle quattro fonti disponibili

| Snapshot | Provenienza | Contenuto | Attestazione |
|---|---|---|---|
| `Beeftext-master.zip` | allegato utente | codice C++/Qt, `.gitmodules`, script installer | hash ZIP, no `.git`/SHA commit |
| `espanso-dev.zip` | allegato utente | codice Rust, licenza GPLv3 | hash ZIP, no `.git`/SHA commit |
| `XMiLib-master.zip` | allegato utente | C++/Qt, test Qt, licenza MIT | hash ZIP, no `.git`/SHA commit |
| `emojilib-main.zip` | allegato utente | JSON keyword, scripts npm, MIT | hash ZIP, no `.git`/SHA commit |

`.gitmodules` del repository Beeftext indica `Submodules/XMiLib` e `Submodules/emojilib`, ma **non include gli oggetti Git del superproject** che registrano gli SHA dei submodule. Non è corretto dichiarare che i due ZIP forniti siano precisamente i commit necessari alla build di Beeftext. La prova più forte di disallineamento è `emojilib-main` v4.0.3 senza `emojis.json` richiesto dall'installer originale.

## Rischi e limiti del build da snapshot

- `Beeftext/CMakeLists.txt` richiede `Submodules/XMiLib/XMiLib`; `XMiLib` richiede Qt 6 e componenti Windows. Le sorgenti XMiLib dell'archivio possono essere ispezionate ma non sono automaticamente la revisione corretta.
- Installer e portable richiedono `Submodules/emojilib/emojis.json`: il file manca dall'archivio `emojilib-main`; per un build fedele occorre la revisione storica compatibile, non basta copiare il JSON moderno con nome diverso.
- Assenza di `.git` = impossibile confermare submodule SHA, storia delle modifiche e corrispondenza al tag 16.0. Nome archivio e `package.json version` non sono garanzie di tag Git.
- `qmake` non è installato in questo ambiente; **nessuna build dell'originale è stata tentata o dichiarata riuscita**.
- Non copiare nei deliverable del nuovo progetto codice GPLv3 di Espanso senza prima ratificare policy/licenza; Beeftext, XMiLib ed emojilib in questi snapshot sono MIT con obblighi di attribuzione se riutilizzati.

## Riproducibilità richiesta in futuro

1. Acquisire clone/tag e l'output attestabile di `git submodule status --recursive`, registrando commit full SHA e data.
2. Verificare se e dove si trova l'originale `emojis.json` nello storico `emojilib`; documentare solo un commit che lo contenga e sia previsto dal superproject.
3. Generare inventario completo di dipendenze CMake/Qt/Windows, SBOM e notices; distinguere runtime, tool di build e dati.
4. Compilare su Windows in VM/runner isolato, conservando log, hash binario, configurazione e prerequisiti. Non assumere che la versione corrente di Qt compili sorgenti legacy invariati.
5. Eseguire manualmente smoke test circoscritti per Win32 hotkey, clipboard, Unicode e catalogo emoji; registrare esito `PASS/FAIL/SKIP` e ambiente.
6. Non serve attendere la compilazione legacy per progettare core puro C#: il suo gate native platform resta indipendente e richiede prove proprie.

## Componenti da trasferire **come concetti**, non come dipendenze

- `GlobalShortcut` -> adapter di registrazione global hotkeys per OS.
- `SystemUtils` -> implementazioni native di output con reporting degli errori.
- `ScopedClipboardAccess` -> gestione della clipboard con ripristino condizionale.
- `SingleInstanceApplication` -> servizio app lifecycle e apertura finestra principale.
- `emojilib` -> catalogo dati separato e versionato, ricerca keyword, senza JS runtime.

**Scopo pratico:** una solution C# con pochissimi progetti e separazione per feature; non introdurre un layer `XMiLib` generico in .NET e non importare i 15 crate di Espanso.
