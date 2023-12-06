using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jint.Runtime.Interop.Metadata
{
 public class MethodData
 {
	Func<object, object[], object> _f;
	MethodInfo _methodInfo;
	ConstructorInfo _constructorInfo;

	internal MethodData(MethodInfo info, bool dynamic = false)
	{
	 _methodInfo = info;
	 if (dynamic)
	 {
		_f = new Func<object, object[], object>((target, parms) => ((DynamicMethodInfo)info).Invoke(target, BindingFlags.Public, null, parms, null));
		ParameterTypes = [typeof(object)];
		ParamsParameterIndex = 0;
	 }
	 else
	 {
		var parameterTypes = new List<Type>();
		var parameters = info.GetParameters();
		for (var i = 0; i < parameters.Length; i++)
		{
		 parameterTypes.Add(parameters[i].ParameterType);
		 if (parameters[i].HasAttribute<ParamArrayAttribute>())
			ParamsParameterIndex = i;
		}
		ParameterTypes = parameterTypes.ToArray();
	 }
	}

	internal MethodData(ConstructorInfo info)
	{
	 _constructorInfo = info;

	 var parameterTypes = new List<Type>();
	 var parameters = info.GetParameters();
	 for (var i = 0; i < parameters.Length; i++)
	 {
		parameterTypes.Add(parameters[i].ParameterType);
		if (parameters[i].HasAttribute<ParamArrayAttribute>())
		 ParamsParameterIndex = i;
	 }
	 ParameterTypes = parameterTypes.ToArray();
	}

	public MethodBase Info
	{
	 get { return (MethodBase)_methodInfo ?? _constructorInfo; }
	}

	public Type[] ParameterTypes { get; }

	public int? ParamsParameterIndex { get; }

	internal Func<object, object[], object> Execute
	{
	 get
	 {
		var f = _f;
		if (f == null)
		 _f = f = _methodInfo != null ? Invoker.GetFunc(_methodInfo) : Invoker.GetFunc(_constructorInfo);

		return f;
	 }
	}
 }
}
