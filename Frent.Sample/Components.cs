using Frent.Components;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Frent.Sample;

internal struct Position : IComponent
{
    public float X;
    public float Y;
}

internal struct Velocity(Vector2 v) : IUniformUpdateComponent<float, Position>
{
    public float DX = v.X;
    public float DY = v.Y;

    public void Update(in float deltaTime, ref Position arg)
    {
        arg.X += DX * deltaTime;
        arg.Y += DY * deltaTime;
    }
}

internal record struct Sprite(Texture Texture, Color Color, Vector2 Scale) : IComponent;