﻿using CommandLine;
using Frent.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Frent.Sample;

internal struct Position(Vector2 xy)
{
    public Vector2 XY = xy;
    public static implicit operator Position(Vector2 xy) => new(xy);
    public static implicit operator Position((float x, float y) pos) => new Vector2(pos.x, pos.y);
}
internal record struct Friction(float Coefficient)
{
    public static implicit operator Friction(float co) => new(co);
}
internal record struct Bounds(Vector2 Size)
{
    public static implicit operator Bounds(Vector2 s) => new(s);
}
internal record struct SinglePixel(Color Color)
{
    public static implicit operator SinglePixel(Color co) => new(co);
}
internal record struct Velocity(Vector2 dxy) : IUniformUpdateComponent<GameRoot, Position, Friction>
{
    public Vector2 DXY = dxy;
    public static implicit operator Velocity(Vector2 dxy) => new(dxy);
    public void Update(in GameRoot uniform, ref Position pos, ref Friction friction)
    {
        pos.XY += DXY * uniform.DeltaTime;
        DXY *= friction.Coefficient;
    }
}
internal record struct MouseController : IUniformUpdateComponent<GameRoot, Velocity, Position>
{
    public void Update(in GameRoot uniform, ref Velocity vel, ref Position arg)
    {
        if (uniform.MouseState.RightButton == ButtonState.Pressed)
            vel.DXY += NormalizeSafe(arg.XY - uniform.MouseState.Position.ToVector2()) * 0.1f;
        if (uniform.MouseState.LeftButton == ButtonState.Pressed)
            vel.DXY -= NormalizeSafe(arg.XY - uniform.MouseState.Position.ToVector2()) * 0.1f;
    }

    private static Vector2 NormalizeSafe(Vector2 vector)
    {
        if (vector == default)
            return Vector2.Zero;
        return Vector2.Normalize(vector);
    }
}