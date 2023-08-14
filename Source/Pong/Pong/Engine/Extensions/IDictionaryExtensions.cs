using System.Collections.Generic;

namespace PumpkinGames.Glitchangels.Extensions
{
    /// <summary>
    /// Extensions to the IDictionary interface.
    /// </summary>
    public static class IDictionaryExtensions
    {
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : default;
        }

        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            return dictionary.ContainsKey(key) ? dictionary[key] : defaultValue;
        }
    }
}
