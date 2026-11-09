namespace Wren.Tests;

public class InheritanceTests
{
  [Test]
  public async Task Returned_object_gets_its_most_derived_class()
  {
    using var vm = new TestVm();

    await Assert.That(vm.Run("System.print(Animal.adopt() is Dog)")).IsEqualTo("true");
  }

  [Test]
  public async Task Subclass_has_inherited_and_own_members()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var dog = Dog.new("Rex")
        System.print(dog.name)
        System.print(dog.fetch())
        """);

    await Assert.That(output).IsEqualTo("Rex\nRex fetches");
  }

  [Test]
  public async Task Overrides_are_called_through_virtual_dispatch()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Animal.new("Generic").speak())
        System.print(Dog.new("Rex").speak())
        System.print(Animal.adopt().speak())
        """);

    await Assert.That(output).IsEqualTo("...\nWoof\nWoof");
  }
}
