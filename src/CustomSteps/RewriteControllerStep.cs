using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;
using Mono.Linker.Steps;
using System;
using System.Linq;

namespace CustomSteps
{
    public class RewriteControllerStep : BaseStep
    {
        protected override void ProcessAssembly(AssemblyDefinition assembly)
        {
            if (Environment.GetEnvironmentVariable("DEBUG_STEP") == "1")
            {
                while (!System.Diagnostics.Debugger.IsAttached) ;
            }

            foreach (var type in assembly.MainModule.Types)
            {
                if (type.BaseType?.FullName == "uController.Controller")
                {
                    foreach (var method in type.Methods.Where(m => !m.IsStatic && m.IsPublic && !m.IsConstructor))
                    {
                        ProcessMethod(type, method);
                    }
                }
            }
        }

        private void ProcessMethod(TypeDefinition declaringType, MethodDefinition method)
        {
            var executeMethod = declaringType.Methods.First(m => m.Name == $"Execute{method.Name}");
            var body = new MethodBody(executeMethod);
            var il = body.GetILProcessor();

            il.Emit(OpCodes.Newobj, declaringType.GetDefaultInstanceConstructor());

            for (var i = 0; i < method.Parameters.Count; i++)
            {
                var varI = new VariableDefinition(method.Parameters[i].ParameterType);
                body.Variables.Add(varI);
            }

            for (var i = 0; i < method.Parameters.Count; i++)
            {
                var parameter = method.Parameters[i];
                if (parameter.CustomAttributes.Any(c => c.AttributeType.FullName == "uController.FromQueryAttribute"))
                {
                    var httpContext = Context.GetType("Microsoft.AspNetCore.Http.HttpContext");
                    var getHttpRequest = httpContext.Methods.First(f => f.Name == "get_Request");
                    var httpRequest = getHttpRequest.MethodReturnType.ReturnType.Resolve();
                    var getHttpQuery = httpRequest.Methods.First(f => f.Name == "get_Query");
                    var query = getHttpQuery.MethodReturnType.ReturnType.Resolve();
                    var getItem = query.Methods.First(f => f.Name == "get_Item");
                    var stringValues = getItem.MethodReturnType.ReturnType.Resolve();
                    var toString = stringValues.Methods.First(f => f.Name == "ToString");

                    il.Emit(OpCodes.Ldarg_0);

                    il.Emit(OpCodes.Callvirt, getHttpRequest);
                    il.Emit(OpCodes.Callvirt, getHttpQuery);
                    il.Emit(OpCodes.Ldstr, parameter.Name);
                    il.Emit(OpCodes.Callvirt, getItem);
                    il.Emit(OpCodes.Callvirt, toString);

                    il.Emit(OpCodes.Stloc, i);
                }
            }

            for (var i = 0; i < method.Parameters.Count; i++)
            {
                il.Emit(OpCodes.Ldloc, i);
            }

            il.Emit(OpCodes.Callvirt, method);
            il.Emit(OpCodes.Ldarg_0);

            var result = Context.GetType("uController.Result");
            var executeAsync = result.Methods.First(f => f.Name == "ExecuteAsync");
            il.Emit(OpCodes.Callvirt, executeAsync);
            il.Emit(OpCodes.Ret);

            executeMethod.Body = body;
            executeMethod.ClearDebugInformation();
        }
    }
}
