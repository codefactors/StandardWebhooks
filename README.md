# Codefactors.DataFabric
## Data synchronisation layer for C# and TypeScript

Codefactors.DataFabric is a library that provides a simple and efficient way to subscribe to data changes in a database. It is designed to be used in a microservices architecture where services need to be notified of changes in a database.

## SignalR Transport
```csharp
using Microsoft.AspNetCore.SignalR;
using Codefactors.DataFabric.Subscriptions;
using Codefactors.DataFabric.Transport;
using Codefactors.DataFabric.Transport.SignalR;

namespace Codefactors.DataFabric.WebApi.Transport;

public class SignalRHelper(Uri hubPath) : ITransportHelper
{
    private readonly Uri _hubPath = hubPath;

    public void Initialise(IServiceCollection services)
    {
        services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();

        services.AddSignalR();

        services.AddSingleton<IDataFabricTransport, SignalRTransport>();
        services.AddSingleton<ISubscriptionFactory, SignalRSubscriptionFactory>();
    }

    public void InitialiseMiddleware(object app)
    {
        if (app is WebApplication application)
        {
            application.UseMiddleware<SignalRAuthenticationMiddleware>(_hubPath.ToString());
        }
        else
        {
            throw new ArgumentException("Unable to initialise transport middleware; invalid application type");
        }
    }

    public void Start(object app)
    {
        if (app is WebApplication application)
        {
            application.MapHub<NotificationHub>(_hubPath.ToString())
                .RequireAuthorization();
        }
        else
        {
            throw new ArgumentException("Unable to start transport; invalid application type");
        }
    }
}


```

### License
This project is licensed under the MIT License.
