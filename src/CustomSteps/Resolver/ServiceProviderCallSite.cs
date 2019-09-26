// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Mono.Cecil;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ServiceProviderCallSite : ServiceCallSite
    {
        public ServiceProviderCallSite(TypeDefinition serviceType, TypeDefinition implementationType) : base(ResultCache.None)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
        }

        public override TypeDefinition ServiceType { get; }
        public override TypeDefinition ImplementationType { get; }
        public override CallSiteKind Kind { get; } = CallSiteKind.ServiceProvider;
    }
}
