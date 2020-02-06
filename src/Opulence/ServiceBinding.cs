using System;

namespace Opulence
{
    public class ServiceBinding
    {
        public static ServiceBinding FromService(Service service)
        {
            if (service is null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            return new ServiceBinding()
            {
                Name = service.Name,
                Protocol = "http",
                Port = 80,
            };
        }

        public ServiceBinding()
        {
        }

        public string? Name { get; set; }
        public string? Protocol { get; set; }
        public int? Port { get; set; }
        public Secret? ConnectionString { get; set; }
    }
}