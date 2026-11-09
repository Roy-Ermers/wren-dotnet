namespace Wren.Tests;

public class StructAndRecordTests
{
  [Test]
  public async Task Struct_setters_and_mutating_methods_change_the_Wren_object()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var v = Vec.new(1, 2)
        v.x = 5
        v.scale(2)
        System.print(v)
        """);

    await Assert.That(output).IsEqualTo("(10, 4)");
  }

  [Test]
  public async Task Record_is_created_with_its_primary_constructor_and_has_readable_properties()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var pair = Pair.new("apples", 3)
        System.print("%(pair.name): %(pair.count)")
        """);

    await Assert.That(output).IsEqualTo("apples: 3");
  }

  [Test]
  public async Task Record_positional_properties_are_not_settable()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("""Pair.new("apples", 3).count = 4""");

    await Assert.That(error.Message).Contains("does not implement 'count=(_)'");
  }

  [Test]
  public async Task Record_struct_is_created_with_its_primary_constructor_and_has_settable_properties()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var p = Point3.new(1, 2, 3)
        p.z = 30
        System.print("%(p.x) %(p.y) %(p.z)")
        """);

    await Assert.That(output).IsEqualTo("1 2 30");
  }
}
