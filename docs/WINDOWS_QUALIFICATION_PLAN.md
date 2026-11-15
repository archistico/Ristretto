# Piano di qualificazione Windows — picker e hotkey

**Baseline da provare:** Alpha 20.28 · **Roadmap:** R2.01, R2.08, R2.09 e completamento R2.

Questa è la matrice da eseguire, non un rapporto di prove già effettuate. Il richiamo base in Blocco note della 20.25 è confermato dall'utente; non viene trasferito automaticamente ai casi sotto. Tutti i nuovi casi iniziano come `NOT_RUN`.

Dopo la 20.27 l'utente ha nuovamente confermato «tramite hotkey funziona», senza i dettagli necessari per attribuire PASS alle righe. La [prova nativa 20.28](ALPHA20_28_NATIVE_HOTKEY_REGISTRATIONS.md) supera 21 casi su worker/coordinatore, con collisioni fra processi, rilascio e riacquisizione: copre parti dei prerequisiti di CAP-12 e REG-02/03/04/05. Non apre Desktop.App né prova gli interi passi UI della matrice; questi rimangono `NOT_RUN`.

La [suite headless 20.27](ALPHA20_27_CAPTURE_INTEGRATION.md) copre parti di CAP-01/02/05/06/07/10/11 e UI-02, inclusi bozza/SQLite e Tab/Shift+Tab. Usa eventi sintetici e pausa/ripristino simulati: non cambia lo stato dei casi desktop sotto. Provare esplicitamente la combinazione acquisita seguita da Shift+Tab/Invio e la disposizione francese/tedesca, corretti in questa versione.

## Preparazione

Annotare versione/hash di Ristretto, versione/build Windows, versione editor, layout tastiera, lingua, tema, DPI e data. Usare una cartella dati nuova e un documento di prova vuoto, con testo sintetico.

```powershell
dotnet run --project src/TextExpander.Desktop/TextExpander.Desktop.csproj -c Release -- --data-dir "$env:LOCALAPPDATA\Ristretto-Qualifica-20-28"
```

Preparare due gruppi e tre snippet con nomi distinti. Assegnare a uno un testo multilinea con accenti combinanti, emoji ZWJ, CJK e RTL. Scegliere combinazioni disponibili nella sessione; per i casi di collisione registrare chi le possiede. Prima delle prove desktop il gate CORE deve essere PASS.

Per ogni riga conservare: versione, passi effettivi, atteso, osservato, esito e riferimento all'evidenza. Gli esiti ammessi sono `PASS`, `FAIL`, `BLOCKED` e `NOT_RUN`. Un esito non verificabile resta tale; non basta che la chiamata nativa abbia restituito un conteggio positivo.

## Prima sessione: acquisizione e ripristino

| ID | Passi | Risultato atteso | Stato iniziale |
| --- | --- | --- | --- |
| CAP-01 | Aprire Registra…, premere Alt+Ctrl+Shift+B e rilasciare | Display `ALT + CTRL + SHIFT + B`, conferma disponibile solo dopo rilascio | NOT_RUN |
| CAP-02 | Confermare senza premere Salva nella libreria | Bozza modificata, assegnazione persistente precedente ancora in uso | NOT_RUN |
| CAP-03 | Salvare CAP-02 e richiamare da un documento vuoto | Una sola copia del testo; picker chiuso | NOT_RUN |
| CAP-04 | Riaprire la form e premere una combinazione già assegnata a Ristretto | La form la rileva; nessun inserimento o apertura del picker durante la cattura | NOT_RUN |
| CAP-05 | Premere una nuova combinazione e scegliere Annulla | Campo e assegnazione precedenti conservati | NOT_RUN |
| CAP-06 | Ripetere CAP-05 con Esc e con il pulsante di chiusura | Stesso esito di annullamento, registrazioni ripristinate | NOT_RUN |
| CAP-07 | Scegliere Rimuovi hotkey, poi salvare | Campo vuoto; precedente combinazione non richiama lo snippet | NOT_RUN |
| CAP-08 | Tenere premuti modificatori/tasto e generare ripetizioni | Nessuna duplicazione; gesto mostrato coerente, conferma disabilitata finché premuto | NOT_RUN |
| CAP-09 | Dopo Ctrl+B rilasciare B, aggiungere Alt; poi provare un nuovo tasto | Il display non può confermare una vecchia combinazione nascosta | NOT_RUN |
| CAP-10 | Provare F12, tastierino, soli modificatori e due lettere insieme; poi correggere | Tasti non supportati segnalati; nessuna conversione inattesa; nuova combinazione valida acquisibile | NOT_RUN |
| CAP-11 | Usare modificatori sinistri/destri, Tab/Shift+Tab e cambiare focus | Nessun tasto bloccato nello stato, comandi raggiungibili e display coerente | NOT_RUN |
| CAP-12 | Aprire/annullare la form più volte e riprovare picker e hotkey | Registrazioni disponibili al termine; nessuna notifica vecchia riapre il picker | NOT_RUN |

## Seconda sessione: conflitti, catalogo e ciclo di vita

| ID | Passi | Risultato atteso | Stato iniziale |
| --- | --- | --- | --- |
| REG-01 | Salvare la stessa combinazione su due snippet, poi sul picker | Conflitto esplicito, dati precedenti conservati | NOT_RUN |
| REG-02 | Far possedere la combinazione a un'altra app o a un secondo profilo | Configurazione inattiva segnalata; nessuna modifica alla registrazione altrui | NOT_RUN |
| REG-03 | Disabilitare/riabilitare snippet e gruppo | Hotkey disattiva/riattiva secondo il catalogo salvato | NOT_RUN |
| REG-04 | Modificare testo/hotkey, eliminare lo snippet, importare o ripristinare una libreria | Nessun richiamo del vecchio contenuto o delle vecchie assegnazioni dopo il reload | NOT_RUN |
| REG-05 | Lasciare in corso un'operazione, aprire/chiudere la form o uscire | Nessun inserimento obsoleto; nessuna registrazione trattenuta dopo la chiusura del processo | NOT_RUN |
| REG-06 | Doppio avvio, due profili, tray e riavvio | Attivazione corretta per profilo; dati separati e conflitti globali spiegati | NOT_RUN |

## Terza sessione: destinazione, appunti e interfaccia

| ID | Passi | Risultato atteso | Stato iniziale |
| --- | --- | --- | --- |
| IO-01 | Inserire testo multilinea/Unicode con hotkey e picker | Testo salvato nel documento corrispondente alla fixture, una sola volta | NOT_RUN |
| IO-02 | Cambiare finestra/campo o chiudere il target mentre si rilasciano i tasti | Richiesta rifiutata senza invio a un destinatario diverso | NOT_RUN |
| IO-03 | Cambiare gli appunti o simularne l'occupazione durante la preparazione | Nessun incolla di dati estranei o tentativo accodato | NOT_RUN |
| IO-04 | Ripetere il comando durante una copia/inserimento pendente | Una sola operazione; nessun retry dopo esito incerto | NOT_RUN |
| IO-05 | Campi sintetici di sola lettura/protetti e app elevata | Rifiuto o limite compatibile documentato; nessuna estensione implicita della matrice | NOT_RUN |
| UI-01 | Due risultati nel picker, frecce e Invio; poi nessun risultato | Inserimento del selezionato; nessun comando se l'elenco è vuoto | NOT_RUN |
| UI-02 | Resize/minimi, due temi e cinque lingue | Contenuto e azioni leggibili, nessun taglio/sovrapposizione | NOT_RUN |
| UI-03 | DPI 100/125/150/200%, tastiera e focus visibile | Comandi raggiungibili e registrazione comprensibile nelle configurazioni dichiarate | NOT_RUN |
| UI-04 | Catturare libreria, picker e form dopo verifica | Screenshot della versione effettiva, dati sintetici e provenienza annotata | NOT_RUN |

## Come chiudere una sessione

Registrare i difetti con una sequenza riproducibile e una priorità, correggere e rieseguire le righe coinvolte. Collegare ciascun risultato a R2 nella roadmap. Non chiudere R2.08/R2.09 sulla sola base dei test di stato o di integrazione UI headless.

Per una ricevuta del documento salvato in Blocco note è disponibile anche la [procedura guidata](BUILD_AND_TEST.md#4-prova-guidata-del-picker-in-blocco-note). Le prove su Edit posseduto appartengono a un perimetro distinto dagli editor esterni.
