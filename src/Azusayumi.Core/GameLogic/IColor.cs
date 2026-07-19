using System.Runtime.CompilerServices;

namespace Azusayumi.Core.GameLogic
{
    internal interface IColor
    {
        internal static abstract bool IsWhite { get; }
    }

    internal readonly struct White : IColor
    {
        public static bool IsWhite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => true;
        }
    }

    internal readonly struct Black : IColor
    {
        public static bool IsWhite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => false;
        }
    }
}
