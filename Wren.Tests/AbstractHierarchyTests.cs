namespace Wren.Tests;

public class AbstractHierarchyTests
{
  [Test]
  public async Task Factory_returns_the_concrete_subclass()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Shape.make("circle") is Circle)
        System.print(Shape.make("square") is Square)
        """);

    await Assert.That(output).IsEqualTo("true\ntrue");
  }

  [Test]
  public async Task Abstract_method_dispatches_to_each_subclass()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Square.new(3).area())
        System.print(Circle.new(2).area() == Num.pi * 4)
        """);

    await Assert.That(output).IsEqualTo("9\ntrue");
  }

  [Test]
  public async Task Base_method_works_on_every_subclass()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Circle.new(1).describe())
        System.print(Shape.make("square").describe())
        """);

    await Assert.That(output).IsEqualTo("I am a circle\nI am a square");
  }

  [Test]
  public async Task Abstract_class_cannot_be_constructed()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("Shape.new()");

    await Assert.That(error.Message).Contains("does not implement 'new()'");
  }
}
