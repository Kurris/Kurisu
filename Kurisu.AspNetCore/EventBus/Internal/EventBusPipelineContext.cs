using System;
using System.Collections.Generic;

namespace Kurisu.AspNetCore.EventBus.Internal;

public static class EventBusPipelineContext
{
    private static List<Type> _types = new List<Type>();

    public static void Use(Type type)
    {
        _types.Add(type);
    }
}
