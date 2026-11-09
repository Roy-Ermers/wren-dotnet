using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Wren.SourceGenerator.Attributes;

public static partial class Templates
{
    public static SourceText WrenTypeAttribute =>
        SourceText.From($$"""
                        #nullable enable
                        using System;
                        using Microsoft.CodeAnalysis;
                        namespace {{Namespace}}
                        {
                            /// <summary>
                            /// Exposes a <c>partial</c> class, struct or record to Wren as a foreign class, or an enum as a Wren
                            /// class with a static getter per member.
                            /// </summary>
                            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum), Embedded]
                            internal sealed class WrenTypeAttribute : Attribute
                            {
                                /// <summary>The Wren module of the class. Defaults to "engine".</summary>
                                public string? Module { get; }

                                /// <summary>The Wren class name. Defaults to the C# type's name.</summary>
                                public string? ClassName { get; }
                                
                                public WrenTypeAttribute()
                                {
                                    
                                }
                                
                                public WrenTypeAttribute(string module)
                                {
                                    Module = module;
                                }  
                                                              
                                public WrenTypeAttribute(string module, string className)
                                {
                                    Module = module;
                                    ClassName = className;
                                }
                            }
                        }
                        """, Encoding.UTF8);
}