using System.Text.Json;
using Solvix.Web.Connected;

namespace Solvix.Connected.Tests;
// Transactional copy-on-write store: exceptions discard writes, no object aliasing.
public sealed class MemoryStore : ICompanyStore
{
    private readonly SemaphoreSlim gate = new(1);
    private Dictionary<string, string> data = new();
    private sealed class Scope(Dictionary<string, string> data) : ICompanyTransaction
    {
        public Task<T?> GetAsync<T>(string path)
            where T : class => Task.FromResult(data.TryGetValue(path, out var json) ? JsonSerializer.Deserialize<T>(json) : null);
        public Task<List<T>> ListAsync<T>(string path)
            where T : class => Task.FromResult(data.Where(p => p.Key.StartsWith(path + "/") && !p.Key[(path.Length + 1)..].Contains('/')).Select(p => JsonSerializer.Deserialize<T>(p.Value)!).ToList());
        public void Put<T>(string path, T value)
            where T : class => data[path] = JsonSerializer.Serialize(value);
        public void Delete(string path) => data.Remove(path);
    }

    public async Task<T?> GetAsync<T>(string path)
        where T : class
    {
        await gate.WaitAsync();
        try
        {
            return await new Scope(data).GetAsync<T>(path);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<List<T>> ListAsync<T>(string path)
        where T : class
    {
        await gate.WaitAsync();
        try
        {
            return await new Scope(data).ListAsync<T>(path);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<T> RunAsync<T>(Func<ICompanyTransaction, Task<T>> action)
    {
        await gate.WaitAsync();
        try
        {
            var next = new Dictionary<string, string>(data);
            var result = await action(new Scope(next));
            data = next;
            return result;
        }
        finally
        {
            gate.Release();
        }
    }
}
