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

            var e1 = _world.Create<Position, Sprite>(
                new() { X = 1, Y = 1 },
                new(_texture, colors[Random.Shared.Next(colors.Length)], Vector2.One * 4));
            var e2 = _world.Create<Position, Sprite>(
                new() { X = 1, Y = 1 },
                new(_texture, colors[Random.Shared.Next(colors.Length)], Vector2.One * 4));

            e1.Add<Velocity>(default);
            e2.Add<Velocity>(default);

            SpriteBatch = new SpriteBatch(GraphicsDevice);

            base.Initialize();
        }
        public float DeltaTime;
        protected override void Update(GameTime gameTime)
        {
            MouseState = Mouse.GetState();
            Window.Title = $"FPS: {1000 / gameTime.ElapsedGameTime.TotalMilliseconds}";

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