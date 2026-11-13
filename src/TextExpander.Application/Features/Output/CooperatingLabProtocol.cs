using System.Globalization;

namespace TextExpander.Application.Features.Output;

/// <summary>
/// Text-free status protocol for a cooperating Windows laboratory process.
/// Transport is the redirected stdout of the exact child launched by the parent;
/// a nonce is a correlation marker, NOT a security credential or OS attestation.
/// This protocol MUST NOT be used to qualify arbitrary external editors.
/// </summary>
public static class CooperatingLabProtocol
{
    private const string ReadyPrefix = "RISTRETTO_LAB_READY";
    private const string ResultPrefix = "RISTRETTO_LAB_RESULT";
    private const int MaximumCount = 1_000_000;

    public static string Ready(Guid session, int processId)
    {
        if (session == Guid.Empty || processId <= 0) throw new ArgumentOutOfRangeException(nameof(session));
        return $"{ReadyPrefix}|{session:N}|{processId.ToString(CultureInfo.InvariantCulture)}";
    }

    public static bool IsReady(string? message, Guid session, int expectedProcessId)
    {
        if (session == Guid.Empty || expectedProcessId <= 0 || message is null || message.Length > 128)
            return false;
        var fields = message.Split('|');
        return fields.Length == 3 && fields[0] == ReadyPrefix &&
               fields[1] == session.ToString("N") &&
               int.TryParse(fields[2], NumberStyles.None, CultureInfo.InvariantCulture, out var pid) &&
               pid == expectedProcessId;
    }

    public static string Result(Guid session, CooperatingLabCounts counts)
    {
        if (session == Guid.Empty || !counts.IsValid) throw new ArgumentOutOfRangeException(nameof(counts));
        return $"{ResultPrefix}|{session:N}|{counts.Verified.ToString(CultureInfo.InvariantCulture)}|" +
               $"{counts.Rejected.ToString(CultureInfo.InvariantCulture)}|{counts.Uncertain.ToString(CultureInfo.InvariantCulture)}";
    }

    public static bool TryReadResult(string? message, Guid session, out CooperatingLabCounts? counts)
    {
        counts = null;
        if (session == Guid.Empty || message is null || message.Length > 128) return false;
        var fields = message.Split('|');
        if (fields.Length != 5 || fields[0] != ResultPrefix || fields[1] != session.ToString("N") ||
            !TryCount(fields[2], out var verified) ||
            !TryCount(fields[3], out var rejected) || !TryCount(fields[4], out var uncertain))
            return false;
        counts = new(verified, rejected, uncertain);
        return counts.IsValid;
    }

    public static bool IsSuccessful(CooperatingLabCounts? counts, bool matchingReady, int exitCode) =>
        matchingReady && exitCode == 0 && counts is { Verified: > 0, Rejected: 0, Uncertain: 0, IsValid: true };

    private static bool TryCount(string value, out int count) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out count) &&
        count >= 0 && count <= MaximumCount;
}

public sealed record CooperatingLabCounts(int Verified, int Rejected, int Uncertain)
{
    public bool IsValid => Verified >= 0 && Verified <= 1_000_000 &&
                           Rejected >= 0 && Rejected <= 1_000_000 &&
                           Uncertain >= 0 && Uncertain <= 1_000_000;

    // No user text is stored in this type, even by record-generated ToString().
}
