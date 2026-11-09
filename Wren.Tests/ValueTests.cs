using Wren.Tests.Fixtures;

namespace Wren.Tests;

public class ValueTests
{
  [Test]
  public async Task Strings_round_trip_as_UTF8()
  {
    using var vm = new TestVm();

    await Assert.That(vm.Run("""System.print(Counter.echo("héllo 🌍"))""")).IsEqualTo("héllo 🌍");
  }

  [Test]
  public async Task Null_passes_to_and_from_nullable_types()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Counter.echo(null))
        System.print(Counter.find(false))
        System.print(Counter.find(true))
        """);

    await Assert.That(output).IsEqualTo("null\nnull\nCounter(1)");
  }

  [Test]
  public async Task Null_for_a_non_nullable_parameter_is_a_runtime_error()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("Counter.new().increment(null)");

    await Assert.That(error.Message).Contains("Argument 'amount' can't be null.");
  }

  [Test]
  public async Task Wrong_argument_type_is_a_runtime_error_naming_the_expected_type()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("""Counter.new().increment("one")""");

    await Assert.That(error.Message).Contains("Expected Number for argument 'amount', got String.");
  }

  [Test]
  public async Task Enums_are_numbers_with_named_getters_and_a_reverse_lookup()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Color.green)
        System.print(Color[2])
        System.print(Color[9])
        System.print(Counter.colorName(Color.blue))
        """);

    await Assert.That(output).IsEqualTo("1\nblue\nnull\nBlue");
  }

  [Test]
  public async Task Objects_are_passed_by_reference()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Counter.shared.same(Counter.shared))
        Counter.shared.label = "shared"
        """);

    await Assert.That(output).IsEqualTo("true");
    await Assert.That(Counter.Shared.Label).IsEqualTo("shared");
  }
}
