using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Jint.Native;
using Jint.Runtime.Interop.Metadata;

namespace Jint.Runtime.Descriptors.Specialized
{
 public sealed class IndexDescriptor : PropertyDescriptor
 {
	private readonly Engine _engine;
	private readonly object _key;
	private readonly object _item;
	private readonly PropertyData _indexer;
	private readonly MethodData _containsKey;

	public IndexDescriptor(Engine engine, TypeData typeData, string key, object item)
	{
	 _engine = engine;
	 _item = item;

	 var isInt = int.TryParse(key, out _);

	 // try to find first indexer having either public getter or setter with matching argument type
	 foreach (var indexer in typeData.IndexProperties)
	 {
		var paramType = indexer.ParameterType;

		if (_engine.ClrTypeConverter.TryConvert(key, paramType, CultureInfo.InvariantCulture, out _key))
		{
		 _indexer = indexer;
		 // get contains key method to avoid index exception being thrown in dictionaries
		 _containsKey = typeData.FindMethod("ContainsKey", null)?.FirstOrDefault(m =>
		 {
			var parameters = m.Info.GetParameters();
			return (parameters.Length == 1 && parameters[0].ParameterType == paramType);
		 });

		 if (!isInt || paramType == typeof(int))
			break;
		}
	 }

	 // throw if no indexer found
	 if (_indexer == null)
	 {
		throw new InvalidOperationException("No matching indexer found.");
	 }

	 Writable = true;
	}

	public override JsValue Value
	{
	 get
	 {
		object[] parameters = { _key };

		if (_containsKey != null)
		{
		 if ((_containsKey.Execute(_item, parameters) as bool?) != true)
		 {
			return JsValue.Undefined;
		 }
		}

		try
		{
		 return JsValue.FromObject(_engine, _indexer.ExecuteGet(_item, parameters));
		}
		catch
		{
		 return JsValue.Undefined;
		}
	 }

	 set
	 {
		object[] parameters = { _key, value != null ? value.ToObject() : null };
		_indexer.ExecuteSet(_item, parameters);
	 }
	}
 }
}