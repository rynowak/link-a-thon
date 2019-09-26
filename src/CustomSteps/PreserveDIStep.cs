using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;

namespace CustomSteps
{
    public class PreserveDIStep : MarkStep
    {
        private readonly List<ServiceDescriptor> services = new List<ServiceDescriptor>();
        private readonly List<TypeReference> options = new List<TypeReference>();
        private readonly List<TypeReference> activated = new List<TypeReference>();

        public override void Process(LinkContext context)
        {
            // This step replaces the MarkStep
            context.Pipeline.RemoveStep(typeof(MarkStep));

            Console.WriteLine($"Preserving types used in DI...");
            Console.WriteLine($"Saving data to {Path.GetFullPath("di.txt")}");

            using (var writer = new StreamWriter("di.txt"))
            {
                services.Clear();
                options.Clear();
                activated.Clear();

                base.Process(context);

                foreach (var service in services)
                {
                    writer.WriteLine($"{service.Lifetime}: {service.ServiceType?.FullName} - {service.ImplementationType?.FullName ?? "(instance/factory)"}");
                }
            }

            StaticContainerInjector.BuildStaticContainer(context, services);
        }

        protected override TypeDefinition MarkType(TypeReference reference)
        {
            if (reference != null && 
                reference.FullName.StartsWith("Microsoft.Extensions.Options") && 
                reference is GenericInstanceType generic &&
                generic.GenericArguments.Count == 1)
            {
                if (generic.GenericArguments[0].Resolve() is TypeDefinition optionsType)
                {
                    options.Add(optionsType);

                    var constructor = optionsType.GetDefaultInstanceConstructor().Resolve();
                    if (constructor != null)
                    {
                        MarkIndirectlyCalledMethod(constructor);
                    }
                }
            }

            return base.MarkType(reference);
        }

        protected override void DoAdditionalMethodProcessing(MethodDefinition method)
        {
            if (method.HasBody)
            {
                var body = method.Body;
                for (var i = 0; i < body.Instructions.Count; i++)
                {
                    var instruction = body.Instructions[i];
                    var operand = instruction.Operand as MethodReference;
                    if (operand == null)
                    {
                        continue;
                    }

                    if (DIFacts.TryParseGenericAddServiceMethod(operand, out var service))
                    {
                        if (service == null)
                        {
                            Console.WriteLine($"Cannot analyze call to {operand} in {method}");
                            continue;
                        }

                        MarkServiceType(service);
                    }
                    else if (DIFacts.TryParseHostedService(operand, out service))
                    {
                        MarkServiceType(service);
                    }
                    else if (DIFacts.TryParseNonGenericAddServiceMethod(body, i, operand, out service))
                    {
                        if (service == null)
                        {
                            Console.WriteLine($"Cannot analyze call to {operand} in {method}");
                            continue;
                        }

                        MarkServiceType(service);
                    }
                    else if (DIFacts.TryParseActivatedType(operand, out var activatedType))
                    {
                        var resolved = activatedType.Resolve();
                        if (resolved == null)
                        {
                            Console.WriteLine($"Cannot analyze call to {operand} in {method}");
                            continue;
                        }

                        activated.Add(activatedType);
                        PreserveConstructors(resolved);
                    }
                }
            }
        }

        private void MarkServiceType(ServiceDescriptor service)
        {
            services.Add(service);

            var type = service.ImplementationType?.Resolve();
            if (type == null || !type.IsClass)
            {
                return;
            }

            PreserveConstructors(type);
        }

        private void PreserveConstructors(TypeDefinition type)
        {
            foreach (var method in type.GetMethods())
            {
                var resolved = method.Resolve();
                if (resolved == null || !resolved.IsConstructor)
                {
                    continue;
                }

                MarkIndirectlyCalledMethod(resolved);
            }
        }
    }
}
