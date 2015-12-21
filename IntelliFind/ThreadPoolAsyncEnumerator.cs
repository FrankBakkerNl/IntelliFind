using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IntelliFind
{
    /// <summary>
    /// Enumerator that returns Items async by delegating MoveNext to the thread pool
    /// </summary>
    class ThreadPoolAsyncEnumerator<T> : IDisposable
    {
        readonly IEnumerator<T> _inner;

        public ThreadPoolAsyncEnumerator(IEnumerable<T> enumerable)
        {
            _inner = enumerable.GetEnumerator();
        }

        public ThreadPoolAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        /// <summary>
        ///  Advances the enumerator to the next element of the collection on a Thread Pool Thread
        /// </summary>
        public Task<bool> MoveNextAsync() => Task.Run(() => _inner.MoveNext());

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        public T Current => _inner.Current;

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}