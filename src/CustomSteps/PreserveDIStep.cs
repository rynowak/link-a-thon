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
            if (Environment.GetEnvironmentVariable("DEBUG_STEP") == "1")
            {
                while (!System.Diagnostics.Debugger.IsAttached) ;
            }

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

        private class PreserveDIWalker: MetadataWalker
        {
            private readonly LinkContext _context;
            private MethodDefinition _currentMethod;

            public PreserveDIWalker(LinkContext context)
            {
                _context = context;
            }

            public void Walk(IEnumerable<AssemblyDefinition> asms)
            {
                foreach(var asm in asms)
                {
                    WalkAssembly(asm);
                }
            }

            protected override void WalkMethod(MethodDefinition method)
            {
                if(method.Parameters.Any(p => p.ParameterType.FullName.Equals("Microsoft.Extensions.DependencyInjection.IServiceCollection")))
                {
                    _currentMethod = method;
                    base.WalkMethod(method);
                    _currentMethod = null;
                }
            }

            protected override bool VisitInstruction(Instruction instruction)
            {
                if(instruction.Operand is MethodReference methodRef && IsDIMethod(methodRef))
                {
                    Console.WriteLine($"Method Call in {_currentMethod.DeclaringType.FullName}.{_currentMethod.Name}: {methodRef.DeclaringType.FullName}.{methodRef.Name}");
                }
                return true;
            }

            private bool IsDIMethod(MethodReference methodRef) => string.Equals(methodRef.DeclaringType.FullName, "Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions");
        }
    }
}
