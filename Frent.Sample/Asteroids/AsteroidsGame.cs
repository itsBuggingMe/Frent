using Apos.Shapes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace Frent.Sample.Asteroids;

public class AsteroidsGame : Game
{
    private World _world;
    private GraphicsDeviceManager _manager;
    private DefaultUniformProvider _uniformProvider;
    private ShapeBatch _sb = null!;
    private Vector2[][] _asteroids;
    private InputState _inputs = new();
    private Entity _player;

    public AsteroidsGame()
    {
        _manager = new GraphicsDeviceManager(this);
        _manager.GraphicsProfile = GraphicsProfile.HiDef;
        Content.RootDirectory = "Content";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;

        _uniformProvider = new();
        _world = new World(_uniformProvider);
        _asteroids = Enumerable.Range(0, 16).Select(i => GenerateAsteroid()).ToArray();
    }

    protected override void Initialize()
    {
        _uniformProvider.Add(_sb = new ShapeBatch(GraphicsDevice, Content));
        _uniformProvider.Add(_world);
        _uniformProvider.Add(_inputs);
        base.Initialize();

        int width = GraphicsDevice.Viewport.Width;
        int height = GraphicsDevice.Viewport.Height;

        _player = _world.Create<Transform, Velocity, Polygon, PlayerController>((width / 2, height / 2), default, new Polygon(default, 
            [
                Vector2.UnitY * -25,
                new Vector2(10, 10),
                Vector2.Zero,
                new Vector2(-10, 10),
            ]), default);
    }

    int timeSinceLastAsteroid = 0;

    protected override void Update(GameTime gameTime)
    {
        _inputs.Previous = _inputs.Current;
        _inputs.Current = Mouse.GetState();
        _inputs.CurrentKeyboard = Keyboard.GetState();

        timeSinceLastAsteroid++;
        if(timeSinceLastAsteroid >= 30)
        {
            timeSinceLastAsteroid = 0;
            int width = GraphicsDevice.Viewport.Width;
            int height = GraphicsDevice.Viewport.Height;
            var playerPos = _player.Get<Transform>();
            var e = _world.Create<Transform, Velocity, Polygon>(
                (RandomSingle(playerPos.X, playerPos.X + width), RandomSingle(playerPos.Y, playerPos.Y +  height)), 
                RandomDirection(), 
                new Polygon(default, _asteroids[Random.Shared.Next(_asteroids.Length)])
                );
        }

        _world.Update();
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        Transform transform = _player.Get<Transform>();
        int width = GraphicsDevice.Viewport.Width;
        int height = GraphicsDevice.Viewport.Height;
        _sb.Begin(view: Matrix.CreateTranslation(-transform.X + width / 2, -transform.Y + height / 2, 0));

        _world.QueryUniform((in ShapeBatch sb, ref Line l) => sb.FillLine(l.A, l.B, l.Thickness, Color.White));
        _world.QueryUniform((in ShapeBatch sb, ref Polygon l, ref Transform position) =>
        {
            Debug.Assert(l.Thickness != 0);
            var verticies = l.Verticies;

            float sine = MathF.Sin(position.Rotation);
            float cos = MathF.Cos(position.Rotation);

            Vector2 prev = prev = Rotate(verticies[0] - l.Origin) + position;
            for (int i = 1; i < verticies.Length; i++)
            {
                sb.FillLine(prev, prev = Rotate(verticies[i] - l.Origin) + position, l.Thickness, Color.White, aaSize: 2);
            }
            sb.FillLine(prev, prev = Rotate(verticies[0] - l.Origin) + position, l.Thickness, Color.White);

            Vector2 Rotate(Vector2 value)
            {
                return new Vector2(value.X * cos - value.Y * sine, value.X * sine + value.Y * cos);
            }
        });
        _sb.End();
        base.Draw(gameTime);
    }

    private Vector2[] GenerateAsteroid(int edges = 16)
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

    private static Vector2 RandomDirection()
    {
        return Vector2.Rotate(Vector2.UnitX, RandomSingle(0, MathHelper.TwoPi));
    }

    private static float RandomSingle(float min, float max)
    {
        return float.Lerp(min, max, Random.Shared.NextSingle());
    }
}

internal class InputState
{
    public MouseState Current { get; set; }
    public KeyboardState CurrentKeyboard { get; set; }
    public MouseState Previous { get; set; }
}