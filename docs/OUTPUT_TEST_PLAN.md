# Piano di verifica del coordinatore e dei metodi di output — v2.6

**Solo specifica.** I test descritti non sono implementati né eseguiti. [Matrice CSV](../data/OUTPUT_SCENARIOS_v2.6.csv) con ID, priorità, livello e risultato atteso. Non duplicare nei test finali la copertura già presente in [matcher](MATCHER_TEST_PLAN.md) e [dominio](DOMAIN_TEST_PLAN.md).

## Livelli delle prove

| Livello | Ambiente | Verifica | Evidenza prevista |
|---|---|---|---|
| `UNIT` | matcher, fake adapter / clock controllato | transizioni di stato, token mismatch, stop dopo errori, no retry | test deterministico, ordine delle chiamate |
| `CONTRACT` | fake backend con stati e ownership modellati | accordo fra `ITextOutput`, clipboard lease e coordinator | matrice errori, esplicito punto di possibile effetto |
| `OS` | macchina reale, permessi concessi | eventi inviati, targhetta focus, clipboard ownership, limita API | OS/versione, layout, permessi, traccia senza dati utente |
| `E2E` | applicazioni di destinazione reali | effettiva stringa e caret, emoji/IME, interruzioni durante paste | screenshot sintetico approvato o confronto input/output in test app non sensibile |
| `SECURITY` | log, sandbox, campi protetti | nessun input sensibile registrato, fallback | report con privacy review |

## Matrici ambiente obbligatorie

- Windows: editor Win32, WPF, browser Chromium/Firefox, Office compatibile, app Electron; normal/elevated; layout IT/DE/US e IME CJK.
- macOS: TextEdit, browser, app Electron e campo Secure Input; Accessibilità/Monitoring con permessi attivi/negati; input source internazionale.
- Linux X11: KDE/XFCE/GNOME Xorg dove disponibili; `PRIMARY` vs `CLIPBOARD`; clipboard manager e cambio layout.
- Wayland: GNOME/KDE/wlroots come configurazioni **separate**; permessi sessione, disponibilità picker/manual fallback; non assumere supporto globale.
- Dataset Unicode: NFC/NFD, emoji ZWJ/skin tones, sequenze con VS16, combinazioni Devanagari, CJK/IME, RTL, tab/newline, codici SQL/JSON letterali.

## Gate di accettazione

1. Nessun effetto se `Protected`, `Composing`, `TargetChanged` prima della prima azione.
2. Nessun doppio output dopo `PartialOrUnknown` o perdita di focus a metà tentativo.
3. Clipboard nuova dell'utente mai sovrascritta quando il sistema segnala cambio ownership.
4. `SubmittedUnverified` mai promosso automaticamente a `VerifiedComplete` per solo successo API.
5. Nessuna cancellazione di trigger con emoji composta in target non qualificato.
6. Cleanup gestisce errore/annullamento/uscita dal picker senza lasciare hook o modificatori incastrati.
7. L'interfaccia comunica con precisione le limitazioni senza rivelare dati inseriti.
8. Almeno un report riproducibile per OS/desktop/backend prima di dichiarare una capacità distribuita come supportata.

## Evidenza per ogni test nativo

`ScenarioId`, OS, versione, app/editor/versione, backend, layout, IME, permessi, azioni, expected, actual, outcome, log con redaction, classificazione (`PASS`, `FAIL`, `SKIPPED` + motivo). Durante lo studio tutti i record hanno `NOT_RUN`. Non basare i test di Unicode solely su `StringInfo` o string lengths; il risultato osservato va confrontato con dati UTF-8 e segmentazione attesa, e successivamente con comportamento del target reale.

## Criteri di triage

- P0 perdita testo, inserimento in target sbagliato, copia utente sovrascritta, password capture, auto-retry dopo effetto.
- P1 Unicode/caret errato, clipboard stale, notifiche ingannevoli, il motore si blocca dopo errore.
- P2 supporto app specifica/documentazione, ritardi empirici, ergonomia del picker.

Riportare sempre `NOT_RUN` fino a prova effettiva con allegati e ambiente descritto.
