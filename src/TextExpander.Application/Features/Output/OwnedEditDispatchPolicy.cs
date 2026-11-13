namespace TextExpander.Application.Features.Output;

/// <summary>
/// Fail-closed classification of messages dispatched to the laboratory's OWN Win32 EDIT.
/// This is a continuity hint, not a proof of a physical keyboard event. The caller must
/// check the HWND before applying it. Directly sent messages are not necessarily visible
/// to a PeekMessage-based pump and must not be inferred from this classifier.
/// </summary>
public static class OwnedEditDispatchPolicy
{
    // Windows message and virtual-key identifiers kept here to make this rule independently
    // testable on every OS; no user32.dll calls, HWNDs, text, offsets or logging.
    private const uint WmKeyDown = 0x0100, WmSysKeyDown = 0x0104;
    private const uint WmChar = 0x0102, WmSysChar = 0x0106, WmUniChar = 0x0109;
    private const uint WmImeStart = 0x010d, WmImeEnd = 0x010e, WmImeComposition = 0x010f;
    private const uint WmImeChar = 0x0286, WmImeNotify = 0x0282, WmInputLanguageChange = 0x0051;
    private const uint WmSetFocus = 0x0007, WmKillFocus = 0x0008;
    private const uint WmLButtonDown = 0x0201, WmLButtonDoubleClick = 0x0203;
    private const uint WmRButtonDown = 0x0204, WmMButtonDown = 0x0207, WmMouseWheel = 0x020a;
    private const uint WmPaste = 0x0302, WmCut = 0x0300, WmCopy = 0x0301;
    private const uint WmClear = 0x0303, WmUndo = 0x0304;
    private const uint WmSetText = 0x000c, WmDropFiles = 0x0233;
    private const uint EmSetSel = 0x00b1, EmReplaceSel = 0x00c2;
    private const uint VkBack = 0x08, VkTab = 0x09, VkReturn = 0x0d, VkEscape = 0x1b;
    private const uint VkPrior = 0x21, VkNext = 0x22, VkEnd = 0x23, VkHome = 0x24;
    private const uint VkLeft = 0x25, VkUp = 0x26, VkRight = 0x27, VkDown = 0x28;
    private const uint VkInsert = 0x2d, VkDelete = 0x2e, VkProcessKey = 0xe5;
    private const uint VkShift = 0x10, VkControl = 0x11, VkMenu = 0x12;

    /// <summary>
    /// commandModifier means Ctrl or Alt is pressed (including AltGr); Shift alone is
    /// not enough to invalidate printable characters. All shortcuts are treated as
    /// potentially editing/navigation actions, even before a text change is observed.
    /// </summary>
    public static OwnedEditDispatchDecision Classify(uint message, nuint wParam, bool commandModifier)
    {
        if (message is WmChar or WmUniChar)
        {
            bool printable = message == WmChar
                ? wParam >= (nuint)0x20 && wParam <= (nuint)0xffff
                : wParam >= (nuint)0x20 && wParam <= (nuint)0x10ffff &&
                  wParam != (nuint)0xffff; // UNICODE_NOCHAR is a probe, not text.
            // WM_CHAR may carry ONE UTF-16 surrogate code unit at a time.
            // Keep it only as a candidate; the committed-delta verifier must reject
            // an unpaired surrogate and requires a complete, well-formed document.
            // WM_UNICHAR instead carries a scalar, so surrogate values are invalid.
            if (message == WmUniChar && wParam >= (nuint)0xd800 && wParam <= (nuint)0xdfff)
                printable = false;
            return printable && !commandModifier
                ? new(OwnedEditMutationSource.OwnedCharacterMessage, false)
                : new(OwnedEditMutationSource.Unknown, true);
        }

        if (message is WmKeyDown or WmSysKeyDown)
        {
            if (message == WmSysKeyDown)
                return new(OwnedEditMutationSource.Unknown, true); // System menu / Alt sequences.
            var key = (uint)wParam;
            var accelerator = commandModifier && key is not (VkShift or VkControl or VkMenu);
            var navigation = key is VkBack or VkTab or VkReturn or VkEscape or
                VkPrior or VkNext or VkEnd or VkHome or VkLeft or VkUp or VkRight or VkDown or
                VkInsert or VkDelete or VkProcessKey;
            return new(OwnedEditMutationSource.Unknown, accelerator || navigation);
        }

        if (message == WmPaste) return new(OwnedEditMutationSource.ClipboardOrPaste, true);
        if (message is WmImeStart or WmImeEnd or WmImeComposition or
            WmImeChar or WmImeNotify or WmInputLanguageChange)
            return new(OwnedEditMutationSource.ImeOrComposition, true);
        if (message is WmSysChar or WmCut or WmCopy or WmClear or WmUndo or
            WmSetText or WmDropFiles or EmSetSel or EmReplaceSel or
            WmSetFocus or WmKillFocus or WmLButtonDown or WmLButtonDoubleClick or
            WmRButtonDown or WmMButtonDown or WmMouseWheel)
            return new(OwnedEditMutationSource.Unknown, true);
        return new(OwnedEditMutationSource.Unknown, false);
    }
}

/// <summary>Only bounded classification metadata; no text, keys, HWND or clipboard data.</summary>
public readonly record struct OwnedEditDispatchDecision(
    OwnedEditMutationSource MutationSource, bool BreaksContinuity);
