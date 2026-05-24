using System;
using System.Linq.Expressions;
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
        private static readonly Func<T, object>[] _getters;
        private static readonly Action<T, object>[] _setters;

        private readonly object[] _values;

        static TempState()
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            _getters = new Func<T, object>[properties.Length];
            _setters = new Action<T, object>[properties.Length];

            for (var i = 0; i < properties.Length; i++)
            {
                _getters[i] = CompileGetter(properties[i]);
                _setters[i] = CompileSetter(properties[i]);
            }
        }

        public TempState(T state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var clonedState = state.CopyState();

            _values = new object[_getters.Length];
            for (var i = 0; i < _getters.Length; i++)
            {
                _values[i] = _getters[i](clonedState);
            }
        }

        public void RestoreTo(T state)
        {
            if (state == null) return;
            for (var i = 0; i < _setters.Length; i++)
            {
                _setters[i](state, _values[i]);
            }
        }

        private static Func<T, object> CompileGetter(PropertyInfo propertyInfo)
        {
            var param = Expression.Parameter(typeof(T), "source");
            var property = Expression.Property(param, propertyInfo);
            var convert = Expression.Convert(property, typeof(object));
            return Expression.Lambda<Func<T, object>>(convert, param).Compile();
        }

        private static Action<T, object> CompileSetter(PropertyInfo propertyInfo)
        {
            if (!propertyInfo.CanWrite)
                return (_, _) => { };

            var target = Expression.Parameter(typeof(T), "target");
            var value = Expression.Parameter(typeof(object), "value");
            var property = Expression.Property(target, propertyInfo);
            var convert = Expression.Convert(value, propertyInfo.PropertyType);
            var assign = Expression.Assign(property, convert);
            return Expression.Lambda<Action<T, object>>(assign, target, value).Compile();
        }
    }
}