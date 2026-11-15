using System.Text.Json;

namespace Ristretto.DevTools;

/// <summary>
/// Contratti strutturali storici trasferiti dal vecchio verificatore PowerShell.
/// Sono guardie conservative, non sostituiscono il compilatore né i test .NET.
/// </summary>
public static class SourceContracts
{
    public static List<string> Check(string root)
    {
        var errors = new List<string>();
        string? Read(string relative)
        {
            var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) { errors.Add("File richiesto mancante: " + relative); return null; }
            return File.ReadAllText(path);
        }
        void Require(string relative, params string[] markers)
        {
            var code = Read(relative);
            if (code is null) return;
            foreach (var marker in markers)
                if (!code.Contains(marker, StringComparison.Ordinal))
                    errors.Add($"Contratto mancante in {relative}: {marker}");
        }
        Require("src/TextExpander.Core/Matching/CommittedInputSession.cs", "public static bool IsValidText(string text)");
        Require("src/TextExpander.Core/Matching/CompiledTriggerCatalog.cs", "GetAlternateLookup<ReadOnlySpan<char>>()");
        Require("src/TextExpander.Core/Matching/TriggerActivationSession.cs", "HasEligiblePrefix(_suffix, exactLength)");
        Require("tests/TextExpander.Tests/TriggerActivationSessionTests.cs", "ImmediateAnywhereModeCanMatchInsideWordWhenExplicitlySelected");
        Require("src/TextExpander.Application/Features/Output/ExpansionAdmission.cs", "");
        Require("src/TextExpander.Application/Features/Output/CommittedEditDeltaVerifier.cs", "CommittedInputSession.IsValidText(");
        Require("src/TextExpander.Application/Features/Output/CommittedEditMutationProvenance.cs", "");
        Require("src/TextExpander.Application/Features/Output/RemoteOwnedEditProtocol.cs", "RemoteOwnedEditOneShotGate", "Interlocked.Exchange");
        Require("src/TextExpander.Application/Features/Input/ExternalFocusBoundarySession.cs", "");
        Require("tests/TextExpander.Tests/ExternalFocusBoundarySessionTests.cs", "");
        Require("src/TextExpander.Infrastructure/Platform/Windows/WindowsPickerHotkeyService.cs", "RegisterHotKey");
        Require("src/TextExpander.Infrastructure/Platform/Windows/WindowsExternalAdapterCapability.cs", "ReplacementCapability.Unsupported", "EveryWriteProof");
        Require("src/TextExpander.Infrastructure/Platform/Windows/WindowsPickerInsertRequest.cs", "");
        Require("src/TextExpander.Infrastructure/Platform/Windows/WindowsPickerPasteService.cs", "");
        Require("src/TextExpander.Desktop/PickerSessionController.cs", "");
        Require("tests/TextExpander.Tests/PickerSessionControllerTests.cs", "");
        Require("src/TextExpander.Desktop/App.axaml.cs", "ConfigurePickerHotkeyAsync", "_pickerSession.Open(", "_pickerSession.IsCurrent(");
        Require("src/TextExpander.Desktop/PickerWindow.axaml.cs", "vm.InsertSelectedAsync(app.InsertFromPickerAsync)", "vm.InsertFirstAsync(app.InsertFromPickerAsync)");
        Require("tools/Ristretto.WindowsOutputLab/Program.cs", "--cooperating-smoke", "--remote-owned-smoke", "--assess-external-adapter");
        Require("tools/Ristretto.WindowsOutputLab/NativeReplacementLab.cs", "target.WaitForMessage()", "target.PumpPendingMessages(AfterOwnTurn)");
        Require("tools/Ristretto.WindowsOutputLab/OwnedEditTarget.cs", "WaitMessage()", "PostOwnedCharacterSequenceFixture");
        Require("scripts/validate-native.cmd", "--cooperating-smoke", "--remote-owned-smoke");
        Require("tests/TextExpander.Tests/WindowsExternalAdapterCapabilityTests.cs", "WindowsExternalAdapterCapabilityPolicy.EveryWriteProof, requirements");
        foreach (var file in new[]
        {
            "tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj",
            "tools/Ristretto.CoreBenchmark/Ristretto.CoreBenchmark.csproj",
            "tools/Ristretto.ReplacementLab/Ristretto.ReplacementLab.csproj",
            "tools/Ristretto.UiReview/Ristretto.UiReview.csproj",
            "tests/TextExpander.Tests/VscodeExportContractTests.cs",
            "tests/TextExpander.Tests/WindowsPickerPasteTests.cs",
            "docs/F1B_ACTIVATION_POLICY.md", "docs/ALPHA20_18_NOTEPAD_DIAGNOSTICS.md",
            "tools/ristretto-vscode-extension/cli/verify-dotnet-export.js"
        }) _ = Read(file);
        try
        {
            var package = Read("tools/ristretto-vscode-extension/package.json");
            if (package != null)
            {
                using var json = JsonDocument.Parse(package);
                var enabled = json.RootElement.GetProperty("contributes").GetProperty("configuration")
                    .GetProperty("properties").GetProperty("ristrettoVscode.enabled").GetProperty("default");
                if (enabled.ValueKind != JsonValueKind.False)
                    errors.Add("Adapter VS Code non disabilitato per impostazione predefinita");
            }
        }
        catch (Exception ex) { errors.Add("Metadati VS Code non validi: " + ex.Message); }
        return errors;
    }
}
