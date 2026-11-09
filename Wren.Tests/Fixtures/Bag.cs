namespace Wren.Tests.Fixtures;

[WrenType("test")]
public partial class Bag
{
  [WrenConstructor]
  public Bag(List<string> items) => Items = items;

  [WrenProperty] public List<string> Items { get; set; }

  [WrenProperty] public Dictionary<string, int> Tags { get; set; } = new();

  [WrenMethod] public static int Sum(IReadOnlyList<int> numbers) => numbers.Sum();

  [WrenMethod] public static int[] Range(int count) => Enumerable.Range(0, count).ToArray();

  [WrenMethod] public static IEnumerable<int> Evens(int count) => Enumerable.Range(0, count).Select(i => i * 2);

  [WrenMethod] public static int CountDistinct(HashSet<int> numbers) => numbers.Count;

  [WrenMethod] public static List<List<int>> Nested() => [[1, 2], [3]];

  [WrenMethod] public static int CountAll(List<int[]> lists) => lists.Sum(list => list.Length);

  [WrenMethod] public static List<Counter?> Counters() => [new Counter(1), null];

  [WrenMethod] public static Dictionary<string, int> Scores() => new() { ["a"] = 1, ["b"] = 2 };

  [WrenMethod]
  public static SortedDictionary<string, int> Sorted(SortedDictionary<string, int> scores) => scores;

  [WrenMethod] public static Dictionary<Color, List<string>> ByColor() => new() { [Color.Red] = ["apple"] };

  [WrenMethod]
  public static string Describe(string name, Dictionary<string, int> scores)
    => name + ": " + string.Join(", ", scores.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}"));
}
