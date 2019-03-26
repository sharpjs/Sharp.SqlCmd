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

        private (string sql, int next) BuildNextBatch(string sql, int start, Match match)
        {
            // If necessary, batch discovery transitions to the less-efficient builder mode, in
            // which the batch is assembled using a StringBuilder.  Here, match is the text element
            // that caused the transition into builder mode.

            Assume.That(sql != null);
            Assume.That(0 <= start && start < sql.Length);
            Assume.That(match != null);
            Assume.That(match.Success);

            // Start with the partial batch found in substring mode
            var builder = InitializeBuilder(sql, start, match.Index);
            int next;

            for (;;)
            {
                // Advance position to just after the found element
                next = match.Index + match.Length;

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
                        PerformDirective(match);
                        break;

                    // Batch separator
                    case 'g':
                    case 'G':
                        return (sql: builder.ToString(), next);
                }

                // Detect end of input
                if (next == sql.Length)
                    break;

                // Get next significant text element
                match = TokenRegex.Match(sql, next);
                if (!match.Success)
                    break;

                builder.Append(sql, next, match.Index - next);
            }

            builder.Append(sql, next, sql.Length - next);

            // Final batch
            return (sql: builder.ToString(), next: sql.Length);
        }

        private StringBuilder InitializeBuilder(string sql, int start, int end)
        {
            const int MinimumBufferSize = 4096;

            // Calculate sizes
            var length   = end - start;
            var capacity = length < MinimumBufferSize
                ? MinimumBufferSize
                : length.GetNextPowerOf2Saturating();

            var builder = _builder;
            if (builder == null)
            {
                // Create builder for first time
                 builder = new StringBuilder(sql, start, length, capacity);
                _builder = builder;
            }
            else // (builder != null)
            {
                // Reuse builder
                builder.Clear();
                builder.EnsureCapacity(capacity);
                builder.Append(sql, start, length);
            }

            return builder;
        }

        private void ReplaceVariablesIn(string sql, int start, int length)
        {
            Assume.That(sql != null);
            Assume.That(0 <= start && start < sql.Length);
            Assume.That(0 <= length && length <= sql.Length - start);

            var end   = start + length;
            var match = VariableRegex.Match(sql, start, length);

            while (match.Success)
            {
                _builder.Append(sql, start, match.Index - start);
                ReplaceVariable(match);

                start = match.Index + match.Length;
                if (start >= end)
                    return;

                match = match.NextMatch();
            }

            _builder.Append(sql, start, end - start);
        }

        private void ReplaceVariable(Match match)
        {
            Assume.That(match != null);
            Assume.That(match.Success);
            Assume.That(match.Groups["name"] != null);

            var name = match.Groups["name"].Value;
            Assume.That(name != null);
            Assume.That(name.Length > 0);

            if (!_variables.TryGetValue(name, out var value))
                throw SqlCmdException.ForVariableNotDefined(name);

            _builder.Append(value);
        }

        private void PerformDirective(Match match)
        {
        }

        private static readonly Regex TokenRegex = new Regex(
            @"
                '    ( [^']  | ''   )*  ( '     | \z ) |     # string
                \[   ( [^\]] | \]\] )*  ( \]    | \z ) |     # quoted identifier
                --   .*?                ( \r?\n | \z ) |     # line comment
                /\*  ( .     | \n   )*? ( \*/   | \z ) |     # block comment
                \$\( (?<name>\w+)       ( \)    | \z ) |     # variable replacement
                ^:r      \s+            ( \r?\n | \z ) |     # include directive
                ^:setvar \s+            ( \r?\n | \z ) |     # set-variable directive
                ^GO                     ( \r?\n | \z )       # batch separator
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
