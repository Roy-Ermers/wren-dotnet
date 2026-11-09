
namespace Wren.Examples;

[WrenType]
public partial class Logger
{
  [WrenMethod]
  public static void Log(string message)
  {
    Console.Write("[WREN]: ");
    Console.WriteLine(message);
  }

  [WrenMethod]
  public static void Log(double message)
  {
    Console.Write("[WREN]: ");
    Console.WriteLine(message);
  }

  [WrenMethod]
  public static void Log(bool message)
  {
    Console.Write("[WREN]: ");
    Console.WriteLine(message);
  }

  [WrenMethod]
  public static void Error(string message)
  {
    Console.Error.Write("[WREN]: ");
    Console.Error.WriteLine(message);
  }
}
