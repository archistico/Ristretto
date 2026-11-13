# Audit sorgenti locali — baseline v2.1

**Fonti primarie fornite dall'utente:** `Beeftext-master.zip` e `espanso-dev.zip`, verificati senza estrarre file fuori dalle directory consentite. L'analisi è riferita **esclusivamente ai byte di questi snapshot**, non a uno specifico tag/release.

| Archivio fornito | SHA-256 ZIP | File dopo estrazione | Stato |
|---|---|---:|---|
| `Beeftext-master.zip` | `d9cea1311f46cc918704df7da080910545a1995c6153823d40dcd6c577b2306c` | 269 | ZIP CRC superato; struttura verificata |
| `espanso-dev.zip` | `edd0cd3d78cf8ff60b3edd2b4d21f46f2ed476b26bd7d46479cd2842515dccee` | 574 | ZIP CRC superato; struttura verificata |

## Limiti e riproducibilità

- Sono **snapshot di branch**: mancano le cartelle `.git`, la commit history e gli SHA Git verificabili. Non attribuire conclusioni alla release Beeftext v16.0 o a una release Espanso specifica senza confronto con tag pinned.
- Beeftext include in `.gitmodules` `Submodules/XMiLib` ed `Submodules/emojilib`, ma nell'archivio entrambe le cartelle risultano **vuote**. Il CMake root include XMiLib: non è una build completa autosufficiente.
- Espanso fornisce `Cargo.lock`, workspace `Cargo.toml` e sorgenti dei 15 crate workspace. Non abbiamo eseguito `cargo build`, `cargo test` o esecuzione nativa sui sistemi target.
- Nel dossier **non copiamo i sorgenti** di terze parti; i due ZIP restano proprietà dell'utente e vengono referenziati per nome, percorso e hash.
- `data/LOCAL_SOURCE_SHA256_v2.1.csv` rende confrontabili tutti i file di entrambi gli snapshot. Non sostituisce un commit Git firmato.
- Nessuna conclusione di test software: verificati solo leggibilità, integrità dei due ZIP, presenza e contenuto statico dei file.

## Cosa cambia rispetto a v2.0

Prima le fonti arrivavano da file online consultati singolarmente: ora esiste una **baseline locale consistente**, sufficiente per costruire un call graph ragionato e verificare affermazioni pregresse. La questione aperta non è più «recuperare il sorgente applicativo», ma **ottenere i submodule e una revisione attestata** per compilare l'originale.

## Licenze e provenienza

- `Beeftext-master/LICENSE`: MIT. Il `README.md` indica Qt sotto LGPLv3, OpenSSL ed emojilib MIT; serve comunque un inventario dipendenze completo.
- `espanso-dev/LICENSE`: GPLv3; i file Rust esaminati riportano GPLv3-or-later negli header. È lecito analizzare le *idee*, ma non copiare porzioni di sorgente in un nuovo prodotto con licenza incompatibile. Fissare prima una policy di clean-room/reimplementazione e una licenza per il prodotto.
- Le licenze di librerie vendorizzate, asset e dipendenze transitive non sono determinate interamente da questi due file LICENSE. È necessaria una futura SBOM per ciascun prodotto e per la nuova app.

## Regola di tracciabilità

Nei rapporti, i riferimenti del tipo `Beeftext/Combo/Combo.cpp:406–453` sono percorsi **relativi alla radice interna** `Beeftext-master/` dell'archivio. I riferimenti Espanso sono analoghi e relativi alla radice interna `espanso-dev/`.
