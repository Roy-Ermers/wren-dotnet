// Polyfill enabling init/records on netstandard2.0.
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}