namespace Wren.Examples;

[WrenType]
public partial class Vector3
{
  [WrenProperty]
  public int X { get; set; }

  [WrenProperty]
  public int Y { get; set; }

  [WrenProperty]
  public int Z { get; set; }

  [WrenProperty]
  public int this[int index]
  {
    get => index switch
    {
      0 => X,
      1 => Y,
      2 => Z,
      _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Vector3 index must be 0, 1 or 2.")
    };
    set
    {
      switch (index)
      {
        case 0: X = value; break;
        case 1: Y = value; break;
        case 2: Z = value; break;
        default: throw new ArgumentOutOfRangeException(nameof(index), index, "Vector3 index must be 0, 1 or 2.");
      }
    }
  }

  [WrenProperty]
  public static Vector3 Zero => new(0, 0, 0);

  [WrenProperty]
  public static Vector3 One => new(0, 0, 0);

  [WrenConstructor]
  public Vector3(int x, int y, int z)
  {
    X = x;
    Y = y;
    Z = z;
  }

  [WrenConstructor]
  public Vector3()
  {
    X = 0;
    Y = 0;
    Z = 0;
  }

  [WrenMethod]
  public double GetLength()
  {
    return Math.Sqrt(X * Y * Z);
  }
  [WrenMethod]
  public override string ToString()
  {
    return $"({X}, {Y}, {Z})";
  }

  [WrenMethod]
  public static Vector3 operator +(Vector3 v1, Vector3 v2)
  {
    return new(
        v1.X + v2.X,
        v1.Y + v2.Y,
        v1.Z + v2.Z
    );
  }


  [WrenMethod]
  public static Vector3 operator *(Vector3 v1, Vector3 v2)
  {
    return new(
        v1.X * v2.X,
        v1.Y * v2.Y,
        v1.Z * v2.Z
    );
  }


  [WrenMethod]
  public static Vector3 operator -(Vector3 v1, Vector3 v2)
  {
    return new(
        v1.X - v2.X,
        v1.Y - v2.Y,
        v1.Z - v2.Z
    );
  }

  [WrenMethod]
  public static Vector3 FromDirection(Direction direction) => direction switch
  {
    Direction.North => new(0, 0, 1),
    Direction.East => new(1, 0, 0),
    Direction.South => new(0, 0, -1),
    Direction.West => new(-1, 0, 0),
    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown direction.")
  };

  [WrenMethod]
  public static Vector3? Parse(string? text)
  {
    var parts = text?.Split(',');
    if (parts is not { Length: 3 })
      return null;

    return int.TryParse(parts[0], out var x) && int.TryParse(parts[1], out var y) && int.TryParse(parts[2], out var z)
        ? new Vector3(x, y, z)
        : null;
  }

  [WrenMethod]
  public static Vector3 operator -(Vector3 v) => new(-v.X, -v.Y, -v.Z);

  [WrenMethod]
  public static Vector3 operator /(Vector3 v1, Vector3 v2)
  {
    return new(
        v1.X / v2.X,
        v1.Y / v2.Y,
        v1.Z / v2.Z
    );
  }

  [WrenMethod]
  public static Vector3 operator /(Vector3 v1, int amount)
  {
    return new(
        v1.X / amount,
        v1.Y / amount,
        v1.Z / amount
    );
  }

  [WrenMethod]
  public static Vector3 operator *(Vector3 v1, int amount)
  {
    return new(
        v1.X * amount,
        v1.Y * amount,
        v1.Z * amount
    );
  }

  [WrenMethod]
  public static Vector3 operator -(Vector3 v1, int amount)
  {
    return new(
        v1.X - amount,
        v1.Y - amount,
        v1.Z - amount
    );
  }

  [WrenMethod]
  public static Vector3 operator +(Vector3 v1, int amount)
  {
    return new(
        v1.X + amount,
        v1.Y + amount,
        v1.Z + amount
    );
  }

  [WrenMethod]
  public static Vector3 operator %(Vector3 v1, Vector3 v2)
  {
    return new(
      v1.X % v2.X,
        v1.Y % v2.Y,
        v1.Z % v2.Z
    );
  }


  [WrenMethod]
  public static bool operator >(Vector3 v1, Vector3 v2)
  {
    return v1.GetLength() > v2.GetLength();
  }

  [WrenMethod]
  public static bool operator <(Vector3 v1, Vector3 v2)
  {
    return v1.GetLength() < v2.GetLength();
  }

  [WrenMethod]
  public static bool Equals(Vector3 v1, Vector3 v2)
  {
    return v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z;
  }

  [WrenConstructor]
  public static Vector3 FromScalar(int s) => new(s, s, s);
}
