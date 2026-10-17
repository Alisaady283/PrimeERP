using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Application.Services.Entities
{
    /// <summary>الشجرة من صفوفٍ مسطّحة</summary>
    public static class Tree
    {
        /// <summary>المطابق وأسلافه</summary>
        public static HashSet<TKey> WithAncestors<T, TKey>(IEnumerable<T> all, IEnumerable<T> matched, Func<T, TKey> key,
            Func<T, TKey> parent) where T : class
        {
            var byKey = all.ToDictionary(key);
            var keep = new HashSet<TKey>();
            foreach (var item in matched)
                for (var current = item; current != null && keep.Add(key(current));)
                    current = parent(current) is { } up && byKey.TryGetValue(up, out var next) ? next : null;
            return keep;
        }

        /// <summary>العقد بأبنائها</summary>
        public static List<TNode> Build<T, TKey, TNode>(IEnumerable<T> items, Func<T, TKey> key, Func<T, TKey> parent,
            Func<T, List<TNode>, TNode> node)
        {
            var list = items.ToList();
            var keys = list.Select(key).ToHashSet();
            var children = list.ToLookup(parent);

            List<TNode> Level(IEnumerable<T> level) => level.Select(item => node(item, Level(children[key(item)]))).ToList();

            return Level(list.Where(item => parent(item) is not { } up || !keys.Contains(up)));
        }
    }
}
