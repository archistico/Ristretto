# Distribuzione e rilascio — policy candidata per Ristretto

**Versione documento:** v2.0 · **Stato:** proposte non ancora ratificate; nessun pacchetto dell'applicazione esiste.

## Obiettivo prodotto

Software open source gratuito, local-first, C#/.NET + Avalonia; GUI moderna e multilingua, temi chiaro/scuro, supporto Unicode completo. Nessun account o cloud obbligatorio. Versione GUI e backend nativi separati, con compatibilità pubblicata in una matrice per OS/desktop.

## Edizioni e dati

| Edizione | Directory binari | Directory dati | Requisiti di sicurezza |
|---|---|---|---|
| Windows installata | directory gestita da installer | profilo utente | disinstallazione non elimina gli snippet; upgrade/migrazione verificata |
| Windows portabile | directory dedicata scelta dall'utente | sottocartella portable documentata | nessun accesso non previsto al profilo installato; notificare se directory readonly |
| macOS | `.app` in Applications o altra posizione consentita | directory dati utente standard | Accessibility/Input Monitoring esplicitati; firma/notarizzazione per rilascio pubblico |
| Linux | pacchetto o archivio dichiarato | XDG data/config/cache | permessi e limiti X11/Wayland chiari; nessun root persistente richiesto salvo specifiche integrazioni opt-in |

**Casi da testare:** due edizioni in parallelo; doppio avvio; cambio utente; upgrade con dati; dati in path Unicode, spazi, sincronizzati, readonly; profilo cifrato; symlink; directory portabile trasferita.

## Artefatti candidati

- Windows: ZIP portabile e installer da scegliere (MSI/MSIX/Inno/altro). Evitare packaging multipli senza necessità verificata.
- macOS: bundle `.app`, distribuzione DMG o PKG, architetture separate se più semplice. Valutare universal binary solo se realmente necessario.
- Linux: `tar.gz` e/o AppImage; DEB/RPM in fase successiva. Non assegnare capacità di input globale a un pacchetto solo perché l'interfaccia si avvia.

## Release engineering minima

1. Versioning semantico e changelog a sorgente unico.
2. `restore`, `build`, `test`, publish; artifact verificati e pacchetti installati su VM pulita.
3. Check vulnerabilità e dipendenze, licenze e THIRD_PARTY_NOTICES, generazione SBOM, checksum artefatti.
4. Firma e/o notarizzazione se richieste dalla piattaforma/distribuzione.
5. Release pubblica con limiti noti dei backend e matrice di compatibilità.
6. Piano rollback che NON ripristina automaticamente database a schema incompatibile; backup prima di migrazioni.

## Sicurezza update (se e quando implementata)

- Prima versione: notifica opzionale o semplice link alla pagina ufficiale; nessun auto-eseguibile scaricato e lanciato senza conferma.
- In futuro: manifesto autentico, verifica firma artefatti, SHA-256, TLS, URL allowedlist, version floor, protezione downgrade, download temporaneo, verifica post-download e procedura di recovery.
- Consentire canale stable; beta/dev solo con opt-in. Non trasmettere dati snippet, tasti digitati o contenuto clipboard.

## Accessibilità e i18n nel rilascio

- Risorse ResX; cambio lingua senza restart ove possibile; fallback chiaro; pseudo-locale e verifica overflow nelle GUI.
- Unicode/emoji/IME e temi chiaro/scuro nella release checklist; test anche su font fallback ed editor con direzionalità RTL.

## Decisioni aperte

- Licenza della nuova codebase; installer Windows; livelli di firma e costi notarizzazione; packaging Linux; scelta runtime self-contained; release automatiche; policy contributori e governance.
- Il supporto automatico ai trigger su Wayland resta condizionato a test compositor-specifici; non promettere universalità.

**Audit di riferimento:** [Beeftext installer/updater](../research/audit/BEEFTEXT_RELEASE_INSTALLER_UPDATER_AUDIT.md), [piano build/SBOM](../research/audit/REPRODUCIBILITY_SUBMODULES_SBOM_PLAN.md).
