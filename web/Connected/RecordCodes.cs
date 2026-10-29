using System.Globalization;
using Google.Cloud.Firestore;
using Solvix.Contracts;

namespace Solvix.Web.Connected;

// One instance per Firestore transaction. Read/reserve first; write only after all reads.
public sealed class RecordCodes(FirestoreDb db, Transaction transaction)
{
    private readonly Dictionary<string, long> counters = new();

    public static string Prefix(Entity entity) => entity switch
    {
        Company => "cmp", Member => "mem", Warehouse => "wh",
        Product => "prd", Supplier => "spr", Stock => "stk",
        Receipt => "rcv", Dispatch => "sal", Transfer => "trf",
        Audit => "aud", Movement => "mov",
        _ => ""
    };

    public async Task<string> ReserveAsync(string scope, string prefix)
    {
        var path = scope + "recordSequences/" + prefix;
        if (!counters.TryGetValue(path, out var last))
        {
            var snapshot = await transaction.GetSnapshotAsync(db.Document(path));
            last = snapshot.Exists ? snapshot.GetValue<long>("last") : 0;
        }
        var next = checked(last + 1);
        counters[path] = next;
        return prefix + "-" + next.ToString("D2", CultureInfo.InvariantCulture);
    }

    public void Commit()
    {
        foreach (var (path, last) in counters)
            transaction.Set(db.Document(path), new Dictionary<string, object>
            {
                ["last"] = last, ["updatedAt"] = FieldValue.ServerTimestamp
            });
    }
}
