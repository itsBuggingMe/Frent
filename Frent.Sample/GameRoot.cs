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

namespace Frent.Sample
{
    public class GameRoot : Game, IUniformProvider
    {
        private GraphicsDeviceManager _manager;
        private Texture2D _texture = null!;
        internal SpriteBatch SpriteBatch = null!;
        private World _world;
        public MouseState MouseState;

        public GameRoot()
        {
            _world = new World(this);
            _manager = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            IsFixedTimeStep = false;
            _manager.SynchronizeWithVerticalRetrace = false;
            _manager.ApplyChanges();
        }

        protected override void Initialize()
        {
            _texture = new Texture2D(GraphicsDevice, 1, 1);
            _texture.SetData([Color.White]);

            Color[] colors = typeof(Color)
                .GetProperties(BindingFlags.Static | BindingFlags.Public)
                .Select(t => t.GetValue(null) as Color?)
                .Where(c => c is not null)
                .Select(c => c!.Value)
                .ToArray();

            for (int i = 0; i < 200_000; i++)
            {
                _world.Create<Velocity, Position, Sprite>(
                    new(Vector2.One), 
                    default,
                    new(_texture, colors[Random.Shared.Next(colors.Length)], Vector2.One * 4));
            }

            SpriteBatch = new SpriteBatch(GraphicsDevice);

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            MouseState = Mouse.GetState();
            Window.Title = $"FPS: {1000 / gameTime.ElapsedGameTime.TotalMilliseconds}";
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