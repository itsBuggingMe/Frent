using Apos.Shapes;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using Frent.Core;
using Frent.Systems;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Parsers.IIS_Trace;

namespace Frent.Sample.Asteroids;

public class AsteroidsGame : Game
{
    private World _world;
    private GraphicsDeviceManager _manager;
    private DefaultUniformProvider _uniformProvider;
    private ShapeBatch _sb = null!;
    private Vector2[][] _asteroidPolys;
    private Vector2[] _enemyPoly;
    private Entity _player;
    private Entity _camera;
    private List<Entity> _enemies = new();

    public AsteroidsGame()
    {
        _manager = new GraphicsDeviceManager(this);
        _manager.GraphicsProfile = GraphicsProfile.HiDef;
        Content.RootDirectory = "Content";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;

        _uniformProvider = new();
        _world = new World(_uniformProvider);
        _asteroidPolys = Enumerable.Range(0, 16).Select(i => GenerateAsteroid()).ToArray();
        _enemyPoly = GeneratePolygon(5);
    }

    protected override void Initialize()
    {
        base.Initialize();

        _uniformProvider.Add(_sb = new ShapeBatch(GraphicsDevice, Content));
        _uniformProvider.Add(_world);
        _uniformProvider.Add(GraphicsDevice.Viewport);

        Reset();
    }

    private void Reset()
    {
        foreach(var item in _enemies)
        {
            if(item.IsAlive())
            {
                item.Delete();
            }
        }
        _enemies.Clear();

        _player = _world.Create<Transform, Velocity, Polygon, PlayerController, CircleCollision>((0, 0), default, new Polygon(default,
        [
            Vector2.UnitY * -25,
            new Vector2(10, 10),
            Vector2.Zero,
            new Vector2(-10, 10),
        ]), default, new() { Radius = 25 });
        _player.Tag<Shootable>();

        _camera = _world.Create<FollowEntity, Transform, Camera>(new(_player), _player.Get<Transform>(), _camera.IsAlive() && _camera.TryGet(out Ref<Camera> c) ? c.Component : default);
        _player.Get<PlayerController>().Camera = _camera;
    }

    int timeSinceLastAsteroid = 0;

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        _uniformProvider.Add(GraphicsDevice.Viewport);


        timeSinceLastAsteroid++;
        if(timeSinceLastAsteroid >= 15 && _player.IsAlive() && _enemies.Count < 300)
        {
            timeSinceLastAsteroid = 0;

            if(Random.Shared.Next() % 2 == 0)
            {
                int width = GraphicsDevice.Viewport.Width;
                int height = GraphicsDevice.Viewport.Height;
                var playerPos = _player.Get<Transform>();
                var e = _world.Create<Transform, Velocity, Polygon, CircleCollision>(
                    RandomDirection() * 2000 + playerPos,
                    RandomDirection(),
                    new Polygon(default, _asteroidPolys[Random.Shared.Next(_asteroidPolys.Length)]),
                    new() { Radius = 16 }
                    );
                e.Tag<Asteroid>();
                e.Tag<Shootable>();
                
                _enemies.Add(e);
            }
            else
            {
                int width = GraphicsDevice.Viewport.Width;
                int height = GraphicsDevice.Viewport.Height;
                var playerPos = _player.Get<Transform>();
                var e = _world.Create<Transform, Velocity, Polygon, CircleCollision, EnemyController>(
                    RandomDirection() * 2000 + playerPos,
                    default,
                    new Polygon(default, _enemyPoly),
                    new() { Radius = 28 },
                    new(_player)
                    );
                e.Tag<Shootable>();
                
                _enemies.Add(e);
            }
        }
        else
        {
            for(int i = _enemies.Count - 1; i >= 0; i--)
            {
                if(!_enemies[i].IsAlive())
                {
                    _enemies.RemoveAt(i);
                }
            }
        }

        if(!_player.IsAlive())
        {
            Reset();

            foreach(var e in _enemies.Where(e => e.IsAlive()))
            {
                e.Get<EnemyController>().Target = _player;
            }
        }

        _world.Query<With<CircleCollision, Transform>>()
            .InlineEntityUniform<InlineOuterCollisionQuery, World, CircleCollision, Transform>(default);
        _world.Update<TickAttribute>();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _sb.Begin(view: _camera.Get<Camera>().View);

        DrawGrid();
        _world.Update<DrawAttribute>();

        _sb.End();
        base.Draw(gameTime);
    }

    private void DrawGrid()
    {
        const int GridInterval = 200;
        Matrix screenToWorld = Matrix.Invert(_camera.Get<Camera>().View);
        Vector2 topLeft = Vector2.Transform(default, screenToWorld);
        Vector2 bottomRight = Vector2.Transform(GraphicsDevice.Viewport.Bounds.Size.ToVector2(), screenToWorld);

        for (int i = (int)(topLeft.X / GridInterval) * GridInterval - GridInterval; i <= bottomRight.X + GridInterval; i += GridInterval)
        {
            for (int j = (int)(topLeft.Y / GridInterval) * GridInterval - GridInterval; j <= bottomRight.Y + GridInterval; j += GridInterval)
            {
                _sb.FillCircle(new Vector2(i, j), 2, Color.White);
            }
        }
    }

    private Vector2[] GenerateAsteroid(int edges = 8)
    {
        Vector2[] verts = new Vector2[edges];
        float rot = 0;
        for(int i = 0; i < verts.Length; i++)
        {
            verts[i] = Vector2.Rotate(Vector2.UnitX * (Random.Shared.NextSingle() + 3) * 8, rot);
            rot += MathHelper.TwoPi / edges;
        }
        return verts;
    }
    private Vector2[] GeneratePolygon(int edges = 8)
    {
        Vector2[] verts = new Vector2[edges];
        float rot = 0;
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = Vector2.Rotate(Vector2.UnitX * 48, rot);
            rot += MathHelper.TwoPi / edges;
        }
        return verts;
    }

    public static Vector2 RandomDirection()
    {
        return Vector2.Rotate(Vector2.UnitX, RandomSingle(0, MathHelper.TwoPi));
    }

    private static float RandomSingle(float min, float max)
    {
        return float.Lerp(min, max, Random.Shared.NextSingle());
    }

    public static void ShootBullet(Entity entity, Vector2 direction, float speed)
    {
        entity.World.Create<Transform, Velocity, Line, DecayTimer, BulletBehavior, CircleCollision>(
            entity.Get<Transform>().XY,
            direction * speed + entity.Get<Velocity>(),
            new() { A = direction * 7, Thickness = 2, Opacity = 1 },
            new((int)(120 / (speed / 24f))),
            new(entity),
            new() { Radius = 12 }
            );
    }

    public static void BlowUpEntity(Entity entity)
    {
        if(entity.TryGet(out Ref<Polygon> polygon) && entity.TryGet(out Ref<Transform> transform))
        {
            World w = entity.World;

            float scale = transform.Component.Scale;
            float sine = MathF.Sin(transform.Component.Rotation);
            float cos = MathF.Cos(transform.Component.Rotation);

            foreach (var (a, b) in polygon.Component)
            {
                var a1 = Rotate(a - polygon.Component.Origin) + transform.Component.XY;
                var b1 = Rotate(b - polygon.Component.Origin) + transform.Component.XY;
                var lineCenter = (a1 + b1) * 0.5f;
                var distanceFromPolyCenter = lineCenter - transform.Component.XY;

                w.Create<Line, Transform, Velocity, AngularVelocity, DecayTimer, Tween>(
                    new() { A = a1 - lineCenter, B = b1 - lineCenter, Thickness = polygon.Component.Thickness, Opacity = 1 },
                    transform.Component.XY + distanceFromPolyCenter,
                    (Vector2.Normalize(distanceFromPolyCenter) + RandomDirection()) * 4,
                    new((Random.Shared.NextSingle() - 0.5f)),
                    new(30),
                    new Tween(TweenType.Parabolic, 30, (e, f) =>
                    {
                        ref var l = ref e.Get<Line>();
                        l.Thickness *= 1.01f;
                        l.Opacity *=  1 - f;
                    }));
            }

            Vector2 Rotate(Vector2 value)
            {
                return new Vector2(value.X * cos - value.Y * sine, value.X * sine + value.Y * cos) * scale;
            }
        }
    }
}


internal struct InlineOuterCollisionQuery : IEntityUniformAction<World, CircleCollision, Transform>
{
    public void Run(Entity entity, World uniform, ref CircleCollision arg1, ref Transform arg2)
    {
        uniform.Query<With<CircleCollision, Transform>>()
            .InlineEntity<InlineCollisionQuery, CircleCollision, Transform>(new(entity, arg1, arg2));
    }
}

internal struct InlineCollisionQuery(Entity original, CircleCollision originalCollison, Vector2 location) : IEntityAction<CircleCollision, Transform>
{
    public void Run(Entity entity, ref CircleCollision arg, ref Transform pos)
    {
        if (original == entity)
            return;
        if (CircleCollision.Intersects(location, originalCollison, pos, arg))
        {
            arg.CollidesWith = original;
        }
    }
}