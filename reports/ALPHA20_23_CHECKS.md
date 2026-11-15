# Verifiche — Ristretto Alpha 20.23

## Baseline precedente

- **Alpha 20.20:** compilazione e test .NET confermati dall'utente.
- **Alpha 20.21:** 742/743 xUnit superati; l'unico errore riguardava un offset UTF-16 errato nella fixture, corretto nei sorgenti 20.22.
- **Picker Notepad Unicode:** inserimento e file salvato corrispondente alla fixture confermati dall'utente.

## Nuova versione — stato delle prove

| Controllo | Esito |
|---|---|
| Nuova sessione live `ExternalClassicEditLiveSession` | Codice implementato, prova xUnit **NOT_RUN** |
| Hook WinEvent opt-in e lettura del solo target notificato | Codice implementato, prova Windows **NOT_RUN** |
| Contratti `Ristretto.DevTools` | Codice aggiornato, esecuzione con .NET **NOT_RUN** |
| GUI, picker, hotkey e SQLite | Invariati rispetto alla 20.22 (controllo del diff) |
| Strumenti Python nel progetto | Nessuno |
| Compilazione .NET 10 e suite xUnit 20.23 | **NOT_RUN** — SDK non presente nell'ambiente di preparazione |
| Prova interattiva WinEvent con campo Edit sintetico | **NOT_RUN** — richiede Windows |
| Espansione automatica nei programmi esterni | **NON IMPLEMENTATA** |
| Test Node adapter facoltativo | **PASS 32/32** — eseguiti qui |
| XML/XAML e risorse | **PASS** — 19 file, 5 localizzazioni con 120 chiavi coerenti |
| Collegamenti Markdown locali | **PASS** — 735 riferimenti controllati |
| Integrità archivio ZIP/manifest SHA-256 | **PASS** — 467 file, 466 hash, estrazione pulita verificata |

I controlli statici disponibili sono stati eseguiti sui sorgenti modificati; manifest e struttura ZIP sono stati verificati anche da una nuova estrazione. Il confronto con la 20.22 non rileva modifiche ai 33 file applicativi critici esaminati. **Un test non eseguito non è un PASS.**
