using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Descriptors.Specialized;
using Jint.Runtime.Interop.Metadata;

namespace Jint.Runtime.Interop
{
 public class TypeReference : FunctionInstance, IConstructor, IObjectWrapper
 {
	private TypeReference(Engine engine)
			: base(engine, null, null, false)
	{
	}

	public TypeData Type { get; set; }

	public static TypeReference CreateTypeReference(Engine engine, Type type)
	{
	 var obj = new TypeReference(engine);
	 obj.Extensible = false;
	 obj.Type = TypeData.Get(type);

	 // The value of the [[Prototype]] internal property of the TypeReference constructor is the Function prototype object
	 obj.Prototype = engine.Function.PrototypeObject;

	 obj.FastAddProperty("length", 0, false, false, false);

	 // The initial value of Boolean.prototype is the Boolean prototype object
	 obj.FastAddProperty("prototype", engine.Object.PrototypeObject, false, false, false);

	 return obj;
	}

	public override JsValue Call(JsValue thisObject, JsValue[] arguments)
	{
	 // direct calls on a TypeReference constructor object is equivalent to the new operator
	 return Construct(arguments);
	}

	public ObjectInstance Construct(JsValue[] arguments)
	{
	 if (arguments.Length == 0 && Type.Type.IsValueType())
	 {
		var instance = Activator.CreateInstance(Type.Type);
		var result = TypeConverter.ToObject(Engine, JsValue.FromObject(Engine, instance));

		return result;
	 }

	 var methods = TypeConverter.FindBestMatch(Engine, Type.ConstructorMethods, arguments).ToList();

	 foreach (var method in methods)
	 {
		var parameters = new object[arguments.Length];
		try
		{
		 for (var i = 0; i < arguments.Length; i++)
		 {
			var parameterType = method.ParameterTypes[i];

			if (parameterType == typeof(JsValue))
			{
			 parameters[i] = arguments[i];
			}
			else
			{
			 parameters[i] = Engine.ClrTypeConverter.Convert(
					 arguments[i].ToObject(),
					 parameterType,
					 CultureInfo.InvariantCulture);
			}
		 }

		 var instance = method.Execute(null, parameters.ToArray());
		 var result = TypeConverter.ToObject(Engine, JsValue.FromObject(Engine, instance));

		 // todo: cache method info

		 return result;
		}
		catch
		{
		 // ignore method
		}
	 }

	 throw new JavaScriptException(Engine.TypeError, "No public methods with the specified arguments were found.");

	}

	public override bool HasInstance(JsValue v)
	{
	 ObjectWrapper wrapper = v.As<ObjectWrapper>();

	 if (wrapper == null)
	 {
		return base.HasInstance(v);
	 }

	 return wrapper.Target.GetType() == this.Type.Type;
	}

	public override bool DefineOwnProperty(string propertyName, PropertyDescriptor desc, bool throwOnError)
	{
	 if (throwOnError)
	 {
		throw new JavaScriptException(Engine.TypeError, "Can't define a property of a TypeReference");
	 }

	 return false;
	}

	public override bool Delete(string propertyName, bool throwOnError)
	{
	 if (throwOnError)
	 {
		throw new JavaScriptException(Engine.TypeError, "Can't delete a property of a TypeReference");
	 }

	 return false;
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
	 // todo: cache members locally

	 if (Type.Type.IsEnum())
	 {
		Array enumValues = Enum.GetValues(Type.Type);
		Array enumNames = Enum.GetNames(Type.Type);

		for (int i = 0; i < enumValues.Length; i++)
		{
		 if (enumNames.GetValue(i) as string == propertyName)
		 {
			return new PropertyDescriptor((int)enumValues.GetValue(i), false, false, false);
		 }
		}
		return PropertyDescriptor.Undefined;
	 }

	 var propertyData = Type.FindProperty(propertyName);
	 if (propertyData != null)
		return new PropertyInfoDescriptor(Engine, propertyData, Target);

	 var methodDatas = Type.FindMethod(propertyName);
	 if (methodDatas != null)
		return new PropertyDescriptor(new MethodInfoFunctionInstance(Engine, methodDatas), false, true, false);

	return PropertyDescriptor.Undefined;
	}

	public object Target
	{
	 get
	 {
		return Type;
	 }
	}

	public override string Class
	{
	 get { return "TypeReference"; }
	}
 }
}
