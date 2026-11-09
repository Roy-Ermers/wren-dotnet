using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Wren.SourceGenerator.Attributes;

public static partial class Templates
{
    public static SourceText WrenConstructorAttribute =>
        SourceText.From($$"""
                        #nullable enable
                        using System;
                        using Microsoft.CodeAnalysis;
                        namespace {{Namespace}}
                        {
                            /// <summary>
                            /// Exposes a constructor to Wren as <c>Type.new(...)</c>, or a static factory method returning the
                            /// declaring type as a named constructor (<c>Type.fromScalar(...)</c>).
                            /// </summary>
                            [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method), Embedded]
                            internal sealed class WrenConstructorAttribute : Attribute
                            {
                            }
                        }
                        """, Encoding.UTF8);
}
