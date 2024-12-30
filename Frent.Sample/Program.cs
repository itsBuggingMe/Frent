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

namespace Frent.Sample
{
    public class Program : Game, IUniformProvider
    {
        private GraphicsDeviceManager _manager;
        private Texture2D _texture = null!;
        private World _world;

        public Program()
        {
            _world = new World(this);
            _manager = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _texture = new Texture2D(GraphicsDevice, 1, 1);
            for(int i = 0; i < 100_000; i++)
            {
                _world.Create<Velocity, Position, Sprite>(
                    new(Vector2.Normalize(new(Random.Shared.NextSingle(), Random.Shared.NextSingle()))), 
                    new() { X = Random.Shared.Next(_manager.PreferredBackBufferWidth), Y = Random.Shared.Next(_manager.PreferredBackBufferHeight) },
                    new(_texture, Color.Red, Vector2.One));
            }

            var e = _world.Create<Velocity, Position, Sprite>(
                new(Vector2.Normalize(new(Random.Shared.NextSingle(), Random.Shared.NextSingle()))),
                new() { X = Random.Shared.Next(_manager.PreferredBackBufferWidth), Y = Random.Shared.Next(_manager.PreferredBackBufferHeight) },
                new(_texture, Color.Red, Vector2.One));
            e.Add<Sprite>(new(_texture, Color.Red, Vector2.One));

            base.Initialize();
        }

        private float _deltaTime;
        protected override void Update(GameTime gameTime)
        {
            _deltaTime = (float)(gameTime.ElapsedGameTime.TotalMilliseconds / (1 / 0.06));
            _world.Update();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            //TODO: Draw systems
            base.Draw(gameTime);
        }

        static void Main()
        {
            using var p = new Program();
            p.Run();
        }

        public T GetUniform<T>() => (T)(object)_deltaTime;
    }
}