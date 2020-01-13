using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Opulence
{
    internal abstract class ApplicationEntry
    {
        public abstract ApplicationGlobals Globals { get; }

        public abstract string RootDirectory { get; }
        public abstract IReadOnlyList<ServiceEntry> Services { get; }
    }

    internal class ServiceEntry
    {
        public ServiceEntry(Service service, string friendlyName, IEnumerable<string>? environments = null)
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (friendlyName is null)
            {
                throw new ArgumentNullException(nameof(friendlyName));
            }

            Service = service;
            FriendlyName = friendlyName;

            Environments = environments?.ToArray() ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> Environments { get; }

        public string FriendlyName { get; }

        public Service Service { get; }

        public List<ServiceOutput> Outputs { get; } = new List<ServiceOutput>();

        public bool HasProject => Service.Source is Project project;

        public bool AppliesToEnvironment(string environment)
        {
            if (environment is null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            return Environments.Count  == 0 || Environments.Contains(environment, StringComparer.OrdinalIgnoreCase);
        } 
    }

    public abstract class ServiceOutput
    {
    }

    public class DockerImageOutput : ServiceOutput
    {
        public DockerImageOutput(string imageName, string imageTag)
        {
            if (imageName is null)
            {
                throw new ArgumentNullException(nameof(imageName));
            }

            if (imageTag is null)
            {
                throw new ArgumentNullException(nameof(imageTag));
            }

            ImageName = imageName;
            ImageTag = imageTag;
        }
        
        public string ImageName { get; }

        public string ImageTag { get; }
    }


    internal class ApplicationWrapper : ApplicationEntry
    {
        private readonly object inner;
        private ApplicationGlobals? globals;
        private List<ServiceEntry>? services;

        public ApplicationWrapper(object inner, string rootDirectory)
        {
            if (inner is null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            if (rootDirectory is null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }

            this.inner = inner;
            RootDirectory = rootDirectory;
        }

        public override ApplicationGlobals Globals 
        {
            get
            {
                if (globals == null)
                {
                    var property = inner.GetType().GetProperty("Globals", BindingFlags.Instance | BindingFlags.Public);
                    if (property != null && property.PropertyType == typeof(ApplicationGlobals))
                    {
                        globals = (ApplicationGlobals?)property.GetValue(inner, null);
                    }

                    globals ??= new ApplicationGlobals();
                }

                return globals!;
            }
        }

        public override string RootDirectory { get; }

        public override IReadOnlyList<ServiceEntry> Services
        {
            get
            {
                if (services == null)
                {
                    services = new List<ServiceEntry>();
                    var properties = inner.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    for (var i = 0; i < properties.Length; i++)
                    {
                        var property = properties[i];
                        if (property.PropertyType == typeof(Service))
                        {
                            var environments = new List<string>();
                            var environmentAttributes = property.GetCustomAttributes<EnvironmentAttribute>();
                            environments.AddRange(environmentAttributes.Select(e => e.EnvironmentName));

                            var value = property.GetValue(inner, null);
                            if (value == null)
                            {
                                throw new InvalidOperationException("Service properties must return a non-null value.");
                            }

                            services.Add(new ServiceEntry((Service)value, property.Name, environments));
                        }
                    }
                }

                return services;
            }
        }
    }

    internal class GroveledApplication : ApplicationEntry
    {
        public GroveledApplication(ApplicationGlobals globals, string rootDirectory, IEnumerable<ServiceEntry> services)
        {
            if (globals is null)
            {
                throw new ArgumentNullException(nameof(globals));
            }

            if (rootDirectory is null)
            {
                throw new ArgumentNullException(nameof(rootDirectory));
            }

            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            Globals = globals;
            RootDirectory = rootDirectory;
            Services = services.ToArray();
        }

        public override ApplicationGlobals Globals { get; }

        public override string RootDirectory { get; }

        public override IReadOnlyList<ServiceEntry> Services { get; }
    }
}