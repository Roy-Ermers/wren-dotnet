namespace Wren.Tests.Fixtures;

[WrenType("test")]
public static partial class Hooks
{
  public static Action<double>? Tick;
  public static Func<Counter, Counter>? Transform;
  public static Func<int>? Answer;

  [WrenMethod] public static void OnTick(Action<double> handler) => Tick = handler;

  [WrenMethod] public static void OnTransform(Func<Counter, Counter> transform) => Transform = transform;

  [WrenMethod] public static void OnAnswer(Func<int> answer) => Answer = answer;

  // Calls into Wren during a foreign call, which isn't allowed.
  [WrenMethod] public static void TickNow(double dt) => Tick!(dt);
}

[WrenType("test")]
public partial class Tracked
{
  public static WeakReference? Last { get; private set; }

  [WrenConstructor]
  public Tracked() => Last = new WeakReference(this);
}
