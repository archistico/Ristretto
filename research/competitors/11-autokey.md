# AutoKey — scheda di ricerca

**Categoria:** Text expansion e script su Linux X11. **Piattaforma osservata:** Linux X11. **Licenza/modello:** Open source (licenza del repo da verificare).  
**Stato di evidenza:** funzioni basate su fonti pubbliche del produttore, limitazioni talvolta dedotte dall'ambito; NON testato direttamente. Consultazione: 12-11-2026.  
**Identificatore scheda:** CMP-11.

## 1. Funzionalità documentate

Phrases, scripts Python, abbreviazioni, hotkey, popup menu e macro; documentazione esplicita X11.

## 2. Punto di forza

Mostra come si gestisce un engine Linux applicativo completo con trigger e shortcut; buona base di limiti su layout internazionali.

## 3. Limiti / compromessi

Il README chiarisce che su Wayland non funziona correttamente; la documentazione segnala problemi noti su layout russo/mandarino/giapponese.

## 4. Meccanismi da studiare / vantaggio per noi

Progettare adapter X11 e compatibilità dichiarate; evitare far passare la compatibilità X11 per Wayland.

## 5. Cosa testare più avanti

Analizzare trigger/phrase e testare con X11, tastiera IT, JP IME e ambienti Wayland senza workaround rischiosi.

## 6. Decisione operativa per il nostro progetto

**Non copiare indiscriminatamente.** Registrare la caratteristica nel feature backlog; priorità e scope secondo `docs/PRODUCT_REQUIREMENTS.md`, usando le fonti per risalire alle semantiche e uno spike se dipende da API native. Limitazioni e compatibilità non verificate restano ipotesi.

## 7. Fonti (primarie ove disponibili)

- https://autokey.github.io/intro.html
- https://github.com/autokey/autokey
