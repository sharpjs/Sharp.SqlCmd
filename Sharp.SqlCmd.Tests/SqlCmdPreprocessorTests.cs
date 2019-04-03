using System;
using System.Linq;
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
        public void Process_Substring_QuotedString()
        {
            const string Sql
                = "SELECT a = 'b''c' FROM d.e;\r\n";

            new SqlCmdPreprocessor()
                .Process(Sql)
                .Should().Equal(Sql);
        }

        [Test]
        public void Process_Substring_QuotedIdentifier()
        {
            const string Sql
                = "SELECT a = [b]]c] FROM d.e;\r\n";

            new SqlCmdPreprocessor()
                .Process(Sql)
                .Should().Equal(Sql);
        }

        [Test]
        public void Process_Substring_LineCommentGo()
        {
            const string Sql
                = BatchA
                + "--" + BatchSeparator
                + BatchB;

            new SqlCmdPreprocessor()
                .Process(Sql)
                .Should().Equal(Sql);
        }

        [Test]
        public void Process_Substring_BlockCommentGo()
        {
            const string Sql
                = BatchA
                + "/*" + BatchSeparator
                + "*/" + Eol
                + BatchB;

            new SqlCmdPreprocessor()
                .Process(Sql)
                .Should().Equal(Sql);
        }

        [Test]
        public void Process_Builder_VariableReplacement()
        {
            const string
                InputSql  = "x$(Foo)y" + Eol,
                OutputSql = "xBary"    + Eol;

            var processor = new SqlCmdPreprocessor();

            processor.Variables["Foo"] = "Bar";

            processor
                .Process(InputSql)
                .Should().Equal(OutputSql);
        }

        [Test]
        public void Process_Builder_QuotedStringWithVariableReplacement()
        {
            const string
                InputSql  = "a'x$(Foo)y''z'b" + Eol,
                OutputSql = "a'xBary''z'b"    + Eol;

            var processor = new SqlCmdPreprocessor();

            processor.Variables["Foo"] = "Bar";

            processor
                .Process(InputSql)
                .Should().Equal(OutputSql);
        }

        [Test]
        public void Process_Builder_QuotedIdentifierWithVariableReplacement()
        {
            const string
                InputSql  = "a[x$(Foo)y]]z]b" + Eol,
                OutputSql = "a[xBary]]z]b"    + Eol;

            var processor = new SqlCmdPreprocessor();

            processor.Variables["Foo"] = "Bar";

            processor
                .Process(InputSql)
                .Should().Equal(OutputSql);
        }

        [Test]
        public void Process_Builder_VariableReplacement_NotFound()
        {
            new SqlCmdPreprocessor()
                .Invoking(p => p.Process("$(Foo)").ToList())
                .Should().Throw<SqlCmdException>()
                .WithMessage("Variable Foo is not defined.");
        }

        private const string
            Eol            = "\r\n",
            BatchA         = "BATCH A" + Eol,
            BatchB         = "BATCH B" + Eol,
            BatchC         = "BATCH C" + Eol,
            BatchSeparator = "GO"      + Eol;
    }
}
