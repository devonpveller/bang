using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Bang.Diagnostics
{
    /// <summary>
    /// Release-safe replacement for <c>Debug.Assert</c> on recoverable misuse.
    /// A failed check is reported (trace error + <see cref="OnViolation"/>) in every configuration,
    /// and the caller recovers instead of crashing or silently continuing in an invalid state.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Raised for every violated check, with a message naming the member that failed.
        /// Games can hook this to forward to their own logger or crash reporter.
        /// </summary>
        public static event Action<string>? OnViolation;

        /// <summary>
        /// Returns <paramref name="condition"/>. If it is false, reports <paramref name="message"/>.
        /// </summary>
        /// <param name="condition">The invariant that should hold.</param>
        /// <param name="message">What went wrong, for the log.</param>
        /// <param name="caller">Filled in by the compiler.</param>
        public static bool Check(bool condition, string message, [CallerMemberName] string? caller = null)
        {
            if (!condition)
            {
                string full = $"[Bang] {caller}: {message}";
                Trace.TraceError(full);
                OnViolation?.Invoke(full);
            }

            return condition;
        }
    }
}
