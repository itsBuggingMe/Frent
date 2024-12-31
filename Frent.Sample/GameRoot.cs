using Frent;
using Frent.Buffers;
using Frent.Components;
using Frent.Collections;
using System.Diagnostics;
using System.Reflection;
using Frent.Updating.Runners;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Resources;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Frent.Sample
{
    public class GameRoot : Game, IUniformProvider
    {
        private GraphicsDeviceManager _manager;
        private Texture2D _texture = null!;
        internal SpriteBatch SpriteBatch = null!;
        private World _world;
        public MouseState MouseState;
        private FastStack<Entity> Entities;

        public GameRoot()
        {
            _world = new World(this);
            _manager = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            IsFixedTimeStep = false;
            _manager.SynchronizeWithVerticalRetrace = false;
            InactiveSleepTime = TimeSpan.Zero;
            _manager.ApplyChanges();
        }
        Color[] colors;
        protected override void Initialize()
        {
            _texture = new Texture2D(GraphicsDevice, 1, 1);
            _texture.SetData([Color.White]);

            colors = typeof(Color)
                .GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Select(t => t.GetValue(null) as Color?)
                .Where(c => c is not null)
                .Select(c => c!.Value)
                .ToArray();

            Entities = FastStack<Entity>.Create(128);

            for (int i = 0; i < 200_000; i++)
            {
                Entities.Push(_world.Create<Position, Velocity, Sprite>(
                    new() { X = 1, Y = 1 },
                    new(Vector2.One),
                    new(_texture, colors[Random.Shared.Next(colors.Length)], Vector2.One * 4)));
            }

            SpriteBatch = new SpriteBatch(GraphicsDevice);

            base.Initialize();
        }
        public float DeltaTime;
        protected override void Update(GameTime gameTime)
        {
            MouseState = Mouse.GetState();
            Window.Title = $"FPS: {1000 / gameTime.ElapsedGameTime.TotalMilliseconds}";
            for(int i = 0; i < 100; i++)
            {
                if (Entities.TryPop(out Entity entity))
                {
                    entity.Delete();
                }

                if(Entities.Count < 10)
                {
                    for (int j = 0; j < 200_000; j++)
                    {
                        Entities.Push(_world.Create<Position, Sprite>(
                            default,
                            new(_texture, colors[Random.Shared.Next(colors.Length)], Vector2.One * 4)));
                    }
                }
            }

            for(int i = 0; i < 1000; i++)
            {
                Span<Entity> e = Entities.AsSpan();
                var rand = e[Random.Shared.Next(e.Length)];

                if (!rand.Has<Velocity>())
                {
                    rand.Add<Velocity>(new(Vector2.One));
                }
            }

            DeltaTime = (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 16.666);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            //TODO: Draw systems
            SpriteBatch.Begin();
            _world.Update();
            SpriteBatch.End();
            base.Draw(gameTime);
        }

        static void Main()
        {
            using var p = new GameRoot();
            p.Run();
        }

        public T GetUniform<T>()
        {
            return (T)(object)this;
        }
    }
}