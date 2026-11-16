
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
  public async Task ResolveModule_receives_the_importing_module()
  {
    var imports = new List<(string Importer, string Name)>();
    using var vm = new TestVm(
        loadModule: name => name switch
        {
          "a" => "import \"b\" for B\nvar A = B + 1",
          "b" => "var B = 41",
          _ => null,
        },
        resolveModule: (importer, name) =>
        {
          imports.Add((importer, name));
          return name;
        });

    var output = vm.Run("""
        import "a" for A
        System.print(A)
        """);

    await Assert.That(output).IsEqualTo("42");
    await Assert.That(imports).IsEquivalentTo([("main", "test"), ("main", "a"), ("a", "b")]);
  }

  [Test]
  public async Task ResolveModule_can_rename_imports()
  {
    using var vm = new TestVm(
        loadModule: name => name == "lib/util" ? "var Answer = 42" : null,
        resolveModule: (importer, name) => name == "test" ? name : "lib/" + name);

    var output = vm.Run("""
        import "util" for Answer
        System.print(Answer)
        """);

    await Assert.That(output).IsEqualTo("42");
  }

  [Test]
  public async Task ResolveModule_returning_null_fails_the_import()
  {
    using var vm = new TestVm(
        loadModule: name => name == "secret" ? "var X = 1" : null,
        resolveModule: (importer, name) => name == "secret" ? null : name);

    var error = vm.RunFailing("import \"secret\" for X");

    await Assert.That(error.Message).Contains("Could not resolve module 'secret' imported from 'main'");
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
