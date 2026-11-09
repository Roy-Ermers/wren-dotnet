namespace Wren.Tests;

public class OperatorTests
{
  [Test]
  public async Task Operators_return_new_objects_and_leave_operands_unchanged()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var a = Vec.new(1, 2)
        var b = Vec.new(3, 4)
        System.print(a + b)
        System.print(-a)
        System.print(a)
        System.print(b)
        """);

    await Assert.That(output).IsEqualTo("(4, 6)\n(-1, -2)\n(1, 2)\n(3, 4)");
  }
}
