using System;

namespace VlyDev.Cs2MaskedInspect;

/// <summary>
/// Thrown by <see cref="InspectLink.Deserialize(string)"/> when the input cannot be a valid
/// inspect-link payload — odd-length hex, non-hex characters, payload shorter than the
/// minimum, or proto bytes that fail to parse cleanly.
/// </summary>
/// <remarks>
/// Extends <see cref="ArgumentException"/> for backwards compatibility with callers
/// that catch the parent class.
/// </remarks>
public class MalformedInspectLinkException : ArgumentException
{
    public MalformedInspectLinkException(string message) : base(message) { }
    public MalformedInspectLinkException(string message, Exception inner) : base(message, inner) { }
}
