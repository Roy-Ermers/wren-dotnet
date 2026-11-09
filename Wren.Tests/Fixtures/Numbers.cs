namespace Wren.Tests.Fixtures;

[WrenType("test")]
public static partial class Numbers
{
  [WrenMethod] public static long Twice(long value) => value * 2;

  [WrenMethod] public static byte ToByte(byte value) => value;

  [WrenMethod] public static decimal Half(decimal value) => value / 2;

  [WrenMethod] public static ulong? Echo(ulong? value) => value;

  [WrenMethod] public static char Next(char letter) => (char)(letter + 1);
}
