﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Function;

namespace Jint.Runtime.Interop
{
    public sealed class MethodInfoFunctionInstance : FunctionInstance
    {
        private readonly MethodInfo[] _methods;

        public MethodInfoFunctionInstance(Engine engine, MethodInfo[] methods)
            : base(engine, null, null, false)
        {
            _methods = methods;
            Prototype = engine.Function.PrototypeObject;
        }

        public override JsValue Call(JsValue thisObject, JsValue[] arguments)
        {
            return Invoke(_methods, thisObject, arguments);
        }

        public JsValue Invoke(MethodInfo[] methodInfos, JsValue thisObject, JsValue[] jsArguments)
        {
            var methods = TypeConverter.FindBestMatch(Engine, methodInfos, jsArguments).ToList();
            var converter = Engine.ClrTypeConverter;

            foreach (var method in methods)
            {
				var methodArguments = ProcessParamsArrays(jsArguments, method);
				var parameters = new object[methodArguments.Length];
                var argumentsMatch = true;

                for (var i = 0; i < methodArguments.Length; i++)
                {
                    var parameterType = method.GetParameters()[i].ParameterType;

                    if (parameterType == typeof(JsValue))
                    {
                        parameters[i] = methodArguments[i];
                    }
                    else if (parameterType == typeof(JsValue[]) && methodArguments[i].IsArray())
                    {
                        // Handle specific case of F(params JsValue[])

                        var arrayInstance = methodArguments[i].AsArray();
                        var len = TypeConverter.ToInt32(arrayInstance.Get("length"));
                        var result = new JsValue[len];
                        for (var k = 0; k < len; k++)
                        {
                            var pk = k.ToString();
                            result[k] = arrayInstance.HasProperty(pk)
                                ? arrayInstance.Get(pk)
                                : JsValue.Undefined;
                        }

                        parameters[i] = result;
                    }
                    else
                    {
                        if (!converter.TryConvert(methodArguments[i].ToObject(), parameterType, CultureInfo.InvariantCulture, out parameters[i]))
                        {
                            argumentsMatch = false;
                            break;
                        }

                        var lambdaExpression = parameters[i] as LambdaExpression;
                        if (lambdaExpression != null)
                        {
                            parameters[i] = lambdaExpression.Compile();
                        }
                    }
                }

                if (!argumentsMatch)
                {
                    continue;
                }

                // todo: cache method info
                try
                {
                    return JsValue.FromObject(Engine, method.Invoke(thisObject.ToObject(), parameters.ToArray()));
                }
                catch (TargetInvocationException exception)
                {
                    var meaningfulException = exception.InnerException ?? exception;
                    var handler = Engine.Options._ClrExceptionsHandler;

                    if (handler != null && handler(meaningfulException))
                    {
                        throw new JavaScriptException(Engine.Error, meaningfulException.Message, meaningfulException);
                    }

                    throw meaningfulException;
                }
            }

            throw new JavaScriptException(Engine.TypeError, "No public methods with the specified arguments were found.");
        }

        /// <summary>
        /// Reduces a flat list of parameters to a params array
        /// </summary>
        private JsValue[] ProcessParamsArrays(JsValue[] jsArguments, MethodBase methodInfo)
        {
            var parameters = methodInfo.GetParameters();
			if (!parameters.Any(p => p.HasAttribute<ParamArrayAttribute>()))
				return jsArguments;

            var nonParamsArgumentsCount = parameters.Length - 1;
            if (jsArguments.Length < nonParamsArgumentsCount)
				return jsArguments;

			var newArgumentsCollection = jsArguments.Take(nonParamsArgumentsCount).ToList();
            var argsToTransform = jsArguments.Skip(nonParamsArgumentsCount).ToList();

            if (argsToTransform.Count == 1 && argsToTransform.FirstOrDefault().IsArray())
				return jsArguments;

			var jsArray = Engine.Array.Construct(Arguments.Empty);
            Engine.Array.PrototypeObject.Push(jsArray, argsToTransform.ToArray());

            newArgumentsCollection.Add(new JsValue(jsArray));
            return newArgumentsCollection.ToArray();
        }

    }
}
