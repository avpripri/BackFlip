using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BackFlip
{
    public class Chevrons3
    {
        /// <summary>
        /// The boundary of the unrotated chevron control
        /// </summary>
        public Size2F Size { get; set; }

        /// <summary>
        /// Maximum angle of attach aka... Alpha  This is also the clean-configuration stall angle of attack
        /// </summary>
        public float alphaMax { get; set; }

        /// <summary>
        /// The ideal angle of attack for the current flight conditions 
        /// </summary>
        public float alphaTarget { get; set; }

        public float alphaActual { get; set; }

        // Used internally
        private struct ChevStruct { public float x0; public float y0; public float y1; }


        static readonly Vector4 Red =   new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        static readonly Vector4 Green = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        static readonly Vector4 Blue =  new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
        static readonly Vector4 Yellow = new Vector4(1.0f, 1.0f, 0.0f, 1.0f);
        static readonly Vector4[] Colors = new [] { Blue, Green, Red, Yellow };

        internal Vector4[] Make()
        {
            var vanishingX = Size.Width * 3.0f;

            var width_2 = Size.Width / 2.0f;

            var dyTarget = (float)((alphaMax - alphaTarget) * Size.Height / alphaMax);
            var m0 = dyTarget / (width_2 + vanishingX);
            var m1 = (Size.Height - dyTarget) / (width_2/2 + vanishingX);

            var dyAlpha = dyTarget - ((float)((alphaMax - alphaActual) * Size.Height / alphaMax));

            var dx = 0.1f; //  Size.Height / Size.Width / 2;
            var z = -1.0f;

            var pts = new[] { 0f, dyTarget, Size.Height }.Select(y => new { y, mChev = (float)(dyTarget - y) / (float)(width_2 + vanishingX) })
                .Select(pt => new ChevStruct { x0 = (float)(width_2 - (Size.Height - pt.y) * dx), y0 = pt.y - dyTarget, y1 = pt.y + (width_2 * pt.mChev) - dyTarget })
                .ToArray();

            // Construct a simple array of all the verticies w/colors in the AoA chevrons (there are 8 total)
            var rightSides = Enumerable.Range(0, 3).SelectMany(i => new[] { new Vector4(pts[i].x0, pts[i].y0, z, 1f), Colors[i] });
            var leftSides  = Enumerable.Range(0, 3).SelectMany(i => new[] { new Vector4(-pts[i].x0, pts[i].y0, z, 1f), Colors[i] });
            var verticies = rightSides.Concat(leftSides)
                .Concat(new[] {
                        new Vector4(0f, pts[0].y1,  z, 1f), Colors[0] ,  // Middles
                        new Vector4(0f, pts[2].y1,  z, 1f), Colors[2] }).ToArray();

            // This is the ordered list of vertex that assemble into the triangle list that makes up the chevron
            var chevrons = new[] {
                6,1,0,
                7,2,1,
                3,4,6,
                5,7,4,
                6,4,1,
                4,7,1,
            }.SelectMany(i => new[] { verticies[i * 2], verticies[i * 2 + 1] });

            float dyBar = Size.Height/300f;

            var targetBar = new[]
            {
                new Vector4(-pts[1].x0, pts[1].y0 + dyBar,  z-0.5f, 1f), Yellow ,  
                new Vector4(+pts[1].x0, pts[1].y0 + dyBar,  z-0.5f, 1f), Yellow ,  
                new Vector4(+pts[1].x0, pts[1].y0 - dyBar,  z-0.5f, 1f), Yellow ,

                new Vector4(-pts[1].x0, pts[1].y0 + dyBar,  z-0.5f, 1f), Yellow ,
                new Vector4(+pts[1].x0, pts[1].y0 - dyBar,  z-0.5f, 1f), Yellow ,
                new Vector4(-pts[1].x0, pts[1].y0 - dyBar,  z-0.5f, 1f), Yellow ,
            };

            float dofSize = Size.Width / 5f;

            var directionOfFlight = new[]
            {
                new Vector4(-dofSize,   dyAlpha,            z-0.9f, 1.0f), Blue ,
                new Vector4(0,          dyAlpha + (dofSize/3),z-0.9f, 1.0f), Blue ,
                new Vector4(+dofSize,   dyAlpha,            z-0.9f, 1.0f), Blue ,
            };

            return directionOfFlight.Concat(targetBar).Concat(chevrons).ToArray();
        }


        public static Vector4 HorizontalFlip(Vector4 vect)
        {
            var arr = vect.ToArray();
            arr[0] *= -1;
            return new Vector4(arr);
        }
    }
}