namespace Wren.Tests;

public class CollectionTests
{
  [Test]
  public async Task Wren_list_is_passed_as_a_list()
  {
    using var vm = new TestVm();

    await Assert.That(vm.Run("System.print(Bag.sum([1, 2, 3]))")).IsEqualTo("6");
  }

  [Test]
  public async Task Array_is_returned_as_a_Wren_list()
  {
    using var vm = new TestVm();

    await Assert.That(vm.Run("System.print(Bag.range(3))")).IsEqualTo("[0, 1, 2]");
  }

  [Test]
  public async Task Any_enumerable_is_returned_as_a_Wren_list()
  {
    using var vm = new TestVm();

    await Assert.That(vm.Run("System.print(Bag.evens(3))")).IsEqualTo("[0, 2, 4]");
  }

  [Test]
  public async Task Collection_with_a_constructor_taking_a_list_is_created_from_a_Wren_list()
  {
    using var vm = new TestVm();

    await Assert.That(vm.Run("System.print(Bag.countDistinct([1, 1, 2]))")).IsEqualTo("2");
  }

  [Test]
  public async Task Collection_with_a_constructor_taking_a_dictionary_is_created_from_a_Wren_map()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var sorted = Bag.sorted({"b": 2, "a": 1})
        System.print(sorted["a"] + sorted["b"])
        """);

    await Assert.That(output).IsEqualTo("3");
  }

  [Test]
  public async Task Nested_lists_are_passed_both_ways()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Bag.nested())
        System.print(Bag.countAll([[1], [2, 3]]))
        """);

    await Assert.That(output).IsEqualTo("[[1, 2], [3]]\n3");
  }

  [Test]
  public async Task List_elements_can_be_objects_and_null()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var counters = Bag.counters()
        System.print(counters[0].value)
        System.print(counters[1])
        """);

    await Assert.That(output).IsEqualTo("1\nnull");
  }

  [Test]
  public async Task Lists_are_copied()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var bag = Bag.new(["a"])
        bag.items.add("b")
        System.print(bag.items)
        bag.items = ["x", "y"]
        System.print(bag.items)
        """);

    await Assert.That(output).IsEqualTo("[a]\n[x, y]");
  }

  [Test]
  public async Task Dictionary_is_returned_as_a_Wren_map()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var scores = Bag.scores()
        System.print(scores["a"] + scores["b"])
        System.print(Bag.byColor()[Color.red])
        """);

    await Assert.That(output).IsEqualTo("3\n[apple]");
  }

  [Test]
  public async Task Wren_map_is_passed_as_a_dictionary()
  {
    using var vm = new TestVm();

    var output = vm.Run("""System.print(Bag.describe("me", {"y": 2, "x": 1}))""");

    await Assert.That(output).IsEqualTo("me: x=1, y=2");
  }

  [Test]
  public async Task Map_property_can_be_set_and_read()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var bag = Bag.new([])
        bag.tags = {"a": 1}
        System.print(bag.tags["a"])
        """);

    await Assert.That(output).IsEqualTo("1");
  }

  [Test]
  public async Task Wrong_element_type_is_a_runtime_error()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("""Bag.sum([1, "two"])""");

    await Assert.That(error.Message).Contains("Expected Number for argument 'numbers[]', got String.");
  }

  [Test]
  public async Task Non_map_for_a_dictionary_is_a_runtime_error()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("""Bag.describe("me", [1])""");

    await Assert.That(error.Message).Contains("Expected Map for argument 'scores', got List.");
  }
}
