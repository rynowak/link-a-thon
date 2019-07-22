using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uHosting
{
    public class MicroContainer : IServiceProvider
    {
        private IDictionary<Type, IEnumerable<ServiceDescriptor>> _services;

        public MicroContainer(IEnumerable<ServiceDescriptor> services)
        {
            _services = services.GroupBy(s => s.ServiceType).ToDictionary(
                g => g.Key, 
                g => (IEnumerable<ServiceDescriptor>)g.ToList());
        }

        public object GetService(Type serviceType)
        {
            if(_services.TryGetValue(serviceType, out var services))
            {
                return services.LastOrDefault()?.Create(this);
            }
            else if(serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var subType = serviceType.GetGenericArguments()[0];
                if(_services.TryGetValue(subType, out var services1))
                {
                    return services.Select(s => s.Create(this)).ToList();
                }
                else
                {
                    return Array.Empty<object>();
                }
            }
            else
            {
                return null;
            }
        }
    }
}
