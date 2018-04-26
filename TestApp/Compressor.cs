using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Threading;

namespace TestApp
{
    class Compressor
    {
        private Queue<Part> readQueue;
        private Queue<Part> writeQueue;
        private int readQueueCounter;
        private int writeQueueCounter; 
        private object compressedLock;

        public int SizeOfReadQueue { get { return readQueue.Count; } }
        public int SizeOfWtiteQueue { get { return writeQueue.Count; } }
        public bool EndSource { get; set; }

        public Compressor()
        {
            readQueue = new Queue<Part>();
            writeQueue = new Queue<Part>();
            readQueueCounter = 0;
            writeQueueCounter = 0;
            compressedLock = new object();
            EndSource = false;
        }

        private Part TakeOutReadQueue()
        {
            Part part;
            lock (compressedLock)
            {
                lock (readQueue)
                {
                    if (readQueue.Count == 0)
                    {
                        if (!EndSource)
                            Monitor.Wait(readQueue);
                        else
                            return null;
                    }
                    part = readQueue.Dequeue();
                    Monitor.Pulse(readQueue);
                }
            }
            return part;
        }
        private void AddToWriteQueue(Part part)
        {
            lock (compressedLock)
            {
                lock (writeQueue)
                {
                    if (writeQueue.Count > 7)
                        Monitor.Wait(writeQueue);
                    writeQueue.Enqueue(part);
                    writeQueueCounter++;
                    Monitor.Pulse(writeQueue);
                }
            }
        }

        public void AddToReadQueue(Part part)
        {
            lock (readQueue)
            {
                if (readQueue.Count > 7)
                    Monitor.Wait(readQueue);
                readQueue.Enqueue(part);
                readQueueCounter++;
                Monitor.Pulse(readQueue);
            }
        }

        public Part TakeOutWriteQueue()
        {
            Part part;
            lock (writeQueue)
            {
                if (writeQueue.Count == 0)
                {
                    if (!EndSource)
                        Monitor.Wait(writeQueue);
                    else
                    {
                        if (readQueueCounter == writeQueueCounter)
                            return null;
                        else
                            Monitor.Wait(writeQueue);
                    }
                }
                part = writeQueue.Dequeue();
                Monitor.Pulse(writeQueue);
            }
            return part;
        }

        public void Compress()
        {
            Part part = TakeOutReadQueue();
            Part compressPart;
            while (part != null)
            {
                using (MemoryStream targetStream = new MemoryStream())
                {
                    using (GZipStream compressedStream = new GZipStream(targetStream, CompressionMode.Compress))
                    using (BinaryWriter writer = new BinaryWriter(compressedStream))
                        writer.Write(part.FilePart, 0, part.FilePart.Length);
                    byte[] toWrite = targetStream.ToArray();
                    compressPart = new Part(toWrite)
                    {
                        ExistCount = part.ExistCount,
                        ExistPosition = part.ExistPosition, 
                        CompCount = toWrite.Length
                    };
                    AddToWriteQueue(compressPart);
                    part = TakeOutReadQueue();
                }
                GC.Collect();
            }
        }

        public void Decompress()
        {
            Part part = TakeOutReadQueue();
            Part decompPart;
            while (part != null)
            {
                byte[] squad = new byte[part.ExistCount];
                using (MemoryStream memory = new MemoryStream())
                using (GZipStream decompStream = new GZipStream(memory, CompressionMode.Decompress))
                {
                    memory.Write(part.FilePart, 0, part.FilePart.Length);
                    memory.Seek(0, SeekOrigin.Begin);
                    decompStream.Read(squad, 0, squad.Length);
                    decompPart = new Part(squad)
                    {
                        ExistCount = part.ExistCount,
                        ExistPosition = part.ExistPosition,
                        CompCount = part.CompCount,
                        CompPosition = part.CompPosition
                    };
                    AddToWriteQueue(decompPart);
                }
                GC.Collect();
                part = TakeOutReadQueue();
            }
        }
        public byte[] CompressMetadata(byte[] metadata)
        {
            using (MemoryStream target = new MemoryStream())
            {
                using (GZipStream compressedStream = new GZipStream(target, CompressionMode.Compress))
                using (BinaryWriter writer = new BinaryWriter(compressedStream))
                    writer.Write(metadata, 0, metadata.Length);
                return target.ToArray();
            }
        }

        public byte[] DecompressMetadata(byte[] data)
        {
            using (MemoryStream memory = new MemoryStream(data))
            using (GZipStream decompressor = new GZipStream(memory, CompressionMode.Decompress))
            using (MemoryStream outputStream = new MemoryStream())
            {
                decompressor.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }
    }
}
