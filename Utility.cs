using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Screenplay
{
    public static class Utility
    {
        public static Color WithAlpha(this Color c, float a)
        {
            c.a = a;
            return c;
        }

        public static void ToItsRight(this in Rect refRect, out Rect newRect, float width)
        {
            newRect = refRect;
            newRect.width = width;
            newRect.x += refRect.width;
        }

        public static void ToItsLeft(this in Rect refRect, out Rect newRect, float width)
        {
            newRect = refRect;
            newRect.width = width;
            newRect.x -= width;
        }

        public static void Split(this Rect r, Span<Rect> rects)
        {
            r.width /= rects.Length;
            for (int i = 0; i < rects.Length; i++)
            {
                rects[i] = r;
                r.x += r.width;
            }
        }

        public static void Split(this in Rect r, out Rect left, out Rect right)
        {
            Span<Rect> span = stackalloc Rect[2];
            r.Split(span);
            left = span[0];
            right = span[1];
        }

        public static void SplitRatio(this in Rect r, float ratio, out Rect left, out Rect right)
        {
            left = r;
            right = r;
            left.width *= ratio;
            right.width = r.width - left.width;
            right.x += left.width;
        }

        public static void SplitWithRightOf(this in Rect r, float sizeRight, out Rect left, out Rect right)
        {
            left = r;
            right = r;
            left.width -= sizeRight;
            right.width = sizeRight;
            right.x += left.width;
        }

        public static void SplitWithLeftOf(this in Rect r, float sizeLeft, out Rect left, out Rect right)
        {
            left = r;
            right = r;
            left.width = sizeLeft;
            right.width -= sizeLeft;
            right.x += left.width;
        }

        public static Rect WithMargin(this Rect r, (float x, float y) size)
        {
            r.xMin += size.x;
            r.xMax -= size.x;
            r.yMin += size.y;
            r.yMax -= size.y;
            return r;
        }

        public static string EmptyToNull(this string s) => s == "" ? null : s;
    }
}