'use strict';
// Cross-runtime contract: MUST be given a file produced by the real .NET
// PortableLibraryService in VscodeExportContractTests. No user data is used.
const fs = require('node:fs');
const assert = require('node:assert/strict');
const path = require('node:path');
const { readPortableLibrary, planAtCaret, MAX_FILE_BYTES } = require('../src/planner');

function verify(file) {
  if (!file || !path.isAbsolute(file)) throw new Error('Percorso export .NET assoluto obbligatorio.');
  if (fs.statSync(file).size > MAX_FILE_BYTES) throw new Error('Export troppo grande.');
  const data = fs.readFileSync(file);
  const json = new TextDecoder('utf-8', { fatal: true }).decode(data);
  const doc = JSON.parse(json);
  assert.equal(doc.format, 'text-expander-studio/snippets');
  assert.equal(doc.version, 1);
  assert.equal(doc.groups.length, 2);
  assert.equal(doc.snippets.length, 6);
  const active = readPortableLibrary(doc);
  assert.equal(active.length, 3); // two disabled; one > adapter content bound
  for (const [line, expected] of [
    [';firma ', 'Caffè ☕ 漢字'],
    [';cafe\u0301 ', 'Ok NFC/NFD'],
    [';👩🏽‍💻 ', 'Unicode 💫']
  ]) {
    const plan = planAtCaret(line, line.length, active);
    assert.equal(plan.status, 'ready', line);
    assert.equal(plan.content, expected, line);
    assert.equal(line.slice(plan.start, plan.end).normalize('NFC'),
      plan.trigger.normalize('NFC'));
  }
  for (const line of [';off ', ';hidden ', ';big ', 'foobar;firma ']) {
    assert.equal(planAtCaret(line, line.length, active).status, 'none', line);
  }
  return { groups: doc.groups.length, snippets: doc.snippets.length,
    eligible: active.length, checks: 7 };
}

if (require.main === module) {
  try {
    const result = verify(process.argv[2]);
    console.log(`CONTRATTO .NET -> Node OK: ${result.groups} gruppi, ${result.snippets} snippet, ${result.eligible} idonei, ${result.checks} verifiche`);
  } catch (err) {
    // Avoid logging file contents or paths, even when the input is malformed.
    console.error('CONTRATTO .NET -> Node NON SUPERATO:', err?.code ?? err?.name ?? 'Error');
    process.exitCode = 1;
  }
}
module.exports = { verify };
