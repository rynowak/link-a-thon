using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CustomSteps
{
    internal static class DIFacts
    {
        private const string DINamespace = "Microsoft.Extensions.DependencyInjection";
        private const string ServiceDescriptorType = DINamespace + ".ServiceDescriptor";
        private const string PrimaryExtensionsType = DINamespace + ".ServiceCollectionServiceExtensions";
        private const string SecondaryExtensionsType = DINamespace + ".Extensions.ServiceCollectionDescriptorExtensions";
        private const string HostedServiceType = DINamespace + ".ServiceCollectionHostedServiceExtensions";
        private const string ActivatorUtilitiesType = DINamespace + ".ActivatorUtilities";

        // This code handles the patterns of our extension methods (generic) and our ServiceDescriptor creation methods.
        //
        // These methods all work the same semantically so they can be analyzed together.
        public static bool TryParseGenericAddServiceMethod(MethodReference method, out TypeReference service, out TypeReference implementation)
        {
            service = null;
            implementation = null;

            if (method.DeclaringType.FullName != PrimaryExtensionsType && 
                method.DeclaringType.FullName != SecondaryExtensionsType &&
                method.DeclaringType.FullName != ServiceDescriptorType)
            {
                return false;
            }

            if (method is GenericInstanceMethod generic)
            {
                if (generic.GenericArguments.Count == 1)
                {
                    // This is a case like services.AddSingleton<TService>()
                    service = generic.GenericArguments[0];
                    implementation = generic.GenericArguments[0];
                }
                else if (generic.GenericArguments.Count == 2)
                {
                    // This is a case like services.AddSingleton<TService, TImplementation>()
                    service = generic.GenericArguments[0];
                    implementation = generic.GenericArguments[1];
                }

                // NOTE: this is commented out because we need to support open-generic service implementations. 
                // Logging has a class that implements IConfigureOptions<TProvider> with an open generic.
                //
                // If either type is a generic type parameter (of the calling method), blank it out - we don't
                // have the ability to analyze that. This is the pattern that's used by things like AddHostedService<T>().
                //if (service != null && service.ContainsGenericParameter)
                //{
                //    service = null;
                //}
                //if (implementation != null && implementation.ContainsGenericParameter)
                //{
                //    implementation = null;
                //}

                // Search for a Func<> argument. We have an overload that accepts a TImplementation type parameter,
                // but the type argument isn't passed through to the underlying DI system. If we see this, then
                // we don't want to record TImplementation because the factory is the source of truth.
                for (var i = 0; i < generic.Parameters.Count; i++)
                {
                    var parameter = generic.Parameters[i];
                    if (parameter.ParameterType.FullName.StartsWith("System.Func"))
                    {
                        implementation = null;
                        break;
                    }
                }

                // Return true even if we didn't capture types, because we figured out what method this is
                // and we fully analyzed it.
                return true;
            }

            return false;
        }

        public static bool TryParseNonGenericAddServiceMethod(
            MethodBody body,
            int ip,
            MethodReference method, 
            out TypeReference service, 
            out TypeReference implementation)
        {
            service = null;
            implementation = null;

            if (method.DeclaringType.FullName != PrimaryExtensionsType &&
                method.DeclaringType.FullName != SecondaryExtensionsType &&
                method.DeclaringType.FullName != ServiceDescriptorType)
            {
                return false;
            }

            if (method is GenericInstanceMethod generic)
            {
                return false;
            }

            var typeCount = 0;
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                var parameter = method.Parameters[i];
                if (parameter.ParameterType.FullName == "System.Type")
                {
                    typeCount++;
                }
            }

            if (typeCount == 0)
            {
                return false;
            }
            else if (typeCount == 1)
            {
                for (var i = ip - 1; i >= 0; i--)
                {
                    var instruction = body.Instructions[i];
                    if (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference type)
                    {
                        service = type;
                        implementation = type;
                        break;
                    }
                }
            }
            else if (typeCount == 2)
            {
                var i = ip;
                for (; i >= 0; i--)
                {
                    var instruction = body.Instructions[i];
                    if (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference type)
                    {
                        implementation = type;
                        break;
                    }
                }

                i--;

                for (; i >= 0; i--)
                {
                    var instruction = body.Instructions[i];
                    if (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference type)
                    {
                        service = type;
                        break;
                    }
                }
            }
            else
            {
                // dunno mang.
                return false;
            }
            
            // These methods also accept Funcs and instances, and that means we don't know the implementation
            // type.
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                var parameter = method.Parameters[i];
                if (parameter.ParameterType.FullName.StartsWith("System.Func"))
                {
                    implementation = null;
                    break;
                }

                if (parameter.ParameterType.FullName == "System.Object")
                {
                    implementation = null;
                    break;
                }
            }

            return true;
        }

        public static bool TryParseHostedService(MethodReference method, out TypeReference service, out TypeReference implementation)
        {
            service = null;
            implementation = null;

            if (method.DeclaringType.FullName != HostedServiceType)
            {
                return false;
            }

            if (method is GenericInstanceMethod generic)
            {
                if (generic.GenericArguments.Count == 1)
                {
                    service = generic.Resolve().GenericParameters[0].Constraints[0].ConstraintType;
                    implementation = generic.GenericArguments[0];
                }

                // Search for a Func<> argument. We have an overload that accepts a TImplementation type parameter,
                // but the type argument isn't passed through to the underlying DI system. If we see this, then
                // we don't want to record TImplementation because the factory is the source of truth.
                for (var i = 0; i < generic.Parameters.Count; i++)
                {
                    var parameter = generic.Parameters[i];
                    if (parameter.ParameterType.FullName.StartsWith("System.Func"))
                    {
                        implementation = null;
                        break;
                    }
                }

                // Return true even if we didn't capture types, because we figured out what method this is
                // and we fully analyzed it.
                return true;
            }

            return false;
        }

        public static bool TryParseActivatedType(MethodReference method, out TypeReference type)
        {
            type = null;

            if (method.DeclaringType.FullName != ActivatorUtilitiesType)
            {
                return false;
            }

            if (method is GenericInstanceMethod generic)
            {
                type = generic.GenericArguments[0];
                return true;
            }

            return false; // No support for the overloads that accept a type yet.
        }
    }
}
