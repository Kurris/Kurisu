namespace Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync.EFCore2x;

#if EFCORE2
    internal class EFCORE2TryCurrentAsyncEnumerator<T>:IAsyncEnumerator<T>
    {
        private readonly IAsyncEnumerator<T> _asyncEnumerator;
        private bool currentMoved=false;

        public EFCORE2TryCurrentAsyncEnumerator(IAsyncEnumerator<T> asyncEnumerator)
        {
            _asyncEnumerator = asyncEnumerator;
        }
        public void Dispose()
        {
            _asyncEnumerator?.Dispose();
        }

        public  async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            var moveNext = await _asyncEnumerator.MoveNext(cancellationToken);
            currentMoved = moveNext;
            return moveNext;
        }

        public T Current => currentMoved ? _asyncEnumerator.Current : default;
    }
#endif