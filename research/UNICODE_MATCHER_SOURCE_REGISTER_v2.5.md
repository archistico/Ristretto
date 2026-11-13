# Fonti di standard e documentazione — matcher Unicode v2.5

| Rif. | Sorgente | Uso | Limite |
|---|---|---|---|
| UAX-15 | https://www.unicode.org/reports/tr15/ | forme NFC/NFD/NFKC/NFKD, identità canonica, normalizzazione concatenazioni | norma del confronto, non prova di compatibilità dei singoli editor |
| UAX-29 | https://www.unicode.org/reports/tr29/ | grapheme cluster e word boundary | comportamento di Backspace non definito da questa norma per ogni applicazione |
| UTS-39 | https://www.unicode.org/reports/tr39/ | warning di stringhe confondibili | non è un normalizzatore del trigger |
| Microsoft StringInfo | https://learn.microsoft.com/en-us/dotnet/api/system.globalization.stringinfo.gettextelementenumerator | strumenti .NET per gli elementi testuali | versione runtime/Unicode e comportamento editor da verificare |
| Microsoft Regex | https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options | limiti `\b/\w`, opzione ECMAScript | non coincide in generale con UAX29 |
| Beeftext sorgenti locali | `Beeftext-master.zip`, `Beeftext/Combo/Combo.cpp` e `ComboManager.cpp` | confronto originali e attivazione | snapshot senza hash commit |
| Espanso sorgenti locali | `espanso-dev.zip`, `espanso-match/src/rolling/{mod,matcher,util}.rs` e `espanso-config/.../yaml/parse.rs` | rolling matcher e delimitatori | branch dev senza commit SHA |
| XMiLib / emojilib | archivi forniti dall'utente | output Unicode Windows e dataset emoji | non garantiscono correttezza del matcher o cancellazione |

Non includere mai codice GPL di Espanso nella nuova applicazione senza una decisione di licenza esplicita e revisione legale; questa documentazione descrive principi e cita file, non riproduce implementazioni.
