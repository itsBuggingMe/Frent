using Microsoft.Xna.Framework;
using Frent.Core;
using FrentSandbox;

namespace Frent.Sample.Asteroids;

internal static class AsteroidsHelper
{
    public static void Shoot(this Entity entity, Vector2 direction, float speed)
    {
        var bullet = entity.World.Create<Transform, Velocity, Line, DecayTimer, BulletBehavior, CircleCollision>(
            entity.Get<Transform>().XY,
            direction * speed + entity.Get<Velocity>(),
            new() { A = direction * 7, Thickness = 2, Opacity = 1 },
            new((int)(120 / (speed / 24f))),
            new(entity),
            new() { Radius = 12 }
            );

        bullet.OnDelete += e =>
        {

        };
    }

    public static void Explode(this Entity entity)
    {
        if (entity.TryGet(out Ref<Polygon> polygon) && entity.TryGet(out Ref<Transform> transform))
        {
            World w = entity.World;

            float scale = transform.Value.Scale;
            float sine = MathF.Sin(transform.Value.Rotation);
            float cos = MathF.Cos(transform.Value.Rotation);

            foreach (var (a, b) in polygon.Value)
            {
                var a1 = Rotate(a - polygon.Value.Origin) + transform.Value.XY;
                var b1 = Rotate(b - polygon.Value.Origin) + transform.Value.XY;
                var lineCenter = (a1 + b1) * 0.5f;
                var distanceFromPolyCenter = lineCenter - transform.Value.XY;

                w.Create<Line, Transform, Velocity, AngularVelocity, DecayTimer, Tween>(
                    new() { A = a1 - lineCenter, B = b1 - lineCenter, Thickness = polygon.Value.Thickness, Opacity = 1 },
                    transform.Value.XY + distanceFromPolyCenter,
                    (Vector2.Normalize(distanceFromPolyCenter) + RandomDirection()) * 4,
                    new((Random.Shared.NextSingle() - 0.5f)),
                    new(30),
                    new Tween(TweenType.Parabolic, 30, (e, f) =>
                    {
                        ref var l = ref e.Get<Line>();
                        l.Thickness *= 1.01f;
                        l.Opacity *= 1 - f;
                    }));
            }

            Vector2 Rotate(Vector2 value)
            {
                return new Vector2(value.X * cos - value.Y * sine, value.X * sine + value.Y * cos) * scale;
            }
        }
    }

    public static Vector2 RandomDirection()
    {
        return Vector2.Rotate(Vector2.UnitX, RandomSingle(0, MathHelper.TwoPi));
    }

    public static float RandomSingle(float min, float max)
    {
        return float.Lerp(min, max, Random.Shared.NextSingle());
    }
}
