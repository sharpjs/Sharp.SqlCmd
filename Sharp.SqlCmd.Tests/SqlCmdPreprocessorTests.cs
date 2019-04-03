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
            TestProcess("");
        }

        [Test]
        public void Process_Substring_SingleBatch()
        {
            TestProcess(BatchA, BatchA);
        }

        [Test]
        public void Process_Substring_MultiBatch()
        {
            TestProcess(
                string.Join(BatchSeparator, BatchA, BatchB, BatchC),
                BatchA, BatchB, BatchC
            );
        }

        [Test]
        public void Process_Substring_QuotedString()
        {
            const string Sql = "SELECT x = 'a''b';" + Eol;

            TestProcess(Sql, Sql);
        }

        [Test]
        public void Process_Substring_QuotedIdentifier()
        {
            const string Sql = "SELECT x = [a]]b];" + Eol;

            TestProcess(Sql, Sql);
        }

        [Test]
        public void Process_Substring_LineCommentGo()
        {
            const string Sql
                = BatchA
                + "--" + BatchSeparator
                + BatchB;

            TestProcess(Sql, Sql);
        }

        [Test]
        public void Process_Substring_BlockCommentGo()
        {
            const string Sql
                = BatchA
                + "/*" + BatchSeparator
                + "*/" + Eol
                + BatchB;

            TestProcess(Sql, Sql);
        }

        [Test]
        public void Process_Builder_VariableReplacement()
        {
            TestProcess(
                p => p.Variables["v"] = "1234",
                "SELECT x = $(v);" + Eol,
                "SELECT x = 1234;" + Eol
            );
        }

        [Test]
        public void Process_Builder_QuotedStringWithVariableReplacement()
        {
            TestProcess(
                p => p.Variables["v"] = "1234",
                "SELECT x = '$(v)';" + Eol,
                "SELECT x = '1234';" + Eol
            );
        }

        [Test]
        public void Process_Builder_QuotedIdentifierWithVariableReplacement()
        {
            TestProcess(
                p => p.Variables["v"] = "1234",
                "SELECT x = [$(v)];" + Eol,
                "SELECT x = [1234];" + Eol
            );
        }

        [Test]
        public void Process_Builder_QuotedIdentifierWithVariableReplacement_Unterminated()
        {
            TestProcess(
                p => p.Variables["v"] = "1234",
                "SELECT x = [$(v)",
                "SELECT x = [1234"
            );
        }

        [Test]
        public void Process_Builder_VariableReplacement_NotFound()
        {
            new SqlCmdPreprocessor()
                .Invoking(p => p.Process("$(Foo)").ToList())
                .Should().Throw<SqlCmdException>()
                .WithMessage("Variable Foo is not defined.");
        }

        private static void TestProcess(
            string          input,
            params string[] outputs)
        {
            TestProcess(null, input, outputs);
        }

        private static void TestProcess(
            Action<SqlCmdPreprocessor> setup,
            string                     input,
            params string[]            outputs)
        {
            var processor = new SqlCmdPreprocessor();

            setup?.Invoke(processor);

            processor.Process(input).Should().Equal(outputs);
        }

        private const string
            Eol            = "\r\n",
            BatchA         = "BATCH A" + Eol,
            BatchB         = "BATCH B" + Eol,
            BatchC         = "BATCH C" + Eol,
            BatchSeparator = "GO"      + Eol;
    }
}
