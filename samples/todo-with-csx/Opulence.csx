#r "/Users/ryan/github.com/rynowak/opulence/src/Opulence/bin/Debug/netstandard2.0/Opulence.dll"

using Opulence;

public class Application
{
    public ApplicationGlobals Globals { get; } = new ApplicationGlobals()
    {
        Registry = new ContainerRegistry("rynowak.azurecr.io"),
    };

    // Define more services and dependencies here as your application grows.
    public Service TodoWeb { get; } = new Service("todo-web");

    public Service TodoWorker { get; } = new Service("todo-worker");
}

Pipeline.Configure<Application>(app =>
{
    // Configure your service bindings here with code.
});
