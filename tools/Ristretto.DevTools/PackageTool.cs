using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Ristretto.DevTools;

public static class PackageTool
{
    private static readonly HashSet<string> Excluded = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", "bin", "obj", "__pycache__", "archive", "beeftext-screenshots", ".local-cache", "TestResults", "node_modules"
    };

    public static int Run(string root, string[] args)
    {
        var output = Options.Value(args, "--output") ??
            Path.Combine(root, ".local-cache", "Ristretto_" + File.ReadAllText(Path.Combine(root, "VERSION.txt"))
                .Trim().Replace("Ristretto ", "").Replace('.', '_') + "_COMPLETO.zip");
        if (!Path.IsPathFullyQualified(output)) output = Path.GetFullPath(Path.Combine(root, output));
        var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(path) != "MANIFEST_SHA256.txt")
            .Where(path => !Path.GetFullPath(path).Equals(output, StringComparison.OrdinalIgnoreCase))
            .Where(path => !File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            .Where(path => !Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar).Any(Excluded.Contains))
            .Where(path => Path.GetExtension(path) is not ".pyc" and not ".pyo")
            .OrderBy(path => Path.GetRelativePath(root, path).Replace('\\', '/'), StringComparer.Ordinal).ToArray();
        var entries = files.Select(file =>
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            return (File: file, Relative: relative, Hash: Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant());
        }).ToArray();
        var manifest = string.Join("\n", entries.Select(e => e.Hash + "  " + e.Relative)) + "\n";
        File.WriteAllText(Path.Combine(root, "MANIFEST_SHA256.txt"), manifest, new UTF8Encoding(false));
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        if (File.Exists(output)) File.Delete(output);
        using (var archive = ZipFile.Open(output, ZipArchiveMode.Create))
        {
            foreach (var e in entries)
                archive.CreateEntryFromFile(e.File, "Ristretto/" + e.Relative, CompressionLevel.Optimal);
            var last = archive.CreateEntry("Ristretto/MANIFEST_SHA256.txt", CompressionLevel.Optimal);
            using var stream = last.Open();
            var bytes = Encoding.UTF8.GetBytes(manifest);
            stream.Write(bytes);
        }
        using (var archive = ZipFile.OpenRead(output))
        {
            if (archive.Entries.Count != entries.Length + 1)
                throw new InvalidOperationException("Conteggio dei file ZIP inatteso");
            foreach (var entry in entries)
            {
                var zipped = archive.GetEntry("Ristretto/" + entry.Relative) ??
                    throw new InvalidOperationException("File non incluso: " + entry.Relative);
                using var stream = zipped.Open();
                using var digest = SHA256.Create();
                var hex = Convert.ToHexString(digest.ComputeHash(stream)).ToLowerInvariant();
                if (hex != entry.Hash) throw new InvalidOperationException("SHA-256 non valido: " + entry.Relative);
            }
        }
        Console.WriteLine($"ARCHIVIO: {output} | {entries.Length + 1} file | SHA-256 {Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output))).ToLowerInvariant()}");
        return 0;
    }
}
