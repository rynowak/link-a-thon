using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;
using Mono.Cecil;
using Mono.Linker;

namespace CustomSteps
{
    internal static class StaticContainerInjector
    {
        public static void BuildStaticContainer(LinkContext context, List<ServiceDescriptor> services)
        {
            var program = GetEntryPoint(context);
            if (program == null)
            {
                return;
            }

            var factory = GetServiceProviderFactory(program);
            if (factory == null)
            {
                return;
            }

            context.Annotations.Mark(factory);
            context.Annotations.SetPreserve(factory, TypePreserve.All);

            var resolved = new List<(ServiceDescriptor, ServiceCallSite)>();
            var unresolved = new List<ServiceDescriptor>();
            var callSiteFactory = new CallSiteFactory(services);
            var resolver = new ILEmitResolverBuilder(context, program.Module);
            for (var i = 0; i < services.Count; i++)
            {
                var service = services[i];
                var callSite = callSiteFactory.GetCallSite(service, new CallSiteChain());
                if (callSite == null)
                {
                    unresolved.Add(service);
                    continue;
                }

                resolved.Add((service, callSite));

                var method = resolver.Build(callSite);
                program.Methods.Add(method);
            }

            Console.WriteLine($"Resolved {resolved.Count} of a total of {services.Count} callsites.");
        }

        private static TypeDefinition GetEntryPoint(LinkContext context)
        {
            var assemblies = context.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i];
                if (assembly.MainModule.Kind != ModuleKind.Dll)
                {
                    return assembly.MainModule.EntryPoint.DeclaringType;
                }
            }

            return null;
        }

        private static TypeDefinition GetServiceProviderFactory(TypeDefinition type)
        {
            for (var i = 0; i < type.NestedTypes.Count; i++)
            {
                var nestedType = type.NestedTypes[i];
                if (nestedType.Name == "ServiceProviderFactory")
                {
                    return nestedType;
                }
            }

            return null;
        }
    }
}
