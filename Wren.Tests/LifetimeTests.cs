using Wren.Tests.Fixtures;

namespace Wren.Tests;

[NotInParallel("tracked")]
public class LifetimeTests
{
  [Test]
  public async Task CSharp_object_is_collectable_once_Wren_collects_its_object()
  {
    using var vm = new TestVm();
    vm.Run("Tracked.new()");

    vm.Vm.CollectGarbage();
    CollectGarbage();

    await Assert.That(Tracked.Last!.IsAlive).IsFalse();
  }

  [Test]
  public async Task CSharp_object_is_kept_alive_by_Wren_until_the_VM_is_disposed()
  {
    var vm = new TestVm();
    vm.Run("var tracked = Tracked.new()");

    vm.Vm.CollectGarbage();
    CollectGarbage();
    await Assert.That(Tracked.Last!.IsAlive).IsTrue();

    vm.Dispose();
    CollectGarbage();
    await Assert.That(Tracked.Last!.IsAlive).IsFalse();
  }

  private static void CollectGarbage()
  {
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
  }
}
