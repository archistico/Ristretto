'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const os = require('node:os');
const fs = require('node:fs');
const path = require('node:path');
const { verify } = require('../cli/verify-dotnet-export');

// These two tests verify the Node gate, NOT the .NET producer. The separate
// validate-export-contract script executes the actual cross-runtime check.
function syntheticShape() {
  const g1 = '00000000-0000-4000-8000-000000000001';
  const g2 = '00000000-0000-4000-8000-000000000002';
  return {
    format: 'text-expander-studio/snippets', version: 1,
    groups: [{ id:g1, name:'Demo Unicode 🌍',isEnabled:true },{id:g2,name:'Off',isEnabled:false}],
    snippets: [
      [';firma','Caffè ☕ 漢字',g1,true],
      [';café','Ok NFC/NFD',g1,true],
      [';👩🏽‍💻','Unicode 💫',g1,true],
      [';off','NON INSERIRE',g1,false],
      [';hidden','NON INSERIRE',g2,true],
      [';big','X'.repeat(70000),g1,true]
    ].map(([trigger,content,groupId,isEnabled],i)=>({
      id:`00000000-0000-4000-8000-${String(i+3).padStart(12,'0')}`,
      groupId,title:`Synth ${i+1}`,trigger,content,description:'',isEnabled
    }))
  };
}
function temporaryFile(document, callback) {
  const folder=fs.mkdtempSync(path.join(os.tmpdir(),'ristretto-vscode-node-check-'));
  try {
    const file=path.join(folder,'synthetic.json');
    fs.writeFileSync(file,JSON.stringify(document));
    return callback(file);
  } finally {fs.rmSync(folder,{recursive:true,force:true});}
}
test('Node contract verifier recognizes its own synthetic input shape',()=>{
  temporaryFile(syntheticShape(), file=>{
    const r=verify(file);
    assert.equal(r.eligible,3);
    assert.equal(r.checks,7);
  });
});
test('Node contract verifier rejects modified exporter format',()=>{
  const d=syntheticShape(); d.version=7;
  temporaryFile(d,file=>assert.throws(()=>verify(file)));
});
