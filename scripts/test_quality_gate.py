"""Regression tests for R1.03-R1.05 release automation, no .NET required."""
import os
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from quality_gate import Executable, discover, plan, run_gate, source_digest


class ToolDetectionTests(unittest.TestCase):
    def test_override_path_with_spaces_not_split(self):
        calls = []
        def run(argv, **_):
            calls.append(argv)
            return SimpleNamespace(returncode=0, stdout='Python 3.12.4', stderr='')
        found = discover('python', ('py','python'), r'Python\s+(\d+)\.', 3,
                         run=run, environ={'RISTRETTO_PYTHON':'C:/Program Files/Python/python.exe'})
        self.assertEqual(('C:/Program Files/Python/python.exe',), found.argv)
        self.assertEqual(['C:/Program Files/Python/python.exe','--version'], calls[0])

    def test_does_not_accept_windows_store_placeholder(self):
        def fail(*_, **__): raise OSError('alias unavailable')
        found = discover('python',('python',),r'Python\s+(\d+)\.',3,
                         run=fail, which=lambda _: '/fake/python',environ={})
        self.assertIsNone(found)

    def test_node_major_checked(self):
        def runner(*_, **__): return SimpleNamespace(returncode=0, stdout='v20.1.0', stderr='')
        self.assertIsNone(discover('node',('node',),r'v?(\d+)\.\d+',22,
                                    run=runner,which=lambda _:'/node',environ={}))

    def test_python_below_3_10_is_not_accepted(self):
        def runner(*_, **__): return SimpleNamespace(returncode=0, stdout='Python 3.9.21', stderr='')
        found=discover('python', ('python',), r'Python\s+(\d+)\.', 3,
                       run=runner, which=lambda _: '/python39', environ={})
        self.assertIsNone(found)

    def test_scoop_path_accepted(self):
        def runner(*_, **__): return SimpleNamespace(returncode=0, stdout='v22.16.0', stderr='')
        found=discover('node',('node',),r'v?(\d+)\.\d+',22,run=runner,
                       which=lambda _: 'C:/Users/Test/scoop/apps/node/current/node.exe',environ={})
        self.assertIsNotNone(found)


class QualityGateTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        (self.root/'VERSION.txt').write_text('Ristretto 3.9.0-alpha.20.7\n')
        (self.root/'Directory.Build.props').write_text('<Project><PropertyGroup><Version>3.9.0-alpha.20.7</Version></PropertyGroup></Project>')
        (self.root/'global.json').write_text('{}')
        for name in ('src','tests','tools','scripts'):
            (self.root/name).mkdir()

    def resolve(self, name, *_): return Executable(('/tools/' + name,), 'test')

    def test_source_gate_only_runs_three_source_checks(self):
        calls = []
        def run(argv, **_):
            calls.append(argv)
            return SimpleNamespace(returncode=0)
        report=run_gate('source',self.root,runner=run,resolver=self.resolve,verbose=False)
        self.assertEqual('PASS', report['result'])
        self.assertEqual(3,len(calls))
        self.assertEqual('NOT_RUN',report['steps'][3]['status'])

    def test_failure_blocks_later_steps_and_writes_report(self):
        def run(argv, **_):
            return SimpleNamespace(returncode=1 if any('audit_source.py' in arg for arg in argv) else 0)
        path=self.root/'result.json'
        report=run_gate('core',self.root,runner=run,resolver=self.resolve,reporter=path,verbose=False)
        self.assertEqual('FAIL',report['result'])
        self.assertEqual('FAIL',report['steps'][0]['status'])
        self.assertEqual('NOT_RUN',report['steps'][5]['status'])
        self.assertIn('"NOT_RUN"',path.read_text())

    def test_core_pass_does_not_claim_node_or_native(self):
        def run(argv, **_): return SimpleNamespace(returncode=0)
        report=run_gate('core',self.root,runner=run,resolver=self.resolve,verbose=False)
        self.assertEqual('PASS',report['result'])
        self.assertEqual('NOT_RUN',report['steps'][11]['status'])
        self.assertEqual('NOT_RUN',report['steps'][13]['status'])
        self.assertEqual('NOT_RUN',report['nuget_vulnerability_audit']['status'])

    def test_ci_runs_node_contract_and_never_runs_native(self):
        calls = []
        def run(argv, **_):
            calls.append(argv)
            if any('VscodeExportContractTests' in arg for arg in argv):
                dest=self.root/'.local-cache/gates/export-from-dotnet.json'
                dest.parent.mkdir(parents=True,exist_ok=True)
                dest.write_text('{}')
            return SimpleNamespace(returncode=0)
        report=run_gate('ci',self.root,runner=run,resolver=self.resolve,
                        os_name='Windows',verbose=False)
        self.assertEqual('PASS', report['result'])
        self.assertEqual('PASS', report['steps'][-2]['status'])
        self.assertEqual('NOT_RUN',report['steps'][-1]['status'])
        self.assertEqual('NOT_RUN', report['desktop_picker_real_io'])
        self.assertEqual('NOT_RUN', report['vs_code_real_host'])
        self.assertFalse(any('--replacement-test' in arg for args in calls for arg in args))

    def test_ci_failure_in_node_is_fail_closed(self):
        def run(argv, **_):
            return SimpleNamespace(returncode=13 if 'check' in argv else 0)
        report=run_gate('ci',self.root,runner=run,resolver=self.resolve,
                        os_name='Windows',verbose=False)
        self.assertEqual('FAIL',report['result'])
        self.assertEqual('FAIL',report['steps'][-3]['status'])
        self.assertEqual('NOT_RUN',report['steps'][-2]['status'])
        self.assertEqual('NOT_RUN',report['steps'][-1]['status'])

    def test_restore_failure_has_fail_status_and_does_not_run_build(self):
        calls=[]
        def run(argv,**_):
            calls.append(argv)
            return SimpleNamespace(returncode=1 if 'restore' in argv else 0)
        report=run_gate('ci',self.root,runner=run,resolver=self.resolve,
                        verbose=False)
        self.assertEqual('FAIL',report['result'])
        self.assertEqual('FAIL',report['restore']['status'])
        self.assertEqual('NOT_RUN',report['steps'][4]['status'])
        self.assertFalse(any('build' in argv for argv in calls))

    def test_ci_export_contract_missing_file_fails(self):
        def run(argv, **_): return SimpleNamespace(returncode=0)
        report=run_gate('ci',self.root,runner=run,resolver=self.resolve,
                        os_name='Windows',verbose=False)
        self.assertEqual('FAIL', report['result'])
        self.assertEqual('FAIL',report['steps'][-2]['status'])

    def test_git_sha_claims_only_valid_ci_environment_sha(self):
        from unittest.mock import patch
        def run(argv, **_): return SimpleNamespace(returncode=0)
        with patch.dict(os.environ, {'GITHUB_SHA': 'not-an-actual-sha'}):
            r = run_gate('source',self.root,runner=run,resolver=self.resolve,verbose=False)
            self.assertIsNone(r['git_commit'])
        sha = 'a' * 40
        with patch.dict(os.environ, {'GITHUB_SHA': sha}):
            r = run_gate('source',self.root,runner=run,resolver=self.resolve,verbose=False)
            self.assertEqual(sha, r['git_commit'])

    def test_workflow_changes_affect_source_fingerprint(self):
        before=source_digest(self.root)
        workflow=self.root/'.github/workflows/ci.yml'
        workflow.parent.mkdir(parents=True)
        workflow.write_text('name: CI\n')
        self.assertNotEqual(before,source_digest(self.root))
        self.assertEqual(source_digest(self.root),source_digest(self.root))

    def test_full_requires_windows_even_when_every_other_step_passes(self):
        def run(argv, **_):
            if any('VscodeExportContractTests' in arg for arg in argv):
                dest=self.root/'.local-cache/gates/export-from-dotnet.json'
                dest.parent.mkdir(parents=True,exist_ok=True)
                dest.write_text('{}')
            return SimpleNamespace(returncode=0)
        report=run_gate('full',self.root,runner=run,resolver=self.resolve,
                        os_name='Linux',verbose=False)
        self.assertEqual('FAIL', report['result'])
        self.assertEqual('FAIL',report['steps'][-1]['status'])

    def test_source_hash_is_stable_and_content_sensitive(self):
        a=source_digest(self.root)
        p=self.root/'src/Example.cs'
        p.write_text('source code')
        self.assertNotEqual(a,source_digest(self.root))
        self.assertEqual(source_digest(self.root),source_digest(self.root))

    def test_full_pass_does_not_infer_gui_or_online_audit(self):
        def run(argv, **_):
            if any('VscodeExportContractTests' in arg for arg in argv):
                dest=self.root/'.local-cache/gates/export-from-dotnet.json'
                dest.parent.mkdir(parents=True,exist_ok=True)
                dest.write_text('{}')
            return SimpleNamespace(returncode=0)
        report=run_gate('full',self.root,runner=run,resolver=self.resolve,
                        os_name='Windows',verbose=False)
        self.assertEqual('PASS',report['result'])
        self.assertEqual('NOT_RUN',report['vs_code_real_host'])
        self.assertEqual('NOT_RUN',report['desktop_picker_real_io'])
        self.assertEqual('NOT_RUN',report['nuget_vulnerability_audit']['status'])


if __name__=='__main__': unittest.main()
