using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

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
                Report(caller, message);
            }

            return condition;
        }

        /// <summary>
        /// Same as <see cref="Check(bool, string, string?)"/>, but an interpolated message is only
        /// formatted when the check fails, so the passing path never allocates.
        /// </summary>
        public static bool Check(
            bool condition,
            [InterpolatedStringHandlerArgument("condition")] ref GuardMessageHandler message,
            [CallerMemberName] string? caller = null)
        {
            if (!condition)
            {
                Report(caller, message.ToStringAndClear());
            }

            return condition;
        }

        /// <summary>
        /// Reports a violated invariant unconditionally. Prefer this over <see cref="Check(bool, string, string?)"/>
        /// when the caller needs its own null test, so the compiler sees the null check and narrows the value.
        /// </summary>
        public static void Fail(string message, [CallerMemberName] string? caller = null) => Report(caller, message);

        private static void Report(string? caller, string message)
        {
            string full = $"[Bang] {caller}: {message}";
            Trace.TraceError(full);
            OnViolation?.Invoke(full);
        }
    }

    /// <summary>
    /// Interpolated string handler that does no work unless the guarded condition failed.
    /// </summary>
    [InterpolatedStringHandler]
    public ref struct GuardMessageHandler
    {
        private readonly StringBuilder? _builder;

        public GuardMessageHandler(int literalLength, int formattedCount, bool condition, out bool shouldAppend)
        {
            shouldAppend = !condition;
            _builder = shouldAppend ? new StringBuilder(literalLength) : null;
        }

        public void AppendLiteral(string value) => _builder!.Append(value);

        public void AppendFormatted<T>(T value) => _builder!.Append(value);

        internal string ToStringAndClear() => _builder?.ToString() ?? string.Empty;
    }
}
