using Frent.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using Frent.Core;
using Frent.Sample.Asteroids;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Frent.Sample
{
    public class GameRoot : Game, IUniformProvider
    {
        private GraphicsDeviceManager _manager;
        private World _world;


        public Texture2D PixelTexture = null!;
        public MouseState MouseState;
        public SpriteBatch SpriteBatch = null!;
        public float DeltaTime;
        private int _count;
        List<Entity> entities = new List<Entity>();

        public GameRoot(int count)
        {
            _count = count;
            _world = new World(this, Config.Singlethreaded);
            _manager = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            IsFixedTimeStep = false;
            _manager.SynchronizeWithVerticalRetrace = false;
            InactiveSleepTime = TimeSpan.Zero;
            _manager.ApplyChanges();
        }

        Color[] colors = null!;

        protected override void Initialize()
        {
            PixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            PixelTexture.SetData([Color.White]);

            colors = typeof(Color)
                .GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Select(t => t.GetValue(null) as Color?)
                .Where(c => c is not null)
                .Select(c => c!.Value)
                .ToArray();

            for (int i = 0; i < _count; i++)
            {
                entities.Add(_world.Create<Position, Velocity, Friction, Bounds, SinglePixel, MouseController>(
                    new Vector2(Random.Shared.Next(_manager.PreferredBackBufferWidth), Random.Shared.Next(_manager.PreferredBackBufferHeight)),
                    Vector2.Normalize(new(Random.Shared.NextSingle() - 0.5f, Random.Shared.NextSingle() - 0.5f)),
                    0.99f,
                    new Vector2(5),
                    colors[Random.Shared.Next(colors.Length)],
                    default));
            }

            SpriteBatch = new SpriteBatch(GraphicsDevice);

            base.Initialize();
        }
        protected override void Update(GameTime gameTime)
        {
            MouseState = Mouse.GetState();
            Window.Title = $"FPS: {1000 / gameTime.ElapsedGameTime.TotalMilliseconds} T: {ThreadPool.ThreadCount} Entities: {entities.Count}";
            DeltaTime = (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 16.666);

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                for (int i = 0; i < 100; i++)
                {
                    Entity e;
                    entities.Add(e = _world.CreateFromObjects([
                        new Position(MouseState.Position.ToVector2()),
                    (Velocity)Vector2.Normalize(new(Random.Shared.NextSingle() - 0.5f, Random.Shared.NextSingle() - 0.5f)),
                    (Friction)0.99f,
                    (Bounds)new Vector2(5),
                    (SinglePixel)colors[Random.Shared.Next(colors.Length)],
                    default(MouseController)]));

                    e.Tag<UserCreated>();
                }
            }
            else
            {
                DeleteRandomEntity();
            }

            _world.Update();

            Vector256<float> deltaTime = Vector256.Create(1f);

            foreach((Span<Position> positions, Span<Velocity> velocities) in _world
                .Query<With<Position>, With<Velocity>>()
                .EnumerateChunks<Position, Velocity>())
            {
                //8 floats/vec
                //2 floats/comp
                //4 comps/vec

                int len = positions.Length - positions.Length & 3;
                int vecCount = len >> 2;
                ref Vector256<float> positionVectors = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<Position, Vector256<float>>(positions));
                ref Vector256<float> velocityVectors = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<Velocity, Vector256<float>>(velocities));

                for (int i = 0; i < vecCount; i++)
                {
                    positionVectors = Fma.MultiplyAdd(velocityVectors, deltaTime, positionVectors);
                    positionVectors = ref Unsafe.Add(ref positionVectors, 1);
                    velocityVectors = ref Unsafe.Add(ref velocityVectors, 1);
                }

                for (int i = len; i < positions.Length; i++)
                {
                    positions[i].XY += velocities[i].DXY;
                }
            }

            foreach ((Ref<Velocity> vel, Ref<Position> pos, Ref<Bounds> bounds) in _world
                .Query<With<Velocity>, With<Position>, With<Bounds>>()
                .Enumerate<Velocity, Position, Bounds>())
            {
                Rectangle window = GraphicsDevice.Viewport.Bounds;
                Vector2 half = bounds.Value.Size * 0.5f;
                Vector2 topLeft = pos.Value.XY - half;
                Vector2 bottomRight = pos.Value.XY + half;

                if (topLeft.X <= 0)
                {
                    pos.Value.XY.X = half.X + 1;
                    vel.Value.DXY.X *= -1;
                }

                if (bottomRight.X >= window.Width)
                {
                    pos.Value.XY.X = window.Width - half.X - 1;
                    vel.Value.DXY.X *= -1;
                }

                if (topLeft.Y <= 0)
                {
                    pos.Value.XY.Y = half.Y + 1;
                    vel.Value.DXY.Y *= -1;
                }

                if (bottomRight.Y >= window.Height)
                {
                    pos.Value.XY.Y = window.Height - half.Y - 1;
                    vel.Value.DXY.Y *= -1;
                }
            }
            //_world.Query<With<Velocity, Position, Bounds>>().ParallelUniform<QueryCollison, GameRoot, Velocity, Position, Bounds>(default);



            base.Update(gameTime);
        }

        private void DeleteRandomEntity()
        {
            if (entities.Count > 0)
            {
                int index = Random.Shared.Next(entities.Count);
                entities[index].Delete();
                entities.RemoveAt(index);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            SpriteBatch.Begin();
            foreach((Ref<SinglePixel> pix, Ref<Position> pos, Ref<Bounds> bounds) in _world
                .Query<With<SinglePixel>, With<Position>, With<Bounds>>()
                .Enumerate<SinglePixel, Position, Bounds>())
            {
                Vector2 topLeft = pos.Value.XY - bounds.Value.Size * 0.5f;
                SpriteBatch.Draw(PixelTexture, new Rectangle(topLeft.ToPoint(), bounds.Value.Size.ToPoint()), pix.Value.Color);
            }
            SpriteBatch.End();
            base.Draw(gameTime);
        }
        public T GetUniform<T>() => (T)(object)this;
    }

    internal struct UserCreated;
}