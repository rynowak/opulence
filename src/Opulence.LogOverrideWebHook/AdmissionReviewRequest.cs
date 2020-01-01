using System.Text.Json;

namespace k8s.Models
{
    // "request": {
    //     "uid": "6aad3b54-2cf9-11ea-a62d-02683bd21d1b",
    //     "kind": { "group": "", "version": "v1", "kind": "Pod" },
    //     "resource": { "group": "", "version": "v1", "resource": "pods" },
    //     "namespace": "default",
    //     "operation": "CREATE",
    //     "userInfo": { "username": "system:serviceaccount:kube-system:replicaset-controller", "uid": "3f2b5f75-1a28-11ea-bc0d-421c2adc9520", "groups": ["system:serviceaccounts", "system:serviceaccounts:kube-system", "system:authenticated"] },
    //     "object": {
    //         "kind": "Pod",
    //         "apiVersion": "v1",
    //         "metadata": {
    //             "generateName": "gamemaster-6d998c5ccd-", "creationTimestamp": null, "labels": { "app.kubernetes.io/instance": "gamemaster", "app.kubernetes.io/name": "gamemaster", "pod-template-hash": "6d998c5ccd" }, "annotations": { "dapr.io/config": "zipkin", "dapr.io/enabled": "true", "dapr.io/id": "gamemaster", "dapr.io/port": "80" },
    //             "ownerReferences": [{ "apiVersion": "apps/v1", "kind": "ReplicaSet", "name": "gamemaster-6d998c5ccd", "uid": "a4ad9854-1a41-11ea-bd51-f6989e6f2c5b", "controller": true, "blockOwnerDeletion": true }]
    //         },
    //         "spec": {
    //             "volumes": [{ "name": "default-token-hbn7t", "secret": { "secretName": "default-token-hbn7t", "defaultMode": 420 } }], "containers": [{ "name": "gamemaster", "image": "rynowak.azurecr.io/rochambot/gamemaster:1.0.2-25-g071a1c5", "ports": [{ "containerPort": 80, "protocol": "TCP" }], "resources": {}, "volumeMounts": [{ "name": "default-token-hbn7t", "readOnly": true, "mountPath": "/var/run/secrets/kubernetes.io/serviceaccount" }], "livenessProbe": { "httpGet": { "path": "/healthz", "port": 80, "scheme": "HTTP" }, "initialDelaySeconds": 5, "timeoutSeconds": 1, "periodSeconds": 5, "successThreshold": 1, "failureThreshold": 3 }, "terminationMessagePath": "/dev/termination-log", "terminationMessagePolicy": "File", "imagePullPolicy": "Always" }, { "name": "daprd", "image": "docker.io/daprio/dapr:latest", "command": ["/daprd"], "args": ["--mode", "kubernetes", "--dapr-http-port", "3500", "--dapr-grpc-port", "50001", "--app-port", "80", "--dapr-id", "gamemaster", "--control-plane-address", "http://dapr-api.dapr-system.svc.cluster.local", "--protocol", "http", "--placement-address", "dapr-placement.dapr-system.svc.cluster.local:80", "--config", "zipkin", "--enable-profiling", "false", "--log-level", "info", "--max-concurrency", "-1"], "ports": [{ "name": "dapr-http", "containerPort": 3500, "protocol": "TCP" }, { "name": "dapr-grpc", "containerPort": 50001, "protocol": "TCP" }], "env": [{ "name": "HOST_IP", "valueFrom": { "fieldRef": { "apiVersion": "v1", "fieldPath": "status.podIP" } } }, { "name": "NAMESPACE", "value": "default" }], "resources": {}, "volumeMounts": [{ "name": "default-token-hbn7t", "readOnly": true, "mountPath": "/var/run/secrets/kubernetes.io/serviceaccount" }], "terminationMessagePath": "/dev/termination-log", "terminationMessagePolicy": "File", "imagePullPolicy": "Always" }], "restartPolicy": "Always", "terminationGracePeriodSeconds": 30, "dnsPolicy": "ClusterFirst", "serviceAccountName": "default", "serviceAccount": "default", "securityContext": {}, "schedulerName": "default-scheduler", "tolerations": [{ "key": "node.kubernetes.io/not-ready", "operator": "Exists", "effect": "NoExecute", "tolerationSeconds": 300 }, { "key": "node.kubernetes.io/unreachable", "operator": "Exists", "effect": "NoExecute", "tolerationSeconds": 300 }],
    //             "priority": 0, "enableServiceLinks": true
    //         },
    //         "status": {}
    //     },
    //     "oldObject": null, 
    //     "dryRun": false
    // }
    public class AdmissionReviewRequest
    {
        public string Uid { get; set; }

        public JsonElement Kind { get; set; }

        public JsonElement Resource { get; set; }

        public string Namespace { get; set; }

        public string Operation { get; set; }

        public JsonElement UserInfo { get; set; }

        public JsonElement Object { get; set; }

        public JsonElement OldObject { get; set; }

        public bool DryRun { get; set; }

        public T GetObjectAs<T>()
        {
            // Note: the k8s SDK uses JSON.NET.
            var obj = Object;
            var serialized = JsonSerializer.Serialize<JsonElement>(obj, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            });
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}