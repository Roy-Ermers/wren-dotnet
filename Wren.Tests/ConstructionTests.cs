namespace Wren.Tests;

public class ConstructionTests
{
  [Test]
  public async Task Constructor_is_chosen_by_argument_count_and_type()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Counter.new().value)
        System.print(Counter.new(5).value)
        System.print(Counter.new("laps").label)
        """);

    await Assert.That(output).IsEqualTo("0\n5\nlaps");
  }

  [Test]
  public async Task Named_constructor_calls_the_static_factory()
  {
    using var vm = new TestVm();

    await Assert.That(vm.Run("System.print(Counter.fromPair(2, 3).value)")).IsEqualTo("5");
  }

  [Test]
  public async Task Arguments_matching_no_constructor_are_a_runtime_error()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("Counter.new(true)");

    await Assert.That(error.Message).Contains("No constructor matches the given argument types.");
  }
}
