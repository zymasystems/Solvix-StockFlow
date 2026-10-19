using Solvix.Web.Contracts.Requests;
using Solvix.Web.Contracts.Responses;
namespace Solvix.Web.Services.Interfaces;
public interface IReportService
{
    Task<InventoryReportResponse> GetInventoryAsync();
}
