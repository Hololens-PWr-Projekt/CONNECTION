using System;

namespace Server.Src.Utils
{
    public static class CompactGenerator
    {
        public static string GeneratePacketId()
        {
            byte[] buffer = new byte[4]; // 32-bit random number
            using var random = System.Security.Cryptography.RandomNumberGenerator.Create();
            random.GetBytes(buffer);
            return BitConverter.ToString(buffer).Replace("-", "");
        }
    }
}
