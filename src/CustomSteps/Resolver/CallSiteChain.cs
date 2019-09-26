// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CustomSteps;
using Mono.Cecil;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class CallSiteChain
    {
        private readonly Dictionary<TypeReference, ChainItemInfo> _callSiteChain;

        public CallSiteChain()
        {
            _callSiteChain = new Dictionary<TypeReference, ChainItemInfo>();
        }

        public void CheckCircularDependency(TypeReference serviceType)
        {
            if (_callSiteChain.ContainsKey(serviceType))
            {
                throw new InvalidOperationException(CreateCircularDependencyExceptionMessage(serviceType));
            }
        }

        public void Remove(TypeReference serviceType)
        {
            _callSiteChain.Remove(serviceType);
        }

        public void Add(TypeReference serviceType, TypeReference implementationType = null)
        {
            _callSiteChain[serviceType] = new ChainItemInfo(_callSiteChain.Count, implementationType);
        }

        private string CreateCircularDependencyExceptionMessage(TypeReference type)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendFormat(Resources.CircularDependencyException, type.FullName);
            messageBuilder.AppendLine();

            AppendResolutionPath(messageBuilder, type);

            return messageBuilder.ToString();
        }

        private void AppendResolutionPath(StringBuilder builder, TypeReference currentlyResolving = null)
        {
            foreach (var pair in _callSiteChain.OrderBy(p => p.Value.Order))
            {
                var serviceType = pair.Key;
                var implementationType = pair.Value.ImplementationType;
                if (implementationType == null || serviceType.FullName == implementationType.FullName)
                {
                    builder.Append(serviceType.FullName);
                }
                else
                {
                    builder.AppendFormat(
                        "{0}({1})",
                        serviceType.FullName,
                        implementationType.FullName);
                }

                builder.Append(" -> ");
            }

            builder.Append(currentlyResolving.FullName);
        }

        private readonly struct ChainItemInfo
        {
            public int Order { get; }
            public TypeReference ImplementationType { get; }

            public ChainItemInfo(int order, TypeReference implementationType)
            {
                Order = order;
                ImplementationType = implementationType;
            }
        }
    }
}
