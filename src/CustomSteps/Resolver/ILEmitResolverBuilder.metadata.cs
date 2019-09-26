// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Mono.Cecil;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed partial class ILEmitResolverBuilder : CallSiteVisitor<ILEmitResolverBuilderContext, object>
    {
        private TypeDefinition ServiceCacheKey;
        private MethodDefinition ServiceCacheKey_Ctor;
        private TypeDefinition ServiceProviderEngineScope;
        private MethodDefinition ServiceProviderEngineScope_CaptureDisposable;
        private MethodDefinition GetTypeFromHandle;

        private void Initialize()
        {
            ServiceCacheKey = _context.GetType("Microsoft.Extensions.DependencyInjectionInjection.ServiceLookup.ServiceCacheKey");
            ServiceCacheKey_Ctor = ServiceCacheKey.Methods.Where(m => m.Parameters.Count == 2).Single();

            ServiceProviderEngineScope = _context.GetType("Microsoft.Extensions.DependencyInjectionInjection.ServiceLookup.ServiceProviderEngineScope");
            ServiceProviderEngineScope_CaptureDisposable = ServiceProviderEngineScope.Methods.Where(m => m.Name == "CaptureDisposable").Single();

            var type = _context.GetType("System.Type");
            GetTypeFromHandle = type.Methods.Where(m => m.Name == "GetTypeFromHandle").Single();
        }
    }
}
