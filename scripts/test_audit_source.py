import tempfile
import unittest
from pathlib import Path
from audit_source import audit


class SourceAuditTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'VERSION.txt').write_text('Ristretto 3.9.0-alpha.20.2\n')
        core = self.root / 'src/Core'
        app = self.root / 'src/App'
        core.mkdir(parents=True)
        app.mkdir(parents=True)
        (core / 'Core.csproj').write_text('<Project/>')
        (app / 'App.csproj').write_text('<Project><ItemGroup><ProjectReference Include="../Core/Core.csproj"/></ItemGroup><PropertyGroup><Version>3.9.0-alpha.20.2</Version></PropertyGroup></Project>')
        (core / 'Core.cs').write_text('namespace TextExpander.Core;\npublic class Value {}')
        (app / 'App.cs').write_text('using TextExpander.Core;\nnamespace TextExpander.App;\npublic class App {}')
        (self.root / 'Ristretto.sln').write_text('Project("{ID}") = "Core", "src\\Core\\Core.csproj", "{CORE}"\nProject("{ID}") = "App", "src\\App\\App.csproj", "{APP}"\n')

    def test_valid_graph(self):
        self.assertEqual([], audit(self.root))

    def test_unknown_using(self):
        p = self.root / 'src/App/App.cs'
        p.write_text('using TextExpander.Missing;\nnamespace TextExpander.App;')
        self.assertTrue(any('using interno sconosciuto' in issue for issue in audit(self.root)))

    def test_missing_project_reference(self):
        p = self.root / 'src/App/App.csproj'
        p.write_text('<Project><PropertyGroup><Version>3.9.0-alpha.20.2</Version></PropertyGroup></Project>')
        self.assertTrue(any('senza ProjectReference' in issue for issue in audit(self.root)))

    def test_version_mismatch(self):
        p = self.root / 'src/App/App.csproj'
        p.write_text('<Project><PropertyGroup><Version>3.9.0-alpha.19</Version></PropertyGroup></Project>')
        self.assertTrue(any('versione' in issue for issue in audit(self.root)))

    def test_cross_assembly_internal_static_member_is_rejected(self):
        core = self.root / 'src/Core/Core.cs'
        app = self.root / 'src/App/App.cs'
        core.write_text('namespace TextExpander.Core;\npublic class Value { internal static bool IsValidText(string value) => true; }')
        app.write_text('using TextExpander.Core;\nnamespace TextExpander.App;\npublic class App { public bool Go() => Value.IsValidText("a"); }')
        self.assertTrue(any('chiamata cross-assembly a membro internal Value.IsValidText' in issue
                            for issue in audit(self.root)))

    def test_public_static_member_is_accessible_across_projects(self):
        core = self.root / 'src/Core/Core.cs'
        app = self.root / 'src/App/App.cs'
        core.write_text('namespace TextExpander.Core;\npublic class Value { public static bool IsValidText(string value) => true; }')
        app.write_text('using TextExpander.Core;\nnamespace TextExpander.App;\npublic class App { public bool Go() => Value.IsValidText("a"); }')
        self.assertEqual([], audit(self.root))

    def test_xunit_import_required(self):
        tests = self.root / 'tests/TextExpander.Tests'
        tests.mkdir(parents=True)
        (tests / 'Tests.csproj').write_text('<Project/>')
        (self.root / 'Ristretto.sln').write_text((self.root / 'Ristretto.sln').read_text() +
            'Project("{ID}") = "Tests", "tests\\TextExpander.Tests\\Tests.csproj", "{TESTS}"\n')
        (tests / 'BrokenTests.cs').write_text('namespace TextExpander.Tests;\npublic class BrokenTests { [Fact] public void Test() {} }')
        self.assertTrue(any('manca using Xunit' in issue for issue in audit(self.root)))


if __name__ == '__main__':
    unittest.main()
