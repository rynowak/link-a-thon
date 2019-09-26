namespace Microsoft.Extensions.DependencyInjectionInjection.ServiceLookup
{
    internal enum CallSiteKind
    {
        Factory,

        Constructor,

        Constant,

        IEnumerable,

        ServiceProvider,

        Scope,

        Transient,

        CreateInstance,

        ServiceScopeFactory,

        Singleton
    }
}