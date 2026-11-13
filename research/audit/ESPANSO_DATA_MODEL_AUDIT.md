# Studio del modello dati Espanso e del suo import (v1.8)

**Perimetro:** documentazione pubblica ufficiale di Espanso v2; non abbiamo analizzato integralmente il sorgente Rust, né eseguito Espanso. Distinguere **capacità documentate** da compatibilità che dovremo verificare.

## Struttura

Espanso separa le opzioni di funzionamento (directory `config`, `default.yml` e config app-specific) dalle regole (`match/base.yml`, altri file YAML). Questo è interessante per noi perché distingue **che cosa espandere** da **quando/come espanderlo**. La struttura è locale e facilmente gestibile con Git, ma richiede che l'utente sappia editare YAML. La nostra GUI dovrebbe fornire un livello visuale senza imporre il formato interno di Espanso.

Esempio minimale documentato:

```yaml
matches:
  - trigger: ":ciao"
    replace: "Ciao 🌍 — Καλημέρα!"
```

Campi e costrutti da riconoscere, *non necessariamente convertire integralmente*:

| Formato | Funzione | Import proposto | Rischio |
|---|---|---|---|
| `matches[].trigger` | trigger singolo | Convertire | check collisioni, Unicode |
| `matches[].triggers` | più alias | Convertire in alias collegati allo stesso snippet, **solo se supportati** | ID/ordine |
| `matches[].replace` | testo | Convertire letteralmente | placeholder dinamici |
| `matches[].form`/`form_fields` | campi input | Conservare non attivo come metadata o segnare incompatibile | interattività non supportata |
| `matches[].vars`, `type: date` | variabili data/ora | eventuale conversione controllata | locale/timezone |
| `matches[].vars`, `type: shell`/`script` | codice/azione | **Non eseguire**; marcare blocco | sicurezza |
| `matches[].regex` | trigger dinamico | Non attivare automaticamente; P3 | ReDoS, sintassi Rust vs .NET |
| `imports` | altri file YAML | non seguire fuori radice; chiedere consenso, anti-cycle | traversal/symlink/DoS |
| `image_path` | allegati | non aprire né caricare percorsi esterni; manifest se futuro | path leak/esfiltrazione |
| config per-app | scope, filtro, enable, includes | futuro mapping con review manuale | Wayland senza app context |
| packages | pacchetti community | non installare da import; supporto futuro separato | supply chain |

### Comportamento quando l'import non è completo

1. Parsing YAML con limiti sul totale byte, numero nodi, nesting e alias; impedire entità espansive e loop.
2. Non usare il loader YAML come interprete di codice; i campi sono strutture testuali.
3. Rappresentare una riga di report per ciascuno snippet: `Converted`, `ConvertedWithWarnings`, `RequiresManualWork`, `Rejected`.
4. Conservare il testo sorgente originale degli snippet che contengono macro sconosciute; non promettere che funzioneranno come Espanso.
5. Preview con nomi delle variabili e macro **senza esecuzione**, warning su riferimenti fuori radice e import esterni.
6. Se l'utente annulla, l'archivio attuale deve restare bit-a-bit invariato (salvo file temporanei rimossi).

### Tradeoff

Il formato YAML è eccellente come configurazione testuale modificabile e per versionamento Git, ma per una GUI generalista comporta commenti, quoting, alias, più file, condizioni per app e macro. **Non serve adottarlo come persistenza interna.** Meglio un modello di dominio unico e un import/export Espanso limitato, versionato e dichiarato. Per utenti avanzati si può valutare un export YAML, ma solo in slice successiva.

## Fonti primarie

- https://espanso.org/docs/configuration/basics/
- https://espanso.org/docs/matches/organizing-matches/
- https://espanso.org/docs/matches/basics/
- https://espanso.org/docs/matches/forms/
- https://espanso.org/docs/matches/regex-triggers/
- https://espanso.org/docs/matches/variables/
- https://espanso.org/docs/matches/extensions/
- https://espanso.org/docs/configuration/app-specific-configurations/
- https://espanso.org/docs/packages/basics/

**Limite:** la documentazione dichiara le configurazioni per-app ancora non supportate su Wayland. Non promettere una migrazione 1:1 dello scope applicativo su Linux.
