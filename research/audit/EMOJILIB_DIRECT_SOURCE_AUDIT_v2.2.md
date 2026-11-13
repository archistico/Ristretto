# Audit diretto emojilib — v2.2

**Baseline locale:** `emojilib-main.zip` consegnato dall'utente, **15 file regolari**, MIT (`LICENSE`, Mu-An Chiou). `package.json:1-37` dichiara `emojilib` versione **4.0.3**, pacchetto npm orientato alla ricerca per keyword; file principale `dist/emoji-en-US.json`. Lo ZIP non fornisce commit Git né il vecchio `emojis.json` atteso da Beeftext.

## Formato e inventario dei dati (verifica locale automatizzata)

La risorsa `dist/emoji-en-US.json` è un **oggetto `emoji visibile -> elenco di parole chiave`**, con **1.914 chiavi**, **15.412 associazioni keyword**, **249** chiavi con U+200D (ZWJ), **365** contenenti U+FE0F (variation selector-16) e **1.730** chiavi con almeno un carattere supplementare (oltre U+FFFF). La sequenza emoji più lunga nel file è composta da **14 unità UTF-16**. I numeri descrivono *questo snapshot*, non l'intero standard Unicode.

Esempi presenti: `😀`, `❤️`, `👨‍👩‍👧‍👦`, `🇮🇹`; `👍🏽` **non compare come chiave autonoma** nello snapshot, benché sia un'emoji valida. Assenza nel catalogo non significa assenza di supporto Unicode in un prodotto.

`README.md` descrive esplicitamente la migrazione dal formato `2.x` (`nome_shortcode -> {char, category, keywords, ...}`) al formato più recente e rinvia ad `unicode-emoji-json` per informazioni su versioni Unicode, categorie e skin tone. Le dipendenze Node elencate in `package.json` sono principalmente di sviluppo del catalogo: non servono per una semplice lettura JSON da C#.

## Incompatibilità concreta con la versione Beeftext esaminata

- `Beeftext/Emoji/EmojiManager.cpp:23-37` cerca **`emojis.json`** in `emojis/` accanto al programma oppure nel percorso del submodule.
- `Beeftext/Emoji/EmojiManager.cpp:133-158` pretende che ciascun *valore* sia un oggetto e contenga `char`; la chiave è assunta come shortcode; `category` è facoltativa.
- `Beeftext/Combo/ComboManager.cpp:230-264` riconosce shortcode delimitati, esegue lookup per keyword e poi espande; `Beeftext/Picker/PickerModel.cpp` include anche le emoji nel picker.
- `Installer/installer.nsi:198-199` e `Installer/PortableEdition/BuildPortableEdition.ps1:57-61` copiano letteralmente `Submodules/emojilib/emojis.json`.
- Lo snapshot `emojilib` v4.0.3 **non contiene** questo file ed espone una struttura incompatibile. Risultato certo dell'analisi: **sostituire senza adattamento il vecchio submodule con questo ZIP non soddisfa lo schema e il packaging attesi**. Non è prova che una vecchia release Beeftext fosse non buildabile: il riferimento Git puntava presumibilmente a un'altra revisione e va verificato con i commit.

## Soluzioni valutate per la nuova applicazione

| Strategia | Vantaggi | Limiti | Proposta |
|---|---|---|---|
| Includere direttamente un JSON pin-nato delle keyword MIT | semplice, offline, veloce, senza runtime Node | copertura e lessico inglese | **candidato preferito**, con notices e versione catalogo |
| Generare al build un catalogo normalizzato con metadati Unicode | ordinamento, categorie e ricerca coerenti | pipeline e dipendenze di dati da validare | valutare durante slice `EmojiCatalog` |
| Scaricare catalogo online a runtime | aggiornamenti rapidi | privacy, supply chain, offline, affidabilità | non necessario per MVP |
| Importare il vecchio `emojis.json` di Beeftext | migrazione eventuale shortcode familiari | serve snapshot storico e mapping | opzionale come importazione dati, non dipendenza runtime |

## Specifica di migrazione catalogo

1. Parsare come dati non eseguibili, con limiti di dimensione e lunghezza; rifiutare valori non-stringa e keyword non valide.
2. Non convertire in modo cieco tutte le keyword in trigger: molte emoji condividono `love`, `face`, `smile`, ecc. Le collisioni renderebbero il matching ambiguo.
3. Assegnare un `stableId` interno per emoji e `aliases` controllati; scegliere preferibilmente l'alias descrittivo principale per la ricerca, senza garantirne l'unicità senza deduplicazione.
4. Conservare esattamente la sequenza Unicode originale (code point, VS16, ZWJ); NON fare NFC/NFKC del payload inserito per ragioni di matching.
5. Rendere keywords e sinonimi localizzabili **indipendentemente dalla lingua della GUI**; il file `en-US` non è multilingua da solo.
6. Distinguere inserimento emoji dal comportamento di sostituzione degli shortcode (abilitazione opzionale, delimitatori non ambigui, esclusioni per app, picker).
7. Versionare origine, file SHA-256 e notices delle risorse; testare copertura e differenze di versione del catalogo.

## Migliorie GUI candidate

Picker compatto con tab `Snippet | Emoji` o filtro integrato, ricerca istantanea per parola chiave, cronologia locale opzionale e anteprima del risultato. Per la v1 non serve una finestra di gestione emoji separata se il picker già fornisce accesso adeguato. Non assumere che la presenza di emoji nel catalogo implichi che ogni font OS le renda allo stesso modo.

## Verifica vs ipotesi

**Verificato staticamente:** parse JSON, conteggi, struttura chiave/valore, riferimenti del compilatore/installer Beeftext, file mancanti, licenza MIT dei due ZIP. **Non verificato:** compatibilità di versioni storiche, resa emoji su OS reali, espansione completa di tutti i 1.914 record, correttezza skin tone con font OS, contributi di altri pacchetti e aggiornamenti delle licenze upstream.
