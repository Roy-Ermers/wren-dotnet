namespace Wren.Tests;

public class NumberTests
{
  [Test]
  public async Task Number_types_are_passed_as_Wren_numbers()
  {
    using var vm = new TestVm();

    var output = vm.Run("""
        System.print(Numbers.twice(3000000000))
        System.print(Numbers.toByte(255))
        System.print(Numbers.half(5))
        System.print(Numbers.echo(null))
        """);

    await Assert.That(output).IsEqualTo("6000000000\n255\n2.5\nnull");
  }

  [Test]
  public async Task Char_is_passed_as_a_one_character_string()
  {
    using var vm = new TestVm();

    await Assert.That(vm.Run("""System.print(Numbers.next("a"))""")).IsEqualTo("b");
  }

  [Test]
  public async Task String_that_isnt_one_character_is_a_runtime_error_for_a_char()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("""Numbers.next("ab")""");

    await Assert.That(error.Message).Contains("Expected a single character for argument 'letter', got \"ab\".");
  }

  [Test]
  public async Task Fractions_are_truncated_for_integer_types()
  {
    using var vm = new TestVm();

    await Assert.That(vm.Run("System.print(Numbers.toByte(2.9))")).IsEqualTo("2");
  }

  [Test]
  public async Task Number_out_of_range_is_a_runtime_error()
  {
    using var vm = new TestVm();

    var error = vm.RunFailing("Numbers.toByte(256)");

    await Assert.That(error.Message).Contains("Argument 'value' doesn't fit in Byte.");
  }
}
