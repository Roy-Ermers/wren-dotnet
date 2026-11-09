using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Wren.SourceGenerator.Attributes;

public static partial class Templates
{
    public static SourceText WrenPropertyAttribute =>
        SourceText.From($$"""
                        #nullable enable
                        using System;
                        using Microsoft.CodeAnalysis;
                        namespace {{Namespace}}
                        {
                            /// <summary>
                            /// Exposes a property, field or indexer of a [WrenType] type to Wren as a getter and/or setter.
                            /// Delegate-typed members are write-only.
                            /// </summary>
                            [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field), Embedded]
                            internal sealed class WrenPropertyAttribute : Attribute
                            {
                                /// <summary>The Wren property name. Defaults to the C# name in lowerCamelCase; ignored for indexers.</summary>
                                public string? Name { get; }

                                public WrenPropertyAttribute()
                                {
                                }

                                public WrenPropertyAttribute(string name)
                                {
                                    Name = name;
                                }
                            }
                        }
                        """, Encoding.UTF8);
}
