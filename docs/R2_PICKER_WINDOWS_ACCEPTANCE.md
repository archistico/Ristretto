# R2.01 — Matrice di accettazione del picker su Windows

**Stato:** aggiornata in Alpha 20.16. L’utente ha confermato in Alpha 20.15 il caso base Invio→Notepad; le prove Unicode file-backed, DPI, clipboard e target negativi restano aperte. H06/H07 hanno test C# simulati, non prove native.

## Preparazione

1. Estrarre la versione completa in una cartella nuova; usare solo un profilo sintetico distinto (`--data-dir`) e verificare `--check-profile` prima di aprire la GUI.
2. Registrare versione Windows, scala DPI, layout tastiera, build Ristretto, tipo e versione dell'editor di destinazione, senza nomi utente, PID, path personali o contenuto degli snippet.
3. Creare una libreria fittizia con trigger `;firma`, un corpo multilinea con `caffè`, un'emoji ZWJ e testo non latino. Non usare dati reali. Preparare Notepad e un secondo campo sintetico per i rifiuti sensibili.
4. Per ogni caso annotare `PASS`/`FAIL`/`NOT_RUN`, motivo sintetico e se l'output è stato **osservato nell'editor** oppure è stato soltanto accettato dall'API di invio.

## Casi da eseguire

| ID | Verifica | Risultato richiesto |
| --- | --- | --- |
| R2-H01 | Hotkey predefinita con editor in primo piano | Picker aperto e destinatario originario conservato |
| R2-H02 | Hotkey modificata nelle impostazioni e riavvio | La nuova scorciatoia richiama il picker; la preferenza persiste |
| R2-H03 | Hotkey disattivata (`None`) | Nessuna apertura tramite scorciatoia globale |
| R2-H04 | Scorciatoia già registrata altrove | Rifiuto del conflitto senza perdere l'impostazione precedente |
| R2-H05 | Avvio minimo in tray e secondo avvio | Nessuna finestra duplicata; profilo/istanza corretti |
| R2-H06 | Hotkey ripetuta mentre picker aperto e attivo | Target originale conservato, Inserisci disponibile se lo era, filtro non perso |
| R2-H07 | Picker visibile, ritorno all'editor B e nuova hotkey | Target A invalidato; nuovo Inserisci destinato solo a B |
| R2-S01 | Ricerca trigger, nome e corpo in italiano | Risultati corretti e ordinamento atteso |
| R2-S02 | NFD/NFC, emoji ZWJ e testo non latino | Ricerca robusta; nessuna sequenza spezzata |
| R2-S03 | Modifica filtro durante caricamento | Nessun risultato o errore della richiesta precedente sovrascrive il corrente |
| R2-C01 | Pulsante Copia Unicode multilinea | Contenuto esatto verificato leggendo dagli appunti dopo l'azione |
| R2-C02 | Appunti occupati o comando concorrente | Esito esplicito, senza inserimento differito o seconda operazione |
| R2-L01 | **Test interattivo su EDIT sintetico multilinea** (`scripts\validate-picker-desktop.cmd`) | Confronto UTF-16 esatto *nel processo proprietario*, ricevuta legata alla sessione; inizialmente `NOT_RUN`, non equivale a Notepad |
| R2-L02 | Ricevuta del laboratorio mancante, malformata, obsoleta oppure controllo terminato | `NOT_RUN` senza riutilizzare una risposta precedente e senza dichiarare inserimento verificato |
| R2-L03 | Contenuto nel controllo diverso dalla fixture (inclusi LF/CRLF o NFC/NFD) | `FAIL`; nessun tentativo automatico di invio successivo |
| R2-I01 | Inserisci elemento selezionato in Notepad | Picker chiuso e testo esatto visibile nel campo atteso |
| R2-I01c | **Alpha 20.16:** generare un profilo SQLite sintetico con `seed-picker-notepad.cmd TOKEN` | Un solo snippet con fixture Unicode esatta, nuovo profilo isolato; rifiuta un secondo tentativo. Prova Windows NOT_RUN |
| R2-I01a | **Alpha 20.12:** `validate-picker-notepad.cmd prepare --open`, Inserisci e salvataggio nel file sintetico, poi `verify --session ID` | `PASS` solo se il file salvato coincide con la fixture; se invariato `NOT_RUN`, se diverso `FAIL`; NON prova quale azione ha modificato il file |
| R2-I01b | Documento sintetico con accenti NFD/NFC, ZWJ, arabo/giapponese e LF/CRLF | Caratteri Unicode preservati esattamente, conversione CRLF/LF tollerata; nessuna normalizzazione NFC/NFD |
| R2-I02 | Invio con filtro attivo | Inserito il **primo risultato filtrato**, non un elemento precedentemente selezionato |
| R2-I03 | Inserisci Unicode multilinea | Emoji, RTL, accenti e newline preservati nell'editor |
| R2-I04 | Cambio focus/chiusura controllo prima di Inserisci | Operazione negata; nessun testo scritto nell'altra finestra |
| R2-I05 | Modificatori premuti/clipboard modificata | Operazione rifiutata o esito incerto; nessun retry automatico |
| R2-I08 | La sequenza clipboard cambia ma il testo contiene contenuto di altro programma | Rifiuto prima di Ctrl+V, nessuna sostituzione, appunti non ripristinati automaticamente |
| R2-I09 | Un'altra app sostituisce la clipboard durante il readback o prima dell'invio | Rifiuto se il cambio è osservato, nessun retry; eventuale race non osservabile = esito incerto |
| R2-I10 | Child window distrutta, disabilitata o PID diverso prima di inviare | Rifiuto del destinatario; nessun invio verso altro controllo |
| R2-I11 | Destinatario cambia mentre la preparazione clipboard è sospesa | Nessun invio al nuovo controllo; se il focus torna durante la race, prova non conclusiva |
| R2-I12 | **Alpha 20.13:** focus diverso o child non disponibile durante l'attesa iniziale | Nessuna chiamata `SetTextAsync`, nessun invio di input; appunti precedenti intatti |
| R2-I13 | **Alpha 20.13:** altra applicazione aggiorna la clipboard prima che il focus sia confermato | Inserisci rifiutato **prima** di scrivere la clipboard; nessun `Ctrl+V` |
| R2-I14 | **Alpha 20.13:** focus o clipboard cambiano durante la scrittura asincrona | Il preflight finale rifiuta `Ctrl+V`; clipboard può già essere modificata e non va ripristinata alla cieca |
| R2-I06 | Campo password e campo sola lettura sintetici | Inserisci non disponibile oppure rifiutato senza alterazioni |
| R2-I07 | Editor con privilegi più elevati | Rifiuto coerente; non promettere scrittura sicura |
| R2-G01 | Chiaro/scuro e cinque lingue | Etichette coerenti e pulsanti visibili, nessun testo tagliato |
| R2-G02 | Scale 100%, 150%, 200% | Focus, selezione e comandi utilizzabili senza sovrapposizioni |
| R2-P01 | Profili temporanei A/B con cartelle distinte | Nessuna contaminazione di impostazioni, libreria o istanze |

## Criterio di chiusura

Per una prova di inserimento **non è sufficiente** `SendInput` riuscito: bisogna osservare l'effettivo contenuto nel destinatario. Se il risultato è incerto, registrare incertezza e non riprovare automaticamente. L'accettazione di R2 richiede la verifica delle prove principali sul desktop e la gestione dei fallimenti; i gate headless in CI non la sostituiscono.

La matrice qualifica il picker **manuale**. Non certifica l'espansione automatica di `;firma` digitato nelle altre applicazioni, che richiede ancora un adapter di prodotto.

## Alpha 20.14 — continuità e annullamento (NOT_RUN sul desktop)

Per ogni scenario usare soltanto un profilo di prova e dati sintetici; registrare
separatamente l'esito del test unitario e l'osservazione desktop:

| Caso | Preparazione | Atteso | Stato desktop |
| --- | --- | --- | --- |
| AN14-01 | Annullare durante l'ultimo controllo modificatori | Nessun `Ctrl+V` se Cancel precede la prenotazione | NOT_RUN |
| AN14-02 | Annullare durante il confronto finale della sequenza appunti | Nessun `Ctrl+V` se Cancel precede la prenotazione | NOT_RUN |
| AN14-03 | Aprire nuova sessione durante il preflight della precedente | Vecchia richiesta rifiutata; nuova sessione indipendente | NOT_RUN |
| AN14-04 | Richiedere l'annullamento dopo inizio invio nativo | Nessun retry; esito non dichiarato revocato o verificato | NOT_RUN |

**Limite:** gli stati di invio sono classificazioni native, non il contenuto
osservato nel documento. Per la qualifica reale usare il laboratorio Edit
posseduto e la prova Notepad su file sintetico.

## Regressione Alpha 20.15 — gesto Invio (percorso base confermato dall’utente su Notepad)

| Caso | Azione | Accettazione |
| --- | --- | --- |
| R2-ENTER-01 | Da Notepad, hotkey e Invio con risultati già caricati | Primo risultato filtrato inviato una sola volta al controllo originale; nessuna ripetizione |
| R2-ENTER-02 | Hotkey e Invio subito, durante il caricamento | Quando i risultati diventano disponibili, stesso filtro e stesso focus: al massimo un invio; se contesto cambia: nessun invio |
| R2-ENTER-03 | Enter mantenuto premuto | Nessuna pressione ripetuta o Return residuo nell'editor dopo il cambio focus |
| R2-ENTER-04 | Esc/cambio focus durante caricamento | Nessun invio differito in finestra diversa |
| R2-ENTER-05 | Hotkey tardiva mentre Inserisci sta inviando | La richiesta in corso non viene annullata/ricatturata; il destinatario non cambia |
| R2-ENTER-06 | Annullamento esplicito prima della prenotazione nativa | Nessun `Ctrl+V`; nessun retry |
| R2-ENTER-07 | Rifiuto in Notepad | Picker ripresentato con codice diagnostico fisso (senza contenuti) |

## Alpha 20.17 — stato sessione Notepad e passaggio interattivo

La sequenza `prepare` → `seed` → `verify` **senza usare il picker nel documento sintetico e salvarlo** produce correttamente `NOT_RUN`. Prima della prova usare `status --session TOKEN` e `launch --session TOKEN` per confermare l’isolamento della libreria. Il test `PASS` resta subordinato a un salvataggio manuale della fixture. [Guida](ALPHA20_17_NOTEPAD_GUIDED_SESSION.md).

## Alpha 20.18 — diagnostica in italiano della prova Notepad

La prova usa soltanto il file sintetico salvato: `verify` non invia tasti e `PASS` non prova la provenienza dell'azione. `prepare --open` conserva il token anche se Blocco note non si avvia; `status` non scrive ricevute. [Guida passo per passo](ALPHA20_18_NOTEPAD_DIAGNOSTICS.md).

| Caso | Prova | Accettazione |
| --- | --- | --- |
| R2-NP-18-01 | Documento ancora invariato | `NOT_RUN` con spiegazione italiana; mai `FAIL` del picker |
| R2-NP-18-02 | Contenuto Unicode multilinea salvato esattamente | `PASS`, ricevuta senza testo, LF/CRLF equivalenti |
| R2-NP-18-03 | Due inserimenti consecutivi | `FAIL` / `snippet_inserted_twice` |
| R2-NP-18-04 | Inserimento nel punto sbagliato | `FAIL` / `snippet_inserted_at_unexpected_position` |
| R2-NP-18-05 | Accenti normalizzati NFC/NFD | `FAIL` / `unicode_normalization_changed`, nessuna conversione tacita |
| R2-NP-18-06 | Notepad non disponibile durante `prepare --open` | Sessione conservata, avviso in italiano, nessun token perso |

Questi casi sono **predisposti nei test Python** ma non attestano il desktop Windows; il loro esito nativo rimane aperto.
