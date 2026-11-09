using System.Text;

namespace Wren.Tests;

internal sealed class TestVm : IDisposable
{
  private const string Import =
      "import \"test\" for Counter, Vec, Pair, Point3, Animal, Dog, Shape, Circle, Square, Color, Hooks, Tracked, Bag, Numbers\n";

  private readonly StringBuilder _output = new();

  public TestVm(Func<string, string?>? loadModule = null)
  {
    Vm = new WrenVm(new WrenVmOptions { Write = text => _output.Append(text), LoadModule = loadModule });
    Vm.AddBinding(WrenBinding.Instance);
  }

  public WrenVm Vm { get; }

  public string Output => _output.ToString().TrimEnd('\n');

  // Once per VM: module "main" can't import the fixtures twice.
  public string Run(string script)
  {
    _output.Clear();
    Vm.Interpret("main", Import + script);
    return Output;
  }

  public WrenException RunFailing(string script)
  {
    try
    {
      Run(script);
    }
    catch (WrenException e)
    {
      return e;
    }

    throw new InvalidOperationException("Expected the script to fail.");
  }

  public void Dispose() => Vm.Dispose();
}
