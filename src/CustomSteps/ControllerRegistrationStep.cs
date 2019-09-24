using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;
using Mono.Linker.Steps;

namespace CustomSteps
{
    public class ControllerRegistrationStep : IStep
    {
        public void Process(LinkContext context)
        {
            var walker = new WalkerTexasRanger(context);

            var assemblies = context.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                walker.WalkAssembly(assembly);
                if (context.Annotations.GetAction(assembly) == AssemblyAction.Link)
                {
                }
            }
        }

        private class WalkerTexasRanger : MetadataWalker
        {
            public WalkerTexasRanger(LinkContext context)
                : base(context)
            {
            }

            public new void WalkAssembly(AssemblyDefinition assembly)
            {
                base.WalkAssembly(assembly);
            }

            protected override void WalkMethodBody(MethodBody body)
            {
                for (var i = 0; i < body.Instructions.Count; i++)
                {
                    var instruction = body.Instructions[i];
                    if (instruction.Operand is MethodReference method &&
                        method.Name == "MapController" &&
                        method.DeclaringType.FullName == "Microsoft.AspNetCore.Builder.uControllerEndpointRouteBuilderExtensions")
                    {
                        var parameter = method.GenericParameters[0].FullName;
                        var thunk = new MethodDefinition(
                            "__register_" + parameter.Replace(".", "__"), 
                            MethodAttributes.Static,
                            method.Module.TypeSystem.Void);
                        method.Module.ImportReference(thunk, method.DeclaringType);


                    }
                }
            }
        }
    }
}
