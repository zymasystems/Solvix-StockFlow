using Google.Cloud.Firestore;
using Solvix.Web.Contracts.Responses;
using Solvix.Web.Services;
using Solvix.Web.Services.Internal;
namespace Solvix.Web.Repositories.Internal;
internal static class FirestoreStore
{
    // Cursor is the last document ID. Ordering is document ID, not name/date.
    public static async Task<PagedResponse<T>> Page<T>(FirestoreDb db,string collection,int size,string? cursor,Func<DocumentSnapshot,T> map)
    {
        if(size<1 || size>100) throw new ServiceException(400,"Page size must be between 1 and 100.");
        Query query=db.Collection(collection).OrderBy(FieldPath.DocumentId);
        if(!string.IsNullOrEmpty(cursor)) query=query.StartAfter(ServiceRules.Id(cursor));
        var docs=(await query.Limit(size+1).GetSnapshotAsync()).Documents;
        return new PagedResponse<T>{Items=docs.Take(size).Select(map).ToList(), NextCursor=docs.Count>size ? docs[size-1].Id : null};
    }
    public static async Task<List<DocumentSnapshot>> All(FirestoreDb db,string collection)
    {
        var docs=(await db.Collection(collection).Limit(10001).GetSnapshotAsync()).Documents;
        if(docs.Count>10000) throw new ServiceException(422,"This report exceeds the 10,000-document limit. Add server-side aggregates before increasing it.");
        return docs.ToList();
    }
    public static async Task<DocumentSnapshot> Get(FirestoreDb db,string collection,string id)
    {
        var s=await db.Collection(collection).Document(ServiceRules.Id(id)).GetSnapshotAsync();
        if(!s.Exists) throw new ServiceException(404,"Record not found.");
        return s;
    }
}
