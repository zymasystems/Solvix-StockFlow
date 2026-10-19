using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Solvix.Web.Services;
using Solvix.Web.Services.Interfaces;
using Solvix.Web.Services.Internal;
using Solvix.Web.Repositories;
using Solvix.Web.Repositories.Interfaces;
namespace Solvix.Web.Extensions;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSolvixServices(this IServiceCollection services,IConfiguration configuration)
    {
        var projectId=configuration["Firebase:ProjectId"];
        if(string.IsNullOrWhiteSpace(projectId)) throw new InvalidOperationException("Set Firebase:ProjectId.");
        // Load service account path from configuration to avoid relying on ADC.
        var saPath = configuration["Firebase:ServiceAccountPath"];
        services.AddSingleton<GoogleCredential>(_ => string.IsNullOrWhiteSpace(saPath)
            ? GoogleCredential.GetApplicationDefault()
            : CredentialFactory.FromFile<ServiceAccountCredential>(saPath).ToGoogleCredential());

        services.AddSingleton(sp => new FirestoreDbBuilder
        {
            ProjectId = projectId,
            GoogleCredential = sp.GetRequiredService<GoogleCredential>()
        }.Build());

        services.AddSingleton(sp => FirebaseApp.Create(
            new AppOptions
            {
                Credential = sp.GetRequiredService<GoogleCredential>(),
                ProjectId = projectId
            },
            "SolvixServices"));

        services.AddSingleton(sp =>
            FirebaseAuth.GetAuth(sp.GetRequiredService<FirebaseApp>()));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService,CurrentUserService>();
        services.AddScoped<IProductService,ProductService>();
        services.AddScoped<ISupplierService,SupplierService>();
        services.AddScoped<ISalesService,SalesService>();
        services.AddScoped<IStockService,StockService>();
        services.AddScoped<IAccountService,AccountService>();
        services.AddScoped<IUserService,UserService>();
        services.AddScoped<IDashboardService,DashboardService>();
        services.AddScoped<IReportService,ReportService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<InventoryWriter>();
        return services;
    }
}
