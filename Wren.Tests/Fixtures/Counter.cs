namespace Wren.Tests.Fixtures;

[WrenType("test")]
public partial class Counter
{
  private readonly int[] _slots = new int[4];

  [WrenConstructor]
  public Counter() { }

  [WrenConstructor]
  public Counter(int start) => Value = start;

  [WrenConstructor]
  public Counter(string label) => Label = label;

  [WrenConstructor]
  public static Counter FromPair(int a, int b) => new(a + b);

  [WrenProperty] public int Value { get; set; }

  [WrenProperty] public string Label { get; set; } = "";

  [WrenProperty] public readonly int Id = 7;

  [WrenProperty] public static int Limit { get; set; } = 10;

  // Same C# object, but a new Wren object on every read.
  [WrenProperty] public static Counter Shared { get; } = new();

  [WrenProperty]
  public int this[int index]
  {
    get => _slots[index];
    set => _slots[index] = value;
  }

  [WrenMethod] public void Increment(int amount) => Value += amount;

  [WrenMethod] public string Describe(int n) => $"int {n}";

  [WrenMethod] public string Describe(string s) => $"string {s}";

  [WrenMethod] public string Describe(Counter other) => $"counter {other.Value}";

  [WrenMethod] public void Fail() => throw new InvalidOperationException("boom");

  [WrenMethod] public bool Same(Counter other) => ReferenceEquals(this, other);

  [WrenMethod] public static string? Echo(string? text) => text;

  [WrenMethod] public static Counter? Find(bool found) => found ? new Counter(1) : null;

  [WrenMethod] public static string ColorName(Color color) => color.ToString();

  [WrenMethod] public override string ToString() => $"Counter({Value})";
}
