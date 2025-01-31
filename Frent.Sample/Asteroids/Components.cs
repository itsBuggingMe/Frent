using Apos.Shapes;
using Frent.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections;
using System.Diagnostics;
using Frent.Sample.Asteroids.Editor;
using static Frent.Sample.Asteroids.AsteroidsGame;

namespace Frent.Sample.Asteroids;

[Editor]
internal struct DecayTimer(int frames) : IEntityComponent
{
    public int Frames = frames;
    [Tick]
    public void Update(Entity entity)
    {
        if (--Frames < 0)
            entity.Delete();
    }
}

[Editor]
internal struct Transform(float x, float y) : IComponentBase
{
    public Vector2 XY = new Vector2(x, y);
    public float Rotation = 0;
    public float Scale = 1;
    [EditorExclude]
    public float X { readonly get => XY.X; set => XY.X = value; }
    [EditorExclude]
    public float Y { readonly get => XY.Y; set => XY.Y = value; }
    public static implicit operator (float, float)(Transform pos) => (pos.X, pos.Y);
    public static implicit operator Transform((float X, float Y) pos) => new(pos.X, pos.Y);
    public static implicit operator Vector2(Transform pos) => new(pos.X, pos.Y);
    public static implicit operator Transform(Vector2 pos) => new(pos.X, pos.Y);
}

[Editor]
internal struct Velocity(float dx, float dy) : IComponent<Transform>
{
    public float DX { readonly get => DXY.X; set => DXY.X = value; }
    public float DY { readonly get => DXY.Y; set => DXY.Y = value; }

    public Vector2 DXY = new(dx, dy);

    [Tick]
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

[Editor]
internal struct Polygon(Vector2 origin, Vector2[] verticies, float thickness = 2) : IEnumerable<(Vector2 A, Vector2 B)>, IUniformComponent<ShapeBatch, Transform>
{
    public float Thickness = thickness;
    public Vector2 Origin = origin;
    public Color Color = Color.White;
    public Vector2[] Verticies = verticies;

    [Draw]
    public void Update(ShapeBatch sb, ref Transform position)
    {
        Debug.Assert(Thickness != 0);
        var verticies = Verticies;

        float scale = position.Scale;
        float sine = MathF.Sin(position.Rotation);
        float cos = MathF.Cos(position.Rotation);

        foreach (var (a, b) in this)
        {
            sb.FillLine(Rotate(a - Origin) + position.XY, Rotate(b - Origin) + position.XY, Thickness, Color);
        }

        Vector2 Rotate(Vector2 value)
        {
            return new Vector2(value.X * cos - value.Y * sine, value.X * sine + value.Y * cos) * scale;
        }
    }

    public PolygonEnumerator GetEnumerator() => new PolygonEnumerator(Verticies);
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
            if (_index == -1)
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

[Editor]
internal struct Line : IUniformComponent<ShapeBatch, Transform>
{
    public float Thickness;
    public float Opacity;
    public Vector2 A;
    public Vector2 B;

    [Draw]
    public void Update(ShapeBatch sb, ref Transform pos)
    {
        sb.FillLine(Vector2.Rotate(A, pos.Rotation) + pos.XY, Vector2.Rotate(B, pos.Rotation) + pos.XY, Thickness, Color.White * Opacity);
    }
}

[Editor]
internal struct PlayerController : IEntityUniformComponent<World, Transform, Velocity>
{
    private int _timeSinceShoot;
    private MouseState _pms;

    [Tick]
    public void Update(Entity entity, World world, ref Transform transform, ref Velocity vel)
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
                pointingDirection * -8 + AsteroidsHelper.RandomDirection(),//make the velocity opposite its current direction
                new(),//triangle
                new(30),//Delete after 30 frames 
                new(Random.Shared.NextSingle() - 0.5f),//some random rotational velocity 
                new Tween(TweenType.Parabolic, 30, (e, f) => //animate the triangle to grow in size, and fade out
                {
                    ref Triangle t = ref e.Get<Triangle>();
                    e.Get<Transform>().Scale = f * 6 + 2;
                    t.Color.A = (byte)((1 - f) * 255);
                }));
        }

        if (ks.IsKeyDown(Keys.A))
            transform.Rotation -= 0.07f;
        if (ks.IsKeyDown(Keys.D))
            transform.Rotation += 0.07f;

        var worldMousePos = Vector2.Transform(ms.Position.ToVector2(), Matrix.Invert(world.UniformProvider.GetUniform<Camera>().Transform));
        var deltaToMouse = worldMousePos - transform;
        transform.Rotation = MathF.Atan2(deltaToMouse.Y, deltaToMouse.X) + MathHelper.PiOver2;
        int delta = _pms.ScrollWheelValue - ms.ScrollWheelValue;
        if (delta != 0)
            transform.Rotation += Math.Sign(delta) * 0.1f;

        if (_timeSinceShoot > 10 && ks.IsKeyDown(Keys.Space))
        {
            entity.Shoot(pointingDirection, 24);
            _timeSinceShoot = 0;
        }

        _pms = ms;
    }
}

[Editor]
internal struct EnemyController(Entity target) : IEntityComponent<Transform, Velocity>
{
    public Entity Target = target;
    private int _shootTimer;

    [Tick]
    public void Update(Entity entity, ref Transform arg1, ref Velocity arg2)
    {
        if (!Target.IsAlive)
            return;
        var pointTo = -Vector2.Normalize(arg1.XY - Target.Get<Transform>());
        arg2 += pointTo * 0.01f;
        arg2 = arg2.DXY * 0.99f;
        _shootTimer++;
        if (_shootTimer > 120)
        {
            _shootTimer = 0;
            entity.Shoot(pointTo, 4);
        }
    }
}

[Editor]
internal struct FollowEntity(Entity toFollow, float smoothing = 0.02f) : IComponent<Transform>
{
    public Entity Follow = toFollow;
    public float Smoothing = smoothing;

    [Tick]
    public void Update(ref Transform arg)
    {
        if (Follow.IsAlive)
        {
            var x = (arg.XY - Follow.Get<Transform>()) * Smoothing;
            arg.XY -= x;
        }
    }
}

[Editor]
internal struct CameraControl : IComponent<Transform>
{
    public Vector2 Location;
    [Tick]
    public void Update(ref Transform transform)
    {
        Location = transform.XY;
    }
}

[Editor]
internal struct Triangle() : IUniformComponent<ShapeBatch, Transform>
{
    public Color Color = Color.White;
    public float RotationOffset;

    public float RotationOffsetDegrees 
    { 
        get => MathHelper.ToDegrees(RotationOffset);
        set => RotationOffset = MathHelper.ToRadians(value);
    }

    [Draw]
    public void Update(ShapeBatch sb, ref Transform pos)
    {
        sb.FillEquilateralTriangle(pos.XY, pos.Scale, Color * (Color.A / 255f), rotation: pos.Rotation + RotationOffset);
    }
}

[Editor]
internal struct AngularVelocity(float dt) : IComponent<Transform>
{
    //delta theta???
    public float DT = dt;

    [Tick]
    public void Update(ref Transform arg)
    {
        arg.Rotation += DT;
    }
}

[Editor]
internal struct CircleCollision : IComponentBase
{
    public float Radius;
    public Entity CollidesWith;

    public static bool Intersects(Vector2 aPos, CircleCollision a, Vector2 bPos, CircleCollision b)
    {
        float radiiSum = a.Radius + b.Radius;
        return Vector2.DistanceSquared(aPos, bPos) <= radiiSum * radiiSum;
    }
}

[Editor]
internal struct BulletBehavior(Entity entity) : IComponent<CircleCollision>
{
    public Entity Parent = entity;

    [Tick]
    public void Update(ref CircleCollision arg)
    {
        if (!arg.CollidesWith.IsNull && arg.CollidesWith != Parent && arg.CollidesWith.IsAlive && arg.CollidesWith.Tagged<Shootable>())
        {
            arg.CollidesWith.Explode();
            arg.CollidesWith.Delete();
            arg.CollidesWith = default;
        }
    }
}

internal struct Asteroid;
internal struct Shootable;