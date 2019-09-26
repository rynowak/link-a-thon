// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed partial class ILEmitResolverBuilder : CallSiteVisitor<ILEmitResolverBuilderContext, object>
    {
        private readonly LinkContext _context;
        private readonly ModuleDefinition _module;
        private readonly ConcurrentDictionary<ServiceCacheKey, MethodDefinition> _scopeResolverCache;
        private readonly Func<ServiceCacheKey, ServiceCallSite, MethodDefinition> _buildTypeDelegate;

        public ILEmitResolverBuilder(LinkContext context, ModuleDefinition module) 
        {
            _context = context;
            _module = module;

            _scopeResolverCache = new ConcurrentDictionary<ServiceCacheKey, MethodDefinition>();
            _buildTypeDelegate = (key, cs) => BuildTypeNoCache(cs);

            Initialize();
        }

        public MethodDefinition Build(ServiceCallSite callSite)
        {
            return BuildType(callSite);
        }

        private MethodDefinition BuildType(ServiceCallSite callSite)
        {
            // Only scope methods are cached
            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
                return _scopeResolverCache.GetOrAdd(callSite.Cache.Key, _buildTypeDelegate, callSite);
            }

            return BuildTypeNoCache(callSite);
        }

        private MethodDefinition BuildTypeNoCache(ServiceCallSite callSite)
        {
            var method = new MethodDefinition($"Create_{Mangle(callSite.ImplementationType.FullName)}", MethodAttributes.Static, callSite.ServiceType);
            GenerateMethodBody(callSite, method.Body);
            return method;
        }

        private static string Mangle(string name)
        {
            return name.Replace(".", "_").Replace("+", "_").Replace("`", "_");
        }

        protected override object VisitDisposeCache(ServiceCallSite transientCallSite, ILEmitResolverBuilderContext argument)
        {
            if (transientCallSite.CaptureDisposable)
            {
                BeginCaptureDisposable(argument);
                VisitCallSiteMain(transientCallSite, argument);
                EndCaptureDisposable(argument);
            }
            else
            {
                VisitCallSiteMain(transientCallSite, argument);
            }
            return null;
        }

        protected override object VisitConstructor(ConstructorCallSite constructorCallSite, ILEmitResolverBuilderContext argument)
        {
            // new T([create arguments])
            foreach (var parameterCallSite in constructorCallSite.ParameterCallSites)
            {
                VisitCallSite(parameterCallSite, argument);
            }
            argument.IL.Emit(OpCodes.Newobj, constructorCallSite.ConstructorInfo);
            return null;
        }

        protected override object VisitRootCache(ServiceCallSite callSite, ILEmitResolverBuilderContext context)
        {
            // ProviderScope
            context.IL.Emit(OpCodes.Ldarg_1);
            context.IL.Emit(OpCodes.Call, generatedMethod.Lambda.GetType().GetMethod("Invoke"));
            throw null;
            //AddConstant(argument, _runtimeResolver.Resolve(callSite, _rootScope));
            //return null;
        }

        protected override object VisitScopeCache(ServiceCallSite scopedCallSite, ILEmitResolverBuilderContext argument)
        {
            throw null;

//            var generatedMethod = BuildType(scopedCallSite);

//            // Type builder doesn't support invoking dynamic methods, replace them with delegate.Invoke calls
//#if SAVE_ASSEMBLIES
//            AddConstant(argument, generatedMethod.Lambda);
//            // ProviderScope
//            argument.Generator.Emit(OpCodes.Ldarg_1);
//            argument.Generator.Emit(OpCodes.Call, generatedMethod.Lambda.GetType().GetMethod("Invoke"));
//#else
//            AddConstant(argument, generatedMethod.Context);
//            // ProviderScope
//            argument.IL.Emit(OpCodes.Ldarg_1);
//            argument.IL.Emit(OpCodes.Call, generatedMethod.Method);
//#endif

//            return null;
        }

        protected override object VisitConstant(ConstantCallSite constantCallSite, ILEmitResolverBuilderContext argument)
        {
            throw null;
            //AddConstant(argument, constantCallSite.DefaultValue);
            //return null;
        }

        protected override object VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, ILEmitResolverBuilderContext argument)
        {
            throw null;
            //// [return] ProviderScope
            //argument.IL.Emit(OpCodes.Ldarg_1);
            //return null;
        }

        protected override object VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, ILEmitResolverBuilderContext argument)
        {
            throw null;
            //// this.ScopeFactory
            //argument.IL.Emit(OpCodes.Ldarg_0);
            //argument.IL.Emit(OpCodes.Ldfld, typeof(ILEmitResolverBuilderRuntimeContext).GetField(nameof(ILEmitResolverBuilderRuntimeContext.ScopeFactory)));
            //return null;
        }

        protected override object VisitIEnumerable(IEnumerableCallSite enumerableCallSite, ILEmitResolverBuilderContext argument)
        {
            throw null;
            //if (enumerableCallSite.ServiceCallSites.Length == 0)
            //{
            //    argument.IL.Emit(OpCodes.Call, ExpressionResolverBuilder.ArrayEmptyMethodInfo.MakeGenericMethod(enumerableCallSite.ItemType));
            //}
            //else
            //{

            //    // var array = new ItemType[];
            //    // array[0] = [Create argument0];
            //    // array[1] = [Create argument1];
            //    // ...
            //    argument.IL.Emit(OpCodes.Ldc_I4, enumerableCallSite.ServiceCallSites.Length);
            //    argument.IL.Emit(OpCodes.Newarr, enumerableCallSite.ItemType);
            //    for (int i = 0; i < enumerableCallSite.ServiceCallSites.Length; i++)
            //    {
            //        // duplicate array
            //        argument.IL.Emit(OpCodes.Dup);
            //        // push index
            //        argument.IL.Emit(OpCodes.Ldc_I4, i);
            //        // create parameter
            //        VisitCallSite(enumerableCallSite.ServiceCallSites[i], argument);
            //        // store
            //        argument.IL.Emit(OpCodes.Stelem, enumerableCallSite.ItemType);
            //    }
            //}

            //return null;
        }

        protected override object VisitFactory(FactoryCallSite factoryCallSite, ILEmitResolverBuilderContext argument)
        {
            throw null;

            //if (argument.Factories == null)
            //{
            //    argument.Factories = new List<Func<IServiceProvider, object>>();
            //}

            //// this.Factories[i](ProviderScope)
            //argument.IL.Emit(OpCodes.Ldarg_0);
            //argument.IL.Emit(OpCodes.Ldfld, FactoriesField);

            //argument.IL.Emit(OpCodes.Ldc_I4, argument.Factories.Count);
            //argument.IL.Emit(OpCodes.Ldelem, typeof(Func<IServiceProvider, object>));

            //argument.IL.Emit(OpCodes.Ldarg_1);
            //argument.IL.Emit(OpCodes.Call, ExpressionResolverBuilder.InvokeFactoryMethodInfo);

            //argument.Factories.Add(factoryCallSite.Factory);
            //return null;
        }

        private void AddConstant(ILEmitResolverBuilderContext argument, object value)
        {
            throw null;

            //if (argument.Constants == null)
            //{
            //    argument.Constants = new List<object>();
            //}

            //// this.Constants[i]
            //argument.IL.Emit(OpCodes.Ldarg_0);
            //argument.IL.Emit(OpCodes.Ldfld, ConstantsField);

            //argument.IL.Emit(OpCodes.Ldc_I4, argument.Constants.Count);
            //argument.IL.Emit(OpCodes.Ldelem, typeof(object));
            //argument.Constants.Add(value);
        }

        private void AddCacheKey(ILEmitResolverBuilderContext argument, ServiceCacheKey key)
        {
            // new ServiceCacheKey(typeof(key.Type), key.Slot)
            argument.IL.Emit(OpCodes.Ldtoken, key.Type);
            argument.IL.Emit(OpCodes.Call, GetTypeFromHandle);
            argument.IL.Emit(OpCodes.Ldc_I4, key.Slot);
            argument.IL.Emit(OpCodes.Newobj, ServiceCacheKey_Ctor);
        }

        private void GenerateMethodBody(ServiceCallSite callSite, MethodBody body)
        {
            var context = new ILEmitResolverBuilderContext()
            {
                Body = body,
                IL = body.GetILProcessor(),
                Constants = null,
                Factories = null
            };

            //  var cacheKey = scopedCallSite.CacheKey;
            //  try
            //  {
            //    var resolvedServices = scope.ResolvedServices;
            //    Monitor.Enter(resolvedServices, out var lockTaken);
            //    if (!resolvedServices.TryGetValue(cacheKey, out value)
            //    {
            //       value = [createvalue];
            //       CaptureDisposable(value);
            //       resolvedServices.Add(cacheKey, value);
            //    }
            // }
            // finally
            // {
            //   if (lockTaken) Monitor.Exit(scope.ResolvedServices);
            // }
            // return value;

            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
                var cacheKeyLocal = new VariableDefinition(_module.ImportReference(ServiceCacheKey));
                context.Body.Variables.Add(cacheKeyLocal);

                var resolvedServicesLocalType = new GenericInstanceType(_module.ImportReference(typeof(IDictionary<,>)));
                resolvedServicesLocalType.GenericArguments.Add(_module.ImportReference(ServiceCacheKey));
                resolvedServicesLocalType.GenericArguments.Add(_module.ImportReference(typeof(object)));
                var resolvedServicesLocal = new VariableDefinition(resolvedServicesLocalType);
                context.Body.Variables.Add(resolvedServicesLocal);

                var lockTakenLocal = new VariableDefinition(_module.ImportReference(typeof(bool)));
                context.Body.Variables.Add(lockTakenLocal);

                var resultLocal = new VariableDefinition(_module.ImportReference(typeof(object)));
                context.Body.Variables.Add(resultLocal);

                // Generate cache key
                AddCacheKey(context, callSite.Cache.Key);

                // and store to local
                Stloc(context.IL, cacheKeyLocal.Index);

                var exceptionBlock = new ExceptionHandler(ExceptionHandlerType.Finally);
                context.IL.Body.ExceptionHandlers.Add(exceptionBlock);
                exceptionBlock.TryStart = context.IL.Body.Instructions[context.IL.Body.Instructions.Count - 1];

                // scope
                context.IL.Emit(OpCodes.Ldarg_1);
                // .ResolvedServices
                context.IL.Emit(OpCodes.Callvirt, resolvedServicesLocalType.GetMethods().Where(m => m.Name == "get_ResolvedServices").Single());
                // Store resolved services
                Stloc(context.IL, resolvedServicesLocal.Index);

                // Load resolvedServices
                Ldloc(context.IL, resolvedServicesLocal.Index);
                // Load address of lockTaken
                context.IL.Emit(OpCodes.Ldloca_S, lockTakenLocal.Index);
                // Monitor.Enter
                context.IL.Emit(OpCodes.Call, _module.ImportReference(ExpressionResolverBuilder.MonitorEnterMethodInfo));

                // Load resolved services
                Ldloc(context.IL, resolvedServicesLocal.Index);
                // Load cache key
                Ldloc(context.IL, cacheKeyLocal.Index);
                // Load address of result local
                context.IL.Emit(OpCodes.Ldloca_S, resultLocal.Index);
                // .TryGetValue
                context.IL.Emit(OpCodes.Callvirt, _module.ImportReference(ExpressionResolverBuilder.TryGetValueMethodInfo));

                // Jump to the end if already in cache
                var skipCreation = context.IL.Create(OpCodes.Nop); // Replaced later with a Brtrue
                context.IL.Append(skipCreation);

                // Create value
                VisitCallSiteMain(callSite, context);
                Stloc(context.IL, resultLocal.Index);

                if (callSite.CaptureDisposable)
                {
                    BeginCaptureDisposable(context);
                    Ldloc(context.IL, resultLocal.Index);
                    EndCaptureDisposable(context);
                    // Pop value returned by CaptureDisposable off the stack
                    context.IL.Emit(OpCodes.Pop);
                }

                // load resolvedServices
                Ldloc(context.IL, resolvedServicesLocal.Index);
                // load cache key
                Ldloc(context.IL, cacheKeyLocal.Index);
                // load value
                Ldloc(context.IL, resultLocal.Index);
                // .Add
                context.IL.Emit(OpCodes.Callvirt, _module.ImportReference(ExpressionResolverBuilder.AddMethodInfo));

                var skipCreationDest = context.IL.Create(OpCodes.Nop);
                context.IL.Append(skipCreationDest);
                context.IL.Replace(skipCreationDest, context.IL.Create(OpCodes.Brtrue, skipCreationDest));

                // End try
                exceptionBlock.TryEnd = context.Body.Instructions[context.Body.Instructions.Count - 1];

                // Start finally
                context.IL.Emit(OpCodes.Nop);
                exceptionBlock.HandlerStart = context.Body.Instructions[context.Body.Instructions.Count - 1];

                // load lockTaken
                Ldloc(context.IL, lockTakenLocal.Index);
                // return if not
                var @return = context.IL.Create(OpCodes.Nop); // Replaced later with a Brtrue
                context.IL.Append(@return);

                // Load resolvedServices
                Ldloc(context.IL, resolvedServicesLocal.Index);
                // Monitor.Exit
                context.IL.Emit(OpCodes.Call, _module.ImportReference(ExpressionResolverBuilder.MonitorExitMethodInfo));

                var returnDest = context.IL.Create(OpCodes.Nop);
                context.IL.Append(returnDest);
                context.IL.Replace(@return, context.IL.Create(OpCodes.Brtrue, returnDest));

                context.IL.Emit(OpCodes.Nop);
                exceptionBlock.HandlerEnd = context.Body.Instructions[context.Body.Instructions.Count - 1];


                // load value
                Ldloc(context.IL, resultLocal.Index);
                // return
                context.IL.Emit(OpCodes.Ret);
            }
            else
            {
                VisitCallSite(callSite, context);
                // return
                context.IL.Emit(OpCodes.Ret);
            }
        }

        private void BeginCaptureDisposable(ILEmitResolverBuilderContext argument)
        {
            argument.IL.Emit(OpCodes.Ldarg_1);
        }

        private void EndCaptureDisposable(ILEmitResolverBuilderContext argument)
        {
            // Call CaptureDisposabl we expect callee and arguments to be on the stack
            argument.IL.Emit(OpCodes.Callvirt, ServiceProviderEngineScope_CaptureDisposable);
        }

        private void Ldloc(ILProcessor generator, int index)
        {
            switch (index)
            {
                case 0:
                    generator.Emit(OpCodes.Ldloc_0);
                    return;
                case 1:
                    generator.Emit(OpCodes.Ldloc_1);
                    return;
                case 2:
                    generator.Emit(OpCodes.Ldloc_2);
                    return;
                case 3:
                    generator.Emit(OpCodes.Ldloc_3);
                    return;
            }

            if (index < byte.MaxValue)
            {
                generator.Emit(OpCodes.Ldloc_S, (byte)index);
                return;
            }

            generator.Emit(OpCodes.Ldloc, index);
        }

        private void Stloc(ILProcessor generator, int index)
        {
            switch (index)
            {
                case 0:
                    generator.Emit(OpCodes.Stloc_0);
                    return;
                case 1:
                    generator.Emit(OpCodes.Stloc_1);
                    return;
                case 2:
                    generator.Emit(OpCodes.Stloc_2);
                    return;
                case 3:
                    generator.Emit(OpCodes.Stloc_3);
                    return;
            }

            if (index < byte.MaxValue)
            {
                generator.Emit(OpCodes.Stloc_S, (byte)index);
                return;
            }

            generator.Emit(OpCodes.Stloc, index);
        }
    }
}
