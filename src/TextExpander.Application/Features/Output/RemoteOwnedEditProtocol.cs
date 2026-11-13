using System.Globalization;

namespace TextExpander.Application.Features.Output;

/// <summary>
/// Alpha 18 laboratory protocol. Transport: redirected stdin/stdout of an EXACT child
/// started by the laboratory; it is not a network protocol, security attestation,
/// authorization token or an API for arbitrary editors. No text or HWND crosses it.
/// </summary>
public enum RemoteOwnedEditReceipt { Verified, RejectedNoChange, PartialOrUnknown, DuplicateOrInvalid }

public static class RemoteOwnedEditProtocol
{
    private const string Prefix = "RISTRETTO_REMOTE_18";
    public const int Sequence = 1;

    public static string Ready(Guid session, int pid)
    {
        CheckSession(session);
        if (pid <= 0) throw new ArgumentOutOfRangeException(nameof(pid));
        return $"{Prefix}|READY|{session:N}|{pid.ToString(CultureInfo.InvariantCulture)}";
    }

    public static bool IsReady(string? value, Guid session, int expectedPid)
    {
        if (!ValidHeader(value, session) || expectedPid <= 0) return false;
        var parts = value!.Split('|');
        return parts.Length == 4 && parts[1] == "READY" &&
            int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out var pid) && pid == expectedPid;
    }

    public static string Request(Guid session)
    {
        CheckSession(session);
        return $"{Prefix}|APPLY|{session:N}|{Sequence}";
    }

    public static bool IsRequest(string? value, Guid session)
    {
        if (!ValidHeader(value, session)) return false;
        var parts = value!.Split('|');
        return parts.Length == 4 && parts[1] == "APPLY" && parts[3] == "1";
    }

    public static string Receipt(Guid session, RemoteOwnedEditReceipt status)
    {
        CheckSession(session);
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        return $"{Prefix}|RESULT|{session:N}|{Sequence}|{status}";
    }

    public static bool TryReadReceipt(string? value, Guid session, out RemoteOwnedEditReceipt status)
    {
        status = RemoteOwnedEditReceipt.PartialOrUnknown;
        if (!ValidHeader(value, session)) return false;
        var parts = value!.Split('|');
        return parts.Length == 5 && parts[1] == "RESULT" && parts[3] == "1" &&
            Enum.TryParse(parts[4], ignoreCase: false, out status) && Enum.IsDefined(status);
    }

    private static bool ValidHeader(string? value, Guid session)
    {
        if (session == Guid.Empty || value is null || value.Length > 128 || value.Any(char.IsWhiteSpace)) return false;
        var parts = value.Split('|');
        return parts.Length is 4 or 5 && parts[0] == Prefix && parts[2] == session.ToString("N");
    }

    private static void CheckSession(Guid session)
    {
        if (session == Guid.Empty) throw new ArgumentException("Empty session", nameof(session));
    }
}

/// <summary>
/// In a private test process, at most ONE command may reach the native edit target.
/// Invalid attempts consume the gate too (fail closed). Never replay after an
/// unknown receipt, crash or timeout. The gate does not establish sender identity.
/// </summary>
public sealed class RemoteOwnedEditOneShotGate(Guid session)
{
    private int _consumed;
    public bool TryConsume(string? command) =>
        Interlocked.Exchange(ref _consumed, 1) == 0 && RemoteOwnedEditProtocol.IsRequest(command, session);
}
