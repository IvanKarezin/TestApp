using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace TestApp
{
    class Writer
    {
        private string createdFile;
        private int chekFile = "Compressed_File".GetHashCode();
        private Compressor compressor;
        private List<PartInf> metadata;
        public bool Cancelled { get; private set; }

        public Writer(string createdFile, Compressor compressor)
        {
            this.createdFile = createdFile;
            this.compressor = compressor;
            metadata = new List<PartInf>();
            Cancelled = false;
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Cancelled = true;
        }

        private void WriteMarkToFile(FileStream stream)
        {
            byte[] mark = BitConverter.GetBytes(chekFile);
            stream.Write(mark, 0, mark.Length);
        }

        private void WriteToMetadata(FileStream stream)
        {
            PartInf[] data = metadata.ToArray();
            BinaryFormatter binary = new BinaryFormatter();
            byte[] compressMetadata; 
            //сериализуем и сжимаем метаданные
            using (MemoryStream memory = new MemoryStream())
            {
                binary.Serialize(memory, data);
                compressMetadata = compressor.CompressMetadata(memory.ToArray());
            }
            int size = compressMetadata.Length;
            stream.Write(compressMetadata, 0, size);
            byte[] sizeOfMetadata = BitConverter.GetBytes(size);
            stream.Write(sizeOfMetadata, 0, sizeOfMetadata.Length);
        }
  
        public void WriteToCompress()
        {
            using (FileStream createStream = File.Create(createdFile))
            {
                Part part = compressor.TakeOutWriteQueue();
                while (part != null && !Cancelled)
                {
                    using (MemoryStream targetStream = new MemoryStream(part.FilePart))
                    {
                        PartInf partInf = new PartInf()
                        {
                            ExistCount = part.ExistCount,
                            ExistPosition = part.ExistPosition,
                            CompCount = part.CompCount,
                            CompPosition = createStream.Position
                        };
                        metadata.Add(partInf);
                        targetStream.CopyTo(createStream);
                    }
                    part = compressor.TakeOutWriteQueue();
                }
                WriteToMetadata(createStream);
                WriteMarkToFile(createStream);
            }
        }

        public void WriteToDecompress()
        {
            using (FileStream createStream = File.Create(createdFile))
            {
                Part part = compressor.TakeOutWriteQueue();
                while (part != null & !Cancelled)
                {
                    using (MemoryStream memory = new MemoryStream(part.FilePart))
                    {
                        createStream.Seek(part.ExistPosition, SeekOrigin.Begin);
                        memory.CopyTo(createStream);
                    }
                    GC.Collect(); 
                    part = compressor.TakeOutWriteQueue();
                }
            }
        }
    }
}
