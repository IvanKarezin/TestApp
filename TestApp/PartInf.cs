using System;

namespace TestApp
{
    [Serializable]
    class PartInf
    {
        public int ExistCount { get; set; }
        public int CompCount { get; set; }
        public long ExistPosition { get; set; }
        public long CompPosition { get; set; }
    }
}
