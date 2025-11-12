using System;
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.State;

namespace Kurisu.AspNetCore.State.Internal;

internal record TempState<T> where T : ICopyable<T>
{
    private static readonly PropertyInfo[] SPropertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    private readonly object[] _values;
    private readonly PropertyInfo[] _propertyInfos;

    public TempState(T state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));

        var clonedState = state.Copy();
        _propertyInfos = SPropertyInfos;
        _values = new object[_propertyInfos.Length];
        for (var i = 0; i < _propertyInfos.Length; i++)
        {
            _values[i] = _propertyInfos[i].GetValue(clonedState);
        }
    }

    public void RestoreTo(T s)
    {
        if (s == null) return;
        for (var i = 0; i < _propertyInfos.Length; i++)
        {
            var propertyInfo = _propertyInfos[i];
            if (!propertyInfo.CanWrite) continue;
            propertyInfo.SetValue(s, _values[i]);
        }
    }
}