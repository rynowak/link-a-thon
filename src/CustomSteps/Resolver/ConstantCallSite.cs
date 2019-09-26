// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Mono.Cecil;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ConstantCallSite : ServiceCallSite
    {
        internal object DefaultValue { get; }

        public ConstantCallSite(TypeDefinition serviceType, object defaultValue) : base(ResultCache.None)
        {
            ServiceType = serviceType;
            DefaultValue = defaultValue;
        }

        public override TypeDefinition ServiceType { get; }
        public override TypeDefinition ImplementationType => ServiceType;
        public override CallSiteKind Kind { get; } = CallSiteKind.Constant;
    }
}
