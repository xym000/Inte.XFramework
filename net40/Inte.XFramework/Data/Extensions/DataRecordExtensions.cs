

using System;
using System.Linq;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using Inte.XFramework.Reflection.Emit;

namespace Inte.XFramework.Data
{
    [Obsolete("Use TypeDeserializer to Load model from DataReader")]
    static class DataRecordExtensions
    {
        /// <summary>
        /// 将 <see cref="IDataRecord"/> 映射为实体 
        /// </summary>
        /// <typeparam reader="T">数据源</typeparam>
        /// <param name="reader">数据源</param>
        /// <param name="define">命令定义</param>
        /// <returns></returns>
        public static T ToModel<T>(this IDataRecord reader, CommandDefine define = null)
        {
            object obj = null;
            object[] values = null;
            int index = 0;

            // 基元类初始化 ########################
            if (Reflection.TypeUtils.IsPrimitive(typeof(T)))
            {
                //if(reader.IsDBNull(0)) return default(T)
                //obj = reader.GetValue(0);
                //return obj is DBNull ? default(T) : (T)obj;
                return reader.IsDBNull(0) ? default(T) : (T)reader.GetValue(0);
            }

            // 匿名类初始化 ########################
            TypeRuntimeInfo runtime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            Inte.XFramework.Reflection.Emit.ConstructorInvoker ctor = runtime.ConstructInvoker;
            if (runtime.IsAnonymousType)
            {
                values = new object[reader.FieldCount];
                reader.GetValues(values);
                for (index = 0; index < values.Length; ++index)
                {
                    if (values[index] is DBNull) values[index] = null;
                }
                return (T)ctor.Invoke(values);
            }

            // 实体类初始化 ########################
            T model = (T)ctor.Invoke();
            values = new object[reader.FieldCount];
            reader.GetValues(values);

            // 计算导航属性所占用的索引，这些索引对应的值不会赋给 T 实体
            var sc = define as CommandDefine_Select;
            if (sc == null || (sc.NavDescriptors != null && sc.NavDescriptors.Count == 0))
            {
                // 直接跑SQL,则不解析导航属性
                for (int i = 0; i < reader.FieldCount; ++i)
                {
                    obj = values[i];
                    if (obj == DBNull.Value) continue;

                    string name = reader.GetName(i);
                    var wrapper = runtime.GetWrapper(name) as MemberAccessWrapper;
                    if (wrapper != null) SetProperty(model, wrapper, obj);
                }
            }
            else
            {
                // 使用表达式查询，解析导航属性
                bool nav = sc.NavDescriptors.Any(x => x.Value.Count > 0);
                int min = nav ? sc.NavDescriptors.Min(x => x.Value.Start) : 0;

                // 第一层
                index = -1;
                foreach (var kvp in sc.Columns)
                {
                    index += 1;
                    obj = values[index];

                    if (obj == DBNull.Value) continue;
                    if (nav && index >= min) break;

                    var wrapper = runtime.GetWrapper(kvp.Value.Name) as MemberAccessWrapper;
                    if (wrapper != null) SetProperty(model, wrapper, obj);
                }

                // 递归导航属性
                if (runtime.NavWrappers.Count > 0) ToModel_Navigation(model, values, sc, string.Empty);
            }

            return model;
        }

        // 导航属性
        private static void ToModel_Navigation(object model, object[] values, CommandDefine_Select define, string typeName)
        {
            // CRM_SaleOrder.Client
            Type pType = model.GetType();
            TypeRuntimeInfo runtime = TypeRuntimeInfoCache.GetRuntimeInfo(pType);
            if (string.IsNullOrEmpty(typeName)) typeName = pType.Name;

            foreach (var kvp in runtime.NavWrappers)
            {
                string keyName = typeName + "." + kvp.Key;
                ColumnNavDescriptor descriptor = null;
                define.NavDescriptors.TryGetValue(keyName, out descriptor);
                if (descriptor == null) continue;

                // 实例化这个导航属性并且给它赋值
                var navWrapper = kvp.Value;
                Type navType = navWrapper.DataType;
                TypeRuntimeInfo navRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navType);
                object navModel = navRuntime.ConstructInvoker.Invoke();

                if (descriptor.Count > 0)
                {
                    int index = -1;
                    foreach (var c_kvp in define.Columns)
                    {
                        index += 1;
                        if (index < descriptor.Start) continue;
                        if (index > (descriptor.Start + descriptor.Count)) break;

                        object obj = values[index];
                        if (obj == DBNull.Value) continue;

                        var wrapper = navRuntime.GetWrapper(c_kvp.Value.Name) as MemberAccessWrapper;
                        if (wrapper != null) SetProperty(navModel, wrapper, obj);
                    }
                }

                SetProperty(model, navWrapper, navModel);

                if (navRuntime.NavWrappers.Count > 0) ToModel_Navigation(navModel, values, define, keyName);
            }
        }

        // 赋值属性
        private static void SetProperty(object model, MemberAccessWrapper wrapper, object value)
        {
            try
            {
                Type memberType = wrapper.DataType;
                if ((memberType == typeof(bool) || memberType == typeof(bool?)) && (value.GetType() == typeof(short) || value.GetType() == typeof(int)))
                {
                    value = value.GetType() == typeof(short) ? (short)value > 0 : (int)value > 0;
                }
                else if (memberType == typeof(Guid))
                {
                    byte[] b = value as byte[];
                    if (b != null) value = new Guid(b);
                }
                //else if (memberType != obj.GetType())
                //{
                //    obj = Convert.ChangeType(obj, memberType);
                //}

                wrapper.Set(model, value);
            }
            catch (Exception ex)
            {
                throw new XfwException("invoke {0}={1} fail.", ex, wrapper.FullName, value);
            }
        }
        
    }
}
