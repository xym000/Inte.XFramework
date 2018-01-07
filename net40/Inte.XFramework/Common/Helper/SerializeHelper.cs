
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;
using System.Runtime.Serialization.Json;

namespace Inte.XFramework
{
    /// <summary>
    /// 序列化助手类
    /// </summary>
    public class SerializeHelper
    {
        /// <summary>
        /// 对象序列化成Json字符串
        /// </summary>
        /// <returns></returns>
        public static string SerializeToJson(object obj,string format = null)
        {
            // 还有个JavascriptSerializer
            DataContractJsonSerializer serializer = string.IsNullOrEmpty(format)
                ? new DataContractJsonSerializer(obj.GetType())
                : new DataContractJsonSerializer(obj.GetType());//, new DataContractJsonSerializerSettings { DateTimeFormat = new DateTimeFormat(format) });
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                string strJson = Encoding.UTF8.GetString(ms.ToArray());
                return strJson;
            }
        }

        /// <summary>
        /// Json字符串反序列化成对象
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="strJson">Json字符串</param>
        /// <returns></returns>
        public static T DeserializeFromJson<T>(string strJson)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(strJson)))
            {
                T obj = (T)serializer.ReadObject(ms);
                return obj;
            }
        }

        /// <summary>
        /// 对象序列化成 XML 字符串
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <returns></returns>
        public static string SerializeToXml<T>(T obj) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return SerializeToXml(serializer, obj);
        }

        /// <summary>
        /// 对象序列化成 XML 字符串
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="root">指定根对象的名称</param>
        /// <returns></returns>
        public static string SerializeToXml<T>(T obj, XmlRootAttribute root) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), root);
            return SerializeToXml(serializer, obj);
        }

        /// <summary>
        /// 对象序列化成 XML 字符串
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="root">指定根对象的名称</param>
        /// <param name="defaultNamespace">xml命名空间</param>
        /// <returns></returns>
        public static string SerializeToXml<T>(T obj, XmlRootAttribute root, string defaultNamespace) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), null, new System.Type[] { }, root, defaultNamespace);
            return SerializeToXml(serializer, obj);
        }

        /// <summary>
        /// 对象序列化成 XML 字符串
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="serializer">序列化器</param>
        /// <param name="obj">要序列化的对象</param>
        /// <returns></returns>
        public static string SerializeToXml<T>(XmlSerializer serializer, T obj) where T : class
        {
            using (MemoryStream ms = new MemoryStream())
            {
                serializer.Serialize(ms, obj);
                string xml = Encoding.UTF8.GetString(ms.ToArray());
                return xml;
            }
        }

        /// <summary>
        /// XML 字符串 反序列化成对象
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="xml">xml内容</param>
        /// <param name="root">指定根对象的名称</param>
        /// <returns></returns>
        public static T DeserializeFromXml<T>(string xml) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return DeserializeFromXml<T>(serializer, xml);
        }

        /// <summary>
        /// XML 字符串 反序列化成对象
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="xml">xml内容</param>
        /// <param name="root">指定根对象的名称</param>
        /// <returns></returns>
        public static T DeserializeFromXml<T>(string xml, XmlRootAttribute root) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), root);
            return DeserializeFromXml<T>(serializer, xml);
        }

        /// <summary>
        /// XML 字符串 反序列化成对象
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="xml">xml内容</param>
        /// <param name="root">指定根对象的名称</param>
        /// <param name="defaultNamespace">xml命名空间</param>
        /// <returns></returns>
        public static T DeserializeFromXml<T>(string xml, XmlRootAttribute root, string defaultNamespace) where T : class
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), null, new System.Type[] { }, root, defaultNamespace);
            return DeserializeFromXml<T>(serializer, xml);
        }

        /// <summary>
        /// XML 字符串 反序列化成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializer">序列化器</param>
        /// <param name="xml">xml内容</param>
        /// <returns></returns>
        public static T DeserializeFromXml<T>(XmlSerializer serializer, string xml) where T : class
        {
            using (Stream xmlStream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
            {
                using (XmlReader xmlReader = XmlReader.Create(xmlStream))
                {
                    T obj = serializer.Deserialize(xmlReader) as T;
                    return obj;
                }
            }
        }
    }
}
