using System;
using System.Collections.Generic;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Opulence.LogOverrideWebHook
{
    public class ConfigMapInjector
    {
        private const string ConfigMapName = "opulence-logoverride";
        private const string VolumeName = "opulence-logoverride";

        private readonly ILogger logger;

        public ConfigMapInjector(ILogger<ConfigMapInjector> logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            this.logger = logger;
        }

        public JsonPatchDocument GetPatches(V1Deployment deployment)
        {
            if (deployment is null)
            {
                throw new ArgumentNullException(nameof(deployment));
            }

            logger.LogInformation(
                        "Got deployment named {DeploymentName} in namespace {Namespace}.",
                        deployment.Metadata.Name,
                        deployment.Metadata.NamespaceProperty ?? "default");
            logger.LogInformation("Got template: {}", Newtonsoft.Json.JsonConvert.SerializeObject(deployment.Spec.Template));

            foreach (var annotation in deployment.Spec.Template.Metadata.Annotations)
            {
                logger.LogInformation("Got annotation {AnnotationKey}={AnnotationValue}", annotation.Key, annotation.Value);
            }

            var patches = new JsonPatchDocument();
            var hasLogOverrideEnabled = false;
            if (deployment.Spec.Template.Metadata.Annotations.TryGetValue("opulence.dotnet.io/enablelogoverride", out var value) &&
                string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                hasLogOverrideEnabled = true;
            }

            logger.LogInformation("Deployment {DeploymentName} has opulence.dotnet.io/enablelogoverride = {LogOverrideEnabled}.", deployment.Metadata.Name, hasLogOverrideEnabled);
            if (!hasLogOverrideEnabled)
            {
                return patches;
            }

            var foundVolume = false;
            var volumes = deployment.Spec.Template.Spec?.Volumes ?? Array.Empty<V1Volume>();
            for (var i = 0; i < volumes.Count; i++)
            {
                var volume = volumes[i];
                if (volume.Name == VolumeName)
                {
                    foundVolume = true;
                    logger.LogInformation("Deployment already has volume {VolumeName}.", VolumeName);
                    break;
                }
            }

            if (!foundVolume)
            {
                logger.LogInformation("Adding volume {VolumnName}.", VolumeName);

                if (volumes.Count == 0)
                {
                    patches.Add("/spec/template/spec/volumes", Array.Empty<object>());
                }

                patches.Add("/spec/template/spec/volumes/-", new V1Volume()
                {
                    Name = VolumeName,
                    ConfigMap = new V1ConfigMapVolumeSource()
                    {
                        Name = ConfigMapName,
                        Optional = true,
                        Items = new List<V1KeyToPath>()
                        {
                            new V1KeyToPath()
                            {
                                Key = $"{deployment.Metadata.Name}.json",
                                Path = $"config.json",
                            }
                        },
                    },
                });
            }

            var containers = deployment.Spec.Template.Spec.Containers;
            for (var i = 0; i < containers.Count; i++)
            {
                var container = containers[i];

                var foundMount = false;
                container.VolumeMounts ??= new List<V1VolumeMount>();
                for (var j = 0; j < container.VolumeMounts.Count; i++)
                {
                    var mount = container.VolumeMounts[j];
                    if (mount.Name == VolumeName)
                    {
                        foundMount = true;
                        logger.LogInformation("Found volume mount {VolumneName} for container {ContainerName}.");
                        break;
                    }
                }

                if (foundMount)
                {
                    continue;
                }

                logger.LogInformation("Adding volumne mount {VolumeName} to {ContainerName}.", "opulence-logoverride", container.Name);

                if (container.VolumeMounts.Count == 0)
                {
                    patches.Add($"/spec/template/spec/containers/{i}/volumeMounts", Array.Empty<object>());
                }

                patches.Add($"/spec/template/spec/containers/{i}/volumeMounts/-", new V1VolumeMount()
                {
                    Name = "opulence-logoverride",
                    MountPath = "/var/opulence.dotnet.io/logoverride",
                    ReadOnlyProperty = true,
                });
            }

            return patches;
        }
    }
}