using System.Text.Json;
using TextExpander.Application.Features.Portability;
using TextExpander.Core;
using TextExpander.Infrastructure.Storage;
using Xunit;

namespace TextExpander.Tests;

/// <summary>
/// Alpha 20: produce un export con il servizio .NET di produzione. Lo script
/// validate-export-contract.cmd lo passa al consumer Node senza file utente.
/// </summary>
public sealed class VscodeExportContractTests
{
    [Fact]
    public async Task ProductionExport_IsSuitableForTheCrossRuntimeContract()
    {
        var root = Path.Combine(Path.GetTempPath(), "ristretto-vscode-contract", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new SqliteLibraryStore(Path.Combine(root, "library.db"));
            await store.InitializeAsync();
            var active = await store.CreateGroupAsync("Demo Unicode 🌍");
            var disabled = await store.CreateGroupAsync("Disabilitati");
            _ = await store.UpdateGroupAsync(disabled, disabled.Name, false);

            await store.SaveSnippetAsync(new SnippetDraft(null, active.Id,
                "Firma sintetica", ";firma", "Caffè ☕ 漢字", "", true, null));
            await store.SaveSnippetAsync(new SnippetDraft(null, active.Id,
                "Combinazioni", ";café", "Ok NFC/NFD", "", true, null));
            await store.SaveSnippetAsync(new SnippetDraft(null, active.Id,
                "Emoji composta", ";👩🏽‍💻", "Unicode 💫", "", true, null));
            await store.SaveSnippetAsync(new SnippetDraft(null, active.Id,
                "Snippet spento", ";off", "NON INSERIRE", "", false, null));
            await store.SaveSnippetAsync(new SnippetDraft(null, disabled.Id,
                "Gruppo spento", ";hidden", "NON INSERIRE", "", true, null));
            await store.SaveSnippetAsync(new SnippetDraft(null, active.Id,
                "Molto lungo", ";big", new string('X', 70_000), "", true, null));

            var exported = await new PortableLibraryService(store)
                .ExportAsync(Path.Combine(root, "production-export.json"));
            var info = new FileInfo(exported);
            Assert.InRange(info.Length, 1L, 8L * 1024 * 1024);
            using (var json = JsonDocument.Parse(await File.ReadAllTextAsync(exported)))
            {
                Assert.Equal(PortableLibraryFormat.Identifier,
                    json.RootElement.GetProperty("format").GetString());
                Assert.Equal(PortableLibraryFormat.Version,
                    json.RootElement.GetProperty("version").GetInt32());
                Assert.Equal(2, json.RootElement.GetProperty("groups").GetArrayLength());
                Assert.Equal(6, json.RootElement.GetProperty("snippets").GetArrayLength());
            }

            // Only the explicit contract gate supplies this variable. The usual
            // xUnit run still tests the export, without leaving files behind.
            var output = Environment.GetEnvironmentVariable("RISTRETTO_VSCODE_EXPORT_OUTPUT");
            if (!string.IsNullOrWhiteSpace(output))
            {
                var destination = Path.GetFullPath(output);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(exported, destination, overwrite: false);
            }
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch (IOException) { }
        }
    }
}
