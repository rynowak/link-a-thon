// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Mono.Cecil;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class FactoryCallSite : ServiceCallSite
    {
        public FactoryCallSite(ResultCache cache, TypeDefinition serviceType) 
            : base(cache)
        {
            ServiceType = serviceType;
        }

        public override TypeDefinition ServiceType { get; }
        public override TypeDefinition ImplementationType => null;

        public override CallSiteKind Kind { get; } = CallSiteKind.Factory;
    }
}
