// Polyfill for IsExternalInit to allow 'init' properties and 'record' types on .NET Framework 4.8
// This is a well-known workaround - the compiler just needs this type to exist.

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
