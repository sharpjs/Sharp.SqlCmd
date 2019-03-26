using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using FluentAssertions;

namespace Sharp.SqlCmd
{
    [TestFixture]
    public class SqlCmdPreprocessorTests
    {
        [Test]
        public void Variables_Initial()
        {
            new SqlCmdPreprocessor().Variables.Should().BeEmpty();
        }
    }
}
