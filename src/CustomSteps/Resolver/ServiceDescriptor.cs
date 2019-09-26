using Mono.Cecil;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceDescriptor
    {
        public ServiceLifetime Lifetime { get; set; }
        public TypeReference ServiceType { get; set; }
        public TypeReference ImplementationType { get; set; }
    }

    public enum ServiceLifetime
    {
        Transient,
        Scoped,
        Singleton,
    }
}
