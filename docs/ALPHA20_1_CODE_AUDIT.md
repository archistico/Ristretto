# Alpha 20.1 — audit di codice, test e documentazione

**Data:** 13 novembre 2026 · **Oggetto:** sorgenti completi dell'Alpha 20, senza nuove funzionalità. **Non** attribuire a questo audit la compilazione C# o una prova Windows che non sono state effettuate.

## Controlli svolti

| Ambito | Metodo | Esito |
| --- | --- | --- |
| Namespace interni e `using` | Indicizzazione dei `namespace` nei `.cs`, risoluzione dei `using TextExpander.*` e `Ristretto.*` e loro raggiungibilità nel grafo dei `ProjectReference` | Superato sui sorgenti aggiornati |
| Attributi xUnit | Presenza di `using Xunit;` nei file di test con `[Fact]`, `[Theory]`, `[InlineData]` | Superato |
| Versioni | Confronto `VERSION.txt` con ogni `<Version>` esplicito, gate PowerShell Windows | **Quattro tool non aggiornati trovati e corretti** |
| XML/XAML, traduzioni e SQLite | Script Python preesistenti | Superato: cinque lingue, 112 chiavi; verifica DDL con SQLite Python, **non provider .NET** |
| Test del nuovo audit | `python -m unittest discover -s scripts -p 'test_audit_source.py' -v` | **5/5 superati** |
| Adapter VS Code | `scripts/validate-vscode.sh`, Node v22 | **32/32 superati** |
| Link Markdown interni | Validatore statico Python | Superato; nessun link relativo mancante |
| Compilazione .NET / xUnit | SDK .NET 10 **non disponibile** nell'ambiente di revisione | **NON ESEGUITI** |
| Test nativi Windows e VS Code Extension Development Host | Windows desktop / VS Code non disponibili nella revisione | **NON ESEGUITI** |
| Contratto JSON .NET → Node | Richiede .NET SDK 10 e Node.js | **NON ESEGUITO** |

## Correzioni applicate

1. **Quattro progetti ausiliari con versione obsoleta:** `Ristretto.CoreBenchmark`, `Ristretto.UiReview`, `Ristretto.ReplacementLab`, `Ristretto.WindowsOutputLab` riportavano ancora `3.9.0-alpha.19`; adesso sono allineati a `3.9.0-alpha.20.1` come il Desktop e `VERSION.txt`.
2. **Controllo di release incompleto:** `verify-release.ps1` esaminava soltanto la versione Desktop; ora verifica tutte le versioni esplicite dei `.csproj` in `src`, `tests` e `tools`. `validate.cmd` e `.sh` includono l'audit offline quando Python è presente.
3. **Controlli anti-regressione dei namespace:** nuovo `scripts/audit_source.py` con test per `using` inesistente, dipendenza non dichiarata, versione discordante e attributi xUnit senza import. Gli `ImplicitUsings` del framework e le classi di terze parti **non** vengono falsamente trattati come namespace interni.
4. **Documentazione dispersiva e stantia:** `INDEX.md` identificava Alpha 19 come corrente, `BUILD_AND_TEST.md` riportava Alpha 11, `HANDOFF.md` conteneva istruzioni di Alpha 16 e `CURRENT_STATE.md` accumulava mini-changelog. Ora il punto d'ingresso descrive Alpha 20.1, con **stato**, **roadmap** e **handoff** separati; la cronologia rimane nei documenti specifici e nel changelog.
5. **Consegna integrale:** i sorgenti, i test e i report storici sono conservati. Nessuna modifica a componenti Win32, hotkey, XAML, algoritmi Unicode o database.

## Limiti dell'audit automatico

Il controllo dei `using` individua errori in namespace di **progetto** e riferimenti tra progetti. Non può risolvere API .NET/NuGet, metodi, overload, tipi generici, codice generato, condizioni `#if`, layout Win32 né verificare il funzionamento di Avalonia. Solo `dotnet build` verifica questi aspetti a livello di compilazione; i test e il runtime sono passaggi distinti.

## Gate Windows prima del prossimo sviluppo

Da un'estrazione nuova dello ZIP:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

Registrare warning/errori, totale xUnit, risultato test export .NET → Node e test nativi; provare manualmente hotkey picker da tray e comando VS Code. In caso di errori **non** proseguire F3 come se la baseline fosse stata verificata: correggere prima le regressioni. La [roadmap](ROADMAP.md) riporta i criteri di completamento.
