namespace TextExpander.Infrastructure.Platform.Windows;

/// <summary>
/// Verifies a picker clipboard write without retaining clipboard contents.
/// A sequence transition alone does not prove that SetTextAsync installed the
/// selected snippet: another process may have replaced the clipboard instead.
/// Windows text clipboard providers can canonicalize line endings to CRLF;
/// no other Unicode normalization is accepted (NFC != NFD).
/// </summary>
public static class PickerClipboardReadbackVerifier
{
    /// <returns>The sequence to check again immediately before Ctrl+V, or zero
    /// when the clipboard cannot be tied to the selected snippet.</returns>
    public static uint VerifiedSequence(
        string requestedText, string? observedText,
        uint beforeWrite, uint afterWrite, uint afterRead)
    {
        ArgumentNullException.ThrowIfNull(requestedText);
        if (!WindowsPickerPasteService.HasClipboardAdvanced(beforeWrite, afterWrite) ||
            afterRead == 0 || afterRead != afterWrite || observedText is null)
            return 0;

        return string.Equals(CanonicalLineEndings(requestedText),
                CanonicalLineEndings(observedText), StringComparison.Ordinal)
            ? afterRead : 0;
    }

    private static string CanonicalLineEndings(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);
}
