using System;
using System.Data;
using System.Runtime.Serialization;

namespace Sharp.SqlCmd
{
    /// <summary>
    ///   Represents an error condition encountered during SQLCMD preprocessing.
    /// </summary>
    public class SqlCmdException : DataException
    {
        private const string
            DefaultMessage            = "An error occurred during SQLCMD preprocessing.",
            VariableNotDefinedMessage = "Variable {0} is not defined.";

        /// <summary>
        ///   Initializes a new <see cref="SqlCmdException"/> instance with a
        ///   default message.
        /// </summary>
        public SqlCmdException()
            : base(DefaultMessage) { }

        /// <summary>
        ///   Initializes a new <see cref="SqlCmdException"/> instance with the
        ///   specified message.
        /// </summary>
        /// <param name="message">
        ///   A message that describes the error condition.
        /// </param>
        public SqlCmdException(string message)
            : base(message) { }

        /// <summary>
        ///   Initializes a new <see cref="SqlCmdException"/> instance with the
        ///   specified message and inner exception.
        /// </summary>
        /// <param name="message">
        ///   A message that explains the error condition.
        /// </param>
        /// <param name="innerException">
        ///   The exception that is the cause of the current exception.
        /// </param>
        public SqlCmdException(string message, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        ///   Initializes a new <see cref="SqlCmdException"/> instance with
        ///   serialized data.
        /// </summary>
        /// <param name="info">
        ///   An object that holds the serialized data.
        /// </param>
        /// <param name="context">
        ///   Contextual information about the source or destination.
        /// </param>
        protected SqlCmdException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        /// <summary>
        ///   Creates a <see cref="SqlCmdException"/> representing the error
        ///   that occurs when a SQLCMD variable is not defined.
        /// </summary>
        /// <param name="name">
        ///   The name of the variable that is not defined.
        /// </param>
        /// <returns>
        ///   An exception representing the error that occurs when
        ///   a SQLCMD variable is not defined.
        /// </returns>
        public static SqlCmdException ForVariableNotDefined(string name)
            => new SqlCmdException(string.Format(VariableNotDefinedMessage, name));
    }
}
