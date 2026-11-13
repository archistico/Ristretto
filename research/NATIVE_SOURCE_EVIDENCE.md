# Fonti primarie, evidenza e limiti — integrazioni native v1.7

**Ultima ricognizione: 12-11-2026.** `D`: documentazione primaria consultata; `I`: inferenza prudente nostra; `V`: verifica empirica da eseguire. Non confondere `D` con certificazione del comportamento dell'app.

| ID | Tema | Cosa supporta la fonte | Tipo | URL |
|---|---|---|---|---|
| NAT-S01 | Windows low-level hook | Message loop, timeout, comportamento di callback e possibile rimozione silenziosa | D | https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc |
| NAT-S02 | Windows synthetic flags | `LLKHF_INJECTED`/`LOWER_IL_INJECTED` | D | https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct |
| NAT-S03 | Windows `SendInput` | UIPI e limiti integrità | D | https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput |
| NAT-S04 | Windows Unicode input | `KEYEVENTF_UNICODE`, `VK_PACKET` | D | https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput |
| NAT-S05 | Windows clipboard serial | `GetClipboardSequenceNumber`, delayed rendering caveat | D | https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboardsequencenumber |
| NAT-S06 | Windows clipboard multi-format | Delayed rendering, owner e formati | D | https://learn.microsoft.com/en-us/windows/win32/dataxchg/clipboard-operations |
| NAT-S07 | Windows password UIA | `UIA_IsPasswordPropertyId` | D | https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-automation-element-propids |
| NAT-S08 | macOS Event Taps | Quartz osserva/filtra eventi previa autorizzazione | D | https://developer.apple.com/documentation/coregraphics/cgevent/tapcreate%28tap%3Aplace%3Aoptions%3Aeventsofinterest%3Acallback%3Auserinfo%3A%29 |
| NAT-S09 | macOS Accessibility | `AXIsProcessTrustedWithOptions` | D | https://developer.apple.com/documentation/applicationservices/1459186-axisprocesstrustedwithoptions |
| NAT-S10 | macOS NSPasteboard | multi-item/multi-formato, owner | D | https://developer.apple.com/documentation/appkit/nspasteboard |
| NAT-S11 | macOS changeCount | varia al cambio proprietà del pasteboard | D | https://developer.apple.com/documentation/appkit/nspasteboard/changecount |
| NAT-S12 | X11 clipboard | PRIMARY vs CLIPBOARD, selezioni e ownership | D | https://specifications.freedesktop.org/clipboard/latest/ |
| NAT-S13 | XDG GlobalShortcuts | sessioni di scorciatoie globali autorizzate | D | https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.GlobalShortcuts.html |
| NAT-S14 | XDG RemoteDesktop | invio input tramite libei o Notify* in sessioni consentite | D | https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.RemoteDesktop.html |
| NAT-S15 | XDG InputCapture | attivazione sotto controllo compositor; non cattura libera | D | https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.InputCapture.html |
| NAT-S16 | Wayland text-input v3 | testo preedit/commit e scope campo client | D | https://wayland.app/protocols/text-input-unstable-v3 |
| NAT-S17 | Wayland virtual-keyboard v1 | API opzionale e autorizzazioni compositor | D | https://wayland.app/protocols/virtual-keyboard-unstable-v1 |
| NAT-S18 | Wayland ext data control | gestione clipboard privilegiata, non generale | D | https://wayland.app/protocols/ext-data-control-v1 |
| NAT-S19 | Espanso backend | `clipboard`, `inject`, `auto`, threshold, overrides | D | https://github.com/espanso/espanso/blob/dev/schemas/config.schema.json |
| NAT-S20 | Espanso issue Wayland | caso di comportamento variabile su KDE/Mutter/wlroots | D: issue e testimonianze, non benchmark | https://github.com/espanso/espanso/issues/2766 |
| NAT-S21 | Avalonia Tray | Windows/macOS, Linux desktop-dependent | D | https://docs.avaloniaui.net/controls/navigation/trayicon/ |
| NAT-S22 | Avalonia NativeMenu | integrazione UI nativa menu e dock | D | https://docs.avaloniaui.net/controls/menus/nativemenu |
| NAT-S23 | Espanso forced output mode | `force_mode` per singolo match; profili | D | https://espanso.org/docs/matches/basics/ |
| NAT-S24 | Apple Secure Event Input | Tecnica di protezione input, disponibilità e vincoli da verificare su release attuale | D: archivio Apple / V: implementazione attuale | https://developer.apple.com/library/cartella storica eliminata |

## Inferenze da confermare (non presentare come risultati)

- `I-01`: se un adapter dispone soltanto di eventi fisici, non può garantire conoscenza di tutto il testo IME committato in ogni editor.
- `I-02`: combinando contatori clipboard e ownership si può ridurre, **non eliminare**, il rischio di sovrascrittura della copia utente.
- `I-03`: Wayland non offre un unico percorso pubblico standard che consenta a una terza app sia osservazione globale del testo sia inserimento senza autorizzazioni particolari.
- `I-04`: una GUI condivisa con profili per applicazione è meno onerosa di un backend di input uniforme forzato.

## Promemoria qualità

Registrare nelle prove: data, versione OS+compositor, nome e versione editor, layout, IME, app permission state, backend, fixture, atteso/osservato, evidenza redatta senza contenuti personali. Le capacità derivano dai test, non dal marketing o dal nome dell'API.
