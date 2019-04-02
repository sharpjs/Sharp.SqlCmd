using System;
using FluentAssertions;
using NUnit.Framework;

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

        [Test]
        public void EnableVariableReplacementInSetvar_Initial()
        {
            new SqlCmdPreprocessor().EnableVariableReplacementInSetvar.Should().BeFalse();
        }

        [Test]
        public void Process_Null()
        {
            new SqlCmdPreprocessor()
                .Invoking(p => p.Process(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Process_Empty()
        {
            new SqlCmdPreprocessor()
                .Process("")
                .Should().BeEmpty();
        }

        [Test]
        public void Process_Substring_SingleBatch()
        {
            new SqlCmdPreprocessor()
                .Process(BatchA)
                .Should().ContainSingle(BatchA);
        }

        [Test]
        public void Process_Substring_MultiBatch()
        {
            const string Sql
                = BatchA + BatchSeparator
                + BatchB + BatchSeparator
                + BatchC;

            new SqlCmdPreprocessor()
                .Process(Sql)
                .Should().Equal(BatchA, BatchB, BatchC);
        }

        [Test]
        public void Process_Substring_CommentedOutGo()
        {
            const string Sql
                = BatchA
                + "--" + BatchSeparator
                + BatchB;

            new SqlCmdPreprocessor()
                .Process(Sql)
                .Should().Equal(Sql);
        }

        private const string
            Eol            = "\r\n",
            BatchA         = "BATCH A" + Eol,
            BatchB         = "BATCH B" + Eol,
            BatchC         = "BATCH C" + Eol,
            BatchSeparator = "GO"      + Eol;
    }
}
