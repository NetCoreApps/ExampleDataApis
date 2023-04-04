using ExampleDataApis.ServiceModel;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

[assembly: HostingStartup(typeof(ConfigureDb))]

namespace ExampleDataApis;

public class ConfigureDb : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => services.AddSingleton<IDbConnectionFactory>(
            new OrmLiteConnectionFactory(
                context.Configuration.GetConnectionString("DefaultConnection") ?? "App_Data/db.sqlite",
                SqliteDialect.Provider)))
        .ConfigureAppHost(afterConfigure: host =>
        {
            using var db = host.Resolve<IDbConnectionFactory>().OpenDbConnection();
            db.SeedXkcd();
        });
}