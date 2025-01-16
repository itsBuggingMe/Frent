using Frent.Components;
using System.Runtime.CompilerServices;

namespace Frent.Sample.Asteroids;

internal struct Tween(TweenType type, float length, Action<Entity, float> action) : IEntityComponent
{
    public TweenType Type = type;
    public float StartValue = 0;
    public float EndValue = 1;
    public float Length = length;
    float DT;

    [Tick]
    public void Update(Entity entity)
    {
        DT += 1;

        float normalized = DT / Length;
        if(normalized > 1)
        {
            action(entity, ComputeAnimatedValue(1));
            entity.Remove<Tween>();
            return;
        }

        action(entity, ComputeAnimatedValue(normalized));
    }

    private float ComputeAnimatedValue(float t)
    {
        return Type switch
        {
            TweenType.Linear => t,
            TweenType.Parabolic => ParabolicInterpolation(t),
            TweenType.Cubic => CubicInterpolation(t),
            TweenType.InverseCubic => InverseCubicInterpolation(t),
            TweenType.Sigmoid => SigmoidInterpolation(t),
            TweenType.InverseParabolic => InverseParabolicInterpolation(t),
            TweenType.EaseInOutQuad => EaseInOutQuadInterpolation(t),
            TweenType.EaseInOutExpo => EaseInOutExpoInterpolation(t),
            TweenType.EaseInOutQuart => EaseInOutQuartInterpolation(t),
            TweenType.EaseOutBounce => EaseOutBounceInterpolation(t),
            TweenType.EaseInOutBounce => EaseInOutBounceInterpolation(t),
            TweenType.EaseInBounce => EaseInBounceInterpolation(t),
            TweenType.Quart => QuarticInterpolation(t),
            TweenType.InverseQuart => InverseQuarticInterpolation(t),
            _ => throw new ArgumentException("No Tween Type implementaion"),
        } * (EndValue - StartValue) + StartValue;
    }

    #region Tweens
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ParabolicInterpolation(float t) => t * t;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CubicInterpolation(float t) => t * t * t;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float QuarticInterpolation(float t) => t * t * t * t;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InverseParabolicInterpolation(float t) => 1 - (1 - t) * (1 - t);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InverseCubicInterpolation(float t) => 1 - (1 - t) * (1 - t) * (1 - t);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float InverseQuarticInterpolation(float t) => 1 - (1 - t) * (1 - t) * (1 - t) * (1 - t);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SigmoidInterpolation(float t) => 1 / (1 + MathF.Exp(10 * (-t + 0.5f)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInOutQuadInterpolation(float x) => x < 0.5 ? 2 * x * x : 1 - MathF.Pow(-2 * x + 2, 2) / 2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInOutQuartInterpolation(float x) => x < 0.5f ? 8 * x * x * x * x : 1 - MathF.Pow(-2 * x + 2, 4) / 2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInOutExpoInterpolation(float x) => x == 0 ? 0 : x == 1 ? 1 : x < 0.5 ? MathF.Pow(2, 20 * x - 10) / 2 : (2 - MathF.Pow(2, -20 * x + 10)) / 2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseOutBounceInterpolation(float x) => x switch { < 1 / 2.75f => 7.5625f * x * x, < 2 / 2.75f => 7.5625f * (x -= 1.5f / 2.75f) * x + 0.75f, < 2.5f / 2.75f => 7.5625f * (x -= 2.25f / 2.75f) * x + 0.9375f, _ => 7.5625f * (x -= 2.625f / 2.75f) * x + 0.984375f };
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInOutBounceInterpolation(float x) => x < 0.5f ? (1 - EaseOutBounceInterpolation(1 - 2 * x)) / 2 : (1 + EaseOutBounceInterpolation(2 * x - 1)) / 2;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EaseInBounceInterpolation(float x) => 1 - EaseOutBounceInterpolation(1 - x);
    #endregion Tweens
}

/// <summary>
/// Used to detemines the Tween style
/// </summary>
internal enum TweenType : byte
{
    //Original Case
    Linear,
    Parabolic,
    Cubic,
    InverseCubic,
    Quart,
    InverseQuart,

    //Bad one
    Sigmoid,

    //New cast
    InverseParabolic,
    EaseInOutQuad,
    EaseInOutQuart,
    EaseInOutExpo,
    EaseOutBounce,
    EaseInOutBounce,
    EaseInBounce,

    //Wildcard
    None,
}