using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Sharp.SqlCmd
{
    internal static class Assume
    {
        [Conditional("DEBUG")]
        [ExcludeFromCodeCoverage]
        internal static void That(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("An assumption has been violated.");
        }
    }
}
