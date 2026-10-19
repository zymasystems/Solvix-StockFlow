using System.Text.Json;
using Google.Cloud.Firestore;
using Solvix.Web.Services;

namespace Solvix.Web.Connected;
public sealed class FirestoreCompanyStore(FirestoreDb db) : ICompanyStore
{
    private static T? Decode<T>(DocumentSnapshot snapshot)
        where T : class => snapshot.Exists && snapshot.TryGetValue<string>("json", out var json) ? JsonSerializer.Deserialize<T>(json) : null;
    public async Task<T?> GetAsync<T>(string path)
        where T : class => Decode<T>(await db.Document(path).GetSnapshotAsync());
    // Fail explicitly rather than silently returning incomplete KPIs or stock.
    public async Task<List<T>> ListAsync<T>(string path)
        where T : class => Read<T>(await db.Collection(path).Limit(1001).GetSnapshotAsync());
    private static List<T> Read<T>(QuerySnapshot snapshot)
        where T : class
    {
        if (snapshot.Count > 1000)
            throw new ServiceException(422, "This workspace exceeds 1,000 records in a collection. Use archival/aggregate queries before expanding it.");
        return snapshot.Documents.Select(Decode<T>).OfType<T>().ToList();
    }

    public Task<T> RunAsync<T>(Func<ICompanyTransaction, Task<T>> action) => db.RunTransactionAsync(async transaction =>
    {
        var scope = new Scope(db, transaction);
        var result = await action(scope);
        scope.Commit(); // All Firestore reads finish before any writes are scheduled.
        return result;
    });
    // Migration-only guard: a legacy source snapshot must still match inside the same transaction.
    public Task<T> RunGuardedAsync<T>(DocumentReference source, Func<DocumentSnapshot, bool> matches, Func<ICompanyTransaction, Task<T>> action) => db.RunTransactionAsync(async transaction =>
    {
        if (!matches(await transaction.GetSnapshotAsync(source)))
            throw new ServiceException(409, "Legacy source changed; rerun the dry run.");
        var scope = new Scope(db, transaction);
        var result = await action(scope);
        scope.Commit();
        return result;
    });
    private sealed class Scope(FirestoreDb db, Transaction transaction) : ICompanyTransaction
    {
        private readonly Dictionary<string, object?> writes = new();
        public async Task<T?> GetAsync<T>(string path)
            where T : class
        {
            if (writes.TryGetValue(path, out var value))
                return value is null ? null : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
            return Decode<T>(await transaction.GetSnapshotAsync(db.Document(path)));
        }

        public async Task<List<T>> ListAsync<T>(string path)
            where T : class => Read<T>(await transaction.GetSnapshotAsync(db.Collection(path).Limit(1001)));
        public void Put<T>(string path, T value)
            where T : class => writes[path] = value;
        public void Delete(string path) => writes[path] = null;
        public void Commit()
        {
            if (writes.Count > 450)
                throw new ServiceException(422, "Operation contains too many writes.");
            foreach (var(path, value)in writes)
                if (value is null)
                    transaction.Delete(db.Document(path));
                else
                    transaction.Set(db.Document(path), new Dictionary<string, object> { ["json"] = JsonSerializer.Serialize(value), ["updatedAt"] = FieldValue.ServerTimestamp });
        }
    }
}
