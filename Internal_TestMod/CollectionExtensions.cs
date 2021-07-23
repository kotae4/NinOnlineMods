using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NinMods
{
    public static class CollectionExtensions
    {
        public static IEnumerable<T> SymmetricExcept<T>(this IEnumerable<T> seq1, IEnumerable<T> seq2)
        {
            HashSet<T> hashSet = new HashSet<T>(seq1);
            hashSet.SymmetricExceptWith(seq2);
            return hashSet.Select(x => x);
        }
    }
}
