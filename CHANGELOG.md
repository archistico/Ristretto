## 3.9.0-alpha.20.19.5 — Hotfix compilazione DevTools CS9174

- Corrette due collection expression in `NotepadQualification.cs` che su Windows impedivano la compilazione (`CS9174`, righe 173 e 198 della Alpha 20.19.4).
- Introdotto `WithoutUtf8Bom(ReadOnlySpan<byte>)`, controllo esplicito e condiviso dell'intestazione BOM UTF-8, senza buffer aggiuntivi.
- Aggiunti tre test C# per documento UTF-8 con BOM, BOM parziale e fixture BOM preparata e riaperta.
- Rafforzato `SourceAudit` per segnalare il ritorno della sintassi problematica, e allineato il test sulla versione corrente.
- Applicazione desktop, picker, hotkey e database non modificati. Compilazione .NET/xUnit della nuova versione da eseguire su Windows.


## 3.9.0-alpha.20.19.4 — Strumenti C# e warning xUnit2000

- Conferma utente: gate CORE della Alpha 20.19.3 superato.
- Eliminata la dipendenza dagli script Python per audit, gate, controlli statici, prove CLI, qualificazione Notepad e packaging; introdotto il progetto C# `Ristretto.DevTools`.
- Aggiornati script di avvio e workflow CI per utilizzare .NET 10.
- Corretto warning `xUnit2000` su `Assert.Equal` in `WindowsExternalAdapterCapabilityTests`.
- Aggiunti test C# degli strumenti e consolidata la documentazione in italiano.
- Build e test della nuova release **da validare** su .NET 10 Windows; il picker non è stato modificato.
