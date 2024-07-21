﻿namespace NDecrypt.N3DS
{
    internal class Constants
    {
        // Setup Keys and IVs
        public static byte[] PlainCounter = [0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        public static byte[] ExefsCounter = [0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        public static byte[] RomfsCounter = [0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        public const int CXTExtendedDataHeaderLength = 0x800;
    }
}
