'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { readPortableLibrary, planAtCaret, isWellFormedUnicode } = require('../src/planner');
const fixture = JSON.parse(fs.readFileSync(path.join(__dirname, '../fixtures/synthetic-export.json'), 'utf8'));
const list = () => readPortableLibrary(structuredClone(fixture));

test('format v1 and enabled group/snippet only', () => {
  const snippets = list();
  assert.equal(snippets.length, 2);
  const file = structuredClone(fixture);
  file.groups[0].isEnabled = false;
  assert.equal(readPortableLibrary(file).length, 0);
  file.groups[0].isEnabled = true;
  file.snippets[0].isEnabled = false;
  assert.equal(readPortableLibrary(file).length, 1);
});

test('does not accept unsupported or invalid library', () => {
  for (const mutate of [
    d => { d.version = 2; }, d => { d.format = 'random'; },
    d => { d.snippets[0].groupId = 'missing'; },
    d => { d.snippets[1].id = d.snippets[0].id; },
    d => { d.groups.push({...d.groups[0]}); },
    d => { d.snippets[0].trigger = 'abc\n'; },
    d => { d.snippets[0].content = '\uD83D'; }
  ]) {
    const x = structuredClone(fixture); mutate(x);
    assert.throws(() => readPortableLibrary(x));
  }
});

test('explicit manual expansion preserves an already-entered delimiter', () => {
  const line = 'test ;firma ';
  const p = planAtCaret(line, line.length, list());
  assert.equal(p.status, 'ready');
  assert.equal(p.start, 5);
  assert.equal(p.end, line.length - 1);
  assert.equal(line.slice(p.end), ' ');
  assert.equal(p.content, 'Caffè ☕ 漢字');
});

test('also permits an explicit command before delimiter, never automatically', () => {
  const p = planAtCaret(';firma', 6, list());
  assert.equal(p.status, 'ready');
  assert.equal(p.delimiterPreserved, false);
});

test('word boundary protects embedded triggers', () => {
  assert.equal(planAtCaret('foobar;firma', 12, list()).status, 'none');
  assert.equal(planAtCaret('foobarbrb', 9, [{trigger:'brb',content:'OK'}]).status, 'none');
  assert.equal(planAtCaret('nota_;firma', 11, list()).status, 'none');
  assert.equal(planAtCaret('(;firma', 7, list()).status, 'ready');
});

test('NFD input resolves NFC trigger with source UTF-16 range', () => {
  const line = ';cafe\u0301 ';
  const p = planAtCaret(line, line.length, list());
  assert.equal(p.status, 'ready');
  assert.equal(line.slice(p.start, p.end), ';cafe\u0301');
  assert.equal(p.end - p.start, 6);
});

test('emoji/ZWJ trigger and UTF-16 offsets are not split', () => {
  const line = '⌚ ;👩🏽‍💻 ';
  const p = planAtCaret(line, line.length, [{trigger:';👩🏽‍💻', content:'ok'}]);
  assert.equal(p.status, 'ready');
  assert.equal(line.slice(p.start, p.end), ';👩🏽‍💻');
  assert.equal(planAtCaret('a😀b', 2, list()).status, 'unsafe');
  assert.equal(isWellFormedUnicode('\uDC00'), false);
});

test('duplicate active trigger is ambiguous instead of arbitrarily replaced', () => {
  const snippets = [{trigger:';firma', content:'a'}, {trigger:';firma',content:'b'}];
  assert.equal(planAtCaret(';firma ',7,snippets).status, 'ambiguous');
});

test('longest exact match at caret is preferred, without speculative prefixes', () => {
  const snippets = [{trigger:';f',content:'short'}, {trigger:';firma',content:'long'}];
  assert.equal(planAtCaret(';firma',6,snippets).content,'long');
});

test('does not match trigger if there is extra text, more than one delimiter or wrong caret', () => {
  const snippets = list();
  assert.equal(planAtCaret(';firma xy',9,snippets).status,'none');
  assert.equal(planAtCaret(';firma  ',8,snippets).status,'none');
  assert.equal(planAtCaret(';firma',300,snippets).status,'unsafe');
});

test('bounded scanning rejects overlarge gaps and unpaired surrogates', () => {
  assert.equal(planAtCaret('x'.repeat(1000)+';firma',1006,list()).status,'none');
  // above line has no word boundary at trigger start.
  assert.equal(planAtCaret(' '.repeat(1000)+';firma',1006,list()).status,'ready');
  assert.equal(planAtCaret(';firma\uD800',7,list()).status,'unsafe');
});

test('one huge but otherwise valid Ristretto snippet is skipped, not truncated or executed', () => {
  const file = structuredClone(fixture);
  file.snippets[0].content = 'x'.repeat(70000);
  const items = readPortableLibrary(file);
  assert.equal(items.length, 1);
  assert.equal(planAtCaret(';firma ',7,items).status, 'none');
});

test('canonical duplicates across NFC/NFD are ambiguous', () => {
  const items = [{trigger:';café',content:'A'}, {trigger:';cafe\u0301',content:'B'}];
  assert.equal(planAtCaret(';cafe\u0301',6,items).status,'ambiguous');
});

test('replacing a multiline snippet has raw content intact in the plan', () => {
  const items = [{trigger:';multi',content:'prima\nseconda 🧑🏽‍💻'}];
  const plan = planAtCaret(';multi',6,items);
  assert.equal(plan.status,'ready');
  assert.equal(plan.content,'prima\nseconda 🧑🏽‍💻');
});

test('portable v1 metadata and GUID invariants follow the producer contract', () => {
  for (const mutate of [
    d => {d.groups[0].id='not-a-guid';},
    d => {d.groups[0].id='00000000-0000-0000-0000-000000000000';},
    d => {d.groups[0].name=' ';},
    d => {d.groups[0].name='X'.repeat(121);},
    d => {d.snippets[0].id='bad';},
    d => {d.snippets[0].title=' ';},
    d => {d.snippets[0].title='X'.repeat(161);},
    d => {d.snippets[0].description=null;},
    d => {d.snippets[0].description='x'.repeat(8001);},
  ]) {
    const d=structuredClone(fixture); mutate(d);
    assert.throws(() => readPortableLibrary(d));
  }
});
