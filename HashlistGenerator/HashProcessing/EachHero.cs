using System.Collections.Generic;

namespace HashlistGenerator.HashProcessing
{
    public class EachHero
    {
        public EachHero()
        {
            DHash = new List<string>();
        }

        public int Id { get; set; }

        public List<string> DHash { get; set; }
    }
}