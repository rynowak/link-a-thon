// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CustomSteps;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class CallSiteFactory
    {
        private const int DefaultSlot = 0;

        private readonly List<ServiceDescriptor> _descriptors;
        private readonly ConcurrentDictionary<TypeReference, ServiceCallSite> _callSiteCache = new ConcurrentDictionary<TypeReference, ServiceCallSite>();
        private readonly Dictionary<TypeReference, ServiceDescriptorCacheItem> _descriptorLookup = new Dictionary<TypeReference, ServiceDescriptorCacheItem>();

        private readonly StackGuard _stackGuard;

        public CallSiteFactory(IEnumerable<ServiceDescriptor> descriptors)
        {
            _stackGuard = new StackGuard();
            _descriptors = descriptors.ToList();
            Populate();
        }

        private void Populate()
        {
            foreach (var descriptor in _descriptors)
            {
                if (descriptor.ServiceType.IsGenericParameter)
                {
                    continue;
                }

                if (descriptor.ServiceType.ContainsGenericParameter)
                {
                    continue;
                }

                if (descriptor.ImplementationType == null)
                {
                    continue;
                }

                var serviceTypeInfo = descriptor.ServiceType.GetType() == typeof(TypeReference) ? descriptor.ServiceType.Resolve() : descriptor.ServiceType;
                if (serviceTypeInfo.HasGenericParameters)
                {
                    var implementationTypeInfo = descriptor.ImplementationType.Resolve();
                    if (implementationTypeInfo == null || !implementationTypeInfo.HasGenericParameters)
                    {
                        throw new ArgumentException(
                            string.Format(Resources.OpenGenericServiceRequiresOpenGenericImplementation, descriptor.ServiceType),
                            "descriptors");
                    }

                    if (implementationTypeInfo.IsAbstract || implementationTypeInfo.IsInterface)
                    {
                        throw new ArgumentException(
                            string.Format(Resources.TypeCannotBeActivated, descriptor.ImplementationType, descriptor.ServiceType));
                    }
                }
                else if (descriptor.ImplementationType != null)
                {
                    var implementationTypeInfo = descriptor.ImplementationType.Resolve();

                    if (implementationTypeInfo.HasGenericParameters ||
                        implementationTypeInfo.IsAbstract ||
                        implementationTypeInfo.IsInterface)
                    {
                        // This can happen in some cases because our analysis with the overloads that accept System.Type is fuzzy.
                        continue;
                    }
                }

                var cacheKey = descriptor.ServiceType;
                _descriptorLookup.TryGetValue(cacheKey, out var cacheItem);
                _descriptorLookup[cacheKey] = cacheItem.Add(descriptor);
            }
        }

        internal ServiceCallSite GetCallSite(TypeReference serviceType, CallSiteChain callSiteChain)
        {
            return _callSiteCache.GetOrAdd(serviceType, type => CreateCallSite(type, callSiteChain));
        }

        internal ServiceCallSite GetCallSite(ServiceDescriptor serviceDescriptor, CallSiteChain callSiteChain)
        {
            if (_descriptorLookup.TryGetValue(serviceDescriptor.ServiceType, out var descriptor))
            {
                var slot = descriptor.GetSlot(serviceDescriptor);
                if (slot == null)
                {
                    return null;
                }

                return TryCreateExact(serviceDescriptor, serviceDescriptor.ServiceType, callSiteChain, slot.Value);
            }

            return null;
        }

        private ServiceCallSite CreateCallSite(TypeReference serviceType, CallSiteChain callSiteChain)
        {
            if (!_stackGuard.TryEnterOnCurrentStack())
            {
                return _stackGuard.RunOnEmptyStack((type, chain) => CreateCallSite(type, chain), serviceType, callSiteChain);
            }

            callSiteChain.CheckCircularDependency(serviceType);

            var callSite = 
                TryCreateExact(serviceType, callSiteChain) ??
                TryCreateOpenGeneric(serviceType, callSiteChain) ??
                TryCreateEnumerable(serviceType, callSiteChain);

            _callSiteCache[serviceType] = callSite;

            return callSite;
        }

        private ServiceCallSite TryCreateExact(TypeReference serviceType, CallSiteChain callSiteChain)
        {
            if (_descriptorLookup.TryGetValue(serviceType, out var descriptor))
            {
                return TryCreateExact(descriptor.Last, serviceType, callSiteChain, DefaultSlot);
            }

            return null;
        }

        private ServiceCallSite TryCreateOpenGeneric(TypeReference serviceType, CallSiteChain callSiteChain)
        {
            if (serviceType.IsGenericInstance
                && _descriptorLookup.TryGetValue((GenericInstanceType)serviceType, out var descriptor))
            {
                return TryCreateOpenGeneric(descriptor.Last, serviceType, callSiteChain, DefaultSlot);
            }

            return null;
        }

        private ServiceCallSite TryCreateEnumerable(TypeReference serviceType, CallSiteChain callSiteChain)
        {
            return null;

            //try
            //{
            //    callSiteChain.Add(serviceType);

            //    if (serviceType.IsGenericInstance && 
            //        serviceType is GenericInstanceType generic &&
            //        generic.Resolve().FullName == typeof(IEnumerable<>).FullName)
            //    {
            //        var itemType = generic.GenericArguments[0];
            //        var cacheLocation = CallSiteResultCacheLocation.Root;

            //        var callSites = new List<ServiceCallSite>();

            //        // If item type is not generic we can safely use descriptor cache
            //        if (!itemType.IsConstructedGenericType &&
            //            _descriptorLookup.TryGetValue(itemType, out var descriptors))
            //        {
            //            for (int i = 0; i < descriptors.Count; i++)
            //            {
            //                var descriptor = descriptors[i];

            //                // Last service should get slot 0
            //                var slot = descriptors.Count - i - 1;
            //                // There may not be any open generics here
            //                var callSite = TryCreateExact(descriptor, itemType, callSiteChain, slot);
            //                Debug.Assert(callSite != null);

            //                cacheLocation = GetCommonCacheLocation(cacheLocation, callSite.Cache.Location);
            //                callSites.Add(callSite);
            //            }
            //        }
            //        else
            //        {
            //            var slot = 0;
            //            // We are going in reverse so the last service in descriptor list gets slot 0
            //            for (var i = _descriptors.Count - 1; i >= 0; i--)
            //            {
            //                var descriptor = _descriptors[i];
            //                var callSite = TryCreateExact(descriptor, itemType, callSiteChain, slot) ??
            //                               TryCreateOpenGeneric(descriptor, itemType, callSiteChain, slot);

            //                if (callSite != null)
            //                {
            //                    slot++;

            //                    cacheLocation = GetCommonCacheLocation(cacheLocation, callSite.Cache.Location);
            //                    callSites.Add(callSite);
            //                }
            //            }

            //            callSites.Reverse();
            //        }


            //        var resultCache = ResultCache.None;
            //        if (cacheLocation == CallSiteResultCacheLocation.Scope || cacheLocation == CallSiteResultCacheLocation.Root)
            //        {
            //            resultCache = new ResultCache(cacheLocation, new ServiceCacheKey(serviceType, DefaultSlot));
            //        }

            //        return new IEnumerableCallSite(resultCache, itemType, callSites.ToArray());
            //    }

            //    return null;
            //}
            //finally
            //{
            //    callSiteChain.Remove(serviceType);
            //}
        }

        private CallSiteResultCacheLocation GetCommonCacheLocation(CallSiteResultCacheLocation locationA, CallSiteResultCacheLocation locationB)
        {
            return (CallSiteResultCacheLocation)Math.Max((int)locationA, (int)locationB);
        }

        private ServiceCallSite TryCreateExact(ServiceDescriptor descriptor, TypeReference serviceType, CallSiteChain callSiteChain, int slot)
        {
            if (serviceType == descriptor.ServiceType)
            {
                var lifetime = new ResultCache(descriptor.Lifetime, serviceType, slot);
                if (descriptor.ImplementationType?.Resolve() != null)
                {
                    return CreateConstructorCallSite(lifetime, descriptor.ServiceType.Resolve(), descriptor.ImplementationType.Resolve(), callSiteChain);
                }
            }

            return null;
        }

        private ServiceCallSite TryCreateOpenGeneric(ServiceDescriptor descriptor, TypeReference serviceType, CallSiteChain callSiteChain, int slot)
        {
            //if (serviceType.IsGenericInstance &&
            //    serviceType.Resolve() == descriptor.ServiceType)
            //{
            //    Debug.Assert(descriptor.ImplementationType != null, "descriptor.ImplementationType != null");
            //    var lifetime = new ResultCache(descriptor.Lifetime, serviceType, slot);
            //    var closedType = descriptor.ImplementationType.MakeGenericType(serviceType.GenericTypeArguments);
            //    return CreateConstructorCallSite(lifetime, serviceType, closedType, callSiteChain);
            //}

            return null;
        }

        private ServiceCallSite CreateConstructorCallSite(
            ResultCache lifetime, 
            TypeDefinition serviceType,
            TypeDefinition implementationType,
            CallSiteChain callSiteChain)
        {
            try
            {
                callSiteChain.Add(serviceType, implementationType);
                var constructors = implementationType
                    .Methods
                    .Where(m => m.IsConstructor)
                    .Where(constructor => constructor.IsPublic)
                    .ToArray();

                ServiceCallSite[] parameterCallSites = null;

                if (constructors.Length == 0)
                {
                    throw new InvalidOperationException(string.Format(Resources.NoConstructorMatch, implementationType));
                }
                else if (constructors.Length == 1)
                {
                    var constructor = constructors[0];
                    var parameters = constructor.Parameters;
                    if (parameters.Count == 0)
                    {
                        return new ConstructorCallSite(lifetime, serviceType, constructor);
                    }

                    parameterCallSites = CreateArgumentCallSites(
                        serviceType,
                        implementationType,
                        callSiteChain,
                        parameters,
                        throwIfCallSiteNotFound: false);
                    if (parameterCallSites == null)
                    {
                        return null;
                    }

                    return new ConstructorCallSite(lifetime, serviceType, constructor, parameterCallSites);
                }


                return null;
            }
            finally
            {
                callSiteChain.Remove(serviceType);
            }
        }

        private ServiceCallSite[] CreateArgumentCallSites(
            TypeReference serviceType,
            TypeDefinition implementationType,
            CallSiteChain callSiteChain,
            Collection<ParameterDefinition> parameters,
            bool throwIfCallSiteNotFound)
        {
            var parameterCallSites = new ServiceCallSite[parameters.Count];
            for (var index = 0; index < parameters.Count; index++)
            {
                var parameterType = parameters[index].ParameterType;
                var callSite = GetCallSite(parameterType, callSiteChain);

                if (callSite == null && parameters[index].HasConstant)
                {
                    callSite = new ConstantCallSite(parameterType.Resolve(), parameters[index].Constant);
                }

                if (callSite == null)
                {
                    if (throwIfCallSiteNotFound)
                    {
                        throw new InvalidOperationException(string.Format(
                            Resources.CannotResolveService,
                            parameterType,
                            implementationType));
                    }

                    return null;
                }

                parameterCallSites[index] = callSite;
            }

            return parameterCallSites;
        }


        public void Add(TypeReference type, ServiceCallSite serviceCallSite)
        {
            _callSiteCache[type] = serviceCallSite;
        }

        private struct ServiceDescriptorCacheItem
        {
            private ServiceDescriptor _item;

            private List<ServiceDescriptor> _items;

            public ServiceDescriptor Last
            {
                get
                {
                    if (_items != null && _items.Count > 0)
                    {
                        return _items[_items.Count - 1];
                    }

                    Debug.Assert(_item != null);
                    return _item;
                }
            }

            public int Count
            {
                get
                {
                    if (_item == null)
                    {
                        Debug.Assert(_items == null);
                        return 0;
                    }

                    return 1 + (_items?.Count ?? 0);
                }
            }

            public ServiceDescriptor this[int index]
            {
                get
                {
                    if (index >= Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    if (index == 0)
                    {
                        return _item;
                    }

                    return _items[index - 1];
                }
            }

            public int? GetSlot(ServiceDescriptor descriptor)
            {
                if (descriptor == _item)
                {
                    return 0;
                }

                if (_items != null)
                {
                    var index = _items.IndexOf(descriptor);
                    if (index != -1)
                    {
                        return index + 1;
                    }
                }

                return null;
            }

            public ServiceDescriptorCacheItem Add(ServiceDescriptor descriptor)
            {
                var newCacheItem = new ServiceDescriptorCacheItem();
                if (_item == null)
                {
                    Debug.Assert(_items == null);
                    newCacheItem._item = descriptor;
                }
                else
                {
                    newCacheItem._item = _item;
                    newCacheItem._items = _items ?? new List<ServiceDescriptor>();
                    newCacheItem._items.Add(descriptor);
                }
                return newCacheItem;
            }
        }
    }
}
