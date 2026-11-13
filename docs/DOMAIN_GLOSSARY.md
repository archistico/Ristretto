# Glossario canonico del dominio — v2.4

| Termine | Significato esatto | Da non confondere con |
|---|---|---|
| **Snippet** | elemento della libreria con trigger, contenuto e gruppo | evento di digitazione, template esportato |
| **Group** | organizzazione e abilitazione collettiva | ambito applicativo o tag |
| **Trigger** | sequenza Unicode e regole di attivazione | keycode, scancode o shortcut OS |
| **Template** | contenuto originale letterale o strutturato | testo già inserito nel target |
| **Committed text** | testo effettivamente composto e confermato, se osservabile affidabilmente | pressione di tasto fisico, composizione IME in corso |
| **Grapheme** | cluster di caratteri percepito come unità grafica | `char` C#, `Rune`, numero di Backspace |
| **Matcher snapshot** | insieme immutabile dei trigger attivi a una revisione libreria | live query database a ogni tasto |
| **MatchResult** | esito tipizzato del riconoscimento | prova che la rimozione è possibile |
| **InputSession** | memoria volatile della digitazione osservata per un target | cronologia persistente dei tasti |
| **ExpansionPlan** | contenuto completamente preparato prima degli effetti esterni | sequenza di key events OS |
| **TargetContext** | fotografia opaca del target e dei permessi | garanzia eterna sul focus |
| **Preflight** | controllo immediato prima della rimozione | eliminazione completa delle race OS |
| **InsertionOutcome** | esito tipizzato dell'esecuzione esterna | semplice booleano di successo |
| **SubmittedUnverified** | invio effettuato senza prova del risultato nell'editor | `VerifiedComplete` |
| **PartialFailure** | output avviato ma incompleto | annullamento sicuro |
| **ClipboardLease** | risorsa temporanea dell'adapter clipboard | transazione universale sugli appunti |
| **ActivationScope** | contesto in cui uno snippet è attivo | controllo che sia sicuro scrivere nel campo |
| **LibraryRevision** | versione della libreria usata per snapshot | ora di modifica |
| **Vertical Slice** | use case con responsabilità completa attraverso livelli | DLL separata per ogni feature |

La lingua della GUI (`AppLanguage`) è indipendente dal testo e dai criteri linguistici dei trigger (`MatchCulture`, se adottata). Unicode corretto nello storage non implica capacità di digitazione automatica in tutti i processi.
