namespace Wren.Tests.Fixtures;

[WrenType("test")]
public partial class Animal
{
  [WrenConstructor]
  public Animal(string name) => Name = name;

  [WrenProperty] public string Name { get; }

  [WrenMethod] public virtual string Speak() => "...";

  [WrenMethod] public static Animal Adopt() => new Dog("Rex");
}

[WrenType("test")]
public partial class Dog : Animal
{
  [WrenConstructor]
  public Dog(string name) : base(name) { }

  public override string Speak() => "Woof";

  [WrenMethod] public string Fetch() => Name + " fetches";
}

// TODO: make this `closed` once the SDK supports closed classes.
[WrenType("test")]
public abstract partial class Shape
{
  protected abstract string Kind { get; }

  [WrenMethod] public abstract double Area();

  [WrenMethod] public string Describe() => "I am a " + Kind;

  [WrenMethod]
  public static Shape Make(string kind) => kind == "circle" ? new Circle(1) : new Square(1);
}

[WrenType("test")]
public sealed partial class Circle : Shape
{
  [WrenConstructor]
  public Circle(double radius) => Radius = radius;

  [WrenProperty] public double Radius { get; }

  protected override string Kind => "circle";

  public override double Area() => Math.PI * Radius * Radius;
}

[WrenType("test")]
public sealed partial class Square : Shape
{
  [WrenConstructor]
  public Square(double side) => Side = side;

  [WrenProperty] public double Side { get; }

  protected override string Kind => "square";

  public override double Area() => Side * Side;
}
