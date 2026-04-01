using System;
using System.Reflection;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Utils.Disposables;
using Kurisu.Extensions.ContextAccessor.Abstractions;

namespace Kurisu.Extensions.ContextAccessor.Internal;

/// <summary>
/// 数据上下文快照管理器
/// </summary>
/// <typeparam name="TContext"></typeparam>
internal class ContextSnapshotManager<TContext> : IContextSnapshotManager<TContext> where TContext : class, IContextable<TContext>, new()
{
    /// <summary>
    /// 上下文快照管理器
    /// </summary>
    /// <param name="contextAccessor"></param>
    public ContextSnapshotManager(IContextAccessor<TContext> contextAccessor)
    {
        ContextAccessor = contextAccessor;
    }

    public IContextAccessor<TContext> ContextAccessor { get; }

    /// <summary>
    /// 上下文快照作用域
    /// </summary>
    /// <param name="setter"></param>
    /// <param name="onDispose"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IDisposable CreateScope(Action<TContext> setter, Action onDispose)
    {
        if (setter == null) throw new ArgumentNullException(nameof(setter) + "不能为null");

        var state = ContextAccessor.Current;
        //创建快照
        var snapshot = new TempState<TContext>(state);

        //设置新上下文
        setter(state);

        return new ActionScope(() =>
        {
            //恢复快照
            snapshot.RestoreTo(state);
            onDispose?.Invoke();
        });
    }

    /// <summary>
    /// 上下文快照作用域
    /// </summary>
    /// <param name="setState"></param>
    /// <param name="onAfterDispose"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IAsyncDisposable CreateScopeAsync(Action<TContext> setter, Func<Task> onAfterDispose)
    {
        if (setter == null) throw new ArgumentNullException(nameof(setter));

        var context = ContextAccessor.Current;
        var snapshot = new TempState<TContext>(context);

        setter(context);

        return new AsyncActionScope(async () =>
        {
            snapshot.RestoreTo(context);
            await onAfterDispose?.Invoke();
        });
    }


    //***************************************************************************************************************************************************


    internal record TempState<T> where T : IContextable<T>, new()
    {
        private static readonly PropertyInfo[] SPropertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        private readonly object[] _values;
        private readonly static PropertyInfo[] _propertyInfos;

        static TempState()
        {
            _propertyInfos = SPropertyInfos;
            //预热反射
            foreach (var propertyInfo in SPropertyInfos)
            {
                _ = propertyInfo.GetMethod;
                _ = propertyInfo.SetMethod;
            }
        }

        public TempState(T state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var clonedState = state.CopyState();

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
}