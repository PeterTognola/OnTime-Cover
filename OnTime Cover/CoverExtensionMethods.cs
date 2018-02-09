using System;
using System.Collections.Generic;
using System.Linq;

namespace OnTime_Cover
{
    public static class CoverExtensionMethods
    {
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(items);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                if (childSelector(next) == null) continue;
                foreach (var child in childSelector(next))
                    stack.Push(child);
            }
        }
    }
}