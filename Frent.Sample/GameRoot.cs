﻿using Frent;
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
            _world = new World(this);
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
            Window.Title = $"FPS: {1000 / gameTime.ElapsedGameTime.TotalMilliseconds}";
            DeltaTime = (float)(gameTime.ElapsedGameTime.TotalMilliseconds / 16.666);

            if(Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                for(int i = 0; i < 100; i++)
                {
                    Entity e;
                    entities.Add(e = _world.CreateFromObjects([
                        new Position(MouseState.Position.ToVector2()),
                    (Velocity)Vector2.Normalize(new(Random.Shared.NextSingle() - 0.5f, Random.Shared.NextSingle() - 0.5f)),
                    (Friction)0.99f,
                    (Bounds)new Vector2(5),
                    (SinglePixel)colors[Random.Shared.Next(colors.Length)],
                    default(MouseController)]));
                }
            }
            else
            {
                DeleteRandomEntity();
            }

            _world.Update();
            _world.InlineQueryUniform<QueryCollison, GameRoot, Velocity, Position, Bounds>(default);

            base.Update(gameTime);
        }

        private void DeleteRandomEntity()
        {
            if(entities.Count > 0)
            {
                int index = Random.Shared.Next(entities.Count);
                entities[index].Delete();
                entities.RemoveAt(index);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            //TODO: Draw systems
            SpriteBatch.Begin();
            QuerySprites querySprites = new QuerySprites(this);
            _world.InlineQuery<QuerySprites, SinglePixel, Position, Bounds>(querySprites);
            SpriteBatch.End();
            base.Draw(gameTime);
        }

        static void Main(string[] args)
        {
            MethodInfo[] methods = typeof(Samples).GetMethods().Where(m => m.GetCustomAttribute<SampleAttribute>() is not null).ToArray();
            Console.WriteLine($"Pick a sample: 0-{methods.Length}");
            Console.WriteLine("[0] Monogame Square Sample");
            for(int i = 0; i < methods.Length; i++)
            {
                Console.WriteLine($"[{i + 1}] {methods[i].Name.Replace('_', ' ')}");
            }

            int userOption;
            while (!int.TryParse(Console.ReadLine(), out userOption) || userOption > methods.Length || userOption < 0)
                Console.WriteLine("Write a valid input");

            if(userOption == 0)
            {
                using var p = new GameRoot(args.Length == 0 ? 100_000 : int.Parse(args[0]));
                p.Run();
            }
            else
            {
                methods[userOption - 1].Invoke(null, []);
            }

            Console.WriteLine("\n\nSample Completed. Press Enter to exit");
            Console.ReadLine();
        }

        public T GetUniform<T>() => (T)(object)this;
    }

    internal struct QuerySprites(GameRoot gameRoot) : IQuery<SinglePixel, Position, Bounds>
    {
        public void Run(ref SinglePixel singlePixel, ref Position position, ref Bounds bounds)
        {
            Vector2 topLeft = position.XY - bounds.Size * 0.5f;
            gameRoot.SpriteBatch.Draw(gameRoot.PixelTexture, new Rectangle(topLeft.ToPoint(), bounds.Size.ToPoint()), singlePixel.Color);
        }
    }

    internal struct QueryCollison : IQueryUniform<GameRoot, Velocity, Position, Bounds>
    {
        public void Run(in GameRoot gameRoot, ref Velocity velocity, ref Position position, ref Bounds bounds)
        {
            Rectangle window = gameRoot.GraphicsDevice.Viewport.Bounds;
            Vector2 half = bounds.Size * 0.5f;
            Vector2 topLeft = position.XY - half;
            Vector2 bottomRight = position.XY + half;

            if (topLeft.X <= 0)
            {
                position.XY.X = half.X + 1;
                velocity.DXY.X *= -1;
            }

            if (bottomRight.X >= window.Width)
            {
                position.XY.X = window.Width - half.X - 1;
                velocity.DXY.X *= -1;
            }

            if (topLeft.Y <= 0)
            {
                position.XY.Y = half.Y + 1;
                velocity.DXY.Y *= -1;
            }

            if (bottomRight.Y >= window.Height)
            {
                position.XY.Y = window.Height - half.Y - 1;
                velocity.DXY.Y *= -1;
            }
        }
    }
}