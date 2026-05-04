using Xunit;

namespace LogViewer.Tests
{
    // Tactical isolation for tests that touch BaseLoggerSink.Instance or
    // BaseLogger's static settings. xUnit runs classes in different collections
    // in parallel; classes sharing a collection run sequentially. The proper
    // fix is to remove the global statics from the production code (planned
    // for v0.3.0); this collection just keeps existing tests stable in the
    // meantime.
    [CollectionDefinition(Name)]
    public sealed class GlobalStateCollection
    {
        public const string Name = "GlobalState";
    }
}
