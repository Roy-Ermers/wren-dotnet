using Wren.Tests.Fixtures;

namespace Wren.Tests;

public class MemberTests
{
  [Test]
  public async Task Property_can_be_set_and_read()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var counter = Counter.new()
        counter.value = 3
        counter.increment(2)
        System.print(counter.value)
        """);

    await Assert.That(output).IsEqualTo("5");
  }

  [Test]
  public async Task Readonly_field_is_readable_but_has_no_setter()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("""
        var counter = Counter.new()
        System.print(counter.id)
        counter.id = 1
        """);

    await Assert.That(vm.Output).IsEqualTo("7");
    await Assert.That(error.Message).Contains("does not implement 'id=(_)'");
  }

  [Test]
  public async Task Static_property_reads_and_writes_the_CSharp_value()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Counter.limit)
        Counter.limit = 20
        """);

    await Assert.That(output).IsEqualTo("10");
    await Assert.That(Counter.Limit).IsEqualTo(20);
  }

  [Test]
  public async Task Overload_is_chosen_by_argument_type()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var counter = Counter.new(4)
        System.print(counter.describe(1))
        System.print(counter.describe("a"))
        System.print(counter.describe(counter))
        """);

    await Assert.That(output).IsEqualTo("int 1\nstring a\ncounter 4");
  }

  [Test]
  public async Task ToString_is_used_by_interpolation_and_print()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var counter = Counter.new(3)
        System.print("got %(counter)")
        System.print(counter)
        """);

    await Assert.That(output).IsEqualTo("got Counter(3)\nCounter(3)");
  }

  [Test]
  public async Task Indexer_is_the_subscript_operator()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var counter = Counter.new()
        counter[2] = 8
        System.print(counter[2])
        System.print(counter[0])
        """);

    await Assert.That(output).IsEqualTo("8\n0");
  }
}
