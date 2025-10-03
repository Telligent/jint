using System;
using System.Linq;
using System.Reflection;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;
using Jint.Runtime.Interop.Metadata;

namespace Jint.Runtime.Interop
{
 /// <summary>
 /// Wraps a CLR instance
 /// </summary>
 public sealed class ObjectWrapper : ObjectInstance, IObjectWrapper
 {
	private TypeData _typeData;
	public Object Target { get; set; }

	public ObjectWrapper(Engine engine, Object obj)
			: base(engine)
	{
	 Target = obj;
	 _typeData = TypeData.Get(obj.GetType());
	}

	public override void Put(string propertyName, JsValue value, bool throwOnError)
	{
	 if (!CanPut(propertyName))
	 {
		if (throwOnError)
		{
		 throw new JavaScriptException(Engine.TypeError);
		}

		return;
	 }

	 var ownDesc = GetOwnProperty(propertyName);

	 if (ownDesc == null)
	 {
		if (throwOnError)
		{
		 throw new JavaScriptException(Engine.TypeError, "Unknown member: " + propertyName);
		}
		else
		{
		 return;
		}
	 }

	 ownDesc.Value = value;
	}

	public override PropertyDescriptor GetOwnProperty(string propertyName)
	{
	 PropertyDescriptor x;
	 if (Properties.TryGetValue(propertyName, out x))
		return x;

	 var propertyData = _typeData.FindProperty(propertyName, Target);
	 if (propertyData != null)
	 {
		var descriptor = new PropertyInfoDescriptor(Engine, propertyData, Target);
		Properties.Add(propertyName, descriptor);
		return descriptor;
	 }

	 var methodDatas = _typeData.FindMethod(propertyName, Target);
	 if (methodDatas != null)
	 {
		var descriptor = new PropertyDescriptor(new MethodInfoFunctionInstance(Engine, methodDatas), false, true, false);
		Properties.Add(propertyName, descriptor);
		return descriptor;
	 }

	 var indexProperties = _typeData.IndexProperties;
	 if (indexProperties.Count > 0)
	 {
		return new IndexDescriptor(Engine, _typeData, propertyName, Target);
	 }

	 return PropertyDescriptor.Undefined;
	}

	private bool EqualsIgnoreCasing(string s1, string s2)
	{
	 bool equals = false;
	 if (s1.Length == s2.Length)
	 {
		if (s1.Length > 0 && s2.Length > 0)
		{
		 equals = (s1.ToLower()[0] == s2.ToLower()[0]);
		}
		if (s1.Length > 1 && s2.Length > 1)
		{
		 equals = equals && (s1.Substring(1) == s2.Substring(1));
		}
	 }
	 return equals;
	}
 }
}
