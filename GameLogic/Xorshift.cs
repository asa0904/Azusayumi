namespace Azusayumi.GameLogic
{
    internal static class Xorshift
    {
        private static ulong Seed = 2006_09_04;

        public static ulong GetRandom()
        {
            ulong x = Seed;

            x ^= x << 13;
            x ^= x >> 7;
            x ^= x << 17;

            Seed = x;

            return x;
        }
    }
}