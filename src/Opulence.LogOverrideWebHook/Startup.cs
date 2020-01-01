using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Opulence.LogOverrideWebHook
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks();
            services.AddSingleton<ConfigMapInjector>();
            services.AddSingleton<JsonSerializerOptions>(new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            services.AddSingleton<Kubernetes>(new Kubernetes(KubernetesClientConfiguration.InClusterConfig()));
        }

        public void Configure(IApplicationBuilder app, ConfigMapInjector injector, JsonSerializerOptions options, Kubernetes kubernetes, ILogger<Startup> logger)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/healthz");

                endpoints.MapPost("/mutate", async context =>
                {
                    if (context.Request.ContentType != "application/json")
                    {
                        context.Response.StatusCode = 400;
                        return;
                    }

                    // The task here is to respond to creation of deployments and:
                    // - if not annotated then do nothing
                    // - if annotated then inject a config map with a well-known filename
                    //
                    // We use a single configmap per-namespace with a well-known name. Each deployment gets its
                    // own key within the configmap.
                    var admissionReview = await JsonSerializer.DeserializeAsync<AdmissionReview>(context.Request.Body, options);
                    if (admissionReview.Kind != "AdmissionReview")
                    {
                        logger.LogInformation("Got unexpected request. Not an AdmissionReview.");
                        context.Response.StatusCode = 400;
                        return;
                    }

                    logger.LogInformation(
                    "Got {Kind} request with API version {ApiVersion} for resource {ResourceKind}.",
                        admissionReview.Kind,
                        admissionReview.ApiVersion,
                        admissionReview.Request.Kind.GetProperty("kind").GetString());

                    if (admissionReview.ApiVersion != "admission.k8s.io/v1beta1" ||
                        admissionReview.Request.Kind.GetProperty("kind").GetString() != "Deployment")
                    {
                        logger.LogInformation("Got unexpected request. Not a deployment creation.");
                        context.Response.StatusCode = 400;
                        return;
                    }

                    var deployment = admissionReview.Request.GetObjectAs<V1Deployment>();
                    var patches = injector.GetPatches(deployment);
                    if (patches.Operations.Count == 0)
                    {
                        logger.LogInformation("No changes needed. Allowing the creation to proceed.");
                        admissionReview.Response = new AdmissionReviewResponse()
                        {
                            Uid = admissionReview.Request.Uid,
                            Allowed = true,
                        };

                        context.Response.ContentType = "application/json";
                        await JsonSerializer.SerializeAsync(context.Response.Body, admissionReview, options);
                        return;
                    }

                    var (patch, encoded) = PatchSerializer.Serialize(patches);
                    logger.LogInformation("Patching deployment {DeploymentName} with patch: {Patch}.", deployment.Metadata.Name, patch);

                    admissionReview.Response = new AdmissionReviewResponse()
                    {
                        Uid = admissionReview.Request.Uid,
                        Allowed = true,
                        PatchType = AdmissionReviewResponse.PatchTypeJsonPatch,
                        Patch = encoded,
                    };

                    context.Response.ContentType = "application/json";
                    await JsonSerializer.SerializeAsync<AdmissionReview>(context.Response.Body, admissionReview, options);
                    logger.LogInformation("Updated deployment {DeploymentName}", deployment.Metadata.Name);
                    return;
                });
            });
        }
    }
}
