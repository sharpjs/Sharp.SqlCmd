using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using FluentAssertions;
using NUnit.Framework;

namespace Sharp.SqlCmd
{
    [TestFixture]
    public class SqlCmdExceptionTests
    {
        [Test]
        public void Construct_Default()
        {
            var e = new SqlCmdException();

            e.Message.Should().Be(SqlCmdException.DefaultMessage);
        }

        [Test]
        public void Construct_Message()
        {
            var e = new SqlCmdException("a");

            e.Message.Should().Be("a");
        }

        [Test]
        public void Construct_MessageAndInnerException()
        {
            var inner = new Exception();
            var e     = new SqlCmdException("a", inner);

            e.Message       .Should().Be("a");
            e.InnerException.Should().BeSameAs(inner);
        }

        [Test]
        public void Roundtrip()
        {
            var input  = new SqlCmdException("a", new Exception("b"));
            var output = null as SqlCmdException;

            using (var memory = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(memory, input);
                memory.Position = 0;
                output = (SqlCmdException) formatter.Deserialize(memory);
            }

            output.Should().BeEquivalentTo(input);
        }
    }
}
