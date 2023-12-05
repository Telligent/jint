using System;
using System.Globalization;
using System.Reflection;
using Jint.Native;

namespace Jint.Runtime.Descriptors.Specialized
{
 public sealed class PropertyInfoDescriptor : PropertyDescriptor
 {
	private readonly Engine _engine;
	private readonly PropertyInfo _propertyInfo;
	private readonly object _item;
	private Func<object, object[], object> _getter;
	private Func<object, object[], object> _setter;

	public PropertyInfoDescriptor(Engine engine, PropertyInfo propertyInfo, object item)
	{
	 _engine = engine;
	 _propertyInfo = propertyInfo;
	 _item = item;

	 Writable = propertyInfo.CanWrite;
	}

	public override JsValue Value
	{
	 get
	 {
		var getter = _getter;
		if (getter == null)
		 _getter = getter = Jint.Runtime.Interop.Invoker.GetFunc(_propertyInfo);

		return JsValue.FromObject(_engine, getter(_item, null));
	 }

	 set
	 {
		var currentValue = value;
		object obj;
		if (_propertyInfo.PropertyType == typeof(JsValue))
		{
		 obj = currentValue;
		}
		else
		{
		 // attempt to convert the JsValue to the target type
		 obj = currentValue.ToObject();
		 if (obj != null && obj.GetType() != _propertyInfo.PropertyType)
		 {
			obj = _engine.ClrTypeConverter.Convert(obj, _propertyInfo.PropertyType, CultureInfo.InvariantCulture);
		 }
		}

		var setter = _setter;
		if (setter == null)
		 _setter = setter = Jint.Runtime.Interop.Invoker.SetFunc(_propertyInfo);

		setter(_item, [obj]);
	 }
	}
 }
}
