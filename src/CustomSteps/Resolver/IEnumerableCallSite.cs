// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Mono.Cecil;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class IEnumerableCallSite : ServiceCallSite
    {
        internal TypeDefinition ItemType { get; }
        internal ServiceCallSite[] ServiceCallSites { get; }

        public IEnumerableCallSite(ResultCache cache, TypeDefinition enumerableType, TypeDefinition itemType, ServiceCallSite[] serviceCallSites) : base(cache)
        {
            ServiceType = enumerableType;
            ItemType = enumerableType;
            ServiceCallSites = serviceCallSites;
        }

        public override TypeDefinition ServiceType { get; }
        public override TypeDefinition ImplementationType => ServiceType;
        public override CallSiteKind Kind { get; } = CallSiteKind.IEnumerable;
    }
}
