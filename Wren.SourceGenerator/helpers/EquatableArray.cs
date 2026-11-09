using System.Collections;
using System.Collections.Immutable;

namespace Wren.SourceGenerator.helpers;

// Incremental generator models must be equatable for caching; ImmutableArray/List compare by reference.
public readonly struct EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
  public static readonly EquatableArray<T> Empty = new([]);

  private readonly T[]? _array = array;

  public int Count => _array?.Length ?? 0;

  public int Length => Count;

  public T this[int index] => _array![index];

  public bool Equals(EquatableArray<T> other)
  {
    var left = _array ?? [];
    var right = other._array ?? [];

    if (left.Length != right.Length)
      return false;

    var comparer = EqualityComparer<T>.Default;
    for (var i = 0; i < left.Length; i++)
    {
      if (!comparer.Equals(left[i], right[i]))
        return false;
    }

    return true;
  }

  public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

  public override int GetHashCode()
  {
    if (_array is null)
      return 0;

    unchecked
    {
      var hash = 17;
      foreach (var item in _array)
        hash = hash * 31 + (item?.GetHashCode() ?? 0);
      return hash;
    }
  }

  public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

  public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

  public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(_array ?? [])).GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class EquatableArrayExtensions
{
  public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source) where T : IEquatable<T>
    => new([.. source]);
}
