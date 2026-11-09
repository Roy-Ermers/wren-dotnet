class Direction {
    static north { 0 }
    static east { 1 }
    static south { 2 }
    static west { 3 }
    static [value] {
        if (__names == null) __names = {0: "north", 1: "east", 2: "south", 3: "west"}
        return __names[value]
    }
}

foreign class Entity {
    foreign static new(health)
    foreign damage(amount)
    foreign describe()
    foreign static spawn(name)
    foreign health
    foreign health=(value)
}
foreign class Events {
    foreign static onTick(handler)
    foreign static onMove(mover)
}
foreign class Logger {
    foreign static log(message)
    foreign static error(message)
}
foreign class Player {
    foreign static new(name, health)
    foreign damage(amount)
    foreign describe()
    foreign static spawn(name)
    foreign name
    foreign name=(value)
    foreign health
    foreign health=(value)
}
foreign class Records {
    foreign static getRecord()
    foreign static consumeRecord(record)
}
foreign class Test {
    foreign static new(a, b)
    foreign a
    foreign b
}
foreign class Vector3 {
    foreign static new(x, y, z)
    foreign static new()
    foreign [index]
    foreign [index]=(value)
    foreign getLength()
    foreign toString
    foreign +(v2)
    foreign *(v2)
    foreign -(v2)
    foreign static fromDirection(direction)
    foreign static parse(text)
    foreign -
    foreign /(v2)
    foreign %(v2)
    foreign >(v2)
    foreign <(v2)
    foreign static equals(v1, v2)
    foreign static fromScalar(s)
    foreign x
    foreign x=(value)
    foreign y
    foreign y=(value)
    foreign z
    foreign z=(value)
    foreign static zero
    foreign static one
}
