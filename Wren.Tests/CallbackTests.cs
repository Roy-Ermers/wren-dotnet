using Wren.Tests.Fixtures;

namespace Wren.Tests;

[NotInParallel("hooks")]
public class CallbackTests
{
  [Test]
  public async Task Function_can_be_called_from_CSharp_after_the_script_ran()
  {
    using var vm = new TestVm();
    vm.Run("""
        Hooks.onTick {|dt| System.print("tick %(dt)") }
        Hooks.onTransform {|counter| Counter.new(counter.value * 2) }
        Hooks.onAnswer { 42 }
        """);

    Hooks.Tick!(0.5);
    var doubled = Hooks.Transform!(new Counter(21));

    await Assert.That(vm.Output).IsEqualTo("tick 0.5");
    await Assert.That(doubled.Value).IsEqualTo(42);
    await Assert.That(Hooks.Answer!()).IsEqualTo(42);
  }

  [Test]
  public async Task Function_called_during_a_foreign_call_is_a_runtime_error()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("""
        Hooks.onTick {|dt| }
        Hooks.tickNow(1)
        """);

    await Assert.That(error.Message).Contains("InvalidOperationException");
  }

  [Test]
  public async Task Wren_error_in_the_function_throws_a_WrenException()
  {
    using var vm = new TestVm();
    vm.Run("""Hooks.onTick {|dt| Fiber.abort("bad tick") }""");

    var error = Assert.Throws<WrenException>(() => Hooks.Tick!(1));

    await Assert.That(error.Message).Contains("bad tick");
  }

  [Test]
  public async Task Released_function_can_no_longer_be_called()
  {
    using var vm = new TestVm();
    vm.Run("Hooks.onTick {|dt| }");

    await Assert.That(WrenCallback.Release(Hooks.Tick)).IsTrue();
    Assert.Throws<ObjectDisposedException>(() => Hooks.Tick!(1));
  }

  [Test]
  public void Function_can_no_longer_be_called_after_the_VM_is_disposed()
  {
    var vm = new TestVm();
    vm.Run("Hooks.onTick {|dt| }");
    vm.Dispose();

    Assert.Throws<ObjectDisposedException>(() => Hooks.Tick!(1));
  }
}
