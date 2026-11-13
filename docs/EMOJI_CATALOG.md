# Catalogo emoji, shortcode e ricerca multilingua — specifica candidata

**Stato:** proposta da ratificare; evidenza in [audit emojilib v2.2](../research/audit/EMOJILIB_DIRECT_SOURCE_AUDIT_v2.2.md). I requisiti P0 del prodotto rimangono **inserire correttamente qualsiasi testo Unicode fornito dall'utente**, indipendentemente dalla disponibilità del catalogo emoji; il picker emoji è una funzionalità distinta.

## Vincoli funzionali

- Preservare **esattamente** il testo dello snippet: `❤️` non è automaticamente intercambiabile con `❤`; le sequenze ZWJ e i modificatori skin tone non devono essere spezzati o riscritti.
- I caratteri possono essere composti da molti code point o unità UTF-16. Nessun algoritmo deve associare `string.Length` ai tasti da inviare, ai backspace, o al numero di grapheme cluster senza una capability verificata.
- Il catalogo è un insieme di dati *opzionale*: assenza, corruzione o versione obsoleta non devono impedire di avviare la libreria snippet.
- Ricerca locale, veloce, anche quando il sistema è offline; le keywords possono essere localizzate. Il contenuto di uno snippet non cambia con la lingua della GUI.
- Evitare collisioni fra shortcut dell'utente e shortcode emoji; l'utente deve poter disabilitare espansione emoji automatica lasciando attivo il picker.

## Modello dati minimo suggerito

| Campo | Scopo | Note |
|---|---|---|
| `id` | Identificatore stabile interno | non dedurre dall'alias tradotto |
| `sequence` | stringa Unicode da inserire | immutabile salvo aggiornamento catalogo consapevole |
| `primaryAlias` | shortcode univoco per lingua o neutro | unicità verificata, non assegnata per tentativi casuali |
| `keywords` | elenco localizzato di ricerca | indicizzazione distinta da trigger automatici |
| `group` | categoria visuale opzionale | non inventarla da emojilib v4 che non la include |
| `catalogVersion` e `sourceHash` | riproducibilità | includere notices e provenienza |

## Pipeline dati

1. **Input:** leggere JSON UTF-8 (sorgente MIT pin-nata) senza eseguire script.
2. **Validate:** formato, lunghezze, stringhe Unicode valide, assenza duplicati ID e collisioni alias, nessun payload arbitrariamente grande.
3. **Normalize search index:** confronto e case folding documentati soltanto nell'indice di ricerca. Evitare di modificare il payload Unicode.
4. **Publish atomically:** conservare catalogo precedente se il nuovo non supera validazione.
5. **Query:** ricerca case-insensitive per alias e keywords, ordinamento stabile e risultati prevedibili.
6. **Insert:** passare sequenza originale al coordinatore di inserimento (capability OS); risultati espliciti in caso di errore.
7. **Telemetry:** non inviare query o snippet a servizi esterni; statistiche solo locali, opzionali.

## UX e accessibilità

Ricerca comune nel picker, filtro `Tutti / Snippet / Emoji`; icona emoji originale come preview ove il font OS lo consenta e testo alternativo tradotto. Se una sequenza non è renderizzata dal font OS, non simulare compatibilità mediante immagini esterne. Supportare input e lettura RTL, screen reader e navigazione tastiera. Richieste di registrazione global shortcut e permessi native separate dalla disponibilità del catalogo.

## Gate per la progettazione esecutiva

Definire licenza e fonte versione dataset, qualità keywords per lingue target, algoritmo collisioni, comportamento alias e UI; testare con emoji composte (ZWJ, VS16, skin tone, bandiere), copie/incolla, backspace e applicazioni reali. Non confondere test di parse JSON con test di espansione nativa riuscita.
