using Boltzenberg.Functions.Logging;

namespace ApiTests.Fakes
{
    /// <summary>
    /// A LogBuffer that discards all output and never calls Telegram.
    /// Constructed via reflection workaround since LogBuffer has no no-op ctor exposed.
    /// We simply create a real LogBuffer with writeOnClose=false and never close it.
    /// </summary>
    public static class NoOpLogBuffer
    {
        public static LogBuffer Create() => new LogBuffer("test", false);
    }
}
