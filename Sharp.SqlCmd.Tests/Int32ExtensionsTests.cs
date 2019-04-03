using FluentAssertions;
using NUnit.Framework;

namespace Sharp.SqlCmd
{
    [TestFixture]
    public class Int32ExtensionsTests
    {
        [Test]
        // value in [min, 0)   => undefined
        // value in [0]        => 0
        [TestCase(0, 0)]
        // value in [1, 2**31] => next power of 2
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 4)]
        [TestCase(4, 4)]
        [TestCase(5, 8)]
        [TestCase(6, 8)]
        [TestCase(7, 8)]
        [TestCase(8, 8)]
        [TestCase(8, 8)]
        [TestCase(0x3FFF_FFFF, 0x4000_0000)]
        [TestCase(0x4000_0000, 0x4000_0000)]
        // value in (2**31, max] => max
        [TestCase(0x4000_0001,      int.MaxValue)]
        [TestCase(int.MaxValue - 1, int.MaxValue)]
        [TestCase(int.MaxValue,     int.MaxValue)]
        public void GetNextPowerOf2Saturating(int input, int output)
        {
            input.GetNextPowerOf2Saturating().Should().Be(output);
        }
    }
}
