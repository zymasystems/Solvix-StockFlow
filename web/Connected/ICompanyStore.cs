namespace Solvix.Web.Connected;
public interface ICompanyReader
{
    Task<List<Solvix.Contracts.Company>> ListCompaniesAsync();
    Task<T?> GetAsync<T>(string path)
        where T : class;
    Task<List<T>> ListAsync<T>(string path)
        where T : class;
}

public interface ICompanyTransaction : ICompanyReader
{
    void Put<T>(string path, T value)
        where T : class;
    void Delete(string path);
}

public interface ICompanyStore : ICompanyReader
{
    Task PurgeCompanyAsync(string companyId);
    Task<T> RunAsync<T>(Func<ICompanyTransaction, Task<T>> action);
}
