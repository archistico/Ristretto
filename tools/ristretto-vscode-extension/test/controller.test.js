'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { createExpandHandler } = require('../src/controller');
const bytes = fs.readFileSync(path.join(__dirname,'../fixtures/synthetic-export.json'));

function fakeVscode(initial, opts={}) {
  const warnings = [];
  let activeOverride = null;
  let focused = opts.focused??true;
  let text = initial;
  let version = 1;
  let edits = 0;
  const pos = offset => {
    const prior = text.slice(0, Math.min(offset, text.length));
    const lines = prior.split('\n');
    return {line:lines.length-1, character:lines.at(-1).length};
  };
  const off = point => {
    const lines = text.split('\n');
    return lines.slice(0,point.line).reduce((s,l)=>s+l.length+1,0)+point.character;
  };
  class Range {
    constructor(a,b,c,d) {
      if (typeof a === 'object') { this.start=a;this.end=b; }
      else {this.start={line:a,character:b};this.end={line:c,character:d};}
    }
  }
  const selection = {isEmpty: true,active:pos(initial.length),isEqual(o){return o===selection;}};
  const doc = {
    uri:{scheme:opts.scheme??'file'},isClosed:false,
    get version(){return version;},
    lineAt(line){ return {text:text.split('\n')[line]}; },
    offsetAt:off,positionAt:pos,
    getText(range){return range?text.slice(off(range.start),off(range.end)):text;}
  };
  const editor = {document:doc,selection,selections:[selection],
    async edit(fn) {
      edits++;
      if (opts.rejectEdit) return false;
      if (opts.concurrent) version++;
      let change;
      fn({replace(range,value){change={range,value};}});
      const start=off(change.range.start), end=off(change.range.end);
      text=text.slice(0,start)+change.value+text.slice(end);
      version++;
      if (opts.throwAfterEdit) throw new Error('injected failure after edit');
      return true;
    }
  };
  const vscode = {
    Uri:{file:p=>({fsPath:p,scheme:'file'})},Range,
    workspace:{isTrusted:opts.trusted??true,
      getConfiguration(){return {get(k,d){return ({enabled:opts.enabled??true,exportFile:'/tmp/fake.json'})[k]??d;}};},
      fs:{async stat(){if (opts.onStat) opts.onStat(context); return {size:opts.size??bytes.length};},
        async readFile(){if (opts.onReadFile) opts.onReadFile(context); return bytes;}}},
    window:{get activeTextEditor(){return activeOverride??editor;},state:{get focused(){return focused;}},
      showWarningMessage:m=>warnings.push(m)}
  };
  const context={vscode,editor,doc,warnings,
    changeEditor(){activeOverride={document:doc};},
    changeFocus(){focused=false;},
    changeSelection(){editor.selection={...selection,isEqual(){return false;}};},
    changeVersion(){version++;},
    get text(){return text;},get edits(){return edits;}, get version(){return version;}};
  return context;
}

test('real command path uses editor.edit and replaces trigger only, retaining delimiter',async()=>{
  const x=fakeVscode('A ;firma ');
  await createExpandHandler(x.vscode)();
  assert.equal(x.edits,1);
  assert.equal(x.text,'A Caffè ☕ 漢字 ');
  assert.deepEqual(x.warnings,[]);
});

test('disabled, untrusted, unfocused and invalid selections never invoke editor.edit',async()=>{
  for(const opts of [{enabled:false},{trusted:false},{focused:false},{scheme:'vscode-remote'},{size:16*1024*1024}]) {
    const x=fakeVscode(';firma ',opts);
    await createExpandHandler(x.vscode)();
    assert.equal(x.edits,0,JSON.stringify(opts));
  }
});

test('no automatic retry after an edit failure',async()=>{
  const x=fakeVscode(';firma ',{rejectEdit:true});
  await createExpandHandler(x.vscode)();
  assert.equal(x.text,';firma ');
  assert.equal(x.edits,1);
});

test('concurrent version change is reported as uncertain and is never retried',async()=>{
  const x=fakeVscode(';firma ',{concurrent:true});
  await createExpandHandler(x.vscode)();
  assert.equal(x.edits,1);
  assert(x.warnings.some(w=>w.includes('non verificabile')));
});

test('unknown trigger is a no-op',async()=>{
  const x=fakeVscode(';never ');
  await createExpandHandler(x.vscode)();
  assert.equal(x.edits,0);
});

test('post-edit exception never claims that no modification was made',async()=>{
  const x=fakeVscode(';firma ',{throwAfterEdit:true});
  await createExpandHandler(x.vscode)();
  assert.equal(x.edits,1);
  assert.equal(x.text,'Caffè ☕ 漢字 ');
  assert(x.warnings.some(w=>w.includes('esito incerto')));
  assert(!x.warnings.some(w=>w.includes('Nessuna modifica avviata')));
});

for (const stage of ['onStat', 'onReadFile']) {
  for (const action of ['changeEditor','changeFocus','changeSelection','changeVersion']) {
    test(`target remains frozen: ${action} during ${stage} forbids edit`, async () => {
      const x = fakeVscode(';firma ', {[stage]: state => state[action]()});
      await createExpandHandler(x.vscode)();
      assert.equal(x.edits, 0);
      assert.equal(x.text, ';firma ');
    });
  }
}

test('an export path is never inspected when the command has no qualified editor', async () => {
  let reads=0;
  const x=fakeVscode(';firma ',{focused:false,onStat:()=>reads++,onReadFile:()=>reads++});
  await createExpandHandler(x.vscode)();
  assert.equal(reads,0);
});
