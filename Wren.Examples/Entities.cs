
namespace Wren.Examples;

[WrenType]
public partial class Entity
{
  [WrenConstructor]
  public Entity(int health)
  {
    Health = health;
  }

  [WrenProperty]
  public int Health { get; set; }

  [WrenMethod]
  public void Damage(int amount) => Health -= amount;

  [WrenMethod]
  public virtual string Describe() => $"Entity with {Health} health";

  // Wren receives the runtime type, a Player.
  [WrenMethod]
  public static Entity Spawn(string name) => new Player(name, 100);
}

[WrenType]
public partial class Player : Entity
{
  [WrenConstructor]
  public Player(string name, int health) : base(health)
  {
    Name = name;
  }

  [WrenProperty]
  public string Name { get; set; }

  // Not marked: Wren calls Entity's describe() binding, and C# virtual dispatch calls this override.
  public override string Describe() => $"Player {Name} with {Health} health";
}
