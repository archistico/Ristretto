# Audit v1.9 — dipendenze, compilazione e licenze

## Evidenze dirette

| Componente | Provenienza e ruolo | Licenza rilevata | Trasferimento nella nuova app |
|---|---|---|---|
| Beeftext | repo `xmichelo/Beeftext`, C++/Qt | MIT nel file LICENSE | studiare idee; eventuale riutilizzo letterale richiede copyright e licenza attribuiti |
| XMiLib | `.gitmodules`: `xmichelo/XMiLib`, utility Qt | MIT in `LICENSE.md` del repository | **non necessario** per l'implementazione .NET; evitare dipendenza nativa inutile |
| emojilib | `.gitmodules`: `muan/emojilib` | MIT nel LICENSE del repository | valutare solo dataset o un'alternativa con licenze/verifiche attuali |
| Qt 6 | build CMake e README Beeftext | README Beeftext dichiara LGPL-3 | non viene portato in Avalonia; verificare licenza dei pacchetti Avalonia usati |
| OpenSSL | dichiarato nel README | OpenSSL e SSLeay, secondo README | nessuna migrazione automatica; verificare dipendenze realmente distribuite |
| Win32/Winmm | `InputManager`, `AutoStart`, CMake target | API di piattaforma, non pacchetto redistribuibile | adapter Windows con P/Invoke dove necessario |
| PowerShell | `Scripts/PowerShell` e target traduzioni | strumenti di build/automazione | non introdurre come prerequisito generale multipiattaforma |
| NSIS | README installer | tool packaging terzo | alternativa per piattaforma, firma pacchetti in futuro |

## Metadati di compilazione verificati

- CMake root `VERSION` non dichiarata, richiede CMake 3.10 e subdir XMiLib/Beeftext.
- `Beeftext/CMakeLists.txt` richiede CMake 3.16 e `project VERSION 14.0`, abilita C++20, Qt6 Core/Gui/Widgets/Network, precompiled headers, PowerShell traduzioni, link `Winmm`.
- `Beeftext/Beeftext.pro` contiene `CONFIG += c++14` e Qt multimedia; l'originale ha anche `.sln` Visual Studio.
- La pagina Releases del progetto mostra v16.0. Questo **non prova** che CMake sia inutilizzabile: indica solo un disallineamento da verificare nel checkout/tag scelto.
- Sono presenti directory root `.github`, `Installer`, `Vendor`, `Website`, `Scripts/PowerShell`; nessun audit completo delle dipendenze transitive, degli asset vendorizzati o delle pipeline CI è stato eseguito.

## Regole proposte per il progetto Avalonia

1. Prima della scelta della licenza del *nuovo* progetto, stabilire se incorporeremo porzioni di codice Beeftext o scriveremo solo codice originale. La licenza MIT dell'originale non ci impone di usare MIT se riscriviamo da zero, ma se copiamo codice dobbiamo rispettare le notice.
2. Per ogni libreria NuGet/asset/font/emoji registrare nome, versione esatta, SPDX, URL, checksum e obblighi di distribuzione; produrre SBOM e notices in release.
3. Nessuna licenza dedotta soltanto dal nome o dal fatto che una libreria sia open source; per Avalonia Free/Pro e i controlli di griglia verificare la licenza specifica della versione usata.
4. Differenziare CI: test puri cross-platform, build artifacts Windows/macOS/Linux, test di runtime nativi su runner/host qualificati, firma solo dopo definizione release pipeline.
5. Target OS e versioni minime rimangono aperti e vanno dichiarati con capacità reali, soprattutto Wayland.

## Checklist checkout riproducibile (non eseguita qui)

```bash
git clone --recurse-submodules https://github.com/xmichelo/Beeftext.git
cd Beeftext
git rev-parse HEAD
git submodule status --recursive
git ls-files > source-inventory.txt
```

Nella fase di benchmark si sceglierà un tag/commit stabile (`v16.0` candidato) e si verificheranno branch, submodules e checksum. Non trattare gli hash brevi dei release notes come identità integrale del checkout. Nessun binario originale compilato; nessun codice terzo incluso in questo dossier.
