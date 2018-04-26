using System;
using System.Threading;
using System.IO;

namespace TestApp
{
    class MainThread
    {
        string sourceFile;
        string createdFile;
        Reader reader;
        Compressor compressor;
        Writer writer;
        Thread readthread;
        Thread writeThread;
        Thread[] compressThreads;

        public MainThread(string sourceFile, string createdFile)
        {
            this.sourceFile = sourceFile;
            this.createdFile = createdFile;
            compressor = new Compressor();
            reader = new Reader(sourceFile, compressor);
            writer = new Writer(createdFile, compressor);
            compressThreads = new Thread[Environment.ProcessorCount];
        }
         
        public int Compress()
        {
            readthread = new Thread(new ThreadStart(reader.ReadToCompress));
            readthread.Start();
            for (int i = 0; i < compressThreads.Length; i++)
            {
                compressThreads[i] = new Thread(new ThreadStart(compressor.Compress));
                compressThreads[i].Start();
            }
            writeThread = new Thread(new ThreadStart(writer.WriteToCompress));
            writeThread.Start();
            writeThread.Join();
            if (writer.Cancelled)
            {
                Cancel();
                return 1;
            }
            Console.WriteLine(0);
            return 0;
        }
        public int Decompress()
        {
            readthread = new Thread(new ThreadStart(reader.ReadToDecompress));
            readthread.Start();
            for (int i = 0; i < compressThreads.Length; i++)
            {
                compressThreads[i] = new Thread(new ThreadStart(compressor.Decompress));
                compressThreads[i].Start();
            }
            writeThread = new Thread(new ThreadStart(writer.WriteToDecompress));
            writeThread.Start();
            writeThread.Join();
            if (writer.Cancelled)
            {
                Cancel();
                return 1;
            }
            Console.WriteLine(0);
            return 0;
        }

        void Cancel()
        {
            readthread.Abort();
            for (int i = 0; i < compressThreads.Length; i++)
            {
                compressThreads[i].Abort();
            }
            File.Delete(createdFile);
            Console.WriteLine("Процесс прерван пользователем");
            Console.WriteLine(1);
            Console.ReadLine();
        }
    }
}
