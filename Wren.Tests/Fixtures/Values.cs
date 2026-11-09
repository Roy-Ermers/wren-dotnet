using System.Globalization;

namespace Wren.Tests.Fixtures;

[WrenType("test")]
public partial struct Vec
{
  [WrenConstructor]
  public Vec(double x, double y) => (X, Y) = (x, y);

  [WrenProperty] public double X { get; set; }

  [WrenProperty] public double Y { get; set; }

  [WrenMethod]
  public void Scale(double factor) => (X, Y) = (X * factor, Y * factor);

  [WrenMethod] public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y);

  [WrenMethod] public static Vec operator -(Vec v) => new(-v.X, -v.Y);

  [WrenMethod] public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"({X}, {Y})");
}

[WrenType("test")]
public partial record Pair(string Name, int Count);

[WrenType("test")]
public partial record struct Point3(int X, int Y, int Z);

[WrenType("test")]
public enum Color { Red, Green, Blue }
