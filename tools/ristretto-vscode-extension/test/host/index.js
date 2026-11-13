'use strict';

// Executed only by VS Code --extensionTestsPath. No npm dependencies or mocks
// of TextEditor/TextDocument: every edit and Undo goes through the real host.
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const vscode = require('vscode');
const { createExpandHandler } = require('../../src/controller');

const command = 'ristrettoVscode.expandAtCursor';
const results = [];
let root;
let exportPath;
let serial = 0;

async function check(name, action) {
  try {
    await action();
    results.push({ name, status: 'PASS' });
    console.log(`PASS: ${name}`);
  } catch (error) {
    // No document/snippet contents, paths or assertion diffs in the report.
    results.push({ name, status: 'FAIL', error: error.code || error.name });
    console.error(`FAIL: ${name} (${error.code || error.name})`);
  }
}

async function configure(enabled = true, file = exportPath) {
  const config = vscode.workspace.getConfiguration('ristrettoVscode');
  await config.update('enabled', enabled, vscode.ConfigurationTarget.Workspace);
  await config.update('exportFile', file, vscode.ConfigurationTarget.Workspace);
}

async function editor(text, local = false) {
  let doc;
  if (local) {
    const file = path.join(root, `document-${++serial}.txt`);
    await fs.writeFile(file, text, 'utf8');
    doc = await vscode.workspace.openTextDocument(vscode.Uri.file(file));
  } else {
    doc = await vscode.workspace.openTextDocument({ content: text, language: 'plaintext' });
  }
  const current = await vscode.window.showTextDocument(doc, { preview: false });
  const caret = doc.positionAt(text.length);
  current.selection = new vscode.Selection(caret, caret);
  await vscode.commands.executeCommand('workbench.action.focusActiveEditorGroup');
  assert.equal(vscode.window.activeTextEditor.document, doc);
  return current;
}

async function expand(text, expected, local = false) {
  const current = await editor(text, local);
  const version = current.document.version;
  await vscode.commands.executeCommand(command);
  assert.equal(current.document.getText(), expected);
  assert.equal(current.document.version, version + (text === expected ? 0 : 1));
  return current;
}

// Pause only export I/O to make races deterministic. All target checks, editor
// objects, versions, selections and the edit API remain the real VS Code ones.
async function race(change) {
  const original = await editor('Before ;firma ');
  const version = original.document.version;
  let release, arrived;
  const entered = new Promise(resolve => { arrived = resolve; });
  const blocked = new Promise(resolve => { release = resolve; });
  const api = {
    window: vscode.window, Uri: vscode.Uri, Range: vscode.Range,
    workspace: {
      get isTrusted() { return vscode.workspace.isTrusted; },
      getConfiguration: (...args) => vscode.workspace.getConfiguration(...args),
      fs: {
        stat: async uri => { arrived(); await blocked; return vscode.workspace.fs.stat(uri); },
        readFile: uri => vscode.workspace.fs.readFile(uri)
      }
    }
  };
  const pending = createExpandHandler(api)();
  try {
    await Promise.race([
      entered,
      pending.then(() => { throw new Error('Export preflight never entered'); }),
      new Promise((_, reject) => {
        const timer = setTimeout(() => reject(new Error('Export barrier timeout')), 5000);
        timer.unref();
      })
    ]);
    await change(original);
  } finally {
    release();
    await pending;
  }
  return { original, version };
}

async function run() {
  const output = process.env.RISTRETTO_VSCODE_HOST_REPORT;
  root = process.env.RISTRETTO_VSCODE_HOST_WORKSPACE;
  const normalized = value => process.platform === 'win32' ? path.resolve(value).toLowerCase() : path.resolve(value);
  if (!output || !root || !vscode.workspace.workspaceFolders?.some(f => normalized(f.uri.fsPath) === normalized(root)))
    throw new Error('Isolated host workspace and report paths required');
  exportPath = path.join(root, 'export.json');
  const fixture = JSON.parse(await fs.readFile(path.join(__dirname, '../../fixtures/synthetic-export.json'), 'utf8'));
  const sample = fixture.snippets[0];
  fixture.snippets.push(
    { ...sample, id: '00000000-0000-4000-8000-000000000004', trigger: ';multi', content: 'First line\nCafé 👩🏽‍💻 漢字\nLast line' },
    { ...sample, id: '00000000-0000-4000-8000-000000000005', trigger: ';👩🏽‍💻', content: 'Emoji 💫' }
  );
  await fs.writeFile(exportPath, JSON.stringify(fixture), 'utf8');
  try {
    await check('real_extension_activation_and_trusted_focused_host', async () => {
      assert.equal(vscode.workspace.isTrusted, true);
      const extension = vscode.extensions.getExtension('ristretto-local-dev.ristretto-vscode-adapter');
      assert.ok(extension);
      await extension.activate();
      assert.ok((await vscode.commands.getCommands(true)).includes(command));
      await editor('');
      assert.equal(vscode.window.state.focused, true);
    });
    await configure();
    await check('registered_command_expands_untitled_and_preserves_delimiter', async () => {
      await expand('Before ;firma ', 'Before Caffè ☕ 漢字 ');
    });
    await check('one_undo_restores_trigger_and_redo_restores_expansion', async () => {
      const current = await expand(';firma ', 'Caffè ☕ 漢字 ');
      await vscode.commands.executeCommand('undo');
      assert.equal(current.document.getText(), ';firma ');
      await vscode.commands.executeCommand('redo');
      assert.equal(current.document.getText(), 'Caffè ☕ 漢字 ');
    });
    await check('registered_command_expands_local_file', async () => {
      await expand(';firma ', 'Caffè ☕ 漢字 ', true);
    });
    await check('nfd_trigger_uses_real_utf16_range', async () => {
      await expand(';cafe\u0301 ', 'Ok NFC/NFD ');
    });
    await check('emoji_zwj_trigger_and_multiline_content', async () => {
      await expand(';👩🏽‍💻 ', 'Emoji 💫 ');
      const current = await expand(';multi ', 'First line\nCafé 👩🏽‍💻 漢字\nLast line ');
      await vscode.commands.executeCommand('undo');
      assert.equal(current.document.getText(), ';multi ');
    });
    await check('large_document_preserves_prefix_above_65536', async () => {
      const prefix = 'x'.repeat(100000) + '\n';
      await expand(prefix + ';firma ', prefix + 'Caffè ☕ 漢字 ');
    });
    await check('embedded_trigger_is_unchanged', async () => {
      await expand('foobar;firma ', 'foobar;firma ');
    });
    await check('disabled_adapter_leaves_document_unchanged', async () => {
      await configure(false);
      try { await expand(';firma ', ';firma '); }
      finally { await configure(); }
    });
    await check('selection_and_multiple_cursors_are_rejected', async () => {
      const current = await editor(';firma ');
      const before = current.document.version;
      current.selection = new vscode.Selection(0, 0, 0, 2);
      await vscode.commands.executeCommand(command);
      assert.equal(current.document.version, before);
      current.selections = [new vscode.Selection(0, 1, 0, 1), new vscode.Selection(0, 7, 0, 7)];
      await vscode.commands.executeCommand(command);
      assert.equal(current.document.version, before);
      assert.equal(current.document.getText(), ';firma ');
    });
    await check('document_switch_during_export_cannot_retarget_command', async () => {
      let other;
      const { original, version } = await race(async () => { other = await editor('Other ;firma '); });
      assert.equal(original.document.getText(), 'Before ;firma ');
      assert.equal(original.document.version, version);
      assert.equal(other.document.getText(), 'Other ;firma ');
    });
    await check('selection_change_during_export_prevents_edit', async () => {
      const { original, version } = await race(async current => {
        current.selection = new vscode.Selection(0, 0, 0, 0);
      });
      assert.equal(original.document.getText(), 'Before ;firma ');
      assert.equal(original.document.version, version);
    });
    await check('concurrent_real_document_change_is_preserved', async () => {
      const { original, version } = await race(async current => {
        assert.equal(await current.edit(b => b.insert(new vscode.Position(0, 0), 'Concurrent ')), true);
      });
      assert.equal(original.document.getText(), 'Concurrent Before ;firma ');
      assert.equal(original.document.version, version + 1);
    });
    await check('invalid_export_leaves_document_unchanged', async () => {
      const invalid = path.join(root, 'invalid.json');
      await fs.writeFile(invalid, '{invalid', 'utf8');
      await configure(true, invalid);
      try { await expand(';firma ', ';firma '); }
      finally { await configure(); }
    });
  } finally {
    const report = {
      schema: 'ristretto/vscode-host-tests/1', version: vscode.version,
      platform: process.platform, timestampUtc: new Date().toISOString(),
      passed: results.filter(r => r.status === 'PASS').length,
      failed: results.filter(r => r.status === 'FAIL').length,
      containsUserText: false,
      qualification: 'Installed VS Code Extension Development Host; registered command and real editor API. Race cases pause export I/O deterministically. No physical typing/IME or other editors qualified.',
      results
    };
    await fs.writeFile(output, JSON.stringify(report, null, 2) + '\n', { flag: 'wx' });
    // The extension test host exits after run(). Keep cleanup within its
    // isolated profile rather than invoking commands that may prompt to save.
  }
  if (!results.length || results.some(r => r.status === 'FAIL')) throw new Error('Host tests failed; see report');
}

module.exports = { run };
