using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;
using Mono.Linker.Steps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CustomSteps
{
    public class PreserveDIStep : IStep
    {
        public void Process(LinkContext context)
        {
            var asms = context
                .GetAssemblies()
                .Where(a => ReferencesDI(a));
            var diWalker = new PreserveDIWalker(context);
            diWalker.Walk(asms);
        }

        private bool ReferencesDI(AssemblyDefinition asm)
        {
            return asm.Modules
                .Any(m => m.AssemblyReferences
                    .Any(ar => string.Equals(ar.Name, "Microsoft.Extensions.DependencyInjection.Abstractions")));
        }

        private class PreserveDIWalker : MetadataWalker
        {
            private readonly LinkContext _context;

            public PreserveDIWalker(LinkContext context)
            {
                _context = context;
            }

            public void Walk(IEnumerable<AssemblyDefinition> asms)
            {
                foreach (var asm in asms)
                {
                    WalkAssembly(asm);
                }
            }

            protected override bool VisitMethod(MethodDefinition method)
            {
                if (method.Name == "CreateServiceProvider")
                {
                    Console.WriteLine();
                }

                return base.VisitMethod(method);
            }

            protected override bool VisitInstruction(Instruction instruction)
            {
                if (instruction.Operand is GenericInstanceMethod method && IsDIMethod(method))
                {
                    for (var i = 0; i < method.GenericArguments.Count; i++)
                    {
                        var type = method.GenericArguments[i].Resolve();
                        if (type == null || !type.IsClass)
                        {
                            continue;
                        }

                        foreach (var methodOnType in type.GetMethods())
                        {
                            var resolved = methodOnType.Resolve();
                            if (resolved == null)
                            {
                                continue;
                            }

                            Console.WriteLine($"Marking {resolved} as instantiated because of {method}.");
                            _context.Annotations.Mark(resolved);
                            _context.Annotations.MarkIndirectlyCalledMethod(resolved);
                            _context.Annotations.SetAction(resolved, MethodAction.Parse);
                            _context.Annotations.AddPreservedMethod(type, resolved);
                        }
                    }
                }

                return true;
            }

            private bool IsDIMethod(MethodReference method) => method.DeclaringType.FullName.StartsWith("Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal);
        }
    }
}
