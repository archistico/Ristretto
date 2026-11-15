# Alpha 20.26 — Registra la hotkey premendo i tasti

**15 novembre 2026 · 3.9.0-alpha.20.26.** L'utente ha confermato che il richiamo tramite hotkey della 20.25 funziona in Blocco note Windows. Questa conferma riguarda il flusso base, non tutti gli editor o i casi di conflitto.

## Uso

1. Nella libreria apri uno snippet e premi **Registra…**, accanto al campo hotkey.
2. Nella finestra **Registra hotkey** premi la combinazione desiderata. L'area ha già il focus; puoi anche cliccarla per riprendere l'acquisizione.
3. Vedrai i tasti in maiuscolo e separati, per esempio **`ALT + CTRL + SHIFT + B`**. Rilascia tutti i tasti: la combinazione resta visibile per controllarla.
4. Premi **Usa combinazione**, poi **Salva** nella libreria per applicarla allo snippet.

Puoi premere un'altra combinazione per correggerla. **Annulla**, Esc o la chiusura della finestra lasciano invariata la bozza. **Rimuovi hotkey** svuota il campo, sempre da salvare con il normale pulsante della libreria. La scrittura manuale del campo rimane disponibile.

Il display mostra esattamente i modificatori rilevati: ALT, CTRL, SHIFT e WIN, seguiti dal tasto principale. Il formato salvato resta compatibile con le hotkey già esistenti. Sono ammesse le stesse combinazioni della 20.25: almeno Ctrl o Alt e una lettera, cifra della tastiera principale, Space o F1–F11. Tasti non supportati e più tasti principali contemporanei vengono mostrati ma non confermati. Il tastierino numerico non viene convertito silenziosamente nelle cifre della tastiera principale.

Durante l'acquisizione Ristretto sospende le proprie hotkey globali, comprese quelle già assegnate: così i tasti arrivano alla form senza inserire testo né aprire il picker. Alla chiusura le registrazioni precedenti vengono ripristinate, anche dopo annullamento o errore; se l'applicazione sta uscendo non vengono ricreate. Le hotkey appartenenti ad altre applicazioni o riservate dal sistema possono intercettare una combinazione prima che raggiunga la form. I conflitti di assegnazione rimangono verificati al salvataggio.

## Implementazione e verifiche

- `HotkeyCaptureState` interpreta soltanto gli eventi della finestra locale: display stabile dopo il rilascio, ripetizioni ignorate, nomi dei tasti coerenti con il contratto di registrazione e conferma disabilitata per gesti incompleti.
- `HotkeyCaptureCoordinator` mantiene sospese le notifiche fino al completamento del ripristino; protegge anche da aperture concorrenti. I worker del picker e degli snippet vengono fermati e ricreati senza scrivere impostazioni temporanee nel database.
- Le notifiche accodate dal vecchio worker del picker sono invalidate; le richieste di inserimento pendenti vengono annullate. Un reload già in corso viene considerato prima del ripristino finale.
- Testi nelle cinque lingue; nessuna modifica allo schema SQLite 2, al JSON o alle regole di inserimento.

**846 xUnit passati**, inclusi **26 nuovi casi**. Due regressioni del display sono state prima riprodotte e poi corrette: cambio dei modificatori dopo il rilascio del tasto principale e correzione di un tasto non supportato mantenendo Ctrl premuto. Display e combinazione confermata restano coerenti.

**GATE CI: PASS**, anche da una nuova estrazione dello ZIP inizialmente priva di `bin`/`obj`: solution, quattro laboratori e DevTools compilati senza warning, 846 xUnit, 23 regressioni C#, dieci controlli CLI, 32 Node e contratto .NET→Node passati. Verificati 492 hash del manifest e la corrispondenza degli hash dei sorgenti fra le due cartelle. Restore NuGet locale; audit vulnerabilità online non eseguito. Gli aggiornamenti conclusivi ai rapporti non modificano il codice compilato.

Le nuove prove esercitano lo stato reale di acquisizione e il coordinatore con callback sintetici: nessun backend Avalonia, hotkey globale, appunto o input nativo viene avviato. Routing dei tasti nella nuova form, focus e sospensione/ripristino effettivi sul desktop restano da qualificare. La conferma utente su Blocco note della 20.25 è conservata separatamente e non trasferita automaticamente al nuovo dialogo.

[Rapporto](../reports/ALPHA20_26_CHECKS.md) · [JSON](../reports/ALPHA20_26_VALIDATION.json) · [Roadmap](ROADMAP.md).
