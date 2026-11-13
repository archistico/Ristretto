# macOS Text Replacements — scheda di ricerca

**Categoria:** Funzione OS integrata, benchmark minimo. **Piattaforma osservata:** macOS, app che la supportano. **Licenza/modello:** Componente del sistema.  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-17.

## 1. Funzionalità documentate

Shortcut e replacement tramite Impostazioni Tastiera; gestione nel sistema e in molte app ma non tutte.

## 2. Punto di forza

Senza daemon terzo, UI minima e buona integrazione con tastiere e dizionari utente anche per cinese/giapponese.

## 3. Limiti / compromessi

Apple dichiara esplicitamente che sostituzioni e quote smart non sono disponibili in tutte le app; non è un gestore ricco di librerie.

## 4. Meccanismi da studiare / vantaggio per noi

Rilevare/descrivere conflitti con sostituzioni di sistema; non inventare compatibilità universale della nostra app.

## 5. Cosa testare più avanti

Provare keyword identiche in app, quando espandono entrambe e come la UI può avvertire l’utente.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://support.apple.com/guide/mac-help/replace-text-punctuation-documents-mac-mh35735/mac
