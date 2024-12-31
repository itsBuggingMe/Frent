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

    public void Update(in GameRoot game, ref Position position, ref Sprite sprite)
    {
        if (game.MouseState.RightButton == ButtonState.Pressed)
        {
            DX += Random.Shared.NextSingle() - 0.5f;
            DY += Random.Shared.NextSingle() - 0.5f;
        }

        position.X += DX * game.DeltaTime;
        position.Y += DY * game.DeltaTime;

        Rectangle bounds = game.GraphicsDevice.Viewport.Bounds;

        if (position.X < 0)
        {
            position.X = 0;
            DX = -DX;
        }
        else if (position.X > bounds.Right - sprite.Scale.X)
        {
            position.X = bounds.Right - sprite.Scale.X;
            DX = -DX;
        }

        if (position.Y < 0)
        {
            position.Y = 0;
            DY = -DY;
        }
        else if (position.Y > bounds.Bottom - sprite.Scale.Y)
        {
            position.Y = bounds.Bottom - sprite.Scale.Y;
            DY = -DY;
        }
    }

}

internal record struct Sprite(Texture2D Texture, Color Color, Vector2 Scale) : IUniformUpdateComponent<GameRoot, Position>
{
    public void Update(in GameRoot game, ref Position pos)
    {
        if(Texture is null)
        {
            Debugger.Break();
        }
        game.SpriteBatch.Draw(Texture, pos.XY, null, Color, 0, default, Scale, SpriteEffects.None, 0);
    }
}