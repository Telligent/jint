using Jint.Native.Object;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Jint.Runtime.Interop.Metadata
{
 public sealed class Invoker
 {
	public static Func<object, object[], object> GetFunc(ConstructorInfo constructorInfo)
	{
	 if (constructorInfo == null)
		throw new ArgumentNullException(nameof(constructorInfo));

	 return CreateConstructorWrapper(constructorInfo);
	}

	public static Func<object, object[], object> GetFunc(MethodInfo methodInfo)
	{
	 if (methodInfo == null)
		throw new ArgumentNullException(nameof(methodInfo));

	 return CreateMethodWrapper(methodInfo);
	}

	public static Func<object, object[], object> GetFunc(PropertyInfo propertyInfo)
	{
	 if (propertyInfo == null)
		throw new ArgumentNullException(nameof(propertyInfo));

	 var getter = propertyInfo.GetGetMethod(false);
	 if (getter == null)
		throw new ArgumentNullException("propertyInfo.GetMethod");

	 return GetFunc(getter);
	}

	public static Func<object, object[], object> SetFunc(PropertyInfo propertyInfo)
	{
	 if (propertyInfo == null)
		throw new ArgumentNullException(nameof(propertyInfo));

	 var setter = propertyInfo.GetSetMethod(false);
	 if (setter == null)
		throw new ArgumentNullException("propertyInfo.SetMethod");

	 return GetFunc(setter);
	}

	private static Func<object, object[], object> CreateConstructorWrapper(ConstructorInfo method)
	{
	 CreateParamsExpressions(method, out ParameterExpression argsExp, out Expression[] paramsExps);

		var invokeExp = Expression.New(method, paramsExps);

		LambdaExpression lambdaExp;

		var resultExp = Expression.Convert(invokeExp, typeof(object));
		lambdaExp = Expression.Lambda(resultExp, argsExp);

		var lambda = lambdaExp.Compile();
		var staticFunc = (Func<object[], object>)lambda;
		return new Func<object, object[], object>((a, b) => staticFunc(b));

	}

	private static Func<object, object[], object> CreateMethodWrapper(MethodInfo method)
	{
	 CreateParamsExpressions(method, out ParameterExpression argsExp, out Expression[] paramsExps);

	 if (method.IsStatic)
	 {
		var invokeExp = Expression.Call(method, paramsExps);

		LambdaExpression lambdaExp;

		if (method.ReturnType != typeof(void))
		{
		 var resultExp = Expression.Convert(invokeExp, typeof(object));
		 lambdaExp = Expression.Lambda(resultExp, argsExp);
		}
		else
		{
		 var constExp = Expression.Constant(null, typeof(object));
		 var blockExp = Expression.Block(invokeExp, constExp);
		 lambdaExp = Expression.Lambda(blockExp, argsExp);
		}

		var lambda = lambdaExp.Compile();
		var staticFunc = (Func<object[], object>)lambda;
		return new Func<object, object[], object>((a, b) => staticFunc(b));
	 }
	 else
	 {
		var targetExp = Expression.Parameter(typeof(object), "target");
		var castTargetExp = Expression.Convert(targetExp, method.DeclaringType);
		var invokeExp = Expression.Call(castTargetExp, method, paramsExps);

		LambdaExpression lambdaExp;

		if (method.ReturnType != typeof(void))
		{
		 var resultExp = Expression.Convert(invokeExp, typeof(object));
		 lambdaExp = Expression.Lambda(resultExp, targetExp, argsExp);
		}
		else
		{
		 var constExp = Expression.Constant(null, typeof(object));
		 var blockExp = Expression.Block(invokeExp, constExp);
		 lambdaExp = Expression.Lambda(blockExp, targetExp, argsExp);
		}

		var lambda = lambdaExp.Compile();
		return (Func<object, object[], object>)lambda;
	 }
	}

	private static void CreateParamsExpressions(MethodBase method, out ParameterExpression argsExp, out Expression[] paramsExps)
	{
	 var parameters = method.GetParameters();

	 argsExp = Expression.Parameter(typeof(object[]), "args");
	 paramsExps = new Expression[parameters.Length];

	 for (var i = 0; i < parameters.Length; i++)
	 {
		var constExp = Expression.Constant(i, typeof(int));
		var argExp = Expression.ArrayIndex(argsExp, constExp);
		paramsExps[i] = Expression.Convert(argExp, parameters[i].ParameterType);
	 }
	}
 }

}
