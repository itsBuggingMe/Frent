using Apos.Shapes;
using Frent.Core;
using Frent.Sample.Asteroids.Editor.UI;
using Frent.Systems;
using FrentSandbox;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;

namespace Frent.Sample.Asteroids;

public partial class AsteroidsGame : Game
{
    private GraphicsDeviceManager _graphics;

    private RenderTarget2D _rt;
    private SpriteBatch _spriteBatch;
    private ShapeBatch _shapeBatch;
    private SpriteFont _font;
    private Camera _camera;
    private Entity _cameraController;
    private Entity _player;
    private Texture2D _whitePixel;

    private World _world = new();
    private DefaultUniformProvider _uniformProvider = new();

    private StringBuilder _sb = new();
    private ImGuiRenderer _imGuiRenderer;

    internal Texture2D Pixel => _whitePixel;
    internal SpriteBatch SpriteBatch => _spriteBatch;
    internal Vector2 DisplaySize => GraphicsDevice.Viewport.Bounds.Size.ToVector2();

    internal Dictionary<string, Entity> AllEntitiesForward { get; } = [];
    internal Dictionary<Entity, string> AllEntitiesBackwards { get; } = [];

    public static AsteroidsGame Instance { get; private set; } = null!;

    private Vector2[][] _polygons;

    #region Display Settings

    public bool DisplayGrid { get; set; } = true;
    public bool Paused { get; set; } = false;

    #endregion

#pragma warning disable 8618
    public AsteroidsGame()
#pragma warning restore 8618
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        _world.UniformProvider = _uniformProvider;

        Instance = this;
        _polygons = Enumerable.Range(3, 7).Select(GeneratePolygon).ToArray();
    }

    protected override void Initialize()
    {
        _uniformProvider
            .Add(_spriteBatch = new SpriteBatch(GraphicsDevice))
            .Add(_font = Content.Load<SpriteFont>("Font"))
            .Add(_camera = new Camera(this))
            .Add(_whitePixel = new Texture2D(GraphicsDevice, 1, 1))
            .Add(_shapeBatch = new ShapeBatch(GraphicsDevice, Content))
            .Add(_world)
            .Add(Content)
            .Add(this)
            ;
        _rt = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        Window.ClientSizeChanged += Window_ClientSizeChanged;

        _imGuiRenderer = new ImGuiRenderer(this);
        _imGuiRenderer.RebuildFontAtlas();

        _whitePixel.SetData([Color.White]);

        _world.EntityCreated += static e =>
        {
            if(e.Has<CameraControl>())
            {
                Instance._cameraController = e;
            }

            const string EntityNameChars = "0123456789abcdef";
            Span<char> buff = stackalloc char[16];
            foreach (ref char c in buff)
            {
                c = EntityNameChars[Random.Shared.Next(EntityNameChars.Length)];
            }

            string name = new string(buff);
            Instance.AllEntitiesForward.Add(name, e);
            Instance.AllEntitiesBackwards.Add(e, name);
        };

        _world.ComponentAdded += static (e, id) =>
        {
            if(id == Component<CameraControl>.ID)
            {
                Instance._cameraController = e;
            }
        };

        _world.EntityDeleted += static e =>
        {
            string name = Instance.AllEntitiesBackwards[e];
            Instance.AllEntitiesForward.Remove(name);
            Instance.AllEntitiesBackwards.Remove(e);

            if(e.Has<EnemyController>())
            {
                Instance._enemyCount--;
            }
        };

        CreateNewPlayer();

        base.Initialize();
    }


    private void CreateNewPlayer()
    {
        _player = _world.Create<Transform, Velocity, Polygon, PlayerController, CircleCollision>((0, 0), default, new Polygon(default,
        [
            Vector2.UnitY * -25,
            new Vector2(10, 10),
            Vector2.Zero,
            new Vector2(-10, 10),
        ]), default, new() { Radius = 25 });
        _player.Tag<Shootable>();
    }

    private void Window_ClientSizeChanged(object? sender, EventArgs e)
    {
        _rt.Dispose();
        _rt = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
    }

    int _enemyCount;

    protected override void Update(GameTime gameTime)
    {   
        InputHelper.TickUpdate(IsActive);
        if (InputHelper.RisingEdge(Keys.Q))
            Paused = !Paused;
        if (InputHelper.RisingEdge(Keys.E))
            DisplayGrid = !DisplayGrid;
        if (!ImGui.GetIO().WantCaptureMouse)
            _camera.Update();
        if(!Paused)
            _world.Update<TickAttribute>();
        if(_cameraController.TryGet(out Ref<CameraControl> cameraControl))
        {
            _camera.Position = -cameraControl.Value.Location;
        }

        if(!Paused && _enemyCount < 20 && Random.Shared.Next(60) == 0)
        {
            _enemyCount++;
            int width = GraphicsDevice.Viewport.Width;
            int height = GraphicsDevice.Viewport.Height;
            var playerPos = _player.Get<Transform>();
            var e = _world.Create<Transform, Velocity, Polygon, CircleCollision, EnemyController>(
                AsteroidsHelper.RandomDirection() * 2000 + playerPos,
                default,
                new Polygon(default, _polygons[Random.Shared.Next(_polygons.Length)]),
                new() { Radius = 28 },
                new(_player)
                );
            e.Tag<Shootable>();
        }

        Query collidables = _world.Query<With<CircleCollision>, With<Transform>, Not<BulletBehavior>>();
        Query bullets = _world.Query<With<CircleCollision>, With<Transform>, With<BulletBehavior>>();
        foreach ((Entity entity1, Ref<CircleCollision> collision1, Ref<Transform> trans1) in collidables.EnumerateWithEntities<CircleCollision, Transform>())
        {
            foreach ((Entity entity2, Ref<CircleCollision> collision2, Ref<Transform> trans2) in bullets.EnumerateWithEntities<CircleCollision, Transform>())
            {
                if(entity1 != entity2)
                {
                    if(CircleCollision.Intersects(
                        trans1.Value, collision2.Value,
                        trans2.Value, collision2.Value))
                    {
                        collision2.Value.CollidesWith = entity1;
                    }
                }
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_rt);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();
        DrawGrid();
        _spriteBatch.End();

        _shapeBatch.Begin(view: _camera.Transform);
        _spriteBatch.Begin(transformMatrix: _camera.Transform);
        _world.Update<DrawAttribute>();
        _spriteBatch.End();
        _shapeBatch.End();

        _imGuiRenderer.BeforeLayout(gameTime);
        ImGuiLayout();
        _imGuiRenderer.AfterLayout();

        GraphicsDevice.SetRenderTarget(null);
        _spriteBatch.Begin();
        _spriteBatch.Draw(_rt, Vector2.Zero, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }



    private void DrawGrid()
    {
        if (!DisplayGrid)
            return;

        const int GridIncrements = 500;

        Color lineColor = new Color(64, 64, 64);

        var invertCamera = Matrix.Invert(_camera.Transform);

        Vector2 start = Vector2.Transform(-new Vector2(GridIncrements), invertCamera);
        Vector2 end = Vector2.Transform(DisplaySize + new Vector2(GridIncrements), invertCamera);
        Vector2 origin = Vector2.Transform(default, _camera.Transform);

        for (float x = (int)(start.X / GridIncrements) * GridIncrements; x < end.X; x += GridIncrements)
        {
            Vector2 transformedStart = Vector2.Transform(new Vector2(x, start.Y), _camera.Transform);
            Vector2 transformedEnd = Vector2.Transform(new Vector2(x, end.Y), _camera.Transform);

            _spriteBatch.Draw(
                _whitePixel,
                new Rectangle((int)transformedStart.X, 0, 1, (int)DisplaySize.Y),
                lineColor
            );

            if (_camera.Scale > 0.15f)
            {
                _spriteBatch.DrawString(_font, _sb.Append(x), new Vector2(transformedStart.X, origin.Y), Color.White);
                _sb.Clear();
            }
        }

        for (float y = (int)(start.Y / GridIncrements) * GridIncrements; y < end.Y; y += GridIncrements)
        {
            Vector2 transformedStart = Vector2.Transform(new Vector2(start.X, y), _camera.Transform);
            Vector2 transformedEnd = Vector2.Transform(new Vector2(end.X, y), _camera.Transform);

            _spriteBatch.Draw(
                _whitePixel,
                new Rectangle(0, (int)transformedStart.Y, (int)DisplaySize.X, 1),
                lineColor
            );

            if (_camera.Scale > 0.15f)
            {
                _spriteBatch.DrawString(_font, _sb.Append(y), new Vector2(origin.X, transformedStart.Y), Color.White);
                _sb.Clear();
            }
        }
    }


    internal class Camera(AsteroidsGame game)
    {
        private AsteroidsGame _game = game;

        public Matrix Transform =>
            Matrix.CreateTranslation(Position.X, Position.Y, 0) *
            Matrix.CreateScale(Scale, Scale, 1) *
            Matrix.CreateTranslation(_game.DisplaySize.X * 0.5f, _game.DisplaySize.Y * 0.5f, 0)
            ;

        public Vector2 Position { get; set; } = Vector2.Zero;
        public float Scale { get; set; } = 2;

        public void Update()
        {
            if (InputHelper.Down(MouseButton.Left))
                Position += (InputHelper.MouseLocation.ToVector2() - InputHelper.PrevMouseState.Position.ToVector2()) / Scale;
            if (InputHelper.DeltaScroll != 0)
                Scale *= InputHelper.DeltaScroll < 0 ? 0.9f : 1 / 0.9f;
        }
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
}