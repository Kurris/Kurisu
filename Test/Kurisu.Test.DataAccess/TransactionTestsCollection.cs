using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Kurisu.Test.DataAccess;

// This file disables xUnit parallelization for the test assembly to avoid
// concurrent access to the shared SQLite file used by these tests.
internal static class TransactionTestsCollection
{
}

