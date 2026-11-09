using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Wren.SourceGenerator.helpers;

// Diagnostic has no value equality and its Location keeps the syntax tree alive, so models store this instead.
public sealed record DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    LocationInfo? Location,
    EquatableArray<string> MessageArgs)
{
  public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params object?[] messageArgs)
    => new(
        descriptor,
        LocationInfo.From(location),
        messageArgs.Select(arg => arg?.ToString() ?? string.Empty).ToEquatableArray()
    );

  public bool IsError => Descriptor.DefaultSeverity == DiagnosticSeverity.Error;

  public Diagnostic ToDiagnostic()
    => Diagnostic.Create(Descriptor, Location?.ToLocation(), [.. MessageArgs]);
}

public sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
  public static LocationInfo? From(Location? location)
  {
    if (location?.SourceTree is null)
      return null;

    return new(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
  }

  public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);
}
