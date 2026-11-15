# Verifiche — Ristretto Alpha 20.22

## Stato di partenza

- **Alpha 20.20:** compilazione e test superati su Windows (conferma utente).
- **Alpha 20.21:** 742/743 test xUnit passati; fallito un test che passava un offset di caret non corretto. La produzione ha rifiutato correttamente il delta.
- Inserimento Unicode sintetico dal picker in Notepad: esito positivo confermato dall'utente.

## Verifiche della nuova versione

| Controllo | Esito |
|---|---|
| Correzione fixture UTF-16 e regressioni | **Implementate, da eseguire con xUnit** |
| Modifica picker, percorso Invio e frecce | **Implementata, da verificare su Windows** |
| XAML editor reattivo | **Implementato, rendering da verificare** |
| Test Node dell'adapter facoltativo | **PASS 32/32**, eseguiti in questo ambiente |
| Parsing XML/XAML e risorse | **PASS**: 19 file XML validi, 5 lingue con 120 chiavi coerenti |
| Contratti GUI/DevTools (controllo statico) | **PASS** per i riferimenti testuali e binding principali; gate DevTools .NET non eseguito |
| Collegamenti documentali | **PASS**: 731 collegamenti locali, nessuno mancante |
| Script Python nel progetto | **Nessuno** |
| Compilazione .NET 10, xUnit e gate CORE | **NOT_RUN** |
| Prove Windows UI e nativa | **NOT_RUN** |
| Espansione automatica globale | **NON IMPLEMENTATA** |

La nuova versione non installa hook globali né modifica il percorso di inserimento nativo. I test che non è possibile eseguire non sono considerati superati.
