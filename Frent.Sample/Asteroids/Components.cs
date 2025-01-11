using Frent.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Frent.Sample.Asteroids;

internal struct DecayTimer(int Frames) : IEntityUpdateComponent
{
    public void Update(Entity entity)
    {
        if(--Frames < 0)
            entity.Delete();
    }
}

internal struct Transform(float x, float y)
{
    public float X = x;
    public float Y = y;
    public float Rotation = 0;
    public float Scale = 1;

    public readonly Vector2 XY => new(X, Y);

    public static implicit operator (float, float)(Transform pos) => (pos.X, pos.Y);
    public static implicit operator Transform((float X, float Y) pos) => new(pos.X, pos.Y);
    public static implicit operator Vector2(Transform pos) => new(pos.X, pos.Y);
    public static implicit operator Transform(Vector2 pos) => new(pos.X, pos.Y);
}

internal struct Velocity(float dx, float dy) : IUpdateComponent<Transform>
{
    public float DX = dx;
    public float DY = dy;
    public readonly Vector2 DXY => new Vector2(DX, DY);
    public void Update(ref Transform arg)
    {
        arg.X += DX;
        arg.Y += DY;
    }

    public static implicit operator (float, float)(Velocity pos) => (pos.DX, pos.DY);
    public static implicit operator Velocity((float X, float Y) pos) => new(pos.X, pos.Y);
    public static implicit operator Vector2(Velocity pos) => new(pos.DX, pos.DY);
    public static implicit operator Velocity(Vector2 pos) => new(pos.X, pos.Y);
}

internal struct Polygon(Vector2 origin, Vector2[] verticies, float thickness = 1)
{
    public float Thickness = thickness;
    public Vector2 Origin = origin;
    public Vector2[] Verticies = verticies;
}

internal struct Line
{
    public float Thickness;
    public Vector2 A;
    public Vector2 B;
}

internal struct PlayerController : IUniformUpdateComponent<InputState, Transform, Velocity>
{
    public void Update(in InputState ms, ref Transform transform, ref Velocity vel)
    {
        if (ms.CurrentKeyboard.IsKeyDown(Keys.W))
            vel -= Vector2.Rotate(Vector2.UnitY, transform.Rotation) * 0.15f;

        if (ms.CurrentKeyboard.IsKeyDown(Keys.A))
            transform.Rotation -= 0.07f;
        if (ms.CurrentKeyboard.IsKeyDown(Keys.D))
            transform.Rotation += 0.07f;
    }
}