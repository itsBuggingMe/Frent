using Frent.Components;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace Frent.Sample;

internal struct Position : IComponent
{
    public float X;
    public float Y;
    public Vector2 XY => new Vector2(X, Y);
}

internal struct Velocity(Vector2 v) : IUniformUpdateComponent<GameRoot, Position, Sprite>
{
    public float DX = v.X;
    public float DY = v.Y;

    public void Update(in GameRoot game, ref Position arg, ref Sprite sprite)
    {
        if(game.MouseState.RightButton == ButtonState.Pressed)
        {
            DX += Random.Shared.NextSingle() - 0.5f;
            DY += Random.Shared.NextSingle() - 0.5f;
        }

        arg.X += DX;
        arg.Y += DY;

        Rectangle bounds = game.GraphicsDevice.Viewport.Bounds;

        if (arg.X < 0)
        {
            arg.X = 0;
            DX *= -1;
        }
        if (arg.Y < 0)
        {
            arg.Y = 0;
            DY *= -1;
        }
        if (arg.X > bounds.Right - sprite.Scale.X)
        {
            arg.X = bounds.Right - sprite.Scale.X;
            DX *= -1;
        }
        if (arg.Y > bounds.Bottom - sprite.Scale.Y)
        {
            arg.Y = bounds.Bottom - sprite.Scale.Y;
            DY *= -1;
        }
    }
}

internal record struct Sprite(Texture2D Texture, Color Color, Vector2 Scale) : IUniformUpdateComponent<GameRoot, Position>
{
    public void Update(in GameRoot game, ref Position pos)
    {
        game.SpriteBatch.Draw(Texture, pos.XY, null, Color, 0, default, Scale, SpriteEffects.None, 0);
    }
}