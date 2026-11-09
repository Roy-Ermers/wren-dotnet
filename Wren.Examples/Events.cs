
namespace Wren.Examples;

// Wren can't be called during a foreign call: invoke these delegates after vm.Interpret returns.
[WrenType]
public partial class Events
{
  private static readonly List<Action<double>> TickHandlers = [];
  private static Func<Vector3, Vector3>? _mover;

  [WrenMethod]
  public static void OnTick(Action<double> handler) => TickHandlers.Add(handler);

  [WrenMethod]
  public static void OnMove(Func<Vector3, Vector3> mover)
  {
    if (_mover != null)
      WrenCallback.Release(_mover);

    _mover = mover;
  }

  public static void Tick(double deltaTime)
  {
    foreach (var handler in TickHandlers)
      handler(deltaTime);
  }

  public static Vector3 Move(Vector3 position) => _mover?.Invoke(position) ?? position;

  public static void Clear()
  {
    foreach (var handler in TickHandlers)
      WrenCallback.Release(handler);
    TickHandlers.Clear();

    if (_mover != null)
      WrenCallback.Release(_mover);
    _mover = null;
  }
}
