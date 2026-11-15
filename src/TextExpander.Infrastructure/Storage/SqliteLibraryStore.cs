using Microsoft.Data.Sqlite;
using TextExpander.Application.Abstractions;
using TextExpander.Core;
using TextExpander.Application.Features.Portability;
using TextExpander.Application.Features.Hotkeys;

namespace TextExpander.Infrastructure.Storage;

/// <summary>
/// Connessioni brevi, scritture serializzate, niente SQL nel callback tastiera.
/// Il processo desktop applica una sola istanza; SQLite gestisce la serializzazione sul file.
/// </summary>
public sealed class SqliteLibraryStore(string databasePath) : ILibraryStore
{
    private const int SchemaVersion = 2;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _path = Path.GetFullPath(databasePath);

    public string DatabasePath => _path;

    private SqliteConnection Open(SqliteOpenMode mode = SqliteOpenMode.ReadWriteCreate)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = _path, Mode = mode, Pooling = false,
            Cache = SqliteCacheMode.Private, ForeignKeys = true, DefaultTimeout = 5
        };
        var connection = new SqliteConnection(builder.ToString());
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = mode == SqliteOpenMode.ReadOnly
            ? "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"
            : "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000; PRAGMA synchronous=FULL;";
        cmd.ExecuteNonQuery();
        return connection;
    }

    private static SqliteCommand Cmd(SqliteConnection db, string sql,
        SqliteTransaction? transaction = null, params (string Name, object? Value)[] parameters)
    {
        var command = db.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    private async Task<T> Exclusive<T>(Func<T> work, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try { return await Task.Run(work, ct).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _ = await Exclusive(() =>
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            using var db = Open();
            using (var journal = Cmd(db, "PRAGMA journal_mode=WAL;"))
            {
                var result = Convert.ToString(journal.ExecuteScalar());
                if (!string.Equals(result, "wal", StringComparison.OrdinalIgnoreCase))
                    throw new DataIntegrityException("SQLite non ha attivato WAL.");
            }
            using (var sync = Cmd(db, "PRAGMA synchronous=FULL;")) sync.ExecuteNonQuery();
            EnsureSchema(db);
            return true;
        }, ct).ConfigureAwait(false);
    }

    private static void EnsureSchema(SqliteConnection db)
    {
        int version;
        using (var cmd = Cmd(db, "PRAGMA user_version;"))
        {
            version = Convert.ToInt32(cmd.ExecuteScalar());
            if (version is < 0 or > SchemaVersion)
                throw new DataIntegrityException("Schema dell'archivio non supportato.");
            if (version == SchemaVersion) return;
        }
        using var tx = db.BeginTransaction();
        if (version == 0)
        {
            using (var schema = Cmd(db, @"
CREATE TABLE IF NOT EXISTS groups (
    id TEXT PRIMARY KEY NOT NULL,
    name TEXT NOT NULL COLLATE NOCASE UNIQUE CHECK(length(name) BETWEEN 1 AND 120),
    enabled INTEGER NOT NULL DEFAULT 1 CHECK(enabled IN (0,1))
);
CREATE TABLE IF NOT EXISTS snippets (
    id TEXT PRIMARY KEY NOT NULL,
    group_id TEXT NOT NULL REFERENCES groups(id) ON DELETE RESTRICT,
    title TEXT NOT NULL CHECK(length(title) BETWEEN 1 AND 160),
    trigger TEXT NOT NULL CHECK(length(trigger) BETWEEN 1 AND 128),
    content TEXT NOT NULL CHECK(length(content)>0),
    description TEXT NOT NULL DEFAULT '',
    enabled INTEGER NOT NULL DEFAULT 1 CHECK(enabled IN (0,1)),
    revision INTEGER NOT NULL DEFAULT 1 CHECK(revision > 0),
    hotkey TEXT NOT NULL DEFAULT '' CHECK(length(hotkey)<=64)
);
CREATE INDEX IF NOT EXISTS idx_snippets_group ON snippets(group_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_snippets_hotkey ON snippets(hotkey) WHERE hotkey<>'';
CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY NOT NULL, value TEXT NOT NULL);
CREATE TABLE IF NOT EXISTS app_meta (id INTEGER PRIMARY KEY CHECK(id=1), library_revision INTEGER NOT NULL DEFAULT 0);
INSERT OR IGNORE INTO app_meta(id,library_revision) VALUES (1,0);
PRAGMA user_version=2;", tx)) schema.ExecuteNonQuery();
        }
        if (version == 1)
        {
            using var migration = Cmd(db, @"ALTER TABLE snippets ADD COLUMN hotkey TEXT NOT NULL DEFAULT '' CHECK(length(hotkey)<=64);
CREATE UNIQUE INDEX idx_snippets_hotkey ON snippets(hotkey) WHERE hotkey<>'';
PRAGMA user_version=2;", tx);
            migration.ExecuteNonQuery();
        }
        tx.Commit();
    }

    public Task<LibrarySnapshot> ReadAsync(CancellationToken ct = default) => Exclusive(() =>
    {
        using var db = Open(SqliteOpenMode.ReadWrite);
        var groups = new List<SnippetGroup>();
        var snippets = new List<Snippet>();
        using (var cmd = Cmd(db, "SELECT id,name,enabled FROM groups ORDER BY name"))
        using (var reader = cmd.ExecuteReader())
            while (reader.Read())
                groups.Add(new(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetInt64(2) != 0));
        using (var cmd = Cmd(db, "SELECT id,group_id,title,trigger,content,description,enabled,revision,hotkey FROM snippets ORDER BY title"))
        using (var reader = cmd.ExecuteReader())
            while (reader.Read())
                snippets.Add(new(Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)),
                    reader.GetString(2), reader.GetString(3), reader.GetString(4),
                    reader.GetString(5), reader.GetInt64(6) != 0, reader.GetInt64(7), reader.GetString(8)));
        using var revision = Cmd(db, "SELECT library_revision FROM app_meta WHERE id=1");
        return new LibrarySnapshot(groups, snippets, Convert.ToInt64(revision.ExecuteScalar()));
    }, ct);

    public Task<SnippetGroup> CreateGroupAsync(string name, CancellationToken ct = default) => Exclusive(() =>
    {
        LibraryRules.ValidateGroupName(name);
        var item = new SnippetGroup(Guid.NewGuid(), name.Trim(), true);
        using var db = Open(SqliteOpenMode.ReadWrite);
        using var tx = db.BeginTransaction();
        using (var cmd = Cmd(db, "INSERT INTO groups(id,name,enabled) VALUES ($id,$name,1)", tx,
               ("$id", item.Id.ToString()), ("$name", item.Name))) cmd.ExecuteNonQuery();
        Touch(db, tx);
        tx.Commit();
        return item;
    }, ct);

    public Task<SnippetGroup> UpdateGroupAsync(SnippetGroup previous, string newName, bool enabled,
        CancellationToken ct = default) => Exclusive(() =>
    {
        LibraryRules.ValidateGroupName(newName);
        if (previous.Id == Guid.Empty) throw new ArgumentException("Gruppo non valido.");
        string cleaned = newName.Trim();
        using var db = Open(SqliteOpenMode.ReadWrite);
        using var tx = db.BeginTransaction();
        using var cmd = Cmd(db, @"UPDATE groups SET name=$newName,enabled=$newEnabled
WHERE id=$id AND name=$oldName AND enabled=$oldEnabled", tx,
            ("$id", previous.Id.ToString()), ("$newName", cleaned),
            ("$newEnabled", enabled ? 1 : 0), ("$oldName", previous.Name),
            ("$oldEnabled", previous.IsEnabled ? 1 : 0));
        if (cmd.ExecuteNonQuery() != 1)
            throw new ConcurrencyException("Gruppo modificato o eliminato: ricaricare la libreria.");
        Touch(db, tx);
        tx.Commit();
        return previous with { Name = cleaned, IsEnabled = enabled };
    }, ct);

    public async Task DeleteGroupAsync(SnippetGroup previous, CancellationToken ct = default)
    {
        _ = await Exclusive(() =>
        {
            using var db = Open(SqliteOpenMode.ReadWrite);
            using var tx = db.BeginTransaction();
            using (var count = Cmd(db, "SELECT COUNT(*) FROM snippets WHERE group_id=$id", tx,
                ("$id", previous.Id.ToString())))
            {
                if (Convert.ToInt64(count.ExecuteScalar()) != 0)
                    throw new InvalidOperationException("Il gruppo contiene snippet: spostarli o eliminarli prima.");
            }
            using (var cmd = Cmd(db, @"DELETE FROM groups
WHERE id=$id AND name=$name AND enabled=$enabled", tx,
                ("$id", previous.Id.ToString()), ("$name", previous.Name),
                ("$enabled", previous.IsEnabled ? 1 : 0)))
                if (cmd.ExecuteNonQuery() != 1)
                    throw new ConcurrencyException("Gruppo modificato o già eliminato.");
            Touch(db, tx);
            tx.Commit();
            return true;
        }, ct).ConfigureAwait(false);
    }

    public Task<ImportSummary> ImportPortableAsync(PortableLibraryDocument document,
        CancellationToken ct = default, long? expectedLibraryRevision = null) => Exclusive(() =>
    {
        PortableLibraryFormat.Validate(document);
        using var db = Open(SqliteOpenMode.ReadWrite);
        using var tx = db.BeginTransaction();
        if (expectedLibraryRevision.HasValue)
        {
            using var revision = Cmd(db, "SELECT library_revision FROM app_meta WHERE id=1", tx);
            if (Convert.ToInt64(revision.ExecuteScalar()) != expectedLibraryRevision.Value)
                throw new ConcurrencyException("La libreria è cambiata dopo l'anteprima: ripetere l'importazione.");
        }
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var names = Cmd(db, "SELECT name FROM groups", tx))
        using (var rows = names.ExecuteReader())
            while (rows.Read()) reserved.Add(rows.GetString(0));
        var newGroupIds = new Dictionary<Guid, Guid>();
        foreach (var group in document.Groups)
        {
            var newId = Guid.NewGuid();
            var name = group.Name.Trim();
            if (reserved.Contains(name))
            {
                for (int suffix = 2; ; suffix++)
                {
                    var ending = $" (import {suffix})";
                    var prefix = name.Length > 120 - ending.Length
                        ? name[..(120 - ending.Length)].TrimEnd() : name;
                    var candidate = prefix + ending;
                    if (reserved.Add(candidate)) { name = candidate; break; }
                    if (suffix == 100_001) throw new DataIntegrityException("Troppi nomi di gruppo duplicati.");
                }
            }
            else reserved.Add(name);
            using var cmd = Cmd(db, "INSERT INTO groups(id,name,enabled) VALUES($id,$name,$on)", tx,
                ("$id", newId.ToString()), ("$name", name), ("$on", group.IsEnabled ? 1 : 0));
            cmd.ExecuteNonQuery();
            newGroupIds.Add(group.Id, newId);
        }
        foreach (var snippet in document.Snippets)
        {
            var hotkey = SnippetHotkeyRules.Normalize(snippet.Hotkey);
            EnsureHotkeyAvailable(db, tx, hotkey, null);
            using var cmd = Cmd(db, @"INSERT INTO snippets
(id,group_id,title,trigger,content,description,enabled,revision,hotkey)
VALUES ($id,$group,$title,$trigger,$content,$description,$enabled,1,$hotkey)", tx,
                ("$id", Guid.NewGuid().ToString()),
                ("$group", newGroupIds[snippet.GroupId].ToString()),
                ("$title", snippet.Title),
                ("$trigger", LibraryRules.CanonicalTrigger(snippet.Trigger)),
                ("$content", snippet.Content), ("$description", snippet.Description),
                ("$enabled", snippet.IsEnabled ? 1 : 0), ("$hotkey", hotkey));
            cmd.ExecuteNonQuery();
        }
        Touch(db, tx);
        tx.Commit();
        return new ImportSummary(document.Groups.Count, document.Snippets.Count);
    }, ct);

    private static void Touch(SqliteConnection db, SqliteTransaction tx)
    {
        using var cmd = Cmd(db, "UPDATE app_meta SET library_revision=library_revision+1 WHERE id=1", tx);
        cmd.ExecuteNonQuery();
    }

    private static void EnsureHotkeyAvailable(SqliteConnection db, SqliteTransaction? tx, string hotkey, Guid? exceptId)
    {
        if (hotkey.Length == 0) return;
        using var picker = Cmd(db, "SELECT value FROM settings WHERE key='picker.hotkey'", tx);
        var pickerValue = picker.ExecuteScalar() as string ?? PickerHotkey.Default;
        if (PickerHotkey.TryParse(pickerValue, out var reserved) && !reserved.IsDisabled && reserved.Canonical == hotkey)
            throw new SnippetHotkeyConflictException();
        using var other = Cmd(db, "SELECT 1 FROM snippets WHERE hotkey=$hotkey AND id<>$except LIMIT 1", tx,
            ("$hotkey", hotkey), ("$except", exceptId?.ToString() ?? ""));
        if (other.ExecuteScalar() is not null) throw new SnippetHotkeyConflictException();
    }

    public Task<Snippet> SaveSnippetAsync(SnippetDraft draft, CancellationToken ct = default) => Exclusive(() =>
    {
        LibraryRules.Validate(draft);
        string canonical = LibraryRules.CanonicalTrigger(draft.Trigger);
        string hotkey = SnippetHotkeyRules.Normalize(draft.Hotkey);
        using var db = Open(SqliteOpenMode.ReadWrite);
        using var tx = db.BeginTransaction();
        EnsureHotkeyAvailable(db, tx, hotkey, draft.Id);
        var id = draft.Id ?? Guid.NewGuid();
        var nextRevision = 1L;
        if (draft.Id is null)
        {
            using var cmd = Cmd(db, @"INSERT INTO snippets
(id,group_id,title,trigger,content,description,enabled,revision,hotkey)
VALUES ($id,$group,$title,$trigger,$content,$description,$enabled,1,$hotkey)", tx,
                ("$id", id.ToString()), ("$group", draft.GroupId.ToString()),
                ("$title", draft.Title), ("$trigger", canonical),
                ("$content", draft.Content), ("$description", draft.Description),
                ("$enabled", draft.IsEnabled ? 1 : 0), ("$hotkey", hotkey));
            cmd.ExecuteNonQuery();
        }
        else
        {
            if (draft.ExpectedRevision is null)
                throw new ArgumentException("Revisione attesa obbligatoria per l'aggiornamento.");
            nextRevision = draft.ExpectedRevision.Value + 1;
            using var cmd = Cmd(db, @"UPDATE snippets SET
 group_id=$group,title=$title,trigger=$trigger,content=$content,description=$description,
 enabled=$enabled,hotkey=$hotkey,revision=revision+1
WHERE id=$id AND revision=$expected", tx,
                ("$id", id.ToString()), ("$group", draft.GroupId.ToString()),
                ("$title", draft.Title), ("$trigger", canonical), ("$content", draft.Content),
                ("$description", draft.Description), ("$enabled", draft.IsEnabled ? 1 : 0),
                ("$expected", draft.ExpectedRevision.Value), ("$hotkey", hotkey));
            if (cmd.ExecuteNonQuery() != 1)
                throw new ConcurrencyException("Lo snippet è cambiato: ricarica prima di salvare.");
        }
        Touch(db, tx);
        tx.Commit();
        return new Snippet(id, draft.GroupId, draft.Title, canonical,
            draft.Content, draft.Description, draft.IsEnabled, nextRevision, hotkey);
    }, ct);

    public async Task DeleteSnippetAsync(Guid id, long expectedRevision, CancellationToken ct = default)
    {
        _ = await Exclusive(() =>
        {
            using var db = Open(SqliteOpenMode.ReadWrite);
            using var tx = db.BeginTransaction();
            using var cmd = Cmd(db, "DELETE FROM snippets WHERE id=$id AND revision=$rev", tx,
                ("$id", id.ToString()), ("$rev", expectedRevision));
            if (cmd.ExecuteNonQuery() != 1)
                throw new ConcurrencyException("Snippet modificato o già eliminato.");
            Touch(db, tx);
            tx.Commit();
            return true;
        }, ct).ConfigureAwait(false);
    }

    public Task<string?> ReadSettingAsync(string key, CancellationToken ct = default) => Exclusive(() =>
    {
        using var db = Open(SqliteOpenMode.ReadWrite);
        using var cmd = Cmd(db, "SELECT value FROM settings WHERE key=$key", null, ("$key", key));
        return cmd.ExecuteScalar() as string;
    }, ct);

    public async Task WriteSettingAsync(string key, string value, CancellationToken ct = default)
    {
        if (key is not ("language" or "theme" or "picker.hotkey")) throw new ArgumentException("Impostazione non consentita.");
        _ = await Exclusive(() =>
        {
            using var db = Open(SqliteOpenMode.ReadWrite);
            using var tx = db.BeginTransaction();
            if (key == "picker.hotkey")
            {
                if (!PickerHotkey.TryParse(value, out var gesture)) throw new ArgumentException("Hotkey picker non valida.");
                value = gesture.Canonical;
                if (!gesture.IsDisabled)
                {
                    using var conflict = Cmd(db, "SELECT 1 FROM snippets WHERE hotkey=$key LIMIT 1", tx, ("$key", value));
                    if (conflict.ExecuteScalar() is not null) throw new SnippetHotkeyConflictException();
                }
            }
            using var cmd = Cmd(db, @"INSERT INTO settings(key,value) VALUES($key,$value)
ON CONFLICT(key) DO UPDATE SET value=excluded.value", tx,
                ("$key", key), ("$value", value));
            cmd.ExecuteNonQuery();
            tx.Commit();
            return true;
        }, ct).ConfigureAwait(false);
    }

    private static SqliteConnection External(string path, SqliteOpenMode mode)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = mode, Pooling = false,
            Cache = SqliteCacheMode.Private, ForeignKeys = true, DefaultTimeout = 5
        }.ToString());
        connection.Open();
        return connection;
    }

    private static void ValidateDatabase(string file)
    {
        using var db = External(file, SqliteOpenMode.ReadOnly);
        using (var integrity = Cmd(db, "PRAGMA integrity_check;"))
            if (!string.Equals(Convert.ToString(integrity.ExecuteScalar()), "ok", StringComparison.OrdinalIgnoreCase))
                throw new DataIntegrityException("Backup SQLite danneggiato.");
        using (var fk = Cmd(db, "PRAGMA foreign_key_check;"))
        using (var rows = fk.ExecuteReader())
            if (rows.Read()) throw new DataIntegrityException("Vincoli relazionali invalidi nel backup.");
        using (var ver = Cmd(db, "PRAGMA user_version;"))
            if (Convert.ToInt32(ver.ExecuteScalar()) is < 1 or > SchemaVersion)
                throw new DataIntegrityException("Schema del backup non supportato.");
        using (var meta = Cmd(db, "SELECT library_revision FROM app_meta WHERE id=1"))
            if (meta.ExecuteScalar() is null) throw new DataIntegrityException("Metadati mancanti.");
    }

    private static void BackupFrom(string sourcePath, string destinationPath)
    {
        using var source = External(sourcePath, SqliteOpenMode.ReadOnly);
        using var destination = External(destinationPath, SqliteOpenMode.ReadWriteCreate);
        source.BackupDatabase(destination);
    }

    public Task<string> CreateBackupAsync(string destinationPath, CancellationToken ct = default) => Exclusive(() =>
    {
        string dest = Path.GetFullPath(destinationPath);
        if (dest == _path) throw new ArgumentException("Destinazione uguale al database attivo.");
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        if (File.Exists(dest)) throw new IOException("Esiste già un file con questo nome.");
        string temp = dest + "." + Guid.NewGuid().ToString("N") + ".partial";
        try
        {
            BackupFrom(_path, temp);
            ValidateDatabase(temp);
            File.Move(temp, dest);
            return dest;
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }, ct);

    public async Task RestoreBackupAsync(string sourcePath, CancellationToken ct = default)
    {
        _ = await Exclusive(() =>
        {
            string source = Path.GetFullPath(sourcePath);
            if (source == _path || !File.Exists(source))
                throw new ArgumentException("Seleziona un backup SQLite esistente, diverso dal DB corrente.");
            ValidateDatabase(source);
            string token = Guid.NewGuid().ToString("N");
            string stage = _path + "." + token + ".stage";
            string recovery = _path + "." + token + ".recovery.db";
            bool movedOriginal = false;
            try
            {
                // Copia consistente: non si lavora mai direttamente sul file scelto dall'utente.
                BackupFrom(source, stage);
                using (var staged = External(stage, SqliteOpenMode.ReadWrite)) EnsureSchema(staged);
                ValidateDatabase(stage);
                using (var live = Open(SqliteOpenMode.ReadWrite))
                using (var checkpoint = Cmd(live, "PRAGMA wal_checkpoint(TRUNCATE);"))
                using (var reader = checkpoint.ExecuteReader())
                    if (!reader.Read() || reader.GetInt32(0) != 0)
                        throw new IOException("Database occupato: impossibile ripristinare in sicurezza.");
                // Fail-closed: non sostituire un DB se persistono sidecar WAL/SHM.
                if (File.Exists(_path + "-wal") || File.Exists(_path + "-shm"))
                    throw new IOException("Database ancora utilizzato: chiudere le altre connessioni.");
                File.Move(_path, recovery);
                movedOriginal = true;
                try
                {
                    File.Move(stage, _path);
                    ValidateDatabase(_path);
                }
                catch
                {
                    if (File.Exists(_path)) File.Delete(_path);
                    File.Move(recovery, _path);
                    movedOriginal = false;
                    throw;
                }
                // Non eliminare recovery: è il punto di ripristino pre-operazione.
                return true;
            }
            finally
            {
                if (File.Exists(stage)) File.Delete(stage);
                if (movedOriginal && !File.Exists(_path) && File.Exists(recovery))
                    File.Move(recovery, _path);
            }
        }, ct).ConfigureAwait(false);
    }
}
