'use strict';

// A bounded, explicit-command subset of Ristretto's trigger contract.
// Independent of VS Code and testable with Node's built-in test runner.
const FORMAT = 'text-expander-studio/snippets';
const MAX_FILE_BYTES = 8 * 1024 * 1024;
const MAX_SNIPPETS = 100000;
const MAX_TRIGGER_UTF16 = 128;
const MAX_CONTENT_UTF16 = 65536;
const MAX_PORTABLE_CONTENT_UTF16 = 1000000;
const MAX_LOOKBACK_UTF16 = 512;
const wordLike = /[\p{L}\p{N}\p{M}_]/u;
const whitespace = /^\p{White_Space}$/u;
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const nonEmptyGuid = value => typeof value === 'string' && guid.test(value) && !/^0{8}-0{4}-0{4}-0{4}-0{12}$/i.test(value);
const segmenter = new Intl.Segmenter('und', { granularity: 'grapheme' });

function isWellFormedUnicode(text) {
  if (typeof text !== 'string') return false;
  for (let i = 0; i < text.length; i++) {
    const c = text.charCodeAt(i);
    if (c >= 0xD800 && c <= 0xDBFF) {
      if (i + 1 >= text.length) return false;
      const next = text.charCodeAt(++i);
      if (next < 0xDC00 || next > 0xDFFF) return false;
    } else if (c >= 0xDC00 && c <= 0xDFFF) return false;
  }
  return true;
}

function validTrigger(value) {
  return typeof value === 'string' && value.length > 0 && value.length <= MAX_TRIGGER_UTF16 &&
    isWellFormedUnicode(value) && !/[\p{Cc}\p{Zl}\p{Zp}]/u.test(value);
}

function readPortableLibrary(document) {
  if (!document || typeof document !== 'object' || Array.isArray(document) ||
      document.format !== FORMAT || document.version !== 1 ||
      !Array.isArray(document.groups) || !Array.isArray(document.snippets) ||
      document.groups.length > MAX_SNIPPETS || document.snippets.length > MAX_SNIPPETS) {
    throw new Error('Formato JSON Ristretto non supportato. Esporta nuovamente in formato v1.');
  }
  const groups = new Map();
  for (const group of document.groups) {
    if (!group || !nonEmptyGuid(group.id) || groups.has(group.id.toLowerCase()) ||
        typeof group.name !== 'string' || !group.name.trim() || group.name.length > 120 ||
        typeof group.isEnabled !== 'boolean') throw new Error('Gruppo non valido o duplicato.');
    groups.set(group.id.toLowerCase(), group.isEnabled);
  }
  const ids = new Set();
  const snippets = [];
  for (const s of document.snippets) {
    if (!s || !nonEmptyGuid(s.id) || ids.has(s.id.toLowerCase()) ||
        !nonEmptyGuid(s.groupId) || !groups.has(s.groupId.toLowerCase()) ||
        typeof s.isEnabled !== 'boolean' ||
        typeof s.title !== 'string' || !s.title.trim() || s.title.length > 160 ||
        typeof s.description !== 'string' || s.description.length > 8000 ||
        !validTrigger(s.trigger) || typeof s.content !== 'string' ||
        !s.content.length || s.content.length > MAX_PORTABLE_CONTENT_UTF16 ||
        !isWellFormedUnicode(s.content)) {
      throw new Error('Snippet non valido, contenuto troppo lungo o identificatore duplicato.');
    }
    ids.add(s.id.toLowerCase());
    // Ristretto permits up to 1M UTF-16 units. This conservative adapter
    // skips longer items rather than invalidating an otherwise valid export.
    if (s.isEnabled && groups.get(s.groupId.toLowerCase()) && s.content.length <= MAX_CONTENT_UTF16) {
      snippets.push({ trigger: s.trigger.normalize('NFC'), content: s.content });
    }
  }
  return snippets;
}

function previousCodePoint(text, offset) {
  if (offset === 0) return '';
  const value = text.codePointAt(offset - 1);
  // At a low surrogate, codePointAt(offset-1) returns its value, so use Array.from.
  return Array.from(text.slice(Math.max(0, offset - 2), offset)).at(-1) ?? '';
}

// All offsets are UTF-16, as required by VS Code's TextDocument positions.
// The caller supplies only the current line and the caret's line-relative offset.
function planAtCaret(line, caret, snippets) {
  if (typeof line !== 'string' || !Number.isInteger(caret) || caret < 0 || caret > line.length ||
      !Array.isArray(snippets) || !isWellFormedUnicode(line)) return { status: 'unsafe' };
  if (caret > 0 && caret < line.length) {
    const before = line.charCodeAt(caret - 1);
    const after = line.charCodeAt(caret);
    if (before >= 0xD800 && before <= 0xDBFF && after >= 0xDC00 && after <= 0xDFFF)
      return { status: 'unsafe' };
  }
  const input = line.slice(0, caret);
  const last = Array.from(input).at(-1) ?? '';
  // A single already-committed whitespace grapheme is preserved, not replaced.
  const delimiter = whitespace.test(last) ? last : '';
  const end = caret - delimiter.length;
  const begin = Math.max(0, end - MAX_LOOKBACK_UTF16);
  const slice = line.slice(begin, end);
  const boundaries = [...segmenter.segment(slice)].map(s => s.index);
  boundaries.push(slice.length);
  const byTrigger = new Map();
  for (const snippet of snippets) {
    if (!validTrigger(snippet.trigger)) continue;
    const key = snippet.trigger.normalize('NFC');
    if (!byTrigger.has(key)) byTrigger.set(key, []);
    byTrigger.get(key).push(snippet);
  }
  const matches = [];
  // Iterate grapheme boundaries once; no O(snippets * graphemes) loop.
  for (const i of boundaries) {
    if (i === slice.length) continue;
    const start = begin + i;
    if (start > 0 && wordLike.test(previousCodePoint(line, start))) continue;
    const part = line.slice(start, end);
    const candidates = byTrigger.get(part.normalize('NFC')) ?? [];
    for (const snippet of candidates) {
      matches.push({ start, end, trigger: snippet.trigger, content: snippet.content, length: end - start });
    }
  }
  if (!matches.length) return { status: 'none' };
  const longest = Math.max(...matches.map(m => m.length));
  const best = matches.filter(m => m.length === longest);
  // No silent tie-breaking when multiple active snippets use the same trigger.
  if (best.length !== 1) return { status: 'ambiguous' };
  return { status: 'ready', ...best[0], delimiterPreserved: delimiter.length > 0 };
}

module.exports = { FORMAT, MAX_FILE_BYTES, MAX_CONTENT_UTF16, isWellFormedUnicode,
  readPortableLibrary, planAtCaret };
