# Riepilogo esecutivo dell'audit submodule v2.2

L'arrivo di `XMiLib-master.zip` e `emojilib-main.zip` ha colmato il **vuoto di consultazione** delle due librerie ma **non ancora il requisito di riproducibilità Git**. Entrambe risultano MIT; i loro snapshot non dimostrano che siano i commit compatibili con il tag Beeftext esaminato.

## Risultati prioritari

1. **Incompatibilità formato emoji (P0):** Beeftext si aspetta `Submodules/emojilib/emojis.json` con valori-oggetto che contengano `char`; `emojilib` 4.0.3 fornisce `dist/emoji-en-US.json` con chiavi-emoji e array di keywords. Anche gli script NSIS e portable richiedono il vecchio file. Una semplice rinomina non risolve la differenza di schema. Vedere [audit emojilib](EMOJILIB_DIRECT_SOURCE_AUDIT_v2.2.md).
2. **Sintesi Unicode Win32 (P0):** nel helper XMiLib utilizzato da Beeftext, il key-up è composto con `KEYEVENTF_KEYUP | KEYEVENTF_KEYUP`, senza flag Unicode. Rischio verificabile nel sorgente, impatto runtime ancora da riprodurre. Vedere [audit XMiLib](XMILIB_DIRECT_SOURCE_AUDIT_v2.2.md).
3. **Non assumere output riuscito (P0):** i metodi XMiLib esaminati non utilizzano il valore di ritorno di `SendInput`; l'adapter C# deve riportare gli errori e mantenere condizioni di sicurezza sul testo di destinazione.
4. **Gestione clipboard (P0):** XMiLib fornisce RAII e retry, ma la logica Beeftext di backup non verifica sempre esplicitamente il successo del guard. Testare clipboard occupata, restore e modifiche dell'utente.
5. **Hotkey Windows (P1):** API supportate via `RegisterHotKey`; il percorso non-Windows non equivale ad implementazione completa. Gestire conflitti e risorse `GlobalAddAtom` in modo sicuro.
6. **Catalogo emoji (P1):** 1.914 chiavi e 15.412 keyword in inglese; dati utili per picker offline, ma nessuna ragione di richiedere Node o Qt nell'app nuova.

## Impatto sulle Vertical Slice

- `Input/InjectText`: adapter nativi con esito verificabile, misure per UTF-16/IME e correzione semantica, non copia del vecchio helper.
- `Picker/Search`: lista unificata snippet/emoji ma categorie e sinonimi separati.
- `EmojiCatalog`: piccolo parser e indice dati puro, testabile senza Avalonia, con catalogo valido pin-nato.
- `Settings/Localization`: lingua della GUI distinta dalle keyword del catalogo; feature emoji disattivabile.
- `AppLifecycle`: single-instance e hotkeys separate dal motore, in un service per OS.
- `Storage/Import`: import di dati in preview, validazione e pubblicazione atomica.

## Limiti e stato

Il codice e il dataset sono stati ispezionati e i JSON validati con Python. Nessuna compilazione Qt, test runtime Windows/macOS/Linux, test degli originali o del nuovo applicativo. La compilazione fedele Beeftext richiede risalire ai commit Gitlink corretti. [Piano di riproducibilità](SUBMODULE_INTEGRATION_REPRODUCIBILITY_v2.2.md) · [36 test candidati](../../data/SUBMODULE_AUDIT_SCENARIOS_v2.2.csv).
