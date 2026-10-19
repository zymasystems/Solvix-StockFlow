using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Solvix.Contracts;
using Solvix.Web.Services;

namespace Solvix.Web.Connected;
public sealed class CompanyService(ICompanyStore store, TimeProvider clock)
{
    public static string Key(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public static string Id(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Contains('/') || value is "." or "..")
            throw new ServiceException(400, "Invalid identifier.");
        return value;
    }

    private static string Root(string company) => "companies/" + Id(company) + "/";
    private static void Require(bool value, string message, int status = 400)
    {
        if (!value)
            throw new ServiceException(status, message);
    }

    private static string Text(string value, int max = 200)
    {
        Require(!string.IsNullOrWhiteSpace(value) && value.Length <= max, "A required text value is missing or too long.");
        return value.Trim();
    }

    private static bool Admin(Member m) => m.Role == "admin";
    private static bool Access(Member m, string warehouse) => Admin(m) || m.WarehouseIds.Contains(warehouse);
    private static async Task<Member> MemberAsync(ICompanyReader data, string root, string uid)
    {
        var member = await data.GetAsync<Member>(root + "members/" + Id(uid));
        Require(member is { Status: "Active" } && (member.Role == "admin" || member.Role == "staff"), "Active company membership is required.", 403);
        return member!;
    }

    private static async Task<Warehouse> WarehouseAsync(ICompanyReader data, string root, Member member, string warehouse)
    {
        Require(Access(member, warehouse), "Warehouse access denied.", 403);
        var value = await data.GetAsync<Warehouse>(root + "warehouses/" + Id(warehouse));
        Require(value is { Active: true }, "Warehouse is unavailable.", 404);
        return value!;
    }

    private static async Task<Product> ProductAsync(ICompanyReader data, string root, string id)
    {
        var value = await data.GetAsync<Product>(root + "products/" + Id(id));
        Require(value is { Active: true }, "Product is unavailable.", 404);
        return value!;
    }

    public async Task<List<Membership>> CompaniesAsync(string uid)
    {
        var ids = await store.ListAsync<Entity>("userCompanies/" + Id(uid) + "/companies");
        var result = new List<Membership>();
        foreach (var id in ids)
        {
            var root = Root(id.Id);
            var company = await store.GetAsync<Company>(root + "meta/company");
            var member = await store.GetAsync<Member>(root + "members/" + Id(uid));
            if (company is not null && member is not null)
                result.Add(new() { Company = company, Member = member });
        }

        return result.OrderBy(x => x.Company.Name).ToList();
    }

    public Task<Result> CreateAsync(string uid, Operation input)
    {
        Require(input.RequestId != Guid.Empty, "RequestId is required.");
        var cid = Key(uid + ":company:" + input.RequestId)[..32];
        var root = Root(cid);
        return store.RunAsync(async tx =>
        {
            var existing = await tx.GetAsync<Company>(root + "meta/company");
            if (existing is not null)
            {
                Require(existing.Name == input.Name.Trim(), "RequestId was reused with different data.", 409);
                return new Result
                {
                    Id = cid
                };
            }

            var company = new Company
            {
                Id = cid,
                Name = Text(input.Name)
            };
            tx.Put(root + "meta/company", company);
            tx.Put(root + "members/" + Id(uid), new Member { Id = uid, Role = "admin", Status = "Active", FullName = string.IsNullOrWhiteSpace(input.Reference) ? uid : Text(input.Reference) });
            tx.Put("userCompanies/" + Id(uid) + "/companies/" + cid, new Entity { Id = cid });
            return new Result
            {
                Id = cid,
                Message = "Company created. You administer this company only."
            };
        });
    }

    public Task<Result> JoinAsync(string uid, string company, Operation input) => store.RunAsync(async tx =>
    {
        var root = Root(company);
        Require(await tx.GetAsync<Company>(root + "meta/company")is not null, "Company not found.", 404);
        var existing = await tx.GetAsync<Member>(root + "members/" + Id(uid));
        if (existing is not null)
            return new Result
            {
                Id = uid,
                Message = "Membership already exists: " + existing.Status
            };
        tx.Put(root + "members/" + Id(uid), new Member { Id = uid, FullName = Text(input.Name) });
        tx.Put("userCompanies/" + Id(uid) + "/companies/" + company, new Entity { Id = company });
        return new Result
        {
            Id = uid,
            Message = "Membership request awaits a company administrator."
        };
    });
    public async Task<Solvix.Contracts.Workspace> LoadAsync(string uid, string company, string warehouse, string? search = null)
    {
        var root = Root(company);
        var member = await MemberAsync(store, root, uid);
        var warehouses = (await store.ListAsync<Warehouse>(root + "warehouses")).Where(w => w.Active && Access(member, w.Id)).ToList();
        if (!string.IsNullOrEmpty(warehouse))
            await WarehouseAsync(store, root, member, warehouse);
        var allowed = string.IsNullOrEmpty(warehouse) ? warehouses.Select(w => w.Id).ToHashSet() : new HashSet<string>
        {
            warehouse
        };
        var products = (await store.ListAsync<Product>(root + "products")).Where(p => p.Active).ToList();
        var stock = new List<Stock>();
        foreach (var wh in allowed)
            stock.AddRange(await store.ListAsync<Stock>(root + "warehouses/" + wh + "/stock"));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var matches = products.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) || p.Barcode.Equals(search, StringComparison.OrdinalIgnoreCase)).Select(p => p.Id).ToHashSet();
            stock = stock.Where(s => matches.Contains(s.ProductId)).ToList();
        }

        var receipts = (await store.ListAsync<Receipt>(root + "receipts")).Where(r => allowed.Contains(r.WarehouseId)).OrderByDescending(r => r.CreatedAt).ToList();
        var dispatches = (await store.ListAsync<Dispatch>(root + "dispatches")).Where(d => allowed.Contains(d.SourceWarehouseId)).OrderByDescending(d => d.CreatedAt).ToList();
        return new Solvix.Contracts.Workspace
        {
            Company = (await store.GetAsync<Company>(root + "meta/company"))!,
            Permission = member,
            Warehouses = warehouses,
            Products = products.Where(p => string.IsNullOrWhiteSpace(search) || p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) || p.Barcode.Equals(search, StringComparison.OrdinalIgnoreCase)).ToList(),
            Stock = stock,
            Suppliers = await store.ListAsync<Supplier>(root + "suppliers"),
            Members = Admin(member) ? await store.ListAsync<Member>(root + "members") : new()
            {
                member
            },
            Receipts = receipts,
            Dispatches = dispatches,
            Transfers = (await store.ListAsync<Transfer>(root + "transfers")).Where(t => allowed.Contains(t.SourceWarehouseId) || allowed.Contains(t.DestinationWarehouseId)).ToList(),
            Audits = (await store.ListAsync<Audit>(root + "audits")).Where(a => allowed.Contains(a.WarehouseId)).OrderByDescending(a => a.CreatedAt).ToList(),
            Movements = (await store.ListAsync<Movement>(root + "movements")).Where(m => allowed.Contains(m.WarehouseId)).OrderByDescending(m => m.At).ToList()
        };
    }

    public Task<Result> ExecuteAsync(string uid, string company, string command, Operation input)
    {
        Require(input.RequestId != Guid.Empty, "RequestId is required.");
        Require(input.Notes.Length <= 1000 && input.Reference.Length <= 200, "Notes/reference are too long.");
        if (input.UiJson is not null)
        {
            Require(Encoding.UTF8.GetByteCount(input.UiJson) <= 400000, "Display details exceed 400 KB. Use smaller images/documents.");
            try
            {
                using var display = JsonDocument.Parse(input.UiJson);
                Require(display.RootElement.ValueKind == JsonValueKind.Object, "Display details must be a JSON object.");
            }
            catch (JsonException)
            {
                throw new ServiceException(400, "Invalid display details.");
            }
        }

        var root = Root(company);
        var operation = Key(uid + ":" + input.RequestId);
        var fingerprint = Key(command + JsonSerializer.Serialize(input));
        var now = clock.GetUtcNow();
        return store.RunAsync(async tx =>
        {
            var member = await MemberAsync(tx, root, uid);
            var replay = await tx.GetAsync<Replay>(root + "operations/" + operation);
            if (replay is not null)
            {
                Require(replay.Fingerprint == fingerprint, "RequestId was reused with different data.", 409);
                return replay.Result;
            }

            var result = new Result
            {
                Id = input.Id.Length == 0 ? operation[..32] : Id(input.Id)
            };
            void Put<T>(string path, T value)
                where T : class => tx.Put(root + path, value);
            async Task<Stock> StockAt(string wh, string pid)
            {
                await WarehouseAsync(tx, root, member, wh);
                await ProductAsync(tx, root, pid);
                return await tx.GetAsync<Stock>(root + $"warehouses/{wh}/stock/{pid}") ?? new Stock
                {
                    Id = pid,
                    ProductId = pid,
                    WarehouseId = wh,
                    CompanyId = company
                };
            }

            async Task Post(string wh, string pid, long delta, string kind, string suffix)
            {
                var stock = await StockAt(wh, pid);
                var before = stock.OnHand;
                long after;
                try
                {
                    after = checked(before + delta);
                }
                catch (OverflowException)
                {
                    throw new ServiceException(400, "Quantity is too large.");
                }

                Require(after >= stock.Reserved + stock.Quarantined, "Insufficient available stock.", 409);
                stock.OnHand = after;
                stock.Version = checked(stock.Version + 1);
                Put($"warehouses/{wh}/stock/{pid}", stock);
                Put("movements/" + operation + "-" + suffix, new Movement { Id = operation + "-" + suffix, WarehouseId = wh, ProductId = pid, Kind = kind, Delta = delta, Before = before, After = after, ActorUid = uid, Reference = input.Reference, At = now });
            }

            var adminCommands = new[]
            {
                "warehouse.save",
                "warehouse.archive",
                "member.review",
                "member.add",
                "supplier.save",
                "company.brand",
                "product.archive",
                "stock.delete",
                "stock.adjust",
                "receipt.approve",
                "receipt.reject",
                "dispatch.review",
                "dispatch.reject",
                "outbound.approve",
                "outbound.reject",
                "audit.approve",
                "audit.reject"
            };
            if (adminCommands.Contains(command))
                Require(Admin(member), "Company administrator permission is required.", 403);
            switch (command)
            {
                case "warehouse.save":
                    var siteName = Text(input.Name);
                    Require(!(await tx.ListAsync<Warehouse>(root + "warehouses")).Any(w => w.Id != result.Id && w.Active && w.Name.Equals(siteName, StringComparison.OrdinalIgnoreCase)), "Warehouse name already exists.", 409);
                    var previousSite = await tx.GetAsync<Warehouse>(root + "warehouses/" + result.Id);
                    Put("warehouses/" + result.Id, new Warehouse { Id = result.Id, Name = siteName, UiJson = input.UiJson ?? previousSite?.UiJson ?? "{}" });
                    break;
                case "warehouse.archive":
                    await WarehouseAsync(tx, root, member, result.Id);
                    Require((await tx.ListAsync<Stock>(root + "warehouses/" + result.Id + "/stock")).All(s => s.OnHand == 0 && s.Reserved == 0 && s.Quarantined == 0), "Warehouse contains stock.", 409);
                    Require(!(await tx.ListAsync<Receipt>(root + "receipts")).Any(r => r.WarehouseId == result.Id && r.Status == "Pending"), "Pending receipts exist.", 409);
                    Require(!(await tx.ListAsync<Dispatch>(root + "dispatches")).Any(d => (d.SourceWarehouseId == result.Id || d.DestinationWarehouseId == result.Id) && d.Status is not ("Posted" or "Rejected")), "Pending dispatches exist.", 409);
                    Require(!(await tx.ListAsync<Audit>(root + "audits")).Any(a => a.WarehouseId == result.Id && a.Status == "Pending"), "Pending audits exist.", 409);
                    var archived = (await tx.GetAsync<Warehouse>(root + "warehouses/" + result.Id))!;
                    archived.Active = false;
                    Put("warehouses/" + result.Id, archived);
                    break;
                case "member.add":
                    Require(await tx.GetAsync<Member>(root + "members/" + result.Id)is null, "Employee is already a company member. Update their assignment instead.", 409);
                    foreach (var wh in input.WarehouseIds.Distinct())
                        await WarehouseAsync(tx, root, member, wh);
                    Put("members/" + result.Id, new Member { Id = result.Id, FullName = Text(input.Name), Email = input.Email, Role = "staff", Status = "Active", WarehouseIds = input.WarehouseIds.Distinct().ToList() });
                    tx.Put("userCompanies/" + result.Id + "/companies/" + company, new Entity { Id = company });
                    break;
                case "member.review":
                    Require(input.Role is "admin" or "staff" && input.Status is "Active" or "Inactive" or "Pending", "Invalid role/status.");
                    Require(result.Id != uid || (input.Role == "admin" && input.Status == "Active"), "You cannot remove your own administrator access.", 409);
                    var target = await tx.GetAsync<Member>(root + "members/" + result.Id);
                    Require(target is not null, "Membership not found.", 404);
                    foreach (var wh in input.WarehouseIds.Distinct())
                        await WarehouseAsync(tx, root, member, wh);
                    if (!string.IsNullOrWhiteSpace(input.Name)) target!.FullName = Text(input.Name);
                    target!.Role = input.Role;
                    target.Status = input.Status;
                    target.WarehouseIds = input.WarehouseIds.Distinct().ToList();
                    Put("members/" + result.Id, target);
                    break;
                case "company.brand":
                    var brand = (await tx.GetAsync<Company>(root + "meta/company"))!;
                    ValidateLogo(input.PhotoUrl);
                    Require(System.Text.RegularExpressions.Regex.IsMatch(input.Color, "^#[0-9a-fA-F]{6}$"), "Choose a six-digit hex theme color.");
                    if (!string.IsNullOrWhiteSpace(input.WarehouseId))
                    {
                        await WarehouseAsync(tx, root, member, input.WarehouseId);
                        brand.DefaultWarehouseId = input.WarehouseId;
                    }

                    brand.UiJson = input.UiJson ?? brand.UiJson;
                    brand.Color = input.Color;
                    brand.Name = Text(input.Name);
                    brand.LogoUrl = input.PhotoUrl ?? "";
                    Put("meta/company", brand);
                    break;
                case "supplier.save":
                    var oldSupplier = await tx.GetAsync<Supplier>(root + "suppliers/" + result.Id);
                    Put("suppliers/" + result.Id, new Supplier { Id = result.Id, Name = Text(input.Name), Email = input.Email.Trim(), Phone = input.Reference.Trim(), UiJson = input.UiJson ?? oldSupplier?.UiJson ?? "{}" });
                    break;
                case "product.save":
                    Require(input.UnitCost >= 0 && decimal.Round(input.UnitCost, 2) == input.UnitCost && input.UnitCost <= 999999999.99m, "Unit cost must have at most two decimals and be within range.");
                    var sku = Text(input.Sku, 60).ToUpperInvariant();
                    var skuPath = "skuIndex/" + Key(sku);
                    var reserved = await tx.GetAsync<Entity>(root + skuPath);
                    Require(reserved is null || reserved.Id == result.Id, "SKU already exists in this company.", 409);
                    var old = await tx.GetAsync<Product>(root + "products/" + result.Id);
                    if (old is not null && old.Sku != sku)
                        tx.Delete(root + "skuIndex/" + Key(old.Sku));
                    var barcode = input.Barcode.Trim();
                    Require(barcode.Length <= 100, "Barcode is too long.");
                    if (barcode.Length > 0)
                    {
                        var entry = await tx.GetAsync<Entity>(root + "barcodeIndex/" + Key(barcode.ToUpperInvariant()));
                        Require(entry is null || entry.Id == result.Id, "Barcode already exists in this company.", 409);
                    }

                    if (old is not null && old.Barcode.Length > 0 && !old.Barcode.Equals(barcode, StringComparison.OrdinalIgnoreCase))
                        tx.Delete(root + "barcodeIndex/" + Key(old.Barcode.ToUpperInvariant()));
                    if (barcode.Length > 0)
                        Put("barcodeIndex/" + Key(barcode.ToUpperInvariant()), new Entity { Id = result.Id });
                    if (input.SupplierId is not null)
                        Require(await tx.GetAsync<Supplier>(root + "suppliers/" + Id(input.SupplierId))is not null, "Supplier not found.", 404);
                    Put(skuPath, new Entity { Id = result.Id });
                    Put("products/" + result.Id, new Product { Id = result.Id, CompanyId = company, Sku = sku, Name = Text(input.Name), Barcode = input.Barcode.Trim(), Category = input.Category.Trim(), UnitCost = input.UnitCost, SupplierId = input.SupplierId, UiJson = input.UiJson ?? old?.UiJson ?? "{}" });
                    break;
                case "product.archive":
                    var product = await ProductAsync(tx, root, result.Id);
                    foreach (var wh in await tx.ListAsync<Warehouse>(root + "warehouses"))
                    {
                        var stock = await tx.GetAsync<Stock>(root + $"warehouses/{wh.Id}/stock/{result.Id}");
                        Require(stock is null || (stock.OnHand == 0 && stock.Reserved == 0 && stock.Quarantined == 0), "Product has warehouse stock.", 409);
                    }

                    await NoPending(tx, root, result.Id, null);
                    product.Active = false;
                    Put("products/" + result.Id, product);
                    break;
                case "stock.configure":
                    var configured = await StockAt(input.WarehouseId, input.ProductId);
                    Require(input.MinStock >= 0, "Minimum stock cannot be negative.");
                    configured.UiJson = input.UiJson ?? configured.UiJson;
                    configured.MinStock = input.MinStock;
                    configured.Bin = input.Bin.Trim();
                    Put($"warehouses/{input.WarehouseId}/stock/{input.ProductId}", configured);
                    break;
                case "stock.delete":
                    var removed = await StockAt(input.WarehouseId, input.ProductId);
                    Require(removed.OnHand == 0 && removed.Reserved == 0 && removed.Quarantined == 0, "Only zero-stock records can be removed.", 409);
                    await NoPending(tx, root, input.ProductId, input.WarehouseId);
                    tx.Delete(root + $"warehouses/{input.WarehouseId}/stock/{input.ProductId}");
                    break;
                case "stock.adjust":
                    Require(input.Quantity != 0, "Adjustment cannot be zero; use an audit for a verified unchanged count.");
                    Text(input.Notes, 1000);
                    await Post(input.WarehouseId, input.ProductId, input.Quantity, "Adjustment", "0");
                    break;
                case "receipt.create":
                    Require(await tx.GetAsync<Entity>(root + "receipts/" + result.Id)is null, "A workflow with this identifier already exists.", 409);
                    Require(input.Quantity > 0, "Received quantity must be positive.");
                    await StockAt(input.WarehouseId, input.ProductId);
                    Put("receipts/" + result.Id, new Receipt { Id = result.Id, WarehouseId = input.WarehouseId, ProductId = input.ProductId, Quantity = input.Quantity, Arrived = false, CreatedBy = uid, CreatedAt = now, Notes = input.Notes, Reference = input.Reference });
                    break;
                case "receipt.arrive":
                case "receipt.verify":
                case "receipt.approve":
                case "receipt.reject":
                    var receipt = await tx.GetAsync<Receipt>(root + "receipts/" + result.Id);
                    Require(receipt is { Status: "Pending" }, "Receipt is not pending.", 409);
                    await WarehouseAsync(tx, root, member, receipt!.WarehouseId);
                    if (command == "receipt.arrive")
                        receipt.Arrived = true;
                    if (command == "receipt.verify")
                    {
                        Require(receipt.Arrived, "Confirm arrival first.", 409);
                        var p = await ProductAsync(tx, root, receipt.ProductId);
                        Require(Matches(p, input.Barcode), "Barcode/SKU does not match.");
                        receipt.Verified = true;
                        receipt.VerifiedBy = uid;
                    }

                    if (command == "receipt.approve")
                    {
                        Require(receipt.Arrived && receipt.Verified, "Arrival and verification are required.", 409);
                        await Post(receipt.WarehouseId, receipt.ProductId, receipt.Quantity, "Inbound", "0");
                        receipt.Status = "Approved";
                        receipt.ReviewedBy = uid;
                    }

                    if (command == "receipt.reject")
                    {
                        receipt.Status = "Rejected";
                        receipt.ReviewedBy = uid;
                    }

                    receipt.ReviewNote = input.Notes;
                    Put("receipts/" + result.Id, receipt);
                    if (receipt.TransferId is not null && command is "receipt.approve" or "receipt.reject")
                    {
                        var transfer = (await tx.GetAsync<Transfer>(root + "transfers/" + receipt.TransferId))!;
                        var related = new List<Receipt>();
                        foreach (var rid in transfer.ReceiptIds)
                            related.Add((await tx.GetAsync<Receipt>(root + "receipts/" + rid))!);
                        transfer.Status = related.Any(r => r.Status == "Rejected") ? "Exception" : related.All(r => r.Status == "Approved") ? "Received" : "InTransit";
                        Put("transfers/" + transfer.Id, transfer);
                    }

                    break;
                case "dispatch.create":
                    Require(await tx.GetAsync<Entity>(root + "dispatches/" + result.Id)is null, "A workflow with this identifier already exists.", 409);
                    await WarehouseAsync(tx, root, member, input.WarehouseId);
                    if (!string.IsNullOrWhiteSpace(input.DestinationWarehouseId))
                    {
                        Require(input.DestinationWarehouseId != input.WarehouseId, "Choose a different destination warehouse.");
                        var wh = await tx.GetAsync<Warehouse>(root + "warehouses/" + Id(input.DestinationWarehouseId));
                        Require(wh is { Active: true }, "Destination warehouse not found.", 404);
                    }

                    Require(input.Lines.Count is> 0 and <= 40, "A dispatch needs 1–40 product lines.");
                    Require(input.Lines.Select(l => l.ProductId).Distinct().Count() == input.Lines.Count, "Duplicate product lines are not allowed.");
                    foreach (var line in input.Lines)
                    {
                        Require(line.Quantity > 0, "Dispatch quantities must be positive.");
                        var available = await StockAt(input.WarehouseId, line.ProductId);
                        Require(available.Available >= line.Quantity, "Insufficient stock.", 409);
                    }

                    Put("dispatches/" + result.Id, new Dispatch { Id = result.Id, SourceWarehouseId = input.WarehouseId, DestinationWarehouseId = input.DestinationWarehouseId, Destination = input.Name, Reference = input.Reference, Notes = input.Notes, HandlerUid = uid, CreatedBy = uid, CreatedAt = now, Lines = input.Lines.Select(l => new DispatchLine { ProductId = l.ProductId, Quantity = l.Quantity }).ToList() });
                    break;
                case "dispatch.pick":
                case "dispatch.stage":
                case "dispatch.review":
                case "dispatch.reject":
                case "outbound.verify":
                case "outbound.approve":
                case "outbound.reject":
                    var dispatch = await tx.GetAsync<Dispatch>(root + "dispatches/" + result.Id);
                    Require(dispatch is not null, "Dispatch not found.", 404);
                    await WarehouseAsync(tx, root, member, dispatch!.SourceWarehouseId);
                    if (command == "dispatch.pick" || command == "outbound.verify")
                    {
                        Require(dispatch.Status == (command == "dispatch.pick" ? "Draft" : "OutboundPending"), "Invalid dispatch state.", 409);
                        var line = dispatch.Lines.FirstOrDefault(l => l.ProductId == input.ProductId);
                        Require(line is not null, "Line not found.", 404);
                        var p = await ProductAsync(tx, root, input.ProductId);
                        Require(Matches(p, input.Barcode), "Barcode/SKU does not match.");
                        if (command == "dispatch.pick")
                            line!.Picked = true;
                        else
                            line!.VerifiedOutbound = true;
                    }

                    if (command == "dispatch.stage")
                    {
                        Require(dispatch.Status == "Draft" && dispatch.Lines.All(l => l.Picked), "Pick all lines first.", 409);
                        dispatch.Status = "Staged";
                    }

                    if (command == "dispatch.review")
                    {
                        Require(dispatch.Status == "Staged", "Stage dispatch first.", 409);
                        dispatch.Status = "OutboundPending";
                        dispatch.StagedReviewedBy = uid;
                    }

                    if (command == "dispatch.reject" || command == "outbound.reject")
                    {
                        Require(dispatch.Status == (command == "dispatch.reject" ? "Staged" : "OutboundPending"), "Invalid rejection state.", 409);
                        dispatch.Status = "Rejected";
                        if (command == "dispatch.reject")
                            dispatch.StagedReviewedBy = uid;
                        else
                            dispatch.OutboundReviewedBy = uid;
                    }

                    if (command == "outbound.approve")
                    {
                        Require(dispatch.Status == "OutboundPending" && dispatch.Lines.All(l => l.VerifiedOutbound), "Verify outbound lines first.", 409);
                        // Recheck availability and post every line in this one transaction.
                        for (var i = 0; i < dispatch.Lines.Count; i++)
                        {
                            var l = dispatch.Lines[i];
                            await Post(dispatch.SourceWarehouseId, l.ProductId, -l.Quantity, "Outbound", i.ToString());
                        }

                        dispatch.Status = "Posted";
                        dispatch.OutboundReviewedBy = uid;
                        if (!string.IsNullOrWhiteSpace(dispatch.DestinationWarehouseId))
                        {
                            var destination = await tx.GetAsync<Warehouse>(root + "warehouses/" + Id(dispatch.DestinationWarehouseId));
                            Require(destination is { Active: true }, "Destination warehouse is unavailable.", 409);
                            var transfer = new Transfer
                            {
                                Id = dispatch.Id,
                                SourceWarehouseId = dispatch.SourceWarehouseId,
                                DestinationWarehouseId = dispatch.DestinationWarehouseId
                            };
                            for (var i = 0; i < dispatch.Lines.Count; i++)
                            {
                                var l = dispatch.Lines[i];
                                var rid = dispatch.Id + "-" + i;
                                transfer.ReceiptIds.Add(rid);
                                Put("receipts/" + rid, new Receipt { Id = rid, TransferId = transfer.Id, ProductId = l.ProductId, WarehouseId = transfer.DestinationWarehouseId, Quantity = l.Quantity, Reference = dispatch.Reference, CreatedBy = uid, CreatedAt = now });
                            }

                            Put("transfers/" + transfer.Id, transfer);
                        }
                    }

                    Put("dispatches/" + result.Id, dispatch);
                    break;
                case "audit.create":
                    Require(await tx.GetAsync<Entity>(root + "audits/" + result.Id)is null, "A workflow with this identifier already exists.", 409);
                    await WarehouseAsync(tx, root, member, input.WarehouseId);
                    Require(input.Counts.Count is> 0 and <= 40 && input.Counts.Select(l => l.ProductId).Distinct().Count() == input.Counts.Count, "An audit needs 1–40 distinct product counts.");
                    foreach (var l in input.Counts)
                    {
                        Require(l.Counted >= 0, "Counts cannot be negative.");
                        var s = await StockAt(input.WarehouseId, l.ProductId);
                        Require(s.Version == l.ExpectedVersion, "Inventory changed; reload before counting.", 409);
                    }

                    Put("audits/" + result.Id, new Audit { Id = result.Id, WarehouseId = input.WarehouseId, CreatedBy = uid, CreatedAt = now, Lines = input.Counts });
                    break;
                case "audit.approve":
                case "audit.reject":
                    var audit = await tx.GetAsync<Audit>(root + "audits/" + result.Id);
                    Require(audit is { Status: "Pending" }, "Audit is not pending.", 409);
                    await WarehouseAsync(tx, root, member, audit!.WarehouseId);
                    if (command == "audit.approve")
                        for (var i = 0; i < audit.Lines.Count; i++)
                        {
                            var l = audit.Lines[i];
                            var s = await StockAt(audit.WarehouseId, l.ProductId);
                            Require(s.Version == l.ExpectedVersion, "Stock changed after count. Recount before approval.", 409);
                            await Post(audit.WarehouseId, l.ProductId, l.Counted - s.OnHand, "Audit", i.ToString());
                        }

                    audit.Status = command == "audit.approve" ? "Approved" : "Rejected";
                    Put("audits/" + result.Id, audit);
                    break;
                default:
                    throw new ServiceException(400, "Unsupported command.");
            }

            Put("operations/" + operation, new Replay { Fingerprint = fingerprint, Result = result });
            return result;
        });
    }

    public async Task RequireAdministratorAsync(string uid, string company)
    {
        var member = await MemberAsync(store, Root(company), uid);
        Require(Admin(member), "Company administrator permission is required.", 403);
    }

    private static void ValidateLogo(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && value.Length <= 2000)
            return;
        var prefix = value.StartsWith("data:image/png;base64,", StringComparison.Ordinal) ? "data:image/png;base64," : value.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal) ? "data:image/jpeg;base64," : "";
        Require(prefix.Length > 0 && value.Length <= 280000, "Use a HTTPS logo URL or PNG/JPEG image under 200 KB.");
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(value[prefix.Length..]);
        }
        catch (FormatException)
        {
            throw new ServiceException(400, "Invalid logo image.");
        }

        Require(bytes.Length <= 204800 && (prefix.Contains("png") ? bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) : bytes.AsSpan().StartsWith(new byte[] { 255, 216, 255 })), "Invalid logo image.");
    }

    private static bool Matches(Product p, string code) => !string.IsNullOrWhiteSpace(code) && (p.Sku.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase)));
    private static async Task NoPending(ICompanyReader data, string root, string product, string? warehouse)
    {
        Require(!(await data.ListAsync<Receipt>(root + "receipts")).Any(r => r.ProductId == product && (warehouse is null || r.WarehouseId == warehouse) && r.Status == "Pending"), "Pending receipt exists.", 409);
        Require(!(await data.ListAsync<Dispatch>(root + "dispatches")).Any(d => d.Lines.Any(l => l.ProductId == product) && (warehouse is null || d.SourceWarehouseId == warehouse || d.DestinationWarehouseId == warehouse) && d.Status is not ("Posted" or "Rejected")), "Pending dispatch exists.", 409);
        Require(!(await data.ListAsync<Audit>(root + "audits")).Any(a => a.Lines.Any(l => l.ProductId == product) && (warehouse is null || a.WarehouseId == warehouse) && a.Status == "Pending"), "Pending audit exists.", 409);
    }

    public sealed class Replay
    {
        public string Fingerprint { get; set; } = "";
        public Result Result { get; set; } = new();
    }
}
