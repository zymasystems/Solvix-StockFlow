// Test-only stand-in for the native preference boundary; the production session source is linked unchanged.
namespace Microsoft.Maui.Storage;
public static class Preferences
{
    public static Store Default { get; } = new();

    public sealed class Store
    {
        private readonly Dictionary<string, bool> values = new();
        public bool Get(string key, bool fallback) => values.GetValueOrDefault(key, fallback);
        public void Set(string key, bool value) => values[key] = value;
    }
}
