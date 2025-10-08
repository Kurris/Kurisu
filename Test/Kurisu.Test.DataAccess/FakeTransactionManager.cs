using System.Threading;
using Kurisu.AspNetCore.Abstractions.DataAccess;

namespace Kurisu.Test.DataAccess;

// This file previously contained a FakeTransactionManager used for unit tests.
// The tests have been switched to use the real SqlSugarDatasourceManager via DI,
// so the fake implementation is intentionally removed to avoid confusion.
// If you need a fake implementation for isolated unit testing in the future,
// reintroduce it here or place it under a TestHelpers folder.
