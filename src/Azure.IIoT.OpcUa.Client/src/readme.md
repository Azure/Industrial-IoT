# Client SDK Design notes

## Requirements

- The SDK shall support trimming but will also provide complex type handling. It will
replace the complex type support that is reflection based.
- SDK shall be used in 2 ways: Fluent and configuration driven. The SDK surface is
small and usage is self-evident. The SDK abstracts all network and protocol
complexity from the user.
- The SDK shall leverage the latest advances in .net and shall be highly scalable and 
performant. This also includes leveraging defacto standards like Polly for resiliency
Rate limiting, and latest diagnostics (tracing, logging and metrics), ValueTask etc.
The SDK can be used with or without DI.
- The SDK will allow building highly available clients by supporting automatic
state synchronization between two or more clients and their contents (sessions, 
subscriptions, monitored items) using event sourcing between the underlying state 
machines and transferring communication channels to the elected leader.
- The SDK will try to adapt to the server, so that the user application does not have 
to. This means e.g. support for pooling of sessions and decoupling subscribing to data
from server side subscriptions (and thus server limitations). 
- The SDK will incorporate the features implemented in OPC Publisher where it makes
sense, this includes subscribing to objects and objects of type.
- Full code coverage at unit test level rather than requiring integration tests.

### Future

- The SDK API should support writing tests without requiring a server similar to 
https://flurl.dev/ experience.
- The SDK will for now use the OPC UA .NET Standard stack from OPC Foundation. The 
SDK will replace the SDK from the OPC Foundation. It sits on top of the model
compiled types and the provided channels and networking protocols. In the future 
we will replace the stack as well as code generation tooling to use source generator.
- The SDK will be used by all Microsoft products requiring OPC UA client functionality.
- The SDK will support the OPC UA PubSub and used by all Microsoft products that 
require OPC UA PubSub functionality.
- Esoteric APIs such as SetTriggering, Query.

## Fluent api 

The fluent api allows to create a client and from that client, sessions, and 
subscriptions. An example is provided below.

```csharp

// buidl client, multiple clients can be used in the same process
var builder = new ClientBuilder();
var client = builder.NewClient()
    .WithName("Test Application")
    .WithUri("uri")
    .WithProductUri("Pro")
    .WithTransportQuota(o => o.SetMaxBufferSize(100))
    .UpdateApplicationFromExistingCert()
    .WithMaxPooledSessions(10)
    .AddLogging(o => o.AddConsole()))
    .AddMetrics(o => o.SetOtelEndpoint(...))
    .Build();

var ownCertificate = await client.Certificates.GetOwnCertificateAsync();
await client.Certificates.AddTrustedCertificateAsync("cert", ...);

// A pooled session can be retrieved from the client that lingers
using var session = await client
    .ConnectTo("opc.tcp://testep")
    .WithSecurityMode(MessageSecurityMode.SignAndEncrypt)
    .WithSecurityPolicy(SecurityPolicies.None)
    .WithServerCertificate([])
    .FromPool
        .WithOption(o => o.WithTimeout(TimeSpan.FromMicroseconds(1000)))
        .WithOption(o => o.WithKeepAliveInterval(TimeSpan.FromSeconds(30)))
        .CreateAsync()
        .ConfigureAwait(false);

await session.Session.CallAsync(null, null).ConfigureAwait(false);
await session.Session.CallAsync(null, null).ConfigureAwait(false);

// A direct session (no pooling) can also be retrieved. 
using var direct = await client
    .ConnectTo("opc.tcp://testep")
    .UseReverseConnect()
    .WithOption(o => o.WithUser(new UserIdentity()))
    .CreateAsync()
    .ConfigureAwait(false);

await foreach (var browse in direct.Session.BrowseAsync("ns=1;s=Test")).ConfigureAwait(false) {
    // Handle browse
})

using var subscription = await direct
    .Subscribe("mysubname", s => s
        .WithPublishingInterval(TimeSpan.FromSeconds(1))
        .WithKeepAliveCount(3)
        .WithLifetimeCount(10)
        .WithMaxNotificationsPerPublish(100)
        .WithPriority(0)
        .To(o => o
            .Events("myevent", o => o
                .OfType("ns=1;s=myevent")
                .WithQueueSize(10))
            .Events("alarms", o => o
                .ThatMatch("select * from AlarmEventType where ..."))
            .Conditions("condition", o => o
                .WithSnapshot(TimeSpan.FromSeconds(1))
            .Variables("first", o => o
                .WithNodeId("ns=1;s=first")
                .SampledEvery(TimeSpan.FromSeconds(1))
                .WithQueueSize(10)
                .DiscardOldest())
            .Variables("second", o => o
                .OfType("ns=1;s=variableTypes")
                .SampledEvery(TimeSpan.FromSeconds(1))
                .AutomaticallySetQueueSize())
            .Objects("second", o => o
                .WithNodeId("ns=1;s=second")
                .IncludeEvents()
                .SampledEvery(TimeSpan.FromSeconds(1)))
            .Objects("second", o => o
                .OfType("ns=1;s=mytypedefintion")
                .IncludeEvents()
                .SampledEvery(TimeSpan.FromSeconds(1)))
            .ModelChangeEvents())
        .TrackModelChanges()))
    .Sample("sampleditems", 
        .Variables("first", o => o
            .WithNodeId("ns=1;s=first")
            .Every(TimeSpan.FromSeconds(2))
            .WithMaxAge(TimeSpan.FromSeconds(1)))
        .Variables("second", o => o
            .FromVariable("ns=1;s=second")
            .Every(TimeSpan.FromSeconds(10)))
        .Objects("second", o => o
            .WithNodeId("ns=1;s=second")
            .SampledEvery(TimeSpan.FromSeconds(1)))
        .Objects("second", o => o
            .OfType("ns=1;s=mytypedefintion")
            .SampledEvery(TimeSpan.FromSeconds(1)))
        .ModelChangeEvents()))
    .CreateAsync()
    .ConfigureAwait(false);

await foreach (var notification in subscription.ReadAsync().ConfigureAwait(false)) {
    switch (notification) {
        case Event e:
            // Handle event
            break;
        case ConditionChange c:
            // Handle condition
            break;
        case DataChange v:
            // Handle variable
            break;
        case PeriodicData d:
            // Handle sampled variables
            break;
        case KeepAlive a:
            // Handle keep alive
            break;
    })
    // Handle notification
})
```

## Configuration driven

This approach integrates well with Dependency Injection and automatically bound options
from configuration files. The configuration approach looks like this:

```csharp

var services = new ServiceCollection();
services.AddOpcUa();
services.ConfigureNamedOption<ClientOptions>("myapp", o => {
    o with {... }
}
services.ConfigureNamedOption<SessionOptions>("mysession", o => {
    o with { ... }
}
services.ConfigureNamedOption<SubscriptionOptions>("mysub", o => {
    o with { ... }
}

var provider = services.BuildServiceProvider();
var client = await provider.ResolveNamed<IClientBuilder>("app").CreateAsync();
var session = await client.CreateSessionAsync("mysession");
var subscription = session.AddSubscription("mysub");

```

## Patterns

Session, subscription and monitored item API is generally about synchronizing the state on 
the server with the state on the client. E.g. the client creates a session using the API
which then is created on the server. The client then updates parameters and thus the session
with the handle is updated with the parameters on the server. This is the case for all three
entities on the server. For efficiency reasons, monitored items can be updated in a bulk
OPC UA service call. The same applies to setting the subscription publishing mode.

The goal of the sdk is to provide a simple API where the implementation manages the state
of all entities (channels, sessions, subscriptions, monitored items). The user can change the
configuration using an options pattern. The entities are created with a set of options that
can be updated at any time and the implementation will synchronize the state on the server.
While the entities are alive they handle their own internal state, e.g. reconnect handling,
the user of the sdk will just provide resiliency configuration (using Polly API).

The sdk uses the IOptionsMonitor<TOption> interface from Microsoft.Extensions.Options to 
leverage the signal that determines an update of the entity is needed.  The update itself 
is then performed by a background task bound to the entity, e.g. the subscription or subscription
manager (session) updates all subscriptions when a subscription state changes (signalled by 
the Option change token) and applies the change.

The benefit of using IOptionsMonitor<TOption> is that these options can be bound to any 
source (e.g. configuration or in memory or a database) while efficiently signalling change
(simple set/get, i.e. non async).


We use an auto reset event to signal any 
