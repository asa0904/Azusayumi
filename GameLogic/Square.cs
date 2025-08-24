namespace Azusayumi.GameLogic
{
    internal static class Square
    {
        public const byte A1 = 00, B1 = 01, C1 = 02, D1 = 03, E1 = 04, F1 = 05, G1 = 06, H1 = 07;
        public const byte A2 = 08, B2 = 09, C2 = 10, D2 = 11, E2 = 12, F2 = 13, G2 = 14, H2 = 15;
        public const byte A3 = 16, B3 = 17, C3 = 18, D3 = 19, E3 = 20, F3 = 21, G3 = 22, H3 = 23;
        public const byte A4 = 24, B4 = 25, C4 = 26, D4 = 27, E4 = 28, F4 = 29, G4 = 30, H4 = 31;
        public const byte A5 = 32, B5 = 33, C5 = 34, D5 = 35, E5 = 36, F5 = 37, G5 = 38, H5 = 39;
        public const byte A6 = 40, B6 = 41, C6 = 42, D6 = 43, E6 = 44, F6 = 45, G6 = 46, H6 = 47;
        public const byte A7 = 48, B7 = 49, C7 = 50, D7 = 51, E7 = 52, F7 = 53, G7 = 54, H7 = 55;
        public const byte A8 = 56, B8 = 57, C8 = 58, D8 = 59, E8 = 60, F8 = 61, G8 = 62, H8 = 63;
        public const byte None = 64;

        public static string ToString(byte squareIndex)
        {
            string file = (squareIndex & 7) switch
            {
                0 => "a",
                1 => "b",
                2 => "c",
                3 => "d",
                4 => "e",
                5 => "f",
                6 => "g",
                7 => "h",
                _ => ""
            };
            string rank = Convert.ToString((squareIndex >> 3) + 1);

            return file + rank;
        }

        public static byte ToByte(string coords)
        {
            int file = coords[0] switch
            {
                'a' => 0,
                'b' => 1,
                'c' => 2,
                'd' => 3,
                'e' => 4,
                'f' => 5,
                'g' => 6,
                'h' => 7,
                _ => throw new FormatException()
            };

            int rank = coords[1] - '1';

            return (byte)(file + (8 * rank));
        }
    }
}
