using System;
using System.Linq;
using System.Collections.Generic;
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
 public sealed class ObjectListWrapper : Jint.Native.Array.ArrayInstance, IObjectWrapper
 {
	private IList<object> _genericList;
	private System.Collections.IList _list;
	private TypeData _typeData;
	private int _length;
	private bool _fullyConverted = false;

	private Engine _engine;

	public Object Target { get; set; }

	public ObjectListWrapper(Engine engine, Object obj)
			: base(engine)
	{
	 Target = obj;
	 Prototype = engine.Array.PrototypeObject;
	 Extensible = false;
	 _typeData = TypeData.Get(obj.GetType());

	 FastAddProperty("prototype", Prototype, false, false, false);

	 _engine = engine;

	 var genericEnumerable = obj as IEnumerable<object>;
	 if (genericEnumerable == null)
	 {
		_list = obj as System.Collections.IList;
		if (_list == null)
		{
		 _genericList = new List<object>();
		 _length = 0;
		}
		else
		 _length = _list.Count;
	 }
	 else
	 {
		_genericList = genericEnumerable.ToList();
		_length = _genericList.Count;
	 }

	 FastAddProperty("length", (uint)_length, false, false, false);
	}

	public override string Class => "Array";

	public override PropertyDescriptor GetOwnProperty(string propertyName)
	{
	 PropertyDescriptor x;
	 if (Properties.TryGetValue(propertyName, out x))
		return x;

	 var propertyData = _typeData.FindProperty(propertyName);
	 if (propertyData != null)
	 {
		var descriptor = new PropertyInfoDescriptor(Engine, propertyData, Target);
		Properties.Add(propertyName, descriptor);
		return descriptor;
	 }

	 var methodDatas = _typeData.FindMethod(propertyName);
	 if (methodDatas != null)
	 {
		var descriptor = new PropertyDescriptor(new MethodInfoFunctionInstance(Engine, methodDatas), false, true, false);
		Properties.Add(propertyName, descriptor);
		return descriptor;
	 }

	 if (propertyName == "length")
		return base.GetOwnProperty(propertyName);

	 uint index;
	 if (Jint.Native.Array.ArrayInstance.IsArrayIndex(propertyName, out index))
		return GetIndexDescriptor((int)index);

	 return PropertyDescriptor.Undefined;
	}

	private void EnsureArrayPropertiesPopulated()
	{
	 if (!_fullyConverted)
	 {
		_fullyConverted = true;
		for (var i = 0; i < _genericList.Count; i++)
		 if (!Properties.ContainsKey(i.ToString()))
			GetIndexDescriptor(i);
	 }
	}

	private PropertyDescriptor GetIndexDescriptor(int index)
	{
	 if (_genericList != null)
	 {
		if (index >= 0 && index <= _genericList.Count)
		{
		 var descriptor = new PropertyDescriptor(JsValue.FromObject(_engine, _genericList[(int)index]), false, true, false);
		 Properties.Add(index.ToString(), descriptor);
		 return descriptor;
		}
	 }

	 if (_list != null)
	 {
		if (index >= 0 && index <= _list.Count)
		{
		 var descriptor = new PropertyDescriptor(JsValue.FromObject(_engine, _list[(int)index]), false, true, false);
		 Properties.Add(index.ToString(), descriptor);
		 return descriptor;
		}
	 }

	 return PropertyDescriptor.Undefined;
	}

	public override IEnumerable<KeyValuePair<string, PropertyDescriptor>> GetOwnProperties()
	{
	 EnsureInitialized();
	 EnsureArrayPropertiesPopulated();
	 return Properties;
	}

	public override bool HasOwnProperty(string p)
	{
	 EnsureArrayPropertiesPopulated();
	 return base.HasOwnProperty(p);
	}

	public override void RemoveOwnProperty(string p)
	{
	 EnsureArrayPropertiesPopulated();
	 base.RemoveOwnProperty(p);
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
