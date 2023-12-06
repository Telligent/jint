using Jint.Runtime.Descriptors.Specialized;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Jint.Runtime.Interop.Metadata
{
 public class TypeData
 {
	private static readonly Type _objectType = typeof(object);
	private static readonly Type dynamicType = typeof(System.Dynamic.DynamicObject);
	private static readonly Type expandoObjectType = typeof(System.Dynamic.ExpandoObject);

	private static ConcurrentDictionary<Type, TypeData> _typeDataByType = new();

	public static TypeData Get(Type type)
	{
	 return _typeDataByType.GetOrAdd(type, (_) =>
	 {
		if (dynamicType.IsAssignableFrom(type) || expandoObjectType.IsAssignableFrom(type))
		 return new DynamicTypeData(type);
		else
		 return new TypeData(type);
	 });
	}

	private readonly Dictionary<string, List<MethodData>> _methodCache = new Dictionary<string, List<MethodData>>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, PropertyData> _propertyCache = new Dictionary<string, PropertyData>(StringComparer.OrdinalIgnoreCase);
	private Type _type;
	private readonly List<PropertyData> _indexProperties = new List<PropertyData>();
	private readonly List<MethodData> _constructorMethods = new List<MethodData>();

	public TypeData(Type type)
	{
	 _type = type;

	 var interfaces = _type.GetInterfaces();
	 PopulateConstructorCache();
	 PopulateMethodCache(interfaces);
	 PopulatePropertyCache(interfaces);
	}

	internal Type Type
	{
	 get { return _type; }
	}

	public List<PropertyData> IndexProperties
	{
	 get
	 {
		return _indexProperties;
	 }
	}

	public List<MethodData> ConstructorMethods
	{
	 get
	 {
		return _constructorMethods;
	 }
	}

	public List<MethodData> FindMethod(string name)
	{
	 if (_methodCache.TryGetValue(name, out List<MethodData> methods))
		return methods;

	 return null;
	}

	public PropertyData FindProperty(string name)
	{
	 if (_propertyCache.TryGetValue(name, out PropertyData cacheEntry))
		return cacheEntry;

	 return null;
	}

	private void PopulateConstructorCache()
	{
	 foreach (var constructor in _type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
		_constructorMethods.Add(new MethodData(constructor));
	}

	private void PopulateMethodCache(Type[] interfaces)
	{
	 List<MethodData> methods;

	 foreach (var i in interfaces)
		foreach (var method in i.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public))
		{
		 if (!_methodCache.TryGetValue(method.Name, out methods))
		 {
			methods = new List<MethodData>();
			_methodCache[method.Name] = methods;
		 }
		 methods.Add(new MethodData(method, parametersAreExactType: true));
		}

	 foreach (MethodInfo method in _type.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public))
	 {
		if (!_methodCache.TryGetValue(method.Name, out methods))
		{
		 methods = new List<MethodData>();
		 _methodCache[method.Name] = methods;
		}
		methods.Add(new MethodData(method, parametersAreExactType: true));
	 }
	}

	private void PopulatePropertyCache(Type[] interfaces)
	{
	 foreach (var field in _type.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public))
		_propertyCache[field.Name] = new PropertyData(field);

	 foreach (var i in interfaces)
		foreach (var property in i.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public))
		{
		 var parameters = property.GetIndexParameters();
		 if (parameters.Length == 0)
			_propertyCache[property.Name] = new PropertyData(property);
		 else if (parameters.Length == 1)
			_indexProperties.Add(new PropertyData(property, parameterType: parameters[0].ParameterType));
		}

	 PropertyInfo[] properties = _type.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
	 foreach (PropertyInfo property in properties)
	 {
		var parameters = property.GetIndexParameters();
		if (parameters.Length == 0)
		 _propertyCache[property.Name] = new PropertyData(property);
		else if (parameters.Length == 1)
		 _indexProperties.Insert(0, new PropertyData(property, parameterType: parameters[0].ParameterType));
	 }
	}
 }
}
