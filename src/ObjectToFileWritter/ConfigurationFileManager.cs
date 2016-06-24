using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace NetObjectToFileWritter {

    internal static class TypeExtensions {
        internal static bool IsDictionary(this Type type) {
            return type.IsGenericType &&  type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
        }

        internal static bool IsList(this Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        internal static bool IsNullable(this Type type) {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        internal static bool IsSimpleType(this Type type) {
            if (type.IsNullable())
                return type.GetGenericArguments()[0].IsSimpleType();
            
            return type.IsPrimitive || type.IsEnum || type.Equals(typeof(string)) || type.Equals(typeof(decimal));
        }
    }

    //starting to think that a visitor would make more sense (if we would prefer json in the future it would be easish to extend with visitor)
    /// <summary>
    /// Manages writting files to a file 
    /// </summary>
    /// <typeparam name="T">Type of the object (with constructor with arity 0) with that will store the settings</typeparam>
    public class ConfigurationFileManager<T> where T : class, new() {
        internal static IEnumerable<XElement> GetElements(string filePath) {
            return XElement.Parse(File.ReadAllText(filePath)).Elements();
        }

        /// <summary>
        /// Writes in output the result of reading the xml file at filePath. Throws exception if a error occurs
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="output"></param>
        public static void Load(string filePath, out IEnumerable<T> output) {
            output = GetElements(filePath).Select(v => FromXml(v));
        }

        /// <summary>
        /// Writes in output the result of reading the xml file at filePath. Throws exception if a error occurs
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="keySelector">Function that receives a XElement and returns the key indetifier for the dictionary</param>
        /// <param name="output"></param>
        public static void Load(string filePath, Func<XElement, string> keySelector, out Dictionary<string, T> output) {
            output = GetElements(filePath).ToDictionary(k => keySelector(k), v => FromXml(v));
        }

        /// <summary>
        /// Writes in output the result of reading the xml file at filePath. Throws exception if a error occurs
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="output"></param>
        public static void Load(string filePath, out T output) {
            IEnumerable<T> result;
            Load(filePath, out result);
            output = result?.First();
        }

        private static T FromXml(XElement element) {
            return FromXmlAux<T>(element);
        }

        private static object XElementToList(XElement el, Type collectionType) {
            //preparing add method to the list
            Type fieldEnumerableType = typeof(List<>).MakeGenericType(collectionType);
            object result = Activator.CreateInstance(fieldEnumerableType);
            MethodInfo addMethod = fieldEnumerableType.GetMethod("Add");
            
            //recursive call due to potential nested object
            Func<XElement, object> process = null;
            if (collectionType.IsSimpleType())
                process = (XElement nestedElement) => XElementToSimpleType(nestedElement, collectionType);
            else
                process = (XElement nestedElement) => XElementToComplexType(nestedElement, collectionType);
       
            foreach (var nestedEl in el.Elements()) {
                object nestedObject = process(nestedEl);

                //append to list
                addMethod.Invoke(result, new object[] { nestedObject });
            }

            return result;
        }

        private static object XElementToComplexType(XElement el, Type type) {
            if (type.IsList()) {
                return XElementToList(el, type.GetGenericArguments()[0]);
            }else if (type.IsDictionary()) {
                return XElementToDictionary(el, type.GetGenericArguments()[0], type.GetGenericArguments()[1]);
            }

            //prepare generic recursive call (with generics)
            //Improve: cache this recursiveCallMethod
            MethodInfo recursiveCallMethod = typeof(ConfigurationFileManager<T>).GetMethod("FromXmlAux", BindingFlags.NonPublic | BindingFlags.Static);
            recursiveCallMethod = recursiveCallMethod.MakeGenericMethod(type);
            return recursiveCallMethod.Invoke(null, new object[] { el });
        }

        private static object XElementToSimpleType(XElement el, Type type) {
            if (type.IsEnum)
                return Enum.Parse(type, el.Value.ToString());
            else
                return Convert.ChangeType(el.Value.ToString(), type, CultureInfo.InvariantCulture);
        }

        private static void DictionaryToXElement(XElement root, IDictionary dic) {
            int count = 0;
            foreach (var dicKey in dic.Keys) {
                object dicValue = dic[dicKey];

                var keyElement = ToXml("Key", dicKey);
                var valueElement = ToXml("Value", dicValue);
                var dicElement = new XElement("Element" + count++);
                dicElement.Add(keyElement);
                dicElement.Add(valueElement);
                root.Add(dicElement);
            }
        }

        private static void EnumerableToXElement(XElement root, IEnumerable list) {
            int count = 0;
            foreach (var item in list)
                root.Add(ToXml("Element" + count++, item));
        }

        private static void CustomObjectToXElement(XElement root, object obj) {
            // ignore properties with only getters
            foreach (var p in obj.GetType().GetProperties().Where(p => p.CanWrite)) {
                root.Add(ToXml(p.Name, p.GetValue(obj)));
            }
        }

        private static object XElementToDictionary(XElement el, Type keyType, Type valueType) {            
            Type fieldDictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            object result = Activator.CreateInstance(fieldDictionaryType);
            MethodInfo addMethod = fieldDictionaryType.GetMethod("Add");

            Func<XElement, object> processKey = null;
            if (keyType.IsSimpleType())
                processKey = (XElement nestedElement) => XElementToSimpleType(nestedElement, keyType);            
            else
                processKey = (XElement nestedElement) => XElementToComplexType(nestedElement, keyType);            

            Func<XElement, object> processValue = null;
            if (valueType.IsSimpleType())
                processValue = (XElement nestedElement) => XElementToSimpleType(nestedElement, valueType);            
            else
                processValue = (XElement nestedElement) => XElementToComplexType(nestedElement, valueType);            

            foreach (var dicElement in el.Elements()) {
                var keyElement = dicElement.Element("Key");
                var valueElement = dicElement.Element("Value");

                object key = processKey(keyElement);
                object value = processValue(valueElement);

                addMethod.Invoke(result, new object[] { key, value });
            }
            return result;
        }

        private static U FromXmlAux<U>(XElement element) where U : class, new() {
            U obj = new U();
            Type typeRoot = obj.GetType();

            foreach (XElement el in element.Elements()) {
                PropertyInfo prop = typeRoot.GetProperty(el.Name.ToString());

                //skip properties that are only getters
                if (!prop.CanWrite) continue;                

                Type propType = prop.PropertyType;

                //check if is nested setting
                object result = null;
                if (propType.IsSimpleType()){
                    result = XElementToSimpleType(el, propType);
                } else {
                    result = XElementToComplexType(el, propType);
                }                

                prop.SetValue(obj, result);
            }

            return obj;
        }


        private static XElement ToXml(string key, object value) {
            var el = new XElement(key);
            if (value != null) {
                Type typeOfValue = value.GetType();
                if (typeOfValue.IsSimpleType()) {
                    el.Add(value.ToString());
                }
                else if (typeOfValue.IsList()) {
                    EnumerableToXElement(el, (IEnumerable)value);
                }
                else if (typeOfValue.IsDictionary()) {
                    DictionaryToXElement(el, (IDictionary)value);
                }
                else {
                    CustomObjectToXElement(el, value);
                }
            }
            return el;
        }
        /// <summary>
        /// Saves the IEnumerable objects to a xml file in the given outputFilePath
        /// </summary>
        /// <param name="outputFilePath"></param>
        /// <param name="objects"></param>
        public static void Save(string outputFilePath, IEnumerable<T> objects) {
            var root = new XElement("root");

            foreach(T ob in objects) {                
                var el = new XElement(typeof(T).Name);
                foreach (var prop in typeof(T).GetProperties().Where(p => p.CanWrite)) {
                    el.Add(ToXml(prop.Name, prop.GetValue(ob)));
                }
                root.Add(el);
            }

            root.Save(outputFilePath, SaveOptions.OmitDuplicateNamespaces);
        }
        
        /// <summary>
        /// Saves the obj to the given outputFilePath
        /// </summary>
        /// <param name="outputFilePath"></param>
        /// <param name="obj"></param>
        public static void Save(string outputFilePath, T obj) {
            Save(outputFilePath, new[] { obj });
        }
    }    
}