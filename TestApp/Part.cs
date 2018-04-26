namespace TestApp
{
    class Part:PartInf
    {
        private byte[] filePart;
        public byte[] FilePart { get { return filePart; } }
        public Part(byte[] part)
        {
            filePart = part;
        }
    }
}
