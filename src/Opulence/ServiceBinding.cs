namespace Opulence
{
    public class ServiceBinding
    {
        public string? Name { get; set; }
        public string? Protocol { get; set; }
        public int? Port { get; set; }
        public Secret? ConnectionString { get; set; }
    }
}