# Fonti — Output controllato v2.6

**Snapshot locali** forniti dall'utente (non redistribuiti nel dossier): `Beeftext-master.zip`, `espanso-dev.zip`, `XMiLib-master.zip`, `emojilib-main.zip`. Percorsi/range nel [rapporto audit](audit/FOUR_SOURCE_OUTPUT_AUDIT_v2.6.md); gli snapshot non contengono commit Git verificato.

## Riferimenti primari ufficiali

| Area | Fonte | Cosa conferma | Cosa NON dimostra |
|---|---|---|---|
| Win32 input | https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput | numero eventi inseriti nel flusso, restrizioni UIPI | testo realmente apparso o target rimasto identico |
| Clipboard Windows | https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboardsequencenumber | sequence number per stazione, incrementi su cambi / limiti delayed rendering | ownership atomica e restore di tutti i formati |
| Clipboard Windows (uso) | https://learn.microsoft.com/en-us/windows/win32/dataxchg/using-the-clipboard | alternative monitor cambi clipboard | conferma asincrona di paste |
| macOS event taps | https://developer.apple.com/documentation/coregraphics/cgevent/tapcreate(tap:place:options:eventsofinterest:callback:userinfo:) | condizioni di funzionamento e permessi degli event taps | consegna certa del testo nella app terza |
| macOS clipboard | https://developer.apple.com/documentation/appkit/nspasteboard/changecount | ownership e variazioni clipboard documentate | restore atomico general-purpose |
| Linux X11 selections | https://www.x.org/releases/X11R7.7/doc/xorgproto/fixesproto.txt | XFIXES SelectionNotify/ownership | clipboard transazionale o testo committato universale |
| Wayland desktop portal | https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.impl.portal.RemoteDesktop.html | sessioni con consenso e metodi di input disponibili per Remote Desktop | lettura globale delle sequenze testuali o permesso permanente di espansione |
| Espanso schema | https://github.com/espanso/espanso/blob/dev/schemas/config.schema.json | metodi auto/event/clipboard e restore delay configurabili | correttezza su ogni app editor |
| Espanso docs | https://espanso.org/docs/get-started/ | ricerca/picker e undo Backspace con limitazioni | undo corretto con tutti i grafemi/editor |

Fonti consultate nel novembre 2026; differenziare versioni del sistema operativo e comportamento dei backend. Non usare fonti web per sostituire l'evidenza del codice disponibile negli ZIP dell'utente.
