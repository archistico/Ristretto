# Visione del prodotto

## Problema da risolvere

Le persone ripetono testi, firme, risposte, snippet SQL/codice e simboli Unicode in applicazioni diverse. Beeftext è un ottimo riferimento funzionale, ma è Windows-only ed è in modalità manutenzione; Espanso è multipiattaforma ma la configurazione YAML è meno accessibile per molti utenti. Gli strumenti più potenti spesso richiedono un'interfaccia più articolata, servizi cloud oppure non coprono Linux.

## Promessa del nuovo prodotto

> Un text expander **rapido, affidabile, visivamente moderno, multilingua e open source**, utilizzabile localmente su Windows, macOS e Linux, con un motore che esplicita la compatibilità reale di ciascuna piattaforma.

## Principi non negoziabili

1. Unicode end-to-end (emoji ZWJ, accenti, alfabeti non latini, RTL, IME) come obiettivo con test specifici, non slogan.
2. Semplicità UX: gruppi/snippet, ricerca/picker sempre disponibili, editor con livelli avanzati opzionali.
3. Nessun account, cloud, telemetria, AI o scripting obbligatori; preferenze e libreria locali per default.
4. Integrità del contenuto e privacy precedono l'espansione: meglio rifiutare una sostituzione incerta che inserire nella finestra sbagliata.
5. Portabilità realistica: su Wayland e contesti protetti non promettere hook universali.
6. Architettura Vertical Slice pragmatica; nessuna proliferazione di microservizi, bus o repository astratti senza caso concreto.
7. GUI coerente e riconoscibile, tema chiaro/scuro/sistema, effetti delicati, basso consumo, accessibilità.
8. Licenza del progetto ancora da selezionare (MIT/Apache-2.0/GPL solo dopo valutazione delle dipendenze e governance).

## Utenti e scenari

- Personale amministrativo: risposte email standard con data, form compilabile, firma.
- Sviluppatori e DBA: SQL/query, comandi shell, frammenti che devono rimanere **testo letterale**, non script eseguito.
- Utenti internazionali: testo misto in più lingue, emoji, alfabeti non latini.
- Utenti power: regole per app, trigger controllati, picker e scorciatoie.
- Utenti Linux: capacità dichiarate in base alla sessione grafica, workaround sicuri in mancanza di espansione globale.

## Non-obiettivi per la prima versione

Automazione generale di finestre e mouse, servizi cloud proprietari, supporto mobile, cooperazione real-time, marketplace di plugin eseguibili, script di terzi non attendibili e rich text inter-app universale.
