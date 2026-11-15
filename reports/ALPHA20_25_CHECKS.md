# Verifiche Alpha 20.25

**Versione:** 3.9.0-alpha.20.25 · **Data:** 15 novembre 2026.

Due errori CS0246 della 20.24 riprodotti; import del namespace corretto e baseline CORE superata con 779 xUnit. Aggiunte hotkey per snippet, schema SQLite 2 e 41 regressioni: suite completa a **820 xUnit passati**.

**GATE CI: PASS** anche da ZIP estratto in cartella nuova, inizialmente senza `bin`/`obj`: solution e cinque tool compilati, 820 xUnit, 23 regressioni C#, dieci CLI, 32 Node e contratto .NET→Node passati. Nessun warning nella build finale. Verificati 483 hash del manifest e la corrispondenza dell'hash sorgenti fra le due cartelle; gli aggiornamenti conclusivi ai rapporti non modificano il codice compilato. Restore NuGet locale, audit vulnerabilità online `NOT_RUN`.

I test usano dati sintetici e API native simulate. Registrazione delle nuove hotkey, clipboard e incolla effettivo sul desktop, GUI, osservatore WinEvent e vero host VS Code restano da provare.

Gli esiti dei gate, il metodo di restore e gli hash sono nel [rapporto JSON](ALPHA20_25_VALIDATION.json). [Log](alpha20-25-validation.log) · [Uso e limiti](../docs/ALPHA20_25_SNIPPET_HOTKEYS.md).
