using System;
using System.Collections.Generic;
using System.Drawing;

namespace HashlistGenerator.HashProcessing
{
    public class HeroIdentifier
    {
        private static readonly AllHero Heroes = new AllHero();

        public static Tuple<int, double> Identify(Bitmap bitmap, List<EachHero> heroInfo)
        {
            return Heroes.GetAns(GetHash(bitmap), heroInfo);
        }

        public static string GetHash(Bitmap bitmap)
        {
            var img1 = new ImgHash(bitmap);
            return img1.GetHash();
        }
    }
}
