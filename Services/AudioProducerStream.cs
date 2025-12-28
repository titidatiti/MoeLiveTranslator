using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace LiveTranslator.Services
{
    public class AudioProducerStream : Stream
    {
        private readonly BlockingCollection<byte[]> _dataQueue = new BlockingCollection<byte[]>();
        private byte[] _currentChunk;
        private int _currentChunkOffset;
        private long _position;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        // Return a large number to simulate infinite stream. 
        // If we return _position, SRE thinks we are at EOF if Position == Length.
        public override long Length => long.MaxValue / 2;
        public override long Position { get => _position; set { /* No-op */ } }

        public override void Flush() { }

        private int _readCount = 0;

        public override int Read(byte[] buffer, int offset, int count)
        {
            _readCount++;
            if (_readCount < 5 || _readCount % 50 == 0) Console.WriteLine($"[Stream] Read x{_readCount} Req={count}");
            if (count == 0) return 0;

            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                if (_currentChunk == null || _currentChunkOffset >= _currentChunk.Length)
                {
                    // If we have already read some bytes, check if more is immediately available.
                    // If not, returns to avoid blocking.
                    if (totalBytesRead > 0 && _dataQueue.Count == 0 && !_dataQueue.IsCompleted)
                    {
                        return totalBytesRead;
                    }

                    if (_dataQueue.IsCompleted && _dataQueue.Count == 0)
                    {
                        return totalBytesRead; // EOF
                    }

                    try
                    {
                        // If we haven't read anything yet, block until we get data.
                        if (totalBytesRead == 0)
                        {
                            _currentChunk = _dataQueue.Take();
                        }
                        else
                        {
                            // If we have data, try to get more but don't block
                            if (!_dataQueue.TryTake(out _currentChunk))
                            {
                                return totalBytesRead;
                            }
                        }
                        _currentChunkOffset = 0;
                    }
                    catch (InvalidOperationException)
                    {
                        return totalBytesRead;
                    }
                }

                int available = _currentChunk.Length - _currentChunkOffset;
                int toCopy = Math.Min(available, count - totalBytesRead);
                Buffer.BlockCopy(_currentChunk, _currentChunkOffset, buffer, offset + totalBytesRead, toCopy);

                _currentChunkOffset += toCopy;
                totalBytesRead += toCopy;
                _position += toCopy;
            }
            return totalBytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > 0)
            {
                var chunk = new byte[count];
                Buffer.BlockCopy(buffer, offset, chunk, 0, count);
                _dataQueue.Add(chunk);
            }
        }

        public void Write(byte[] buffer, int bytesRecorded)
        {
            if (bytesRecorded > 0)
            {
                var chunk = new byte[bytesRecorded];
                Buffer.BlockCopy(buffer, 0, chunk, 0, bytesRecorded);
                _dataQueue.Add(chunk);
            }
        }

        public void Complete()
        {
            _dataQueue.CompleteAdding();
        }

        public override long Seek(long offset, SeekOrigin origin) => 0; // Fake seek
        public override void SetLength(long value) { /* No-op */ }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _dataQueue.Dispose();
            }
        }
    }
}
