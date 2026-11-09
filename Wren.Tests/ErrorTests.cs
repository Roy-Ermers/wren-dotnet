using Wren.Native;

namespace Wren.Tests;

public class ErrorTests
{
  [Test]
  public async Task Compile_error_throws_with_module_and_line()
  {
    using var vm = new TestVm();

    var error = Assert.Throws<WrenException>(() => vm.Vm.Interpret("broken", "System.print(1)\nvar = 2"));

    await Assert.That(error.Result).IsEqualTo(WrenInterpretResult.CompileError);
    await Assert.That(error.Errors[0].Type).IsEqualTo(WrenErrorType.Compile);
    await Assert.That(error.Errors[0].Module).IsEqualTo("broken");
    await Assert.That(error.Errors[0].Line).IsEqualTo(2);
  }

  [Test]
  public async Task CSharp_exception_becomes_a_runtime_error_with_a_stack_trace()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("Counter.new().fail()");

    await Assert.That(error.Result).IsEqualTo(WrenInterpretResult.RuntimeError);
    await Assert.That(error.Message).Contains("Counter.fail(): InvalidOperationException: boom");
    await Assert.That(error.Errors.Any(e => e.Type == WrenErrorType.StackTrace && e.Module == "main")).IsTrue();
  }

  [Test]
  public async Task CSharp_exception_can_be_caught_in_Wren()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        var fiber = Fiber.new { Counter.new().fail() }
        System.print(fiber.try())
        System.print("still running")
        """);

    await Assert.That(output).IsEqualTo("Counter.fail(): InvalidOperationException: boom\nstill running");
  }
}
