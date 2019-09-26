// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Mono.Cecil;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ConstructorCallSite : ServiceCallSite
    {
        internal MethodDefinition ConstructorInfo { get; }
        internal ServiceCallSite[] ParameterCallSites { get; }

        public ConstructorCallSite(ResultCache cache, TypeDefinition serviceType, MethodDefinition constructorInfo) 
            : this(cache, serviceType, constructorInfo, Array.Empty<ServiceCallSite>())
        {
        }

        public ConstructorCallSite(ResultCache cache, TypeDefinition serviceType, MethodDefinition constructorInfo, ServiceCallSite[] parameterCallSites) : base(cache)
        {
            ServiceType = serviceType;
            ConstructorInfo = constructorInfo;
            ParameterCallSites = parameterCallSites;
        }

        public override TypeDefinition ServiceType { get; }
        public override TypeDefinition ImplementationType => ConstructorInfo.DeclaringType;
        public override CallSiteKind Kind { get; } = CallSiteKind.Constructor;
    }
}
