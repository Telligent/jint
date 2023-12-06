using System;
using System.Globalization;
using System.Reflection;
using Jint.Native;
using Jint.Runtime.Interop.Metadata;

namespace Jint.Runtime.Descriptors.Specialized
{
    public sealed class PropertyInfoDescriptor : PropertyDescriptor
 {
	private readonly Engine _engine;
	private readonly PropertyData _propertyData;
	private readonly object _item;

	public PropertyInfoDescriptor(Engine engine, PropertyData propertyData, object item)
	{
	 _engine = engine;
	 _propertyData = propertyData;
	 _item = item;

	 if (propertyData.Info is PropertyInfo propertyInfo)
		Writable = propertyInfo.CanWrite;
	 else
		Writable = true;
	}

	public override JsValue Value
	{
	 get
	 {
		return JsValue.FromObject(_engine, _propertyData.ExecuteGet(_item, null));
	 }

	 set
	 {
		if (!_propertyData.CanWrite)
		 return;

		var currentValue = value;
		object obj;
		if (_propertyData.PropertyType == typeof(JsValue))
		{
		 obj = currentValue;
		}
		else
		{
		 // attempt to convert the JsValue to the target type
		 obj = currentValue.ToObject();
		 if (obj != null && obj.GetType() != _propertyData.PropertyType)
		 {
			obj = _engine.ClrTypeConverter.Convert(obj, _propertyData.PropertyType, CultureInfo.InvariantCulture);
		 }
		}

		_propertyData.ExecuteSet(_item, [obj]);
	 }
	}
 }
}
