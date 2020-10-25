﻿namespace Fixie
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    static class MethodInfoExtensions
    {
        static MethodInfo? startAsTask;

        public static async Task<object?> ExecuteAsync(this MethodInfo method, object? instance, params object?[] parameters)
        {
            if (method.ReturnType == typeof(void) && method.HasAsyncKeyword())
                throw new NotSupportedException(
                    "Async void methods are not supported. Declare async methods with a " +
                    "return type of Task to ensure the task actually runs to completion.");

            if (method.ContainsGenericParameters)
                throw new Exception("Could not resolve type parameters for generic method.");

            object? result;

            try
            {
                result = method.Invoke(instance, parameters.Length == 0 ? null : parameters);
            }
            catch (TargetInvocationException exception)
            {
                throw new PreservedException(exception);
            }

            if (result == null)
                return null;

            if (!ConvertibleToTask(result, out var task))
                return result;

            if (task.Status == TaskStatus.Created)
                throw new InvalidOperationException("The test returned a non-started task, which cannot be awaited. Consider using Task.Run or Task.Factory.StartNew.");

            await task;

            if (method.ReturnType.IsGenericType)
            {
                var property = task.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)!;

                return property.GetValue(task, null);
            }

            return null;
        }

        static bool HasAsyncKeyword(this MethodInfo method)
        {
            return method.Has<AsyncStateMachineAttribute>();
        }

        static bool ConvertibleToTask(object result, [NotNullWhen(true)] out Task? task)
        {
            if (result is Task t)
            {
                task = t;
                return true;
            }

            var resultType = result.GetType();

            if (IsFSharpAsync(resultType))
            {
                task = ConvertFSharpAsyncToTask(result, resultType);
                return true;
            }

            task = null;
            return false;
        }

        static bool IsFSharpAsync(Type resultType)
        {
            return resultType.IsGenericType &&
                   resultType.GetGenericTypeDefinition().FullName == "Microsoft.FSharp.Control.FSharpAsync`1";
        }

        static Task ConvertFSharpAsyncToTask(object result, Type resultType)
        {
            try
            {
                if (startAsTask == null)
                    startAsTask = resultType
                        .Assembly
                        .GetType("Microsoft.FSharp.Control.FSharpAsync")!
                        .GetRuntimeMethods()
                        .Single(x => x.Name == "StartAsTask");
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Unable to locate F# Control.Async.StartAsTask method.", exception);
            }

            var genericStartAsTask = startAsTask.MakeGenericMethod(resultType.GetGenericArguments());

            return (Task) genericStartAsTask.Invoke(null, new[] { result, null, null })!;
        }
    }
}
