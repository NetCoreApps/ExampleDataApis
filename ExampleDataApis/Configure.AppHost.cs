using Funq;
using ServiceStack;
using ExampleDataApis.ServiceInterface;
using ExampleDataApis.ServiceModel;

[assembly: HostingStartup(typeof(ExampleDataApis.AppHost))]

namespace ExampleDataApis;

public class AppHost : AppHostBase, IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            // Configure ASP.NET Core IOC Dependencies
        });

    public AppHost() : base("SSG Examples", typeof(MyServices).Assembly) {}

    public override void Configure(Container container)
    {
        // Configure ServiceStack only IOC, Config & Plugins
        SetConfig(new HostConfig {
            UseSameSiteCookies = true,
        });
        
        Plugins.Add(new CorsFeature(new[] {
            "http://localhost:5173", //vite dev
            "http://localhost:5000", //dotnet dev
            "https://localhost:5001", //dotnet dev
            "https://xkcd.netcore.io"
        }, allowCredentials:true));
    }
}
