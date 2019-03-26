using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if NETFRAMEWORK
using System.Security;
#endif

// COM Compliance
[assembly: ComVisible(false)]

#if NETFRAMEWORK
// Security
[assembly: SecurityRules(SecurityRuleSet.Level2)]
#endif

[assembly: InternalsVisibleTo("Sharp.SqlCmd.Tests")]
