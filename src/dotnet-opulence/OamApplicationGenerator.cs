using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Opulence
{
    internal static class OamApplicationGenerator
    {
        public static Task WriteOamApplicationAsync(TextWriter writer, OutputContext output, ApplicationEntry application, string applicationName, string environment)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (application is null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (applicationName is null)
            {
                throw new ArgumentNullException(nameof(applicationName));
            }

            if (environment is null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            var documents = new List<YamlDocument>();

            foreach (var service in application.Services)
            {
                var schematic = CreateComponentSchematic(service);
                if (schematic != null)
                {
                    documents.Add(schematic);
                }
            }

            var root = new YamlMappingNode();

            root.Add("kind", "ApplicationConfiguration");
            root.Add("apiVersion", "core.oam.dev/v1alpha1");

            var metadata = new YamlMappingNode();
            root.Add("metadata", metadata);
            metadata.Add("name", applicationName.ToLowerInvariant());

            var spec = new YamlMappingNode();
            root.Add("spec", spec);

            var components = new YamlSequenceNode();
            spec.Add("components", components);

            foreach (var service in application.Services)
            {
                if (!service.Outputs.OfType<DockerImageOutput>().Any())
                {
                    continue;
                }

                var component = new YamlMappingNode();
                components.Add(component);

                component.Add("componentName", service.Service.Name);
                component.Add("instanceName", $"{environment.ToLowerInvariant()}-{service.Service.Name}");
            }

            documents.Add(new YamlDocument(root));

            var stream = new YamlStream(documents.ToArray());
            stream.Save(writer, assignAnchors: false);

            return Task.CompletedTask;
        }

        private static YamlDocument? CreateComponentSchematic(ServiceEntry service)
        {
            var images = service.Outputs.OfType<DockerImageOutput>().ToArray();
            if (images.Length == 0)
            {
                return null;
            }

            var root = new YamlMappingNode();

            root.Add("kind", "ComponentSchematic");
            root.Add("apiVersion", "core.oam.dev/v1alpha1");

            var metadata = new YamlMappingNode();
            root.Add("metadata", metadata);
            metadata.Add("name", service.Service.Name);

            var spec = new YamlMappingNode(); 
            root.Add("spec", spec);
            spec.Add("workloadType", "core.oam.dev/v1alpha1.Server");
            
            var containers = new YamlSequenceNode();
            spec.Add("containers", containers);

            for (var i = 0; i < images.Length; i++)
            {
                var image = images[i];

                var container = new YamlMappingNode();
                containers.Add(container);
                container.Add("name", service.Service.Name); // NOTE: to really support multiple images we'd need to generate unique names.
                container.Add("image", $"{image.ImageName}:{image.ImageTag}");
            }

            return new YamlDocument(root);
        }
    }
}