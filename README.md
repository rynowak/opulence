# Opulence

White-glove service for .NET Core and Kubernetes

What Opulence can do:

- Automatically build Docker images by following .NET's conventions.
- Generate manifests for Kubenetes (static deployments/services, Helm Charts, OAM).
- Express relationships and dependencies between services. (in progress)
- Make secret management and service discovery easy. (in progress)
- Let you configure all of this with C# code! (`.csx`)

## Installing dotnet-opulence

First, install the [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) (or newer if you are viewing this in the far future).

Then... run the following command with the version of the latest build. Unfortunately `dotnet tool install` requires you to specify the exact package version.

```sh
dotnet tool install -g --add-source https://f.feedz.io/opulence/opulence/nuget/index.json --version "0.1.*" Opulence.dotnet-opulence

opulence --version
> 0.1.27-alpha+feada8b0f8
```

You can find the latest version [here](https://feedz.io/org/opulence/repository/opulence/packages/Opulence.dotnet-opulence).

[![feedz.io](https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Fopulence%2Fopulence%2Fshield%2FOpulence.dotnet-opulence%2Flatest)](https://f.feedz.io/opulence/opulence/packages/Opulence.dotnet-opulence/latest/download)

## Getting started

You will need:

- A Container Registry.
- A Kubernetes Cluster - and the permissions to manage it.

[This](https://docs.microsoft.com/en-us/azure/aks/tutorial-kubernetes-prepare-app) tutorial series for Azure (AKS + ACR) will get you set up with everything you need.

### Step 0

Create a .net core web application:

```sh
dotnet new web
dotnet new sln
dotnet sln add .
dotnet build
```

The project should be able to build without errors.

*note: Opulence currently works best with a solution file, even if you're using a single project.*

### Step 1

Initialize Opulence:

```sh
opulence init
```

This will prompt you for various options.

- Choose your solution root as the root directory.
- Enter your container registry when prompted for the registry.
- Choose to initialize the list of services from the solution file.

Example:

```txt
White-Glove service for .NET and Kubernetes...
Someone will be right with you!

💰 Looking For Existing Config...
    Not Found
💰 Looking For .sln File...
    Use '/Users/ryan/exampleapp' as Root? (y/n): y
💰 Writing Opulence.csx ...
    Enter the Container Registry (ex: 'example.azurecr.io' for Azure or 'example' for dockerhub): rynowak
    Use solution file '/Users/ryan/exampleapp/exampleapp.sln' to initialize services? (y/n): y
    Initialized Opulence Config at '/Users/ryan/exampleapp/Opulence.csx'.
```

Opulence will generate an `Opulence.csx` in the directory you chose as the root.

`Opulence.csx` is intended to be checked in to source control and maintained along with your source code.

### Step 2

Deploy to Kubernetes:

```sh
opulence deploy
```

Opulence will:

- Create a docker image.
- Push the docker image to your repository.
- Generate a Kubernetes `Deployment` and `Service`.
- Apply the generated `Deployment` and `Service` to your current Kubernetes context.

### Step 3

Test it out!

```sh
kubectl get pods
```

```txt
NAME                                                     READY   STATUS    RESTARTS   AGE
exampleapp-7588f78b7c-hcbn8                              1/1     Running   0          2m36s
```

This shows that your app has been deployed!

In this case `exampleapp` is the name of the service that was deployed. Replace the
value `exampleapp` with your own app's name in the following command.

```sh
kubectl port-forward svc/exampleapp 5000:80
```

```txt
Forwarding from 127.0.0.1:5000 -> 80
Forwarding from [::1]:5000 -> 80
```

This will open a local port (`5000`) that forwards to your application running in Kubernetes. Visit `http://localhost:5000` in a browser to see it running.

When you're done, hit `ctrl+c` to stop port-forwarding.

## Getting started with OAM

This section requires deploying Rudr to your Kubernetes cluster. See [Rudr's repo](https://github.com/oam-dev/rudr) for the latest instructions using Rudr.

Follow the normal *Getting started* instructions up to step 1.

Open Opulence.csx in your editor of choice, and change the deployment kind to `DeploymentKind.Oam`.

```C#
public class Application
{
    public ApplicationGlobals Globals { get; } = new ApplicationGlobals()
    {
        DeploymentKind = DeploymentKind.Oam,
        Registry = new ContainerRegistry("example.azurecr.io"),
    };

    ...
}
```

Now deploy:

```sh
opulence deploy
```

Opulence will:

- Create a docker image.
- Push the docker image to your repository.
- Generate an OAM `Component` and `ApplicationConfiguration`.
- Apply the generated `Component` and `ApplicationConfiguration` to your current Kubernetes context.
