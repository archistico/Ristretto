'use strict';
const { createExpandHandler } = require('./controller');

function activate(context) {
  const vscode = require('vscode');
  const expandAtCursor = createExpandHandler(vscode);
  context.subscriptions.push(
    vscode.commands.registerCommand('ristrettoVscode.expandAtCursor', expandAtCursor),
    vscode.commands.registerCommand('ristrettoVscode.chooseExport', async () => {
      const selected = await vscode.window.showOpenDialog({
        canSelectMany: false,
        openLabel: 'Usa export Ristretto',
        filters: { 'Ristretto JSON': ['json'] }
      });
      if (!selected || selected.length !== 1 || selected[0].scheme !== 'file') return;
      await vscode.workspace.getConfiguration('ristrettoVscode').update(
        'exportFile', selected[0].fsPath, vscode.ConfigurationTarget.Global);
      vscode.window.showInformationMessage('Ristretto: export selezionato. Abilita la funzione nelle Impostazioni.');
    })
  );
}
function deactivate() {}
module.exports = { activate, deactivate };
