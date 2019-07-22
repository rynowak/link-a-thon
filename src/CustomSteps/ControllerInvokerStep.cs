using System;
using System.Diagnostics;
using System.Threading;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;

namespace CustomSteps
{
    public class ControllerInvokerStep : IStep
    {
        public void Process(LinkContext context)
        {
            var assemblies = context.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (context.Annotations.GetAction(assembly) == AssemblyAction.Link)
                {
                    foreach (var type in assembly.MainModule.GetTypes())
                    {
                        ProcessType(context, type);
                    }
                }
            }
        }

        private void ProcessType(LinkContext context, TypeDefinition type)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                ProcessType(context, nestedType);
            }

            if (type.IsClass && 
                !type.IsInterface && 
                !type.IsAbstract &&
                type.BaseType?.FullName == "uController.Controller")
            {
                context.Annotations.Mark(type);
                context.Annotations.SetPreserve(type, TypePreserve.All);
                Console.WriteLine($"Preserving controller type: {type.FullName}");
            }
        }
    }
}
