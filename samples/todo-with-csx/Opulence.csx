#r "/Users/ryan/github.com/rynowak/opulence/src/Opulence/bin/Debug/netstandard2.0/Opulence.dll"

using Opulence;

public class Application
{
    public ApplicationGlobals Globals { get; } = new ApplicationGlobals()
    {
        Registry = new ContainerRegistry("rynowak"),
    };

    public Service TodoWeb { get; } = new Service("todo-web");

    public Service TodoWorker { get; } = new Service("todo-worker");
}

Pipeline.Configure<Application>(app =>
{
    app.TodoWeb.Bindings.Add(ServiceBinding.FromService(app.TodoWorker));
});
