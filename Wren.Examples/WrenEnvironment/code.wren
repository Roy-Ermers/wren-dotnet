import "engine" for Vector3, Logger, Events, Direction, Entity, Player, Records

var vector1 = Vector3.new(1, 2, 3)
var vector2 = Vector3.new(2, 4, 6)

var addition = vector1 + vector2
var multiplication = vector1 * vector2
var subtraction = vector1 - vector2
var division = vector1 / vector2
var mod = vector1 % vector2

var equal = Vector3.equals(vector1, vector2)

Logger.log("hello")
Logger.log(5)

System.print("Equal: %(equal)")
System.print("Addition: %(addition.toString)")
System.print("Multiplication: %(multiplication.toString)")
System.print("Subtraction: %(subtraction.toString)")
System.print("Division: %(division.toString)")
System.print("Modulo: %(mod.toString)")



var total = 0
Events.onTick {|dt|
  total = total + dt
  System.print("tick %(dt), total %(total)")
}

Events.onMove {|position| position + Vector3.new(1, 0, 0) }

// Indexer: Vector3's `this[int index]` is Wren's subscript operator.
var indexed = Vector3.new(1, 2, 3)
indexed[2] = 30
System.print("Indexer: %(indexed[0]), %(indexed[1]), %(indexed[2])")

// Unary operator: Vector3's `operator -(Vector3 v)`.
System.print("Negated: %(-vector1)")

System.print("Parsed: %(Vector3.parse("4,5,6"))")
System.print("Parsed invalid: %(Vector3.parse("nope"))")
System.print("Parsed null: %(Vector3.parse(null))")

// Enum: a plain Wren class with a static getter per member, and an indexer for the reverse lookup.
System.print("Direction.east: %(Direction.east), Direction[2]: %(Direction[2]), Direction[9]: %(Direction[9])")
System.print("From direction: %(Vector3.fromDirection(Direction.west))")

// Inheritance: Entity.spawn is declared to return an Entity but returns a Player, so Wren gets a Player.
var hero = Entity.spawn("Ann")
hero.damage(30)                                        // inherited from Entity
System.print("Spawned a Player: %(hero is Player)")    // true
System.print("%(hero.name): %(hero.describe())")      // Player's override of Entity.describe()

var record = Records.getRecord()
Records.consumeRecord(record)
