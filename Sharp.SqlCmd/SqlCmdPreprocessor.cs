using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static System.Text.RegularExpressions.RegexOptions;

namespace Sharp.SqlCmd
{
    /// <summary>
    ///   A simple SQL preprocessor that supports a limited subset of SQLCMD capability.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The following SQLCMD features are supported:
    ///   </para>
    ///   <list type="bullet">
    ///     <item><c>GO</c> batch splitting</item>
    ///     <item><c>$(foo)</c> variable replacement</item>
    ///     <item><c>:r</c> inclusion</item>
    ///     <item><c>:setvar</c> variable (re)definition</item>
    ///   </list>
    /// </remarks>
    public class SqlCmdPreprocessor
    {
        private readonly Dictionary<string, string> _variables;
        private          StringBuilder              _builder;

        /// <summary>
        ///   Initializes a new <see cref="SqlCmdPreprocessor"/> instance.
        /// </summary>
        public SqlCmdPreprocessor()
        {
            _variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///   Gets the dictionary of preprocessor variables.
        /// </summary>
        public IDictionary<string, string> Variables => _variables;

        /// <summary>
        ///   Gets or sets whether variable replacement occurs within <c>:setvar</c> statements.
        ///   The default is <c>false</c> to match SQLCMD behavior.
        /// </summary>
        public bool EnableVariableReplacementInSetvar { get; set; }

        /// <summary>
        ///   Processes the specified SQL text, splitting batches, replacing preprocessor
        ///   variables, and performing supported SQLCMD directives.
        /// </summary>
        /// <param name="sql">
        ///   The SQL text to be processed.
        /// </param>
        /// <returns>
        ///   The batch(es) resulting from processing <paramref name="sql"/>.
        ///   If <paramref name="sql"/> is empty, the returned enumerable is empty.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="sql"/> is <c>null</c>.
        /// </exception>
        public IEnumerable<string> Process(string sql)
        {
            if (sql == null)
                throw new ArgumentNullException(nameof(sql));

            // The generator is in separate method, so that checks above are performed on
            // invocation rather than on enumeration.
            return ProcessCore(sql);
        }

        // Generator for batches
        private IEnumerable<string> ProcessCore(string sql)
        {
            Assume.That(sql != null);

            var index  = 0;
            var length = sql.Length;
            string batch;

            while (index < length)
            {
                (batch, index) = GetNextBatch(sql, index);
                yield return batch;
            }
        }

        private (string sql, int next) GetNextBatch(string sql, int start)
        {
            // Batch discovery starts in the more-efficient substring mode, in which the found
            // batch will be a substring (or the entirety) of the original SQL verbatim.  This is
            // possible if the batch contains no variable replacements or preprocessor directives.
            // If any of those are found, batch discovery switches to builder mode.

            Assume.That(sql != null);
            Assume.That(0 <= start && start < sql.Length);

            var next = start;

            do
            {
                // Find next significant text element
                var match = TokenRegex.Match(sql, next);
                if (!match.Success)
                    break;

                // Advance position to just after the found element
                next = match.Index + match.Length;

                // Interpret the found element
                switch (sql[match.Index])
                {
                    // Comment
                    case '-':
                    case '/':
                        // Ignore comments
                        continue;

                    // Quoted string/identifier
                    case '\'':
                    case '[':
                        // Variable replacement requires builder mode
                        // NOTE: Match instead of IsMatch avoids allocation in usual case
                        if (VariableRegex.Match(sql, match.Index + 1, match.Length - 1).Success)
                            return BuildNextBatch(sql, start, match);

                        // Ignore other quoted strings/identifiers
                        continue;

                    // Variable replacement or preprocessor directive
                    case '$':
                    case ':':
                    default:
                        // Requires builder mode
                        return BuildNextBatch(sql, start, match);

                    // Batch separator
                    case 'g':
                    case 'G':
                        // Return substring as batch
                        return (sql.Substring(start, match.Index - start), next);
                }
            }
            while (next < sql.Length);

            // Return final batch
            return (sql.Substring(start), next: sql.Length);
        }

        private (string sql, int next) BuildNextBatch(string sql, int index, Match match)
        {
            // If necessary, batch discovery transitions to the less-efficient builder mode, in
            // which the batch is assembled using a StringBuilder.  Here, match is the text element
            // that caused the transition into builder mode.

            Assume.That(sql != null);
            Assume.That(0 <= index && index < sql.Length);
            Assume.That(match?.Success == true);

            var builder = InitializeBuilder(match.Index - index);

            for (;;)
            {
                // Append unmatched prefix to batch
                builder.Append(sql, index, match.Index - index);

                // Advance position to just after the found element
                index = match.Index + match.Length;

                // Interpret the found element
                switch (sql[match.Index])
                {
                    // Comment
                    case '-':
                    case '/':
                        // Add comments to batch verbatim
                        builder.Append(sql, match.Index, match.Length);
                        break;

                    // Quoted string/identifier
                    case '\'':
                    case '[':
                        ReplaceVariablesIn(sql, match.Index, match.Length);
                        break;

                    // Variable replacement
                    case '$':
                        ReplaceVariable(match);
                        break;

                    // Preprocessor directive
                    case ':':
                    default:
                        PerformDirective(match);
                        break;

                    // Batch separator
                    case 'g':
                    case 'G':
                        // Return non-final batch
                        return (sql: builder.ToString(), index);
                }

                // Detect end of input
                if (index == sql.Length)
                    break;

                // Get next significant text element
                match = TokenRegex.Match(sql, index);
                if (match.Success)
                    continue;

                // Append unmatched suffix of final token
                builder.Append(sql, index, sql.Length - index);
                break;
            }

            // Return non-final batch
            return (sql: builder.ToString(), next: sql.Length);
        }

        private StringBuilder InitializeBuilder(int capacity)
        {
            const int MinimumBufferSize = 4096;

            // Calculate actual capacity
            capacity = capacity < MinimumBufferSize
                ? MinimumBufferSize
                : capacity.GetNextPowerOf2Saturating();

            // Create builder if first time
            var builder =_builder;
            if (builder == null)
                return _builder = new StringBuilder(capacity);

            // Recycle existing builder
            builder.Clear().EnsureCapacity(capacity);
            return builder;
        }

        private void ReplaceVariablesIn(string sql, int index, int length)
        {
            Assume.That(sql != null);
            Assume.That(0 <= index  && index  <  sql.Length);
            Assume.That(0 <= length && length <= sql.Length - index);

            var end   = index + length;
            var match = VariableRegex.Match(sql, index, length);

            while (match.Success)
            {
                _builder.Append(sql, index, match.Index - index);
                ReplaceVariable(match);

                index = match.Index + match.Length;
                if (index >= end)
                    return;

                match = match.NextMatch();
            }

            _builder.Append(sql, index, end - index);
        }

        private void ReplaceVariable(Match match)
        {
            Assume.That(!string.IsNullOrEmpty(match?.Groups?["name"]?.Value));

            var name = match.Groups["name"].Value;

            if (!_variables.TryGetValue(name, out var value))
                throw SqlCmdException.ForVariableNotDefined(name);

            _builder.Append(value);
        }

        private void PerformDirective(Match match)
        {
            Assume.That(!string.IsNullOrEmpty(match?.Groups?["name"]?.Value));

            var groups = match.Groups;
            var name   = groups["name"] .Value;
            var args   = groups["args"]?.Value ?? "";

            if (name.Equals("r", StringComparison.OrdinalIgnoreCase))
                PerformIncludeDirective(match, args);
            else // setvar
                PerformSetvarDirective(match, args);
        }

        private void PerformIncludeDirective(Match match, string args)
        {
            // TODO: Implement
        }

        private void PerformSetvarDirective(Match match, string args)
        {
            // TODO: Implement
        }

        private static readonly Regex TokenRegex = new Regex(
            @"
                --     .             *?                   ( \r?\n | \z ) | # line comment
                /\*  ( .     | \n   )*?                   ( \*/   | \z ) | # block comment
                '    ( [^']  | ''   )*                    ( '     | \z ) | # string
                \[   ( [^\]] | \]\] )*                    ( \]    | \z ) | # quoted identifier
                \$\( (?<name>\w+)                         ( \)    | \z ) | # variable replacement
                ^\s* :(?<name>r)      (\s+ (?<args>.*?))? ( \r?\n | \z ) | # include directive
                ^\s* :(?<name>setvar) (\s+ (?<args>.*?))? ( \r?\n | \z ) | # set-variable directive
                ^GO                                       ( \r?\n | \z )   # batch separator
            ",
            Options
        );

        private static readonly Regex VariableRegex = new Regex(
            @"
                \$\( (?<name>\w+) \)
            ",
            Options
        );

        private const RegexOptions Options
            = Multiline
            | IgnoreCase
            | CultureInvariant
            | IgnorePatternWhitespace
            | ExplicitCapture
            | Compiled;
    }
}
