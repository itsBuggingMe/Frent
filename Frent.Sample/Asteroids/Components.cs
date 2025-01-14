using Frent.Components;
using Iced.Intel;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections;
using System.Transactions;
using Frent.Core;

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

internal struct Polygon(Vector2 origin, Vector2[] verticies, float thickness = 2) : IEnumerable<(Vector2 A, Vector2 B)>
{
    public float Thickness = thickness;
    public Vector2 Origin = origin;
    public Vector2[] Verticies = verticies;


    PolygonEnumerator GetEnumerator() => new PolygonEnumerator(Verticies);
    IEnumerator<(Vector2 A, Vector2 B)> IEnumerable<(Vector2 A, Vector2 B)>.GetEnumerator() => new PolygonEnumerator(Verticies);
    IEnumerator IEnumerable.GetEnumerator() => new PolygonEnumerator(Verticies);

    internal struct PolygonEnumerator(Vector2[] verticies) : IEnumerator<(Vector2 A, Vector2 B)>
    {
        private Vector2 _prev = verticies[^1];
        private Vector2[] Verticies = verticies;
        private int _index = -1;
        public (Vector2 A, Vector2 B) Current => (_prev, Verticies[_index]);
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if(_index == -1)
            {
                _index++;
                return true;
            }
            _prev = Verticies[_index];
            bool canNext = ++_index < Verticies.Length;
            return canNext;
        }

        public void Reset()
        {
            _index = 0;
            _prev = Verticies[^1];
        }
        public void Dispose() { }
    }
}

internal struct Line
{
    public float Thickness;
    public float Opacity;
    public Vector2 A;
    public Vector2 B;
}

internal struct PlayerController : IEntityUniformUpdateComponent<World, Transform, Velocity>
{
    private int _timeSinceShoot;
    private MouseState _pms;
    public Entity Camera;
    public void Update(Entity entity, in World world, ref Transform transform, ref Velocity vel)
    {
        _timeSinceShoot++;
        vel = vel.DXY * 0.95f;

        var ks = Keyboard.GetState();
        var ms = Mouse.GetState();

        Vector2 pointingDirection = Vector2.Rotate(-Vector2.UnitY, transform.Rotation);

        if (ks.IsKeyDown(Keys.W))
        {
            vel += pointingDirection * 0.60f;

            world.Create<Transform, Velocity, Triangle, DecayTimer, AngularVelocity, Tween>(
                transform,//copy the transform of the player
                pointingDirection * -8 + AsteroidsGame.RandomDirection(),//make the velocity opposite its current direction
                new(),//triangle
                new(30),//Delete after 30 frames 
                new(Random.Shared.NextSingle() - 0.5f),//some random rotational velocity 
                new Tween(TweenType.Parabolic, 30, (e, f) => //animate the triangle to grow in size, and fade out
                {
                    ref Triangle t = ref e.Get<Triangle>();
                    t.Size = f * 6 + 2;
                    t.Opacity = 1 - f;
                }));
        }

        if (ks.IsKeyDown(Keys.A))
            transform.Rotation -= 0.07f;
        if (ks.IsKeyDown(Keys.D))
            transform.Rotation += 0.07f;

        var worldMousePos = Vector2.Transform(ms.Position.ToVector2(), Matrix.Invert(Camera.Get<Camera>().View));
        var deltaToMouse = worldMousePos - transform;
        transform.Rotation = MathF.Atan2(deltaToMouse.Y, deltaToMouse.X) + MathHelper.PiOver2;
        int delta = _pms.ScrollWheelValue - ms.ScrollWheelValue;
        if (delta != 0)
            transform.Rotation += Math.Sign(delta) * 0.1f;

        if (_timeSinceShoot > 10 && ks.IsKeyDown(Keys.Space))
        {
            AsteroidsGame.ShootBullet(entity, pointingDirection, 24);
            _timeSinceShoot = 0;
        }

        _pms = ms;
    }
}

internal struct EnemyController(Entity target) : IEntityUpdateComponent<Transform, Velocity>
{
    public Entity Target = target;
    private int _shootTimer;
    public void Update(Entity entity, ref Transform arg1, ref Velocity arg2)
    {
        if (!Target.IsAlive())
            return;
        var pointTo = -Vector2.Normalize(arg1.XY - Target.Get<Transform>());
        arg2 += pointTo * 0.01f;
        arg2 = arg2.DXY * 0.99f;
        _shootTimer++;
        if(_shootTimer > 120)
        {
            _shootTimer = 0;
            AsteroidsGame.ShootBullet(entity, pointTo, 4);
        }
    }
}

internal struct FollowEntity(Entity toFollow, float smoothing = 0.02f) : IUpdateComponent<Transform>
{
    public Entity Follow = toFollow;
    public float Smoothing = smoothing;

    public void Update(ref Transform arg)
    {
        if (Follow.IsAlive())
            arg -= (arg.XY - Follow.Get<Transform>()) * Smoothing;
    }
}

internal struct Camera : IUniformUpdateComponent<Viewport, Transform>
{
    public Matrix View;
    public void Update(in Viewport uniform, ref Transform transform)
    {
        int width = uniform.Width;
        int height = uniform.Height;
        View = Matrix.CreateTranslation(-transform.X + width / 2, -transform.Y + height / 2, 0);
    }
}

internal struct Triangle
{
    public float Size;
    public float Opacity;
}

internal struct AngularVelocity(float dt)  : IUpdateComponent<Transform>
{
    //delta theta???
    public float DT = dt;

    public void Update(ref Transform arg)
    {
        arg.Rotation += DT;
    }
}

internal struct CircleCollision
{
    public float Radius;
    public Entity CollidesWith;

    public static bool Intersects(Vector2 aPos, CircleCollision a, Vector2 bPos, CircleCollision b)
    {
        float radiiSum = a.Radius + b.Radius;
        return Vector2.DistanceSquared(aPos, bPos) <= radiiSum * radiiSum;
    }
}

internal struct BulletBehavior(Entity entity) : IUpdateComponent<CircleCollision>
{
    public Entity Parent = entity;
    public void Update(ref CircleCollision arg)
    {
        if(!arg.CollidesWith.IsNull && arg.CollidesWith != Parent && arg.CollidesWith.IsAlive() && arg.CollidesWith.Tagged<Shootable>())
        {
            AsteroidsGame.BlowUpEntity(arg.CollidesWith);
            arg.CollidesWith.Delete();
            arg.CollidesWith = default;
        }
    }
}

internal struct Asteroid;
internal struct Shootable;