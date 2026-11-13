# Hammerspoon — scheda di ricerca

**Categoria:** API macOS per automazione (riferimento tecnico). **Piattaforma osservata:** macOS. **Licenza/modello:** Open source (verifica licenza repo).  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-16.

## 1. Funzionalità documentate

`hs.eventtap` osserva/può trasformare eventi input e offre keyStrokes/keyStroke, API tramite Lua.

## 2. Punto di forza

Mostra la distinzione tra keyStrokes testuali e combinazioni di tasti, oltre alle API di intercept e target application.

## 3. Limiti / compromessi

Non è un'app text-expander pronta per utenti finali, richiede scripting e permessi; rischio ampiezza del motore.

## 4. Meccanismi da studiare / vantaggio per noi

Studiare contract native adapter e differenza testo/tasti senza dipendere da Lua o imitarne API generali.

## 5. Cosa testare più avanti

Confrontare comportamento `keyStrokes` su emoji e input field con tool di test macOS.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://www.hammerspoon.org/docs/hs.eventtap
- https://www.hammerspoon.org/
