
namespace Wren.Tests;

public class ModuleTests
{
  [Test]
  public async Task LoadModule_serves_imports_not_provided_by_bindings()
  {
    using var vm = new TestVm(loadModule: name => name == "util" ? "var Answer = 42" : null);

    var output = vm.Run("""
        import "util" for Answer
        System.print(Answer)
        """);

    await Assert.That(output).IsEqualTo("42");
  }

  [Test]
  public async Task Generated_module_source_declares_the_classes()
  {
    var source = WrenBinding.GetModuleSource("test");

    await Assert.That(WrenBinding.Modules).Contains("test");
    await Assert.That(source).Contains("foreign class Counter");
    await Assert.That(source).Contains("class Color");
    await Assert.That(source).Contains("foreign static new(name, count)");
    await Assert.That(WrenBinding.GetModuleSource("missing")).IsNull();
  }
}
