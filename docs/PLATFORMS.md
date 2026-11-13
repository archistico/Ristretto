# Supporto piattaforme — capacità e rischi (v1.7)

**Nessun backend è ancora implementato o testato. Tutti i valori sono obiettivi, candidati o limitazioni documentali.**

| Funzionalità | Windows | macOS | Linux X11 | Linux Wayland |
|---|---|---|---|---|
| Avalonia GUI | Obiettivo | Obiettivo | Obiettivo | Obiettivo |
| Tray | Documentata Avalonia | Documentata | Dipende da DE/AppIndicator | Dipende da DE/portal/tray |
| Global shortcut | OS API da provare | API/permessi da provare | X11 da provare | Portal GlobalShortcuts, backend-dependent |
| Osservazione di tasti | Hook con vincoli | Quartz Event Tap e permessi | X11 | Non disponibile universalmente per processi normali |
| Osservazione **testo committato** | Non garantita da hook fisico | Non garantita da event tap | Non garantita da keycode | Text-input v3 vale nel contesto client, non come sniff globale |
| Identificazione target protetto | UIA parziale, fail-closed | Accessibility/Secure Input, fail-closed | Non universalmente attestata | Per terzi spesso non disponibile |
| Unicode output sintetico | KEYEVENTF_UNICODE, con limiti target/UIPI | CGEvent/testare | XTest limitato da layout | Iniezione solo quando supportata/autorizzata |
| Clipboard multi-formato | Win32 | NSPasteboard | PRIMARY/CLIPBOARD | Protocolli/permessi dipendenti dal compositor |
| Espansione auto | Da qualificare | Da qualificare | Da qualificare | Da qualificare **per compositor** |
| Fallback | Picker/copia | Picker/copia | Picker/copia | GUI+picker+copia, secondo capacità |

## Punti verificati documentalmente

- Windows `WH_KEYBOARD_LL` va gestito con callback breve e loop messaggi. `SendInput` è soggetto a UIPI; `KEYEVENTF_UNICODE` non certifica tutte le app. Clipboard serial number non è un lock.
- macOS Quartz Event Tap richiede autorizzazioni adeguate e NSPasteboard è multi-tipo. Secure Input e permessi possono limitare il funzionamento.
- X11 mantiene `PRIMARY` distinto da `CLIPBOARD`. Anche se XTest è disponibile, l'input Unicode non è automatico.
- Su Wayland `GlobalShortcuts` abilita azioni, non intercettazione del testo; `RemoteDesktop` abilita invio input in sessione approvata; `InputCapture` è attivata dal compositor; text-input-v3 opera nel contesto del client.

## Policy di dichiarazione supporto

Supporto condizionato da prove OS+release+sessione+compositor+app+permessi+backend+layout+IME+Unicode. Nessuna prova sostituisce un'altra. Vedere [policy delle capacità](PLATFORM_CAPABILITY_POLICY.md), [contratto tecnico](NATIVE_INPUT_OUTPUT.md) e [piano degli spike](../research/NATIVE_SPIKE_PLAN.md).

Fonti: [Evidence v1.7](../research/NATIVE_SOURCE_EVIDENCE.md).

## Contratti d'architettura v2.3

Le API e il confine `Application ↔ Infrastructure/Platform` sono descritti in [PLATFORM_ADAPTER_CONTRACTS.md](PLATFORM_ADAPTER_CONTRACTS.md). Il nuovo documento contiene soltanto **contratti candidati**, non un incremento di supporto verificato. Wayland, IME e campi protetti rimangono da qualificare.
