using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

namespace TextExpander.Desktop.SingleInstance;

/// <summary>Un processo proprietario del database per profilo. Il secondo avvio richiede di mostrare la GUI.</summary>
public sealed class SingleInstanceHost : IDisposable
{
    private readonly Mutex _mutex;
    private readonly bool _ownsMutex;
    private readonly string _pipeName;
    private readonly FileStream? _profileLease;
    private readonly CancellationTokenSource _stop = new();
    private Task? _listener;

    public bool IsPrimary => _ownsMutex;

    public static string InstanceKey(string dataDirectory)
    {
        var path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(dataDirectory));
        if (OperatingSystem.IsWindows()) path = path.ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(path)))[..24];
    }

    public SingleInstanceHost(string dataDirectory)
    {
        var key = InstanceKey(dataDirectory);
        _pipeName = "TextExpanderStudio_" + key;
        _mutex = new Mutex(initiallyOwned: true, name: "TextExpanderStudioMutex_" + key, createdNew: out _ownsMutex);
        if (_ownsMutex)
        {
            try
            {
                Directory.CreateDirectory(dataDirectory);
                // The OS file lock also rejects a second owner through a symlink/junction alias.
                // Keep the empty file after shutdown: unlinking it could allow a new inode to bypass the lock.
                _profileLease = new FileStream(Path.Combine(dataDirectory, ".ristretto.profile.lock"),
                    FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
                _stop.Dispose();
                throw;
            }
        }
    }

    public void StartListening(Action activate)
    {
        if (!_ownsMutex) throw new InvalidOperationException("Solo l'istanza principale può ricevere richieste.");
        if (_listener is not null) return;
        _listener = Task.Run(async () =>
        {
            while (!_stop.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync(_stop.Token);
                    using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    readTimeout.CancelAfter(TimeSpan.FromSeconds(3));
                    var request = new byte[1];
                    if (await server.ReadAsync(request.AsMemory(), readTimeout.Token) == 1 && request[0] == 0x53)
                        activate();
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested) { break; }
                catch (OperationCanceledException) { /* Client connesso senza messaggio: riprendi. */ }
                catch (IOException) { /* Pipe interrotta: non terminare l'app. */ }
            }
        });
    }

    public async Task<bool> RequestActivationAsync()
    {
        if (_ownsMutex) return false;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                await client.ConnectAsync(120);
                await client.WriteAsync(new byte[] { 0x53 });
                await client.FlushAsync();
                return true;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException)
            {
                await Task.Delay(80);
            }
        }
        return false;
    }

    public void Dispose()
    {
        _stop.Cancel();
        // Il listener può trovarsi in WaitForConnectionAsync: consentire alla cancellazione
        // di liberare la pipe prima di distruggere il token.
        try { _listener?.Wait(TimeSpan.FromSeconds(1)); }
        catch (AggregateException) { /* Il processo sta già terminando. */ }
        _stop.Dispose();
        _profileLease?.Dispose();
        if (_ownsMutex) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
