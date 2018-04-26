using System;
using System.IO;

namespace TestApp
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                string sourceFile = args[1];
                string createdFile = args[2];
                FileInfo file = new FileInfo(sourceFile);
                if (!file.Exists)
                    throw new FileNotFoundException();
                MainThread mainThread = new MainThread(sourceFile, createdFile);
                if (args[0] == "Compress" || args[0] == "compress")
                {
                    return mainThread.Compress();
                }
                else
                {
                    if (args[0] == "Decompress" || args[0] == "decompress")
                    {
                        return mainThread.Decompress();
                    }
                    else
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Приложение имеет три параметра: [режим] [имя исходного файла] [имя архива]");
                Console.ReadLine();
                return 1;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                return 1;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                return 1;
            }
        }
    }
}
