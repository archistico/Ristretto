# Stato corrente — Ristretto 3.9.0-alpha.20.19.5

## Dove siamo

- **R1:** C#/.NET 10, Avalonia, SQLite, import/export e quality gate. Strumenti di validazione migrati da Python a un tool C# (`tools/Ristretto.DevTools`) nella 20.19.4; hotfix CS9174 applicato nella 20.19.5.
- **R2:** picker, ricerca Unicode e hotkey configurabile; il test manuale Notepad ha dato `PASS` sul documento sintetico, con inserimento confermato dall'utente. Resta da qualificare il comportamento in altri editor.
- **R3:** adapter VS Code facoltativo; per ora non è prioritario.
- **R5:** motore di trigger Unicode e laboratorio Win32 per ricerca e confini di focus. Non esiste ancora un'espansione automatica globale qualificata.

## Evidenze e stato dei test

- **Alpha 20.19.3:** `GATE CORE: PASS` confermato dall'utente, con compilazione e xUnit superati.
- **Alpha 20.19.4:** `validate-source.cmd` e `validate.cmd` si arrestano durante la compilazione di `Ristretto.DevTools` con **due errori CS9174** in `NotepadQualification.cs`, come confermato dall'utente. Non implicano un errore del picker.
- **Alpha 20.19.5:** due chiamate corrette e regressioni C# aggiunte. L'ambiente di preparazione non dispone dell'SDK .NET 10: **build C#, regressioni C# e gate Windows NON ESEGUITI**. Non segnare `PASS` fino alla verifica effettiva.

Sono rimasti invariati il codice dell'applicazione, il picker, la hotkey e SQLite.

## Obiettivo

Espandere automaticamente un trigger come `;firma` nelle applicazioni compatibili, rispettando Unicode, IME, focus e sicurezza dei dati. Il picker rimane la modalità manuale di riserva.

## Prossimi passi

1. Confermare su Windows il nuovo gate C# (`.\scripts\validate-source.cmd`, poi `.\scripts\validate.cmd`) e l’assenza degli errori CS9174 e del warning xUnit2000.
2. Correggere ogni regressione emersa dalla migrazione degli strumenti, senza modificare il picker funzionante.
3. Riprendere R5: provenienza del testo Unicode committato e supporto degli editor esterni qualificati.

Per istruzioni complete vedere [BUILD_AND_TEST](BUILD_AND_TEST.md) e per la sequenza dei lavori [ROADMAP](ROADMAP.md).
