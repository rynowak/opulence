using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Opulence.LogOverrideWebHook
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    
                    var certificate = new X509Certificate2(Environment.GetEnvironmentVariable("TLS_CERT_FILE"));

                    var rsa = RSA.Create();
                    var content = File
                        .ReadAllText(Environment.GetEnvironmentVariable("TLS_KEY_FILE"))
                        .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                        .Replace("-----END RSA PRIVATE KEY-----", "")
                        .Replace("\n", "");
                    rsa.ImportRSAPrivateKey(Convert.FromBase64String(content), out var _);

                    certificate = certificate.CopyWithPrivateKey(rsa);
                    webBuilder.ConfigureKestrel(kestrel =>
                    {
                        kestrel.Listen(IPAddress.Any, 80, listen =>
                        {
                        });
                        kestrel.Listen(IPAddress.Any, 443, listen =>
                        {
                            listen.UseHttps(certificate);
                        });
                    });
                });
    }
}
