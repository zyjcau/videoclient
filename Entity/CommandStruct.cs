using System;
using System.Runtime.InteropServices;

namespace VideoClient.Entity
{
    public struct CommandStruct
    {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }
}