
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.EventBus.Abstractions;
using Kurisu.Extensions.SqlSugar.Utils;

namespace Kurisu.Extensions.EventBus.Defaults;

public class DefaultEventBusLocalMessageHandler(
    IDbContext db,
    IEventBusSerializer serializer,
    IEventBusUniqueCodeGenerator codeGenerator)
    : IEventBusLocalMessageHandler
{
    public async Task<string> PersistAsync<TMessage>(TMessage message) where TMessage : EventMessage
    {
        var content = serializer.Serialize(message);

        var code = codeGenerator.GenerateUniqueCode();
        var localMessage = new LocalMessage()
        {
            Code = code,
            Content = content,
            Processed = false,
            Retry = 0
        };

        await db.InsertAsync(localMessage);
        message.Code = code;

        return code;
    }

    public async Task<ILocalMessageTracker> BeginTrackingAsync(string code, CancellationToken cancellationToken = default)
    {
        var localMessage = await db.Queryable<LocalMessage>().SingleAsync(x => x.Code == code);
        localMessage.Retry += 1;
        // 指数退避：下次重试时间 = 当前时间 + 2^Retry 分钟
        localMessage.NextRetryTime = DateTime.Now.AddMinutes(Math.Pow(2, localMessage.Retry));
        return new LocalMessageTracker(db, localMessage, cancellationToken);
    }

    private sealed class LocalMessageTracker(IDbContext db, LocalMessage localMessage, CancellationToken cancellationToken)
        : ILocalMessageTracker
    {
        private string _error;
        private bool _completed;

        public void Complete() => _completed = true;

        public void Fail(string error) => _error = error;

        public async ValueTask DisposeAsync()
        {
            if (!string.IsNullOrEmpty(_error))
            {
                localMessage.Result = _error;
                // 失败时保留 NextRetryTime 供定时任务重试
            }
            else if (_completed)
            {
                localMessage.Processed = true;
                localMessage.NextRetryTime = null;
            }

            await db.UpdateAsync(localMessage, cancellationToken);
        }
    }
}
