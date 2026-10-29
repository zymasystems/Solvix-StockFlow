# Sequential record codes

The backend assigns readable codes when both the website and mobile API save records. Firebase document IDs, Firebase Auth UIDs, SKU/barcodes, and all foreign-key fields stay unchanged. A code is a reference, not an authentication credential or permission check. Existing page layouts are unchanged.

| Record | Example | Counter scope |
| --- | --- | --- |
| Company | `cmp-01` | System-wide |
| Registered user | `usr-01` | System-wide |
| Company membership | `mem-01` | Company |
| Warehouse | `wh-01` | Company |
| Product | `prd-01` | Company |
| Supplier | `spr-01` | Company |
| Warehouse stock record | `stk-01` | Company (across warehouses) |
| Receipt / receiving | `rcv-01` | Company |
| Sale / outbound dispatch order | `sal-01` | Company |
| Warehouse transfer | `trf-01` | Company |
| Stock audit | `aud-01` | Company |
| Stock movement / adjustment | `mov-01` | Company |

Codes are stored as the top-level `code` field so they are visible in the Firebase console, and as `Code` in company records' existing JSON payload. Shared API entities, command results and profile responses expose `Code` (`code` under normal web JSON serialization). The API must still receive the original IDs for operations. An unmodified mobile build continues working because codes are additive response fields.

`sal` identifies the dispatch order used by the sales workflow; its stock ledger entries have separate `mov` codes. New movements retain `SourceId`, so the website's sales API adapter can return the same order code after posting. A transfer also has a separate `trf` record and destination receipts. Codes do not bypass approval gates.

Counters live at `recordSequences/{prefix}` for system-wide records, and `companies/{companyId}/recordSequences/{prefix}` for company records. Each counter update and record write commit in one Firestore transaction, with all reads before writes. Concurrent transactions retry automatically. A replacement/update preserves the stored code; submitted entity codes are ignored. Replaying an operation's existing request ID returns the same result without allocating another code. Deleted codes are never recycled; do not delete/reset counters. Numbering continues from `99` to `100`. Sequence order is transaction allocation order, not guaranteed chronological order of user clicks.

Existing records receive a code on their next successful save. For untouched records, the operator-only backfill is optional, dry-run by default, repeatable, and preserves every ID and business field. It pages through records and re-reads each inside a transaction; it can be resumed safely. It does not import legacy global inventory. Use the existing legacy migration separately before numbering imported company data.

```sh
# Authenticate on a trusted operator machine with the intended project's ADC.
# Preview one company's existing records and the global registered users:
dotnet run --project tools/Solvix.Migrate -- --record-codes --project YOUR_PROJECT_ID --company ORIGINAL_COMPANY_ID --users

# After reviewing the preview, persist the codes:
dotnet run --project tools/Solvix.Migrate -- --record-codes --project YOUR_PROJECT_ID --company ORIGINAL_COMPANY_ID --users --apply
```

Repeat with each company's original ID; run `--users` only once (reruns skip numbered users). No live Firebase data is changed by merging this code alone. Deploy the backend before using the backfill.

Validation uses the isolated Firestore emulator to check concurrent creation, stable edits, client-supplied-code rejection, deletion/non-reuse, rollback, company isolation, multiple warehouse stock records, numbering after 99, request replay, global user codes, and backfill reruns. Keep direct client writes to inventory and sequence collections disabled by Firestore rules; all business operations use the authorized backend.
