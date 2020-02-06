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

    [Environment("development")]
    public Service Redis { get; } = new Service("redis")
    {
        Port = 6379,
        Protocol = "redis",
    };
}

Pipeline.Configure<Application>(app =>
{
    app.TodoWeb.Bindings.Add(new ServiceBinding(app.TodoWorker));

    app.TodoWorker.Bindings.Add(new ServiceBinding(app.Redis));
});
