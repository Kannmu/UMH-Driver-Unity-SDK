using System;

namespace UMH
{
    public class UMH_RingBuffer
    {
        private readonly byte[] _buffer;
        private readonly int _capacity;
        private int _readIndex;
        private int _writeIndex;
        private int _count;
        private readonly object _lock = new object();

        public int Count
        {
            get { lock (_lock) return _count; }
        }

        public UMH_RingBuffer(int capacity = 8192)
        {
            _capacity = capacity;
            _buffer = new byte[capacity];
            _readIndex = 0;
            _writeIndex = 0;
            _count = 0;
        }

        public void Write(byte[] data, int length)
        {
            lock (_lock)
            {
                if (length > _capacity - _count)
                {
                    // Buffer overflow strategy: Discard old data or Reject?
                    // For serial comms, discarding old data might be safer to prevent deadlock, 
                    // but rejecting ensures integrity. 
                    // Let's go with "Clear if full" or just "Reject" for now, but given the use case,
                    // we might want to just wrap around if we implemented a true circular overwrite, 
                    // but standard is usually to stop writing.
                    // Simplified approach: Clear buffer if we run out of space to recover from bad states.
                    Clear();
                }

                if (length > _capacity) return; // Data too big for buffer

                int firstChunk = Math.Min(length, _capacity - _writeIndex);
                Array.Copy(data, 0, _buffer, _writeIndex, firstChunk);
                _writeIndex = (_writeIndex + firstChunk) % _capacity;

                int secondChunk = length - firstChunk;
                if (secondChunk > 0)
                {
                    Array.Copy(data, firstChunk, _buffer, _writeIndex, secondChunk);
                    _writeIndex = (_writeIndex + secondChunk) % _capacity;
                }

                _count += length;
            }
        }

        public int Peek(byte[] destination, int count)
        {
            lock (_lock)
            {
                if (count > _count) count = _count;
                if (count == 0) return 0;

                int currentRead = _readIndex;
                int firstChunk = Math.Min(count, _capacity - currentRead);
                Array.Copy(_buffer, currentRead, destination, 0, firstChunk);

                int secondChunk = count - firstChunk;
                if (secondChunk > 0)
                {
                    Array.Copy(_buffer, 0, destination, firstChunk, secondChunk);
                }

                return count;
            }
        }
        
        public byte PeekByte(int offset)
        {
             lock (_lock)
            {
                if (offset >= _count) return 0;
                int index = (_readIndex + offset) % _capacity;
                return _buffer[index];
            }
        }

        public void Read(byte[] destination, int count)
        {
            lock (_lock)
            {
                if (count > _count) count = _count;
                if (count == 0) return;

                int firstChunk = Math.Min(count, _capacity - _readIndex);
                Array.Copy(_buffer, _readIndex, destination, 0, firstChunk);
                _readIndex = (_readIndex + firstChunk) % _capacity;

                int secondChunk = count - firstChunk;
                if (secondChunk > 0)
                {
                    Array.Copy(_buffer, 0, destination, firstChunk, secondChunk);
                    _readIndex = (_readIndex + secondChunk) % _capacity;
                }

                _count -= count;
            }
        }

        public void Skip(int count)
        {
            lock (_lock)
            {
                if (count > _count) count = _count;
                _readIndex = (_readIndex + count) % _capacity;
                _count -= count;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _readIndex = 0;
                _writeIndex = 0;
                _count = 0;
            }
        }
    }
}
