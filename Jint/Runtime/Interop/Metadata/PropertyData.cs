using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jint.Runtime.Interop.Metadata
{
 public class PropertyData
 {
	Func<object, object[], object> _getF;
	Func<object, object[], object> _setF;

	PropertyInfo _propertyInfo;
	FieldInfo _fieldInfo;
	Type _parameterType;

	internal PropertyData(PropertyInfo info, bool dynamic = false, Type parameterType = null)
	{
	 _propertyInfo = info;
	 _parameterType = parameterType;
	 CanWrite = _propertyInfo.CanWrite;
	 if (dynamic)
		_getF = new Func<object, object[], object>((target, parms) => ((DynamicPropertyInfo)info).GetValue(target, BindingFlags.Public, null, parms, null));
	}

	internal PropertyData(FieldInfo info)
	{
	 _fieldInfo = info;
	 CanWrite = !info.Attributes.HasFlag(FieldAttributes.InitOnly);
	}

	public bool CanRead
	{
	 get { return _propertyInfo?.CanRead ?? true; }
	}

	public bool CanWrite
	{
	 get;
	}

	public MemberInfo Info
	{
	 get { return (MemberInfo)_propertyInfo ?? _fieldInfo; }
	}

	public Type PropertyType
	{
	 get { return _propertyInfo?.PropertyType ?? _fieldInfo?.FieldType; }
	}

	public Type ParameterType
	{
	 get { return _parameterType; }
	}

	internal Func<object, object[], object> ExecuteGet
	{
	 get
	 {
		var f = _getF;
		if (f == null)
		{
		 if (_propertyInfo != null)
			_getF = f = Invoker.GetFunc(_propertyInfo);
		 else
			_getF = f = new Func<object, object[], object>((target, _) => _fieldInfo.GetValue(target));
		}

		return f;
	 }
	}

	internal Func<object, object[], object> ExecuteSet
	{
	 get
	 {
		var f = _setF;
		if (f == null)
		{
		 if (_propertyInfo != null)
			_setF = f = Invoker.SetFunc(_propertyInfo);
		 else
			_setF = f = new Func<object, object[], object>((target, args) =>
			{
			 _fieldInfo.SetValue(target, args[0]);
			 return null;
			});
		}

		return f;
	 }
	}
 }
}
