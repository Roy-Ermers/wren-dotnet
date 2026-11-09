using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Wren.SourceGenerator.Attributes;

public static partial class Templates
{
    public static SourceText WrenMethodAttribute =>
        SourceText.From($$"""
                        #nullable enable
                        using System;
                        using Microsoft.CodeAnalysis;
                        namespace {{Namespace}}
                        {
                            /// <summary>Exposes a method or operator of a [WrenType] type to Wren.</summary>
                            [AttributeUsage(AttributeTargets.Method), Embedded]
                            internal sealed class WrenMethodAttribute : Attribute
                            {
                                /// <summary>The Wren method name. Defaults to the C# name in lowerCamelCase.</summary>
                                public string? Name { get; }

                                public WrenMethodAttribute()
                                {
                                }

                                public WrenMethodAttribute(string name)
                                {
                                    Name = name;
                                }
                            }
                        }
                        """, Encoding.UTF8);
}
