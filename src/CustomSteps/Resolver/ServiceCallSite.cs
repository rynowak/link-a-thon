// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Mono.Cecil;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    /// <summary>
    /// Summary description for IServiceCallSite
    /// </summary>
    internal abstract class ServiceCallSite
    {
        protected ServiceCallSite(ResultCache cache)
        {
            Cache = cache;
        }

        public abstract TypeDefinition ServiceType { get; }
        public abstract TypeDefinition ImplementationType { get; }
        public abstract CallSiteKind Kind { get; }
        public ResultCache Cache { get; }

        public bool CaptureDisposable =>
            ImplementationType == null ||
            ImplementationType.Interfaces.Any(i => i.InterfaceType.Name == "IDisposable") ||
            ImplementationType.Interfaces.Any(i => i.InterfaceType.Name == "IAsyncDisposable");
    }
}
