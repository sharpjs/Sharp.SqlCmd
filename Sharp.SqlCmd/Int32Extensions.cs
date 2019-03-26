namespace Sharp.SqlCmd
{
    internal static class Int32Extensions
    {
        internal static int GetNextPowerOf2Saturating(this int value)
        {
            // https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
            // but saturating at int.MaxValue instead of overflow

            value--;
            value |= value >>  1;
            value |= value >>  2;
            value |= value >>  4;
            value |= value >>  8;
            value |= value >> 16;

            return value == int.MaxValue
                ? value         // edge case: avoid overflow
                : value + 1;    // normal
        }
    }
}
