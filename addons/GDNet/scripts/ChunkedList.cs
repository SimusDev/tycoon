using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GDNetUtils
{
    public class ChunkedList<T> : IEnumerable<T>
    {
        private List<T[]> _chunks = new();
        private readonly int _chunkSize;
        private int _count;

        public ChunkedList(int chunkSize = 256)
        {
            if (chunkSize < 1)
                throw new ArgumentException("Chunk size must be at least 1", nameof(chunkSize));
            _chunkSize = chunkSize;
        }

        public void Add(T item)
        {
            int chunkIndex = _count / _chunkSize;
            int indexInChunk = _count % _chunkSize;

            if (indexInChunk == 0)
            {
                _chunks.Add(new T[_chunkSize]);
            }

            _chunks[chunkIndex][indexInChunk] = item;
            _count++;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }

        public int Count => _count;
        public int ChunkCount => _chunks.Count;
        public int ChunkSize => _chunkSize;

        public T[] GetChunk(int index)
        {
            if (index < 0 || index >= _chunks.Count)
                throw new IndexOutOfRangeException($"Chunk index {index} out of range");
            return _chunks[index];
        }

        public List<T[]> GetAllChunks()
        {
            return _chunks;
        }

        private object _lock = new();
        public List<T[]> TakeOwnership()
        {
            var chunks = _chunks;
            _chunks = new List<T[]>();
            _count = 0;
            return chunks;
        }

        public void ProcessInParallelAndClear(Action<T[]> processChunk)
        {
            var chunks = TakeOwnership();
            if (chunks.Count == 0) return;
            Parallel.ForEach(chunks, processChunk);
        }

        public void ProcessInParallelAndClear(Action<int, T[]> processChunk)
        {
            var chunks = TakeOwnership();
            if (chunks.Count == 0) return;
            Parallel.For(0, chunks.Count, (i) =>
            {
                processChunk(i, chunks[i]);
            });
        }

        public void ProcessElementsInParallelAndClear(Action<T> processElement)
        {
            var chunks = TakeOwnership();
            if (chunks.Count == 0) return;
            Parallel.ForEach(chunks, (chunk) =>
            {
                for (int i = 0; i < chunk.Length; i++)
                {
                    processElement(chunk[i]);
                }
            });
        }

        public void Clear()
        {
            _chunks.Clear();
            _count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var chunk in _chunks)
            {
                for (int i = 0; i < chunk.Length; i++)
                {
                    yield return chunk[i];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}