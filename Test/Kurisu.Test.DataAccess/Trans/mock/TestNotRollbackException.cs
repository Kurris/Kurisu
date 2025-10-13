using System;

namespace Kurisu.Test.DataAccess.Trans.Mock;

public class TestNotRollbackException : Exception
{
    public TestNotRollbackException(string message) : base(message)
    {
    }
}

