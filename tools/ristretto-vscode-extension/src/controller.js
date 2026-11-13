'use strict';
const { readPortableLibrary, planAtCaret, MAX_FILE_BYTES } = require('./planner');

// Bind a manual command to the document that was focused when it was invoked.
// No asynchronous read is allowed to silently retarget the operation.
function captureTarget(vscode) {
  const editor = vscode.window.activeTextEditor;
  if (!editor || !vscode.window.state.focused || !vscode.workspace.isTrusted ||
      editor.selections.length !== 1 || !editor.selection.isEmpty ||
      !['file', 'untitled'].includes(editor.document.uri.scheme) ||
      editor.document.isClosed) return null;
  return {
    editor, document: editor.document, version: editor.document.version,
    selection: editor.selection
  };
}

function isCurrentTarget(vscode, target) {
  return !!target && vscode.workspace.isTrusted && vscode.window.state.focused &&
    vscode.window.activeTextEditor === target.editor &&
    target.editor.document === target.document && !target.document.isClosed &&
    target.document.version === target.version &&
    target.editor.selections.length === 1 && target.editor.selection.isEmpty &&
    target.editor.selection.isEqual(target.selection);
}

function createExpandHandler(vscode) {
  let inProgress = false;
  return async function expandAtCursor() {
    if (inProgress) return; // Prevent same-extension double-click/reentrancy.
    inProgress = true;
    let editAttempted = false;
    try {
      const config = vscode.workspace.getConfiguration('ristrettoVscode');
      if (!config.get('enabled', false)) {
        vscode.window.showWarningMessage('Ristretto: abilita ristrettoVscode.enabled nelle Impostazioni.');
        return;
      }
      if (!vscode.workspace.isTrusted) {
        vscode.window.showWarningMessage('Ristretto: workspace non attendibile; espansione rifiutata.');
        return;
      }
      // IMPORTANT: capture BEFORE the first await, even before export stat/read.
      const target = captureTarget(vscode);
      if (!target) {
        vscode.window.showWarningMessage('Ristretto: serve un solo cursore in un editor locale attivo.');
        return;
      }
      const path = config.get('exportFile', '').trim();
      if (!path || !require('node:path').isAbsolute(path)) {
        vscode.window.showWarningMessage('Ristretto: seleziona prima un export JSON locale.');
        return;
      }
      const uri = vscode.Uri.file(path);
      const stat = await vscode.workspace.fs.stat(uri);
      if (stat.size > MAX_FILE_BYTES) {
        vscode.window.showWarningMessage('Ristretto: export oltre 8 MiB; nessuna modifica.');
        return;
      }
      // Cancel if the user moved elsewhere while VS Code was loading the export.
      if (!isCurrentTarget(vscode, target)) return;
      const data = await vscode.workspace.fs.readFile(uri);
      if (data.byteLength > MAX_FILE_BYTES) throw new Error('Export troppo grande.');
      if (!isCurrentTarget(vscode, target)) return;
      const decoded = new TextDecoder('utf-8', { fatal: true }).decode(data);
      const snippets = readPortableLibrary(JSON.parse(decoded));
      if (!isCurrentTarget(vscode, target)) return;
      const editor = target.editor;
      const doc = target.document;
      const caret = target.selection.active;
      const line = doc.lineAt(caret.line).text;
      const match = planAtCaret(line, caret.character, snippets);
      if (match.status !== 'ready') {
        if (match.status === 'ambiguous')
          vscode.window.showWarningMessage('Ristretto: trigger duplicato; espansione annullata.');
        else if (match.status === 'unsafe')
          vscode.window.showWarningMessage('Ristretto: posizione Unicode non sicura.');
        return;
      }
      const range = new vscode.Range(caret.line, match.start, caret.line, match.end);
      const startOffset = doc.offsetAt(range.start);
      const expectedTrigger = line.slice(match.start, match.end);
      // Preflight checks the originally observed trigger, not a tautological
      // reread of the same range. A version/focus change forbids dispatch.
      if (!isCurrentTarget(vscode, target) || doc.getText(range) !== expectedTrigger ||
          !config.get('enabled', false)) return;
      editAttempted = true;
      const applied = await editor.edit(builder => builder.replace(range, match.content), {
        undoStopBefore: true, undoStopAfter: true
      });
      if (!applied) {
        vscode.window.showWarningMessage('Ristretto: modifica rifiutata da VS Code.');
        return;
      }
      // VS Code does not guarantee transactional compare-and-swap. A changed
      // version/result is uncertain: never retry or automatically roll back.
      const resultRange = new vscode.Range(doc.positionAt(startOffset),
        doc.positionAt(startOffset + match.content.length));
      if (doc.version !== target.version + 1 || doc.getText(resultRange) !== match.content) {
        vscode.window.showWarningMessage('Ristretto: esito concorrente non verificabile. Non riprovare automaticamente.');
      }
    } catch (error) {
      // No stack traces, snippet contents or filesystem paths in diagnostics.
      vscode.window.showWarningMessage(editAttempted
        ? 'Ristretto: errore durante la modifica; esito incerto. Non riprovare automaticamente.'
        : 'Ristretto: export non leggibile o non valido. Nessuna modifica avviata.');
    } finally {
      inProgress = false;
    }
  };
}

module.exports = { createExpandHandler, captureTarget, isCurrentTarget };
