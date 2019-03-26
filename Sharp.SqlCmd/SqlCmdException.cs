using System;
using System.Data;
using System.Runtime.Serialization;

namespace Sharp.SqlCmd
{
    public class SqlCmdException : DataException
    {
        private const string
            DefaultMessage            = "An error occurred during SQLCMD preprocessing.",
            VariableNotDefinedMessage = "Variable {0} is not defined.";

        public SqlCmdException()
            : base(DefaultMessage) { }

        public SqlCmdException(string message)
            : base(message) { }

        public SqlCmdException(string message, Exception innerException)
            : base(message, innerException) { }

        protected SqlCmdException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }

        public static SqlCmdException ForVariableNotDefined(string name)
            => new SqlCmdException(string.Format(VariableNotDefinedMessage, name));
    }
}
