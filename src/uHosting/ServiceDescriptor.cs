using System;
using System.Runtime.InteropServices.ComTypes;

namespace uHosting
{
    public abstract class ServiceDescriptor
    {
        public ServiceDescriptor(ServiceLifetime lifetime, Type serviceType, Func<IServiceProvider, object> factory)
        {
            Lifetime = lifetime;
            ServiceType = serviceType;
            Factory = factory;
        }

        public ServiceLifetime Lifetime { get; }
        public Type ServiceType { get; }
        protected Func<IServiceProvider, object> Factory { get; }

        public abstract object Create(IServiceProvider services);

        public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, object> factory)
            => new SingletonServiceDescriptor(typeof(TService), factory);
        public static ServiceDescriptor Singleton(Type service, Func<IServiceProvider, object> factory)
            => new SingletonServiceDescriptor(service, factory);
        public static ServiceDescriptor Transient<TService>(Func<IServiceProvider, object> factory)
            => new TransientServiceDescriptor(typeof(TService), factory);
        public static ServiceDescriptor Transient(Type service, Func<IServiceProvider, object> factory)
            => new TransientServiceDescriptor(service, factory);
    }

    internal class SingletonServiceDescriptor : ServiceDescriptor
    {
        private static readonly object UninitializedSentinel = new object();
        private object _instance = UninitializedSentinel;
        private object _lock = new object();

        public SingletonServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory)
            : base(ServiceLifetime.Singleton, serviceType, factory)
        {
        }

        public override object Create(IServiceProvider services)
        {
            lock (_lock)
            {
                if (ReferenceEquals(_instance, UninitializedSentinel))
                {
                    _instance = Factory(services);
                }
                return _instance;
            }
        }
    }

    internal class TransientServiceDescriptor : ServiceDescriptor
    {
        public TransientServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory)
            : base(ServiceLifetime.Transient, serviceType, factory)
        {
        }

        public override object Create(IServiceProvider services) => Factory(services);
    }
}