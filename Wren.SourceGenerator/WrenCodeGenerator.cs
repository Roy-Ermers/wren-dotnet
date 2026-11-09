using Wren.SourceGenerator.helpers;
using Wren.SourceGenerator.Models;

namespace Wren.SourceGenerator;

public class WrenCodeGenerator
{
  public static string Generate(WrenClassModel model)
  {
    IndentedStringBuilder sb = new IndentedStringBuilder();
    Generate(sb, model);
    return sb.ToString();
  }

  public static void Generate(IndentedStringBuilder sb, WrenClassModel model)
  {
    sb.Append("foreign class ").Append(model.Name).AppendLine(" {");
    using (sb.Indented())
    {
      GenerateMethods(sb, model.Methods);
      GenerateProperties(sb, model.Properties);
    }

    sb.AppendLine("}");
  }

  public static string GenerateEnum(WrenEnumModel model)
  {
    var sb = new IndentedStringBuilder();

    sb.Append("class ").Append(model.Name).AppendLine(" {");
    using (sb.Indented())
    {
      foreach (var member in model.Members)
        sb.AppendLine($"static {member.Name} {{ {member.Value} }}");

      var names = model.Members
          .GroupBy(m => m.Value)
          .Select(g => $"{g.Key}: \"{g.First().Name}\"");

      // __names is a Wren static field, created on first use.
      sb.AppendLine("static [value] {");
      using (sb.Indented())
      {
        sb.AppendLine($"if (__names == null) __names = {{{string.Join(", ", names)}}}");
        sb.AppendLine("return __names[value]");
      }
      sb.AppendLine("}");
    }

    sb.AppendLine("}");
    return sb.ToString();
  }

  // No allocator: constructors are static foreign methods named `new` (see WrenBindingSite).
  private static void GenerateMethods(IndentedStringBuilder sb, EquatableArray<WrenMethod> methods)
  {
    foreach (var signature in methods.GroupBy(m => (m.Name, m.IsStatic, m.IsGetter, m.Parameters.Count)))
      AppendLines(sb, WrenBindingSite.ForMethods(signature.ToList()).Declaration);
  }

  private static void GenerateProperties(IndentedStringBuilder sb, EquatableArray<WrenProperty> properties)
  {
    foreach (var property in properties)
    {
      if (property.Getter)
        AppendLines(sb, WrenBindingSite.ForGetter(property).Declaration);

      if (property.Setter)
        AppendLines(sb, WrenBindingSite.ForSetter(property).Declaration);
    }
  }

  private static void AppendLines(IndentedStringBuilder sb, IEnumerable<string> lines)
  {
    foreach (var line in lines)
      sb.AppendLine(line);
  }
}
