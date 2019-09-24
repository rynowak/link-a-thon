using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;

namespace CustomSteps
{
    public abstract class MetadataWalker
    {
        public MetadataWalker(LinkContext context)
        {
            Context = context;
        }

        protected LinkContext Context { get; }

        protected AssemblyDefinition CurrentAssembly { get; private set; }

        protected ModuleDefinition CurrentModule { get; private set; }

        protected TypeDefinition CurrentType { get; private set; }

        protected MethodDefinition CurrentMethod { get; private set; }

        public void Walk(IEnumerable<AssemblyDefinition> asms)
        {
            foreach (var asm in asms)
            {
                WalkAssembly(asm);
            }
        }


        protected virtual void WalkAssembly(AssemblyDefinition assembly)
        {
            if (VisitAssembly(assembly))
            {
                CurrentAssembly = assembly;

                WalkCustomAttributes(assembly);

                foreach (var module in assembly.Modules)
                {
                    WalkModule(module);
                }

                CurrentAssembly = null;
            }
        }

        protected virtual void WalkModule(ModuleDefinition module)
        {
            if (VisitModule(module))
            {
                CurrentModule = module;

                WalkCustomAttributes(module);

                foreach (var type in module.Types)
                {
                    WalkType(type);
                }

                CurrentModule = null;
            }
        }

        protected virtual void WalkType(TypeDefinition type)
        {
            if (VisitType(type))
            {
                CurrentType = type;

                WalkCustomAttributes(type);

                foreach (var nested in type.NestedTypes)
                {
                    WalkType(nested);
                }

                foreach (var field in type.Fields)
                {
                    WalkField(field);
                }

                foreach (var prop in type.Properties)
                {
                    WalkProperty(prop);
                }

                foreach (var method in type.Methods)
                {
                    WalkMethod(method);
                }

                foreach (var evt in type.Events)
                {
                    WalkEvent(evt);
                }

                CurrentType = type;
            }
        }

        protected virtual void WalkEvent(EventDefinition evt)
        {
            if (VisitEvent(evt))
            {
                WalkCustomAttributes(evt);

                if (evt.AddMethod != null)
                {
                    WalkMethod(evt.AddMethod);
                }

                if (evt.RemoveMethod != null)
                {
                    WalkMethod(evt.RemoveMethod);
                }
            }
        }

        protected virtual void WalkMethod(MethodDefinition method)
        {
            if (VisitMethod(method))
            {
                CurrentMethod = method;

                WalkCustomAttributes(method);

                foreach (var param in method.Parameters)
                {
                    WalkParameter(param);
                }

                if (method.Body != null)
                {
                    WalkMethodBody(method.Body);
                }

                CurrentMethod = null;
            }
        }

        protected virtual void WalkParameter(ParameterDefinition param)
        {
            if (VisitParameter(param))
            {
                WalkCustomAttributes(param);
            }
        }

        protected virtual void WalkMethodBody(MethodBody body)
        {
            foreach (var local in body.Variables)
            {
                VisitLocal(local);
            }

            foreach (var instruction in body.Instructions)
            {
                VisitInstruction(instruction);
            }
        }

        protected virtual void WalkProperty(PropertyDefinition prop)
        {
            if (VisitProperty(prop))
            {
                WalkCustomAttributes(prop);

                if (prop.GetMethod != null)
                {
                    WalkMethod(prop.GetMethod);
                }

                if (prop.SetMethod != null)
                {
                    WalkMethod(prop.SetMethod);
                }
            }
        }

        protected virtual void WalkField(FieldDefinition field)
        {
            if (VisitField(field))
            {
                WalkCustomAttributes(field);
            }
        }

        protected virtual bool VisitEvent(EventDefinition evt) => true;

        protected virtual bool VisitMethod(MethodDefinition method) => true;

        protected virtual bool VisitProperty(PropertyDefinition prop) => true;

        protected virtual bool VisitField(FieldDefinition field) => true;

        protected virtual bool VisitCustomAttribute(CustomAttribute attribute, ICustomAttributeProvider parent) => true;

        protected virtual bool VisitInstruction(Instruction instruction) => true;

        protected virtual bool VisitLocal(VariableDefinition local) => true;

        protected virtual bool VisitParameter(ParameterDefinition param) => true;

        protected virtual bool VisitType(TypeDefinition type) => true;

        protected virtual bool VisitModule(ModuleDefinition module) => true;

        protected virtual bool VisitAssembly(AssemblyDefinition assembly) => true;

        private void WalkCustomAttributes(ICustomAttributeProvider attributeProvider)
        {
            foreach (var attribute in attributeProvider.CustomAttributes)
            {
                VisitCustomAttribute(attribute, attributeProvider);
            }
        }
    }
}
