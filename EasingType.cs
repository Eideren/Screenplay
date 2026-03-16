using System;
using UnityEngine;

namespace Screenplay
{
    public enum EasingType
    {
        Linear,
        In, Out, InOut,
        InBack, OutBack, InOutBack,
        InExpo, OutExpo, InOutExpo,
        InBounce, OutBounce, InOutBounce,
        InElastic, OutElastic, InOutElastic
    }

    public static class EasingExtension
    {
        // https://easings.net

        public static float EaseIn(float t) => t * t * t;

        public static float EaseOut(float t) => 1f-EaseIn(1f-t);
        public static float EaseInOut(float t) => Mathf.Lerp(EaseIn(t), EaseOut(t), t);

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            float omt = t - 1f;
            return 1f + c3 * omt*omt*omt + c1 * omt*omt;
        }

        public static float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            return c3 * t * t * t - c1 * t * t;
        }

        public static float EaseInOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;

            return t < 0.5f
                ? (MathF.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
                : (MathF.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
        }

        public static float EaseInExpo(float x) => x == 0 ? 0 : MathF.Pow(2, 10 * x - 10);

        public static float EaseOutExpo(float x) => x == 1 ? 1 : MathF.Pow(2, -10 * x);
        public static float EaseInOutExpo(float x)
        {
            return x == 0f
                ? 0f
                : x == 1f
                    ? 1f
                    : x < 0.5f ? MathF.Pow(2f, 20f * x - 10f) / 2f
                        : (2f - MathF.Pow(2f, -20f * x + 10f)) / 2f;
        }

        public static float EaseInElastic(float x) {
            const float c4 = (2f * MathF.PI) / 3f;

            return x == 0f
                ? 0f
                : x == 1f
                    ? 1f
                    : -MathF.Pow(2f, 10f * x - 10f) * MathF.Sin((x * 10f - 10.75f) * c4);

        }

        public static float EaseOutElastic(float x) {
            const float c4 = (2f * MathF.PI) / 3f;

            return x == 0f
                ? 0f
                : x == 1f
                    ? 1f
                    : MathF.Pow(2f, -10f * x) * MathF.Sin((x * 10f - 0.75f) * c4) + 1f;

        }

        public static float EaseInOutElastic(float x) {
            const float c5 = (2f * MathF.PI) / 4.5f;

            return x == 0f
                ? 0
                : x == 1f
                    ? 1
                    : x < 0.5
                        ? -(MathF.Pow(2f, 20f * x - 10f) * MathF.Sin((20f * x - 11.125f) * c5)) / 2f
                        : (MathF.Pow(2f, -20f * x + 10f) * MathF.Sin((20f * x - 11.125f) * c5)) / 2f + 1f;

        }

        public static float EaseInBounce(float x) {
            return 1 - EaseOutBounce(1 - x);
        }

        public static float EaseOutBounce(float x) {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (x < 1 / d1) {
                return n1 * x * x;
            } else if (x < 2 / d1) {
                return n1 * (x -= 1.5f / d1) * x + 0.75f;
            } else if (x < 2.5 / d1) {
                return n1 * (x -= 2.25f / d1) * x + 0.9375f;
            } else {
                return n1 * (x -= 2.625f / d1) * x + 0.984375f;
            }
        }

        public static float EaseInOutBounce(float x) {
            return x < 0.5f
                ? (1f - EaseOutBounce(1f - 2f * x)) / 2f
                : (1f + EaseOutBounce(2f * x - 1f)) / 2f;
        }

        public static float Apply(this EasingType easing, float t)
        {
            return easing switch
            {
                EasingType.Linear => t,
                EasingType.In => EaseIn(t),
                EasingType.Out => EaseOut(t),
                EasingType.InOut => EaseInOut(t),
                EasingType.InBack => EaseInBack(t),
                EasingType.OutBack => EaseOutBack(t),
                EasingType.InOutBack => EaseInOutBack(t),
                EasingType.InExpo => EaseInExpo(t),
                EasingType.OutExpo => EaseOutExpo(t),
                EasingType.InOutExpo => EaseInOutExpo(t),
                EasingType.InBounce => EaseInBounce(t),
                EasingType.OutBounce => EaseOutBounce(t),
                EasingType.InOutBounce => EaseInOutBounce(t),
                EasingType.InElastic => EaseInElastic(t),
                EasingType.OutElastic => EaseOutElastic(t),
                EasingType.InOutElastic => EaseInOutElastic(t),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
