// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal static class ExpressionResolverBuilder
    {
        internal static readonly MethodInfo InvokeFactoryMethodInfo = GetMethodInfo<Action<Func<IServiceProvider, object>, IServiceProvider>>((a, b) => a.Invoke(b));
        internal static readonly MethodInfo TryGetValueMethodInfo = GetMethodInfo<Func<IDictionary<ServiceCacheKey, object>, ServiceCacheKey, object, bool>>((a, b, c) => a.TryGetValue(b, out c));
        internal static readonly MethodInfo AddMethodInfo = GetMethodInfo<Action<IDictionary<ServiceCacheKey, object>, ServiceCacheKey, object>>((a, b, c) => a.Add(b, c));
        internal static readonly MethodInfo MonitorEnterMethodInfo = GetMethodInfo<Action<object, bool>>((lockObj, lockTaken) => Monitor.Enter(lockObj, ref lockTaken));
        internal static readonly MethodInfo MonitorExitMethodInfo = GetMethodInfo<Action<object>>(lockObj => Monitor.Exit(lockObj));
        internal static readonly MethodInfo ArrayEmptyMethodInfo = typeof(Array).GetMethod(nameof(Array.Empty));

        private static Expression Convert(Expression expression, Type type)
        {
            // Don't convert if the expression is already assignable
            if (type.GetTypeInfo().IsAssignableFrom(expression.Type.GetTypeInfo()))
            {
                return expression;
            }

            return Expression.Convert(expression, type);
        }

        private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method;
        }
    }
}
