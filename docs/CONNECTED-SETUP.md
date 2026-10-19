# Connected mobile and web inventory

Both clients authenticate with the same Firebase project. The ASP.NET backend verifies Firebase ID tokens for mobile API calls and issues a secure cookie after website sign-in. Inventory data lives in Firestore and is accessed only by the backend. Never package an Admin SDK key in the mobile app or browser.

## Projects

| Folder | Responsibility |
| --- | --- |
| `shared/Solvix.Contracts` | IDs, request/response models and client interface |
| `shared/Solvix.Workspace` | Website operations UI; the mobile app retains its uploaded original UI |
| `web/Connected` | Company access, stock transactions, approval rules, profiles and Firestore adapter |
| `web/ApiControllers/CompaniesApiController.cs` | Authenticated mobile JSON endpoints |
| `mobile/Services/Authentication` | Firebase email/password login, refresh, recovery, secure refresh-token storage |
| `mobile/Services/ApiService.cs` | HTTPS, bearer tokens, ProblemDetails and bounded read retries |
| `mobile/Services/MobileWorkspaceClient.cs` | HTTP implementation of the shared client interface |
| `mobile/Services/InventoryService.cs` | API-backed adapter for the original mobile pages/models |
| `connected.tests` | Tenant/warehouse isolation, HTTP and emulator transaction tests |
| `mobile-tests` | Production authentication and HTTP code tested without a device |
| `tools/Solvix.Migrate` | Dry-run-first, resumable legacy import |

The mobile presentation is restored from the supplied `mobile.zip`: all pages, three layouts, bottom navigation, company modal, scanner components, fonts/styles and images are retained. Its original `InventoryService` interface is adapted to asynchronous API operations; no demo login or local stock ledger is authoritative. The mobile project references the shared contracts, not the website's `Solvix.Workspace` UI library. The website continues to use that library.

`tools/verify-mobile-ui.py` checks the visible source of 39 supplied components and 18 assets against the uploaded baseline. Handler code and injection directives can change; visual markup cannot change accidentally. The audit confirmation text now states that administrator approval is pending. Routes add the authentication/data gate, connection feedback and refresh behavior without replacing any existing screen. Notification settings and the original achievements screen remain as supplied; no push-delivery or new gamification backend is introduced.

## Configure the website

1. Enable Email/Password under Firebase Authentication > Sign-in method. Use the same project for both clients. Add your website domain to Firebase Authentication authorized domains. Configure Firebase email action links and templates for password reset and verified email change. Google/Microsoft website buttons also require those providers to be enabled.
2. Set `Firebase:ProjectId` and the public `Firebase:Web` configuration in `web/appsettings.json` (the placeholder API key must be replaced). Firebase web API keys identify the project; they are different from Admin SDK private keys.
3. Give the server Application Default Credentials. On a managed Google host, use an attached service account with the required Firestore and Firebase Auth IAM permissions. Locally, set `GOOGLE_APPLICATION_CREDENTIALS` to a NEW service-account JSON file outside the repo, or set `Firebase:ServiceAccountPath` through .NET user secrets. Both Firestore and Firebase Admin use the same registered credential. Revoke any previously exposed private key.
4. Run `dotnet run --project web/Solvix.Web.csproj --launch-profile https`. Trust your development HTTPS certificate. Visit `/Account/Register`, then sign in. A Firebase identity may open the onboarding shell without a global profile approval; company membership controls actual inventory access.
5. Create your company. This creates an administrator membership **only for that new company**. Add warehouses under Operations. Share the generated company ID with staff; joining creates a Pending membership. Review it, assign warehouses and activate it under Operations. Never make a client-supplied role authoritative.

Use user secrets for local overrides:

```powershell
dotnet user-secrets init --project web/Solvix.Web.csproj
dotnet user-secrets set "Firebase:ServiceAccountPath" "C:\SolvixSecrets\new-service-account.json" --project web/Solvix.Web.csproj
```

Do not put the key under `wwwroot`. Do not commit it. Existing `users/{uid}.role` and `.status` are retained only for legacy profile compatibility and do not grant access to any connected company.

## Configure the mobile app

1. Put the same public Firebase project ID/API key in `mobile/authsettings.json`.
2. Set `Solvix.Api.BaseUrl` in `mobile/appsettings.json` to your actual **HTTPS** backend address with a trailing slash. A physical phone cannot reach your computer through `localhost`. Use a reachable HTTPS host/tunnel with a trusted certificate for development. Certificate validation is never bypassed.
3. Install .NET 10, MAUI Android, Android SDK 36 and JDK 21. Open/build `mobile/Solvix.Sims.csproj`. CI builds a debug APK without production signing secrets; use a separate protected release job for store signing.
4. Register/sign in using Firebase credentials. Only refresh tokens for remembered sessions go into platform SecureStorage. ID tokens stay in memory and are refreshed before expiry. Session UI starts signed out and verifies the backend identity before showing protected pages.
5. Both clients load company memberships and permitted warehouses from the backend. The selected company/warehouse is reflected in `InventoryService` and `SessionState`; switching discards stale loads and clears previous company data. Sign-out removes tokens and the protected workspace. No local inventory database is treated as authoritative.

The API client retries GET network/429/502/503/504 failures up to three total attempts, with short bounded delays. It does not automatically retry writes. Each command has a stable `RequestId`; retry an uncertain submission with the same unchanged body. Reusing that ID with different data returns 409. A 401 clears authentication; a 403 means access was denied and does not log the user out. An offline app displays an error; it does not pretend a stock operation succeeded.

## Data model and rules

Firestore connected documents use a typed JSON payload in a `json` field and a server `updatedAt` timestamp. Do not manually create individual fields using the legacy global schema. DTO timestamps inside JSON are ISO 8601 UTC. Integer quantities represent whole units; monetary unit cost is decimal with up to two fractional digits. Selling price, currency conversion, taxes and commercial invoicing are not implemented; do not interpret legacy selling prices as costs.

| Path below `companies/{companyId}` | Contents |
| --- | --- |
| `meta/company` | Name, HTTPS logo URL, theme color |
| `members/{firebaseUid}` | Role, approval status, assigned warehouse IDs |
| `warehouses/{warehouseId}` | Site name and active state |
| `products/{productId}` | Company catalog, SKU, barcode, cost and supplier link |
| `warehouses/{warehouseId}/stock/{productId}` | On-hand, reserved/quarantined quantities, reorder minimum, bin, version |
| `skuIndex`, `barcodeIndex` | Company-local normalized unique identifiers |
| `suppliers` | Company supplier directory |
| `receipts`, `dispatches`, `transfers`, `audits` | Workflow records |
| `movements` | Immutable transaction-generated quantity changes |
| `operations` | UID + request ID replay protection |
| `legacyImports`, `legacyArchive` | Migration markers and preserved source snapshots |

`userCompanies/{uid}/companies/{companyId}` is a discovery index, never authorization. Every write re-reads active company membership and warehouse access inside its transaction. Admins access company warehouses and manage membership/approvals; staff access only assigned warehouses, create products/receipts/dispatches and submit audits. Direct stock adjustments require a company administrator. A handler is the authenticated employee; choosing another employee never impersonates them.

Products and stock are separate. A SKU/barcode is unique in its company. Low stock is `OnHand <= MinStock`. Zero-stock records can be deleted only without pending workflows. Product archival requires zero quantities across all warehouses and no pending workflows, preserving identifiers/history. Warehouse archival also requires no stock and no pending receipts, dispatches or audits.

- Receiving: Pending -> arrival confirmation -> barcode/SKU verification -> admin approval -> stock added once. Rejection never changes stock.
- Dispatch: Draft -> all lines picked -> Staged -> admin stage approval -> OutboundPending -> outbound verification -> admin final approval -> Posted. Stock is rechecked and deducted atomically only at final approval. Rejection never deducts stock.
- Transfer: final outbound approval atomically creates destination receipt(s). Destination stock remains unchanged until arrival, verification and receipt approval. All approved receipts make the transfer Received. A rejected destination receipt makes it Exception; it does **not** silently put stock back at the source. An administrator must investigate/reconcile with an explained adjustment or return workflow.
- Audits: every count includes the observed stock version. Approval refuses changed versions; recount rather than overwriting intervening movements. Stock and ledger changes commit together.

Dispatches/audits support up to 40 distinct lines. This first implementation bounds a collection to 1,000 documents and fails explicitly above that rather than displaying incomplete totals. Resource endpoints paginate responses with stable document-ID cursors, but currently read a bounded authorized workspace; they are not scalable indexed queries. Before exceeding this capacity, implement indexed queries, archived workflow feeds and persisted aggregates. The dashboard presents cost valuation, low stock and daily movement counts; advanced forecasting is not included.

## Endpoints

All `/api` calls require `Authorization: Bearer <Firebase ID token>`; browser cookies do not authenticate API calls.

| Method and route | Purpose |
| --- | --- |
| GET `/api/me/companies` | Memberships, including pending requests |
| POST `/api/companies` | New company with its own owner membership |
| POST `/api/companies/{id}/join` | Request access to an existing company |
| GET `/api/companies/{id}/workspace?warehouseId=...&search=...` | Permitted snapshot |
| GET `/api/companies/{id}/barcode/{code}?warehouseId=...` | Exact SKU/barcode lookup |
| GET `/api/companies/{id}/resources/{resource}?warehouseId=...&pageSize=20&cursor=...` | Page of products, warehouses, suppliers, members, receipts, dispatches, transfers, audits or movements |
| POST `/api/companies/{id}/commands/{command}` | Validated, idempotent transaction |
| GET/PUT `/api/me/profile` | Name and profile picture |

POST `/api/companies/{id}/employees` lets a company administrator add an already-registered Firebase account by verified directory lookup. The original mobile employee form uses this endpoint. Selecting “All warehouses” assigns the currently permitted company warehouse IDs explicitly; newly created sites require an updated assignment.

Command names are defined in `CompanyService.ExecuteAsync` and used directly by the shared UI. Send JSON `Operation` fields using camelCase. IDs are backend IDs, never display names or list indexes. The shared `IWorkspaceClient` is the transport contract; `InventoryService` maps its DTOs into the original mobile models and calls it from the original event handlers. Existing global inventory API/MVC controllers are deliberately undiscoverable; deploying old and new clients together requires a coordinated cutover.

Profile pictures support private PNG/JPEG uploads up to 200 KB or HTTPS URLs. The original mobile photo pickers remain; images are compressed on the device before persistence. Display-only product/company/supplier/warehouse details are stored in bounded `UiJson` fields, separate from authoritative IDs, roles and quantities. Warehouse display details/documents remain warehouse-scoped. Large warehouse attachments still require dedicated file storage: JSON display records accept at most 400 KB and oversized submissions fail explicitly. Profile email comes from Firebase Auth. The website login-email flow reauthenticates with the current password and requests verification of the new address, then signs out. The original mobile profile form requests Firebase verification using the current authenticated session; Firebase may require the user to sign in again if that session is too old. Neither client writes an unverified email into its profile as if the login had changed. Email/password reauthentication currently applies to password accounts; federated accounts require provider-specific reauthentication before adding this UI flow.

## Migrate existing global data

Back up/export Firestore first. Stop legacy writers during the cutover; retire old deployments and any direct client writes. Create the intended company and warehouse using the new UI. Confirm who owns them and whether the legacy `price` means cost. This tool deliberately refuses to infer a company or reinterpret a selling price.

```sh
dotnet run --project tools/Solvix.Migrate -- --project YOUR_PROJECT --company COMPANY_ID --warehouse WAREHOUSE_ID --owner-uid FIREBASE_UID --legacy-price-is-cost
```

That is a read-only dry run. Fix duplicate SKUs, invalid prices, negative counts and destination conflicts before proceeding. Run the same command with `--apply` only after reviewing the dry run and backup. No production migration has been run as part of this change.

The importer preserves product/supplier IDs, creates company SKU indexes and warehouse opening stock, and writes one MigrationOpening movement per product. It archives legacy `products`, `suppliers`, `sales` and `adjustments` snapshots without replaying historical quantities. Timestamp values are preserved as ISO strings; legacy integer dates remain integers in the archive and are not fabricated into dates. Existing source documents remain intact. Per-document fingerprint markers make retries safe and detect modified source snapshots, including inside the import transaction. An interrupted import is resumable; it is not one all-or-nothing transaction across the entire database. Each source collection is limited to 1,000 rows; partition larger migrations explicitly.

Deploy the supplied `firestore.rules.connected` only after reviewing any unrelated existing rules. It denies direct browser/mobile Firestore access; Admin SDK IAM remains the server authorization boundary. Broad legacy client-write rules would bypass the approval ledger and must be closed during cutover.

## HTTPS hosting

`web/Dockerfile` builds from the repository root: `docker build -f web/Dockerfile -t solvix-web .`. Run behind a TLS reverse proxy with WebSockets enabled for server-side Blazor. Set the backend public DNS, `AllowedHosts`, `Firebase__ProjectId`, public Firebase web settings and server ADC through host configuration. Persist ASP.NET data-protection keys for stable cookies across restarts. Configure only the actual trusted proxy IPs in `Hosting__KnownProxies__0` etc.; do not trust arbitrary forwarded headers. An IIS/Kestrel HTTPS deployment is also supported. `/health/live` is a liveness endpoint, not a Firebase readiness check.

No live HTTPS host was provisioned and no production Firebase configuration was changed. Provide the resulting host URL to the mobile developer after deployment.

## Verification

```sh
dotnet test web.tests/Solvix.Web.Tests.csproj -m:1
dotnet test mobile-tests/Solvix.Sims.Tests.csproj -m:1
dotnet test connected.tests/Solvix.Connected.Tests.csproj -m:1
```

Without `FIRESTORE_EMULATOR_HOST`, emulator-specific tests are skipped. CI starts an isolated Firestore emulator at `127.0.0.1:8089` and runs them. HTTP tests use two independent clients against the same persisted test store and a test-only identity handler; production Firebase verification remains unchanged. Authentication core tests mock the Firebase REST service and do not use real accounts.

Before release, run a real-device smoke test against a staging backend: two registered accounts, company creation/join approval, warehouse assignments, inbound rejection/approval, transfer destination verification, concurrent outbound attempts, profile/email verification, disconnect/retry, sign-out and company switching. CI builds Android, but a build does not verify camera permissions or device SecureStorage behavior.


## Visual Studio: mobile closes or stops during startup

Set `mobile/Solvix.Sims.csproj` as the startup project and select the intended Android emulator or USB device. This project currently targets Android; Windows desktop is not a configured target. Build and debug with the .NET 10 SDK and the matching MAUI Android workload installed.

The splash and login screens can open before backend settings are loaded. API settings are read asynchronously when a request is made; missing, malformed, placeholder or non-HTTPS backend addresses produce a handled configuration message instead of throwing during service construction. Edit `mobile/appsettings.json` and `mobile/authsettings.json` and rebuild to package those changes. An emulator/device must be able to reach and trust the backend's HTTPS address; its `localhost` is the device itself.

If the app still closes, run it with the Visual Studio debugger attached. Open **Debug > Windows > Exception Settings**, enable breaking on thrown **Common Language Runtime Exceptions**, and reproduce the launch. Copy the first exception, its inner exception and stack trace, plus the adjacent **Output > Debug** messages. Include whether this is an emulator or physical device and its Android version. A successful APK build alone does not verify launch on that device. Avoid including tokens, passwords or service-account JSON in logs you share.
