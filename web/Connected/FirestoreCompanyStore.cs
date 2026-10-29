using System.Text.Json;
using Google.Cloud.Firestore;
using Solvix.Web.Services;

namespace Solvix.Web.Connected;
public sealed class FirestoreCompanyStore(FirestoreDb db) : ICompanyStore
{
    private static T? Decode<T>(DocumentSnapshot snapshot)
        where T : class => snapshot.Exists && snapshot.TryGetValue<string>("json", out var json) ? JsonSerializer.Deserialize<T>(json) : null;
    public async Task<List<Solvix.Contracts.Company>> ListCompaniesAsync() => ReadCompanies(await db.CollectionGroup("meta").Limit(1001).GetSnapshotAsync());
    private static List<Solvix.Contracts.Company> ReadCompanies(QuerySnapshot snapshot)
    {
        if (snapshot.Count > 1000) throw new ServiceException(422, "Company directory exceeds 1,000 records. Add a paginated directory before expanding it.");
        return snapshot.Documents.Where(d => d.Id == "company" && d.Reference.Parent.Parent?.Parent.Id == "companies").Select(Decode<Solvix.Contracts.Company>).OfType<Solvix.Contracts.Company>().ToList();
    }
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
        await scope.CommitAsync(result); // All Firestore reads finish before any writes are scheduled.
        return result;
    }, TransactionOptions.ForMaxAttempts(10));
    // Migration-only guard: a legacy source snapshot must still match inside the same transaction.
    public Task<T> RunGuardedAsync<T>(DocumentReference source, Func<DocumentSnapshot, bool> matches, Func<ICompanyTransaction, Task<T>> action) => db.RunTransactionAsync(async transaction =>
    {
        if (!matches(await transaction.GetSnapshotAsync(source)))
            throw new ServiceException(409, "Legacy source changed; rerun the dry run.");
        var scope = new Scope(db, transaction);
        var result = await action(scope);
        await scope.CommitAsync(result);
        return result;
    }, TransactionOptions.ForMaxAttempts(10));
    private static string BoundedJson(object value)
    {
        var json = JsonSerializer.Serialize(value);
        if (System.Text.Encoding.UTF8.GetByteCount(json) > 900000)
            throw new ServiceException(422, "This record is too large. Use smaller images or documents.");
        return json;
    }

    private sealed class Scope(FirestoreDb db, Transaction transaction) : ICompanyTransaction
    {
        private readonly Dictionary<string, object?> writes = new();
        public async Task<List<Solvix.Contracts.Company>> ListCompaniesAsync() => ReadCompanies(await transaction.GetSnapshotAsync(db.CollectionGroup("meta").Limit(1001)));
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
        public async Task CommitAsync(object? result)
        {
            if (writes.Count > 450)
                throw new ServiceException(422, "Operation contains too many writes.");
            var codes = new RecordCodes(db, transaction);
            // A replacement entity cannot overwrite an existing code or supply its own.
            // Resolve every prior record and counter before scheduling Firestore writes.
            foreach (var (path, value) in writes)
            {
                if (value is not Solvix.Contracts.Entity entity) continue;
                var prefix = RecordCodes.Prefix(entity);
                if (prefix.Length == 0) continue; // membership indexes/replays are not business records.
                var previous = Decode<Solvix.Contracts.Entity>(await transaction.GetSnapshotAsync(db.Document(path)));
                var scope = entity is Solvix.Contracts.Company ? "" : string.Join('/', path.Split('/').Take(2)) + "/";
                entity.Code = !string.IsNullOrEmpty(previous?.Code) ? previous.Code : await codes.ReserveAsync(scope, prefix);
            }
            if (result is Solvix.Contracts.Result operation)
            {
                var primary = writes.Values.OfType<Solvix.Contracts.Entity>()
                    .FirstOrDefault(e => e.Id == operation.Id && e.Code.Length > 0);
                if (primary is not null) operation.Code = primary.Code;
            }
            codes.Commit();
            foreach (var(path, value)in writes)
                if (value is null)
                    transaction.Delete(db.Document(path));
                else
                {
                    var fields = new Dictionary<string, object> { ["json"] = BoundedJson(value), ["updatedAt"] = FieldValue.ServerTimestamp };
                    if (value is Solvix.Contracts.Entity entity && entity.Code.Length > 0) fields["code"] = entity.Code;
                    transaction.Set(db.Document(path), fields);
                }
        }
    }
}
