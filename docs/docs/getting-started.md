# Getting Started

Frent is on Nuget!

> [!CAUTION]
> Frent is still in alpha. There may be bugs and the API is changing
> Treat the currently available packages as a demo

```pwsh
dotnet add package Frent --version 0.2.3-alpha
```


### Code Sample

```csharp
using Frent;
using System;
using Frent.Components;

class Program
{
    static void Main()
    {
        //create a world
        World world = new World();
        
        //
        Entity entity = world.Create<Position, Velocity, Sprite>(
            new(4, 8),
            new(2, 0),
            new('@')
            );

        do
        {
            Console.Clear();
            world.Update();
        } while(Console.ReadLine() != "exit")

        //dispose of the world after done
        world.Dispose();
    }
}

struct Position(int x, int y)
{
    public int X = x;
    public int Y = y;
}

struct Velocity(int dx, int dy) : IUpdateComponent<Position>
{
    public int DX = dx;
    public int DY = dy;

    public void Update(ref Position pos)
    {
        pos.X += DX;
        pos.Y += DY;
    }
}

struct Sprite(char value) : IUpdateComponent<Position>
{
    public char Sprite = value;

    public void Update(ref Position pos)
    {
        Console.SetCursorPosition(pos.X, pos.Y);
        Console.Write(Sprite);
    }
}
```