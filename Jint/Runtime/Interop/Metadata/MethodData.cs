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
	bool _parametersAreExactTypes;
	MethodInfo _methodInfo;
	ConstructorInfo _constructorInfo;

	internal MethodData(MethodInfo info, bool dynamic = false, bool parametersAreExactType = false)
	{
	 _methodInfo = info;
	 _parametersAreExactTypes = parametersAreExactType;
	 if (dynamic)
		_f = new Func<object, object[], object>((target, parms) => ((DynamicMethodInfo)info).Invoke(target, BindingFlags.Public, null, parms, null));

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

	internal MethodData(ConstructorInfo info, bool parametersAreExactType = false)
	{
	 _constructorInfo = info;
	 _parametersAreExactTypes = parametersAreExactType;

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
		{
		 f = _methodInfo != null ? Invoker.GetFunc(_methodInfo) : Invoker.GetFunc(_constructorInfo);
		 if (_parametersAreExactTypes)
		 {
			_f = f;
		 }
		 else
		 {
			var innerF = f;
			_f = f = new Func<object, object[], object>((target, parms) =>
			{
			 for (var i = 0; i < parms.Length; i++)
			 {
				if (parms[i] != null)
				{
				 var parmType = parms[i].GetType();
				 if (parmType != ParameterTypes[i] && typeof(IConvertible).IsAssignableFrom(parmType) && typeof(IConvertible).IsAssignableFrom(ParameterTypes[i]))
					parms[i] = Convert.ChangeType(parms[i], ParameterTypes[i]);
				}
			 }

			 return innerF.Invoke(target, parms);
			});
		 }
		}

		return f;
	 }
	}
 }
}
