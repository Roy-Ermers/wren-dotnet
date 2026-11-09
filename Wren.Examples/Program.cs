using Wren;
using Wren.Examples;

var content = File.ReadAllText("./WrenEnvironment/code.wren");

foreach (var module in WrenBinding.Modules)
{
  File.WriteAllText($"./WrenEnvironment/{module}.wren", WrenBinding.GetModuleSource(module));
}

using var vm = new WrenVm(new WrenVmOptions
{
  Write = ConsoleOutput.Write,
  Error = ConsoleOutput.Error,
});
vm.AddBinding(WrenBinding.Instance);

try
{
  vm.Interpret("main", content);

  // Wren functions stay valid after Interpret returns: call them from C#, then release them.
  Console.WriteLine("Calling Wren callbacks from C#:");
  Events.Tick(0.5);
  Events.Tick(0.25);

  var position = Events.Move(new Vector3(1, 2, 3));
  Console.WriteLine($"Moved to {position}");

  Events.Clear();
  Events.Tick(1.0);
}
catch (WrenException e)
{
  // The errors were already reported to ConsoleOutput.Error.
  Console.Error.WriteLine($"Wren failed: {e.Result}");
}

[WrenType]
public partial record Test(int a, bool b);

[WrenType]
partial class Records
{
  [WrenMethod]
  public static Test GetRecord()
  {
    return new Test(5, true);
  }

  [WrenMethod]
  public static void ConsumeRecord(Test record)
  {
    Console.WriteLine(record);
  }
}
static class ConsoleOutput
{
  public static void Write(string text)
  {
    if (string.IsNullOrWhiteSpace(text))
      Console.Write(text);

    else
      Console.Write("[WREN]: " + text);
  }

  // Wren.Native isn't imported: its WrenType enum would clash with the [WrenType] attribute.
  public static void Error(WrenError error)
  {
    switch (error.Type)
    {
      case Wren.Native.WrenErrorType.Compile:
        Console.Error.WriteLine($"Wren compile error in {error.Module}:{error.Line} : {error.Message}");
        break;

      case Wren.Native.WrenErrorType.StackTrace:
        Console.Error.WriteLine($"at {error.Message} in {error.Module}:{error.Line}");
        break;

      case Wren.Native.WrenErrorType.Runtime:
        Console.Error.WriteLine(string.IsNullOrEmpty(error.Module)
            ? $"Wren error: {error.Message}"
            : $"Wren error in {error.Module}: {error.Message}");
        break;
    }
  }
}
