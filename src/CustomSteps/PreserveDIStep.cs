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
            Console.WriteLine($"Preserving types used in DI...");
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

            public bool CaptureTypeTokens { get; set; }

            public void Walk(IEnumerable<AssemblyDefinition> asms)
            {
                foreach (var asm in asms)
                {
                    WalkAssembly(asm);
                }
            }

            protected override void WalkMethodBody(Mono.Cecil.Cil.MethodBody body)
            {
                if (body.Method.Name == "AddConfiguration" && body.Method.DeclaringType.Name == "LoggingBuilderConfigurationExtensions")
                {
                    Console.WriteLine();
                }

                CaptureTypeTokens = false;
                base.WalkMethodBody(body);

                if (CaptureTypeTokens)
                {
                    for (var i = 0; i < body.Instructions.Count; i++)
                    {
                        var instruction = body.Instructions[i];
                        if (instruction.OpCode == OpCodes.Ldtoken && instruction.Operand is TypeReference type)
                        {
                            PreserveConstructors(type, "typeof()");
                        }
                    }
                }
            }

            protected override bool VisitInstruction(Instruction instruction)
            {
                var method = instruction.Operand as MethodReference;
                if (method != null && IsDIMethod(method))
                {
                    if (method is GenericInstanceMethod generic)
                    {
                        // This is a case like services.TryAddSingleton<TService, TImpl>();
                        for (var i = 0; i < generic.GenericArguments.Count; i++)
                        {
                            PreserveConstructors(generic.GenericArguments[i], method.ToString());
                        }
                    }
                    else if (method != null && method.Parameters.Any(p => p.ParameterType.FullName == "System.Type"))
                    {
                        // This is a case like services.TryAddSingleton(typeof(TService), typeof(TImpl));
                        //
                        // We want to grovel all typeof(T) usages in this method.
                        CaptureTypeTokens = true;
                    }
                }

                return true;
            }

            private bool IsDIMethod(MethodReference method) => method.DeclaringType.FullName.StartsWith("Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal);

            private void PreserveConstructors(TypeReference typeReference, string reason)
            {
                var type = typeReference.Resolve();
                if (type == null || !type.IsClass)
                {
                    return;
                }

                foreach (var method in type.GetMethods())
                {
                    var resolved = method.Resolve();
                    if (resolved == null || !resolved.IsConstructor)
                    {
                        continue;
                    }

                    _context.Annotations.Mark(resolved);
                    _context.Annotations.MarkIndirectlyCalledMethod(resolved);
                    _context.Annotations.SetAction(resolved, MethodAction.Parse);
                    _context.Annotations.AddPreservedMethod(type, resolved);
                }
            }
        }
    }
}
