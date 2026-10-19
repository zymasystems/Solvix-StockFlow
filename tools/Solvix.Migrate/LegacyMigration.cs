using System.Text.Json;
using Google.Cloud.Firestore;
using Solvix.Contracts;
using Solvix.Web.Connected;

namespace Solvix.Migrate;
public static class LegacyMigration
{
    private sealed class Imported : Entity
    {
        public string Fingerprint { get; set; } = "";
    }

    public sealed class Archive : Entity
    {
        public string Source { get; set; } = "";
        public string SourceJson { get; set; } = "";
    }

    private sealed record Planned(DocumentSnapshot Source, string Json, string Hash);
    private static object? Normalize(object? value) => value switch
    {
        Timestamp timestamp => timestamp.ToDateTime().ToString("O"),
        IDictionary<string, object> map => map.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => Normalize(p.Value)),
        IEnumerable<object> sequence => sequence.Select(Normalize).ToArray(),
        DocumentReference reference => reference.Path,
        _ => value
    };
    private static string Json(DocumentSnapshot d) => JsonSerializer.Serialize(Normalize(d.ToDictionary()));
    private static string Text(DocumentSnapshot d, string name) => d.TryGetValue<string>(name, out var value) ? value : "";
    private static long Number(DocumentSnapshot d, string name) => d.TryGetValue<long>(name, out var value) ? value : 0;
    private static Product Product(DocumentSnapshot d, string company)
    {
        var name = Text(d, "name").Trim();
        var sku = Text(d, "sku").Trim().ToUpperInvariant();
        if (name.Length is 0 or > 200 || sku.Length is 0 or > 60)
            throw new InvalidOperationException("Repair product name/SKU: " + d.Id);
        var price = d.TryGetValue<double>("price", out var value) ? value : 0;
        if (!double.IsFinite(price) || price < 0 || price > 999999999.99 || decimal.Round((decimal)price, 2) != (decimal)price)
            throw new InvalidOperationException("Repair price (two decimal cost required): " + d.Id);
        if (Number(d, "stock") < 0 || Number(d, "minStock") < 0)
            throw new InvalidOperationException("Reconcile negative quantity: " + d.Id);
        return new()
        {
            Id = CompanyService.Id(d.Id),
            CompanyId = company,
            Name = name,
            Sku = sku,
            Category = Text(d, "category"),
            UnitCost = (decimal)price
        };
    }

    public static async Task<int> RunAsync(FirestoreDb db, string company, string warehouse, string owner, bool priceIsCost, bool apply)
    {
        if (!priceIsCost)
            throw new InvalidOperationException("Confirm the legacy price is unit COST with --legacy-price-is-cost. Do not reinterpret a selling price.");
        var root = "companies/" + CompanyService.Id(company) + "/";
        CompanyService.Id(warehouse);
        CompanyService.Id(owner);
        var store = new FirestoreCompanyStore(db);
        if (await store.GetAsync<Company>(root + "meta/company")is null || await store.GetAsync<Member>(root + "members/" + owner)is not { Role: "admin", Status: "Active" } || await store.GetAsync<Warehouse>(root + "warehouses/" + warehouse)is not { Active: true })
            throw new InvalidOperationException("Create the destination company and warehouse, with an active owner membership, before migration.");
        var plan = new List<Planned>();
        foreach (var collection in new[]
        {
            "products",
            "suppliers",
            "sales",
            "adjustments"
        }

        )
        {
            var docs = await db.Collection(collection).Limit(1001).GetSnapshotAsync();
            if (docs.Count > 1000)
                throw new InvalidOperationException("More than 1,000 source records: partition the migration before importing " + collection);
            foreach (var doc in docs.Documents)
            {
                var json = Json(doc);
                if (json.Length > 400000)
                    throw new InvalidOperationException("Source document too large: " + doc.Reference.Path);
                plan.Add(new(doc, json, CompanyService.Key(json)));
            }
        }

        var products = plan.Where(p => p.Source.Reference.Parent.Id == "products").Select(p => Product(p.Source, company)).ToList();
        if (products.GroupBy(p => p.Sku, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Duplicate legacy SKUs must be reconciled before import.");
        // Validate every destination before the first write. Individual imports remain atomic and resumable.
        foreach (var item in plan)
        {
            var collection = item.Source.Reference.Parent.Id;
            var id = item.Source.Id;
            var marker = root + "legacyImports/" + CompanyService.Key(item.Source.Reference.Path);
            var existing = await store.GetAsync<Imported>(marker);
            if (existing is not null)
            {
                if (existing.Fingerprint != item.Hash)
                    throw new InvalidOperationException("Source changed after import: " + item.Source.Reference.Path);
                continue;
            }

            if (collection is "products" or "suppliers" && await store.GetAsync<Entity>(root + collection + "/" + id)is not null)
                throw new InvalidOperationException("Destination already exists: " + collection + "/" + id);
            if (collection == "products")
            {
                var product = Product(item.Source, company);
                if (await store.GetAsync<Entity>(root + "skuIndex/" + CompanyService.Key(product.Sku))is not null || await store.GetAsync<Stock>(root + $"warehouses/{warehouse}/stock/{id}")is not null)
                    throw new InvalidOperationException("SKU/stock destination already exists: " + id);
            }
        }

        if (!apply)
            return plan.Count;
        foreach (var item in plan)
            await store.RunGuardedAsync(item.Source.Reference, snapshot => snapshot.Exists && CompanyService.Key(Json(snapshot)) == item.Hash, async tx =>
            {
                var collection = item.Source.Reference.Parent.Id;
                var id = item.Source.Id;
                var marker = root + "legacyImports/" + CompanyService.Key(item.Source.Reference.Path);
                var member = await tx.GetAsync<Member>(root + "members/" + owner);
                var wh = await tx.GetAsync<Warehouse>(root + "warehouses/" + warehouse);
                if (member is not { Role: "admin", Status: "Active" } || wh is not { Active: true })
                    throw new InvalidOperationException("Destination access changed.");
                var previous = await tx.GetAsync<Imported>(marker);
                if (previous is not null)
                {
                    if (previous.Fingerprint != item.Hash)
                        throw new InvalidOperationException("Source changed after import.");
                    return false;
                }

                // The guard above also detects source changes from an older deployed server.
                var current = await tx.GetAsync<Archive>(root + "legacyArchive/" + CompanyService.Key(item.Source.Reference.Path));
                if (current is not null)
                    throw new InvalidOperationException("Unexpected archive collision.");
                if (collection == "products")
                {
                    var p = Product(item.Source, company);
                    var index = root + "skuIndex/" + CompanyService.Key(p.Sku);
                    var stockPath = root + $"warehouses/{warehouse}/stock/{id}";
                    if (await tx.GetAsync<Entity>(root + "products/" + id)is not null || await tx.GetAsync<Entity>(index)is not null || await tx.GetAsync<Stock>(stockPath)is not null)
                        throw new InvalidOperationException("Destination changed during migration.");
                    tx.Put(root + "products/" + id, p);
                    tx.Put(index, new Entity { Id = id });
                    var quantity = Number(item.Source, "stock");
                    tx.Put(stockPath, new Stock { Id = id, ProductId = id, CompanyId = company, WarehouseId = warehouse, OnHand = quantity, MinStock = Number(item.Source, "minStock"), Version = 1 });
                    tx.Put(root + "movements/migration-" + id, new Movement { Id = "migration-" + id, ProductId = id, WarehouseId = warehouse, Kind = "MigrationOpening", Delta = quantity, Before = 0, After = quantity, ActorUid = owner, Reference = "Legacy snapshot; archived historical rows are not reposted", At = DateTimeOffset.UtcNow });
                }
                else if (collection == "suppliers")
                {
                    if (await tx.GetAsync<Entity>(root + "suppliers/" + id)is not null)
                        throw new InvalidOperationException("Supplier destination changed.");
                    tx.Put(root + "suppliers/" + id, new Supplier { Id = id, Name = Text(item.Source, "name"), Email = Text(item.Source, "email"), Phone = Text(item.Source, "phone") });
                }

                var key = CompanyService.Key(item.Source.Reference.Path);
                tx.Put(root + "legacyArchive/" + key, new Archive { Id = key, Source = item.Source.Reference.Path, SourceJson = item.Json });
                tx.Put(marker, new Imported { Id = id, Fingerprint = item.Hash });
                return true;
            });
        return plan.Count;
    }
}
