using Google.Cloud.Firestore;
using Solvix.Contracts;
using Solvix.Web.Connected;

namespace Solvix.Migrate;

// Operator-only utility. No public endpoint and no business-field or document-ID changes.
public static class RecordCodeBackfill
{
    public static async Task<int> RunAsync(FirestoreDb db, string? companyId, bool includeUsers, bool apply)
    {
        var store = new FirestoreCompanyStore(db);
        int missing = 0;
        async Task One<T>(string path) where T : Entity
        {
            var value = await store.GetAsync<T>(path);
            if (value is null || value.Code.Length > 0) return;
            missing++;
            Console.WriteLine((apply ? "Assign code: " : "Would assign code: ") + path);
            if (!apply) return;
            await store.RunAsync(async tx =>
            {
                var current = await tx.GetAsync<T>(path);
                if (current is not null && current.Code.Length == 0) tx.Put(path, current);
                return 0;
            });
        }
        async Task Collection<T>(string path) where T : Entity
        {
            // Page document IDs, then re-read the current record inside its transaction.
            Query query = db.Collection(path).OrderBy(FieldPath.DocumentId).Limit(200);
            while (true)
            {
                var page = await query.GetSnapshotAsync();
                if (page.Count == 0) break;
                foreach (var doc in page.Documents) await One<T>(path + "/" + doc.Id);
                query = db.Collection(path).OrderBy(FieldPath.DocumentId).StartAfter(page.Documents[^1]).Limit(200);
            }
        }

        if (!string.IsNullOrEmpty(companyId))
        {
            var root = "companies/" + CompanyService.Id(companyId) + "/";
            if (await store.GetAsync<Company>(root + "meta/company") is null)
                throw new ArgumentException("Company not found.");
            await One<Company>(root + "meta/company");
            await Collection<Member>(root + "members");
            await Collection<Warehouse>(root + "warehouses");
            await Collection<Product>(root + "products");
            await Collection<Supplier>(root + "suppliers");
            await Collection<Receipt>(root + "receipts");
            await Collection<Dispatch>(root + "dispatches");
            await Collection<Transfer>(root + "transfers");
            await Collection<Audit>(root + "audits");
            await Collection<Movement>(root + "movements");
            // Includes archived warehouses. Existing stock remains linked by product ID.
            Query warehouses = db.Collection(root + "warehouses").OrderBy(FieldPath.DocumentId).Limit(200);
            while (true)
            {
                var page = await warehouses.GetSnapshotAsync();
                if (page.Count == 0) break;
                foreach (var doc in page.Documents) await Collection<Stock>(root + "warehouses/" + doc.Id + "/stock");
                warehouses = db.Collection(root + "warehouses").OrderBy(FieldPath.DocumentId).StartAfter(page.Documents[^1]).Limit(200);
            }
        }
        if (includeUsers)
        {
            Query query = db.Collection("users").OrderBy(FieldPath.DocumentId).Limit(200);
            while (true)
            {
                var page = await query.GetSnapshotAsync();
                if (page.Count == 0) break;
                foreach (var user in page.Documents)
                {
                    if (user.ContainsField("code") && !string.IsNullOrEmpty(user.GetValue<string>("code"))) continue;
                    missing++;
                    Console.WriteLine((apply ? "Assign code: " : "Would assign code: ") + user.Reference.Path);
                    if (!apply) continue;
                    await db.RunTransactionAsync(async tx =>
                    {
                        var current = await tx.GetSnapshotAsync(user.Reference);
                        if (!current.Exists || (current.TryGetValue<string>("code", out var code) && code.Length > 0)) return;
                        var codes = new RecordCodes(db, tx);
                        var next = await codes.ReserveAsync("", "usr");
                        codes.Commit();
                        tx.Update(user.Reference, new Dictionary<string, object> { ["code"] = next });
                    });
                }
                query = db.Collection("users").OrderBy(FieldPath.DocumentId).StartAfter(page.Documents[^1]).Limit(200);
            }
        }
        return missing;
    }
}
