using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace TestApp
{
    class Reader
    {
        private string source;
        private int chekFile = "Compressed_File".GetHashCode();
        private Compressor compressor;

        public Reader(string SourceFile, Compressor compressor)
        {
            source = SourceFile;
            this.compressor = compressor;
        }

        public void ReadToCompress()
        {
            int pieceSize = 1048576;
            long counter;
            using (FileStream readStream = File.OpenRead(source))
            {
                for (counter = 0; counter <= (readStream.Length - pieceSize); 
                    counter += pieceSize)
                {
                    compressor.AddToReadQueue(CreatePartOfExistFile(readStream, pieceSize));
                }
                if (counter < readStream.Length)
                {
                    pieceSize = (int)(readStream.Length - counter);
                    compressor.AddToReadQueue(CreatePartOfExistFile(readStream, pieceSize));
                }
                compressor.EndSource = true;
            }
        }

        public void ReadToDecompress()
        {
            using (FileStream inputStream = File.OpenRead(source))
            {
                if (!FileChecking(inputStream))
                    System.Environment.Exit(1);
                PartInf[] metodata = ReadMetаdata(inputStream);
                foreach (PartInf partInf in metodata)
                {
                    byte[] temp = new byte[partInf.CompCount];
                    inputStream.Seek(partInf.CompPosition, SeekOrigin.Begin);
                    inputStream.Read(temp, 0, temp.Length);
                    Part part = new Part(temp)
                    {
                        CompCount = partInf.CompCount,
                        CompPosition = partInf.CompPosition,
                        ExistCount = partInf.ExistCount,
                        ExistPosition = partInf.ExistPosition
                    };
                    compressor.AddToReadQueue(part);
                }
                compressor.EndSource = true;
            }
        }

        private Part CreatePartOfExistFile(FileStream stream, int pieceSize)
        {
            byte[] temp = new byte[pieceSize];
            stream.Read(temp, 0, pieceSize);
            Part part = new Part(temp)
            {
                ExistCount = pieceSize,
                ExistPosition = stream.Position - pieceSize
            };
            return part;
        }

        private bool FileChecking(FileStream inputStream)
        {
            try
            {
                byte[] mark = new byte[4];
                inputStream.Seek(-4, SeekOrigin.End);
                inputStream.Read(mark, 0, mark.Length);
                int check = BitConverter.ToInt32(mark, 0);
                if (chekFile != check)
                    throw new Exception("Исходный файл не был сжат в этой программе");
                else
                    return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                return false;
            }
        }

        private PartInf[] ReadMetаdata(FileStream stream)
        {
            byte[] sizeOfMetadata = new byte[4];
            stream.Seek(-8, SeekOrigin.End);
            stream.Read(sizeOfMetadata, 0, sizeOfMetadata.Length);
            int size = BitConverter.ToInt32(sizeOfMetadata, 0);
            stream.Seek(-(8+size), SeekOrigin.End);
            byte[] metodata = new byte[size];
            stream.Read(metodata, 0, size);
            byte[] data = compressor.DecompressMetadata(metodata);
            BinaryFormatter binary = new BinaryFormatter();
            PartInf[] parts;
            using (MemoryStream memory = new MemoryStream(data))
            {
                parts = (PartInf[])binary.Deserialize(memory);
            }
                return parts;
        }
    }
}
