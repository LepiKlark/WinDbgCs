﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CsDebugScript.Engine.Utility
{
    /// <summary>
    /// Helper method which invalidates all class fields which are caches
    /// starting from the given root objects.
    /// </summary>
    public static class CacheInvalidator
    {
        /// <summary>
        /// Invalidates all the instances of type <see cref="ICache" /> and
        /// <see cref="DictionaryCache{TKey, TValue}" /> which are fields of class given as root object and any
        /// fields of the same type in child fields recursively.
        /// Use it when there are massive changes and all the caches need to be invalidated.
        /// </summary>
        /// <param name="rootObject">Root object from which we recursively drop the caches.</param>
        public static void InvalidateCaches(object rootObject)
        {
            if (rootObject == null)
            {
                return;
            }

            // Gets all the cache fields of given type.
            IEnumerable<FieldInfo> cacheFields =
                rootObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(fieldInfo => fieldInfo.FieldType.GetInterfaces().Contains(typeof(ICache)));

            foreach (FieldInfo field in cacheFields)
            {
                ICache cache = field.GetValue(rootObject) as ICache;

                // Clear only fields which are cached.
                if (cache != null)
                {
                    object[] cacheEntries = cache.OfType<object>().ToArray();
                    cache.InvalidateCache();

                    // Invalidate all the cached object if any.
                    foreach (object cachedCollectionEntry in cacheEntries)
                    {
                        InvalidateCaches(cachedCollectionEntry);
                    }
                }
            }
        }
    }
}
