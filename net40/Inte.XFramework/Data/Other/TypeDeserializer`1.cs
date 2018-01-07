
using System;
using System.Linq;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

using Inte.XFramework.Reflection.Emit;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 类型序列化器
    /// </summary>
    public class TypeDeserializer<T>
    {
        static MethodInfo _isDBNull = typeof(IDataRecord).GetMethod("IsDBNull", new Type[] { typeof(int) });
        static MethodInfo _getFieldType = typeof(IDataRecord).GetMethod("GetFieldType", new Type[] { typeof(int) });
        static MethodInfo _getBoolean = typeof(IDataRecord).GetMethod("GetBoolean", new Type[] { typeof(int) });
        static MethodInfo _getByte = typeof(IDataRecord).GetMethod("GetByte", new Type[] { typeof(int) });
        static MethodInfo _getChar = typeof(IDataRecord).GetMethod("GetChar", new Type[] { typeof(int) });
        static MethodInfo _getDateTime = typeof(IDataRecord).GetMethod("GetDateTime", new Type[] { typeof(int) });
        static MethodInfo _getDecimal = typeof(IDataRecord).GetMethod("GetDecimal", new Type[] { typeof(int) });
        static MethodInfo _getDouble = typeof(IDataRecord).GetMethod("GetDouble", new Type[] { typeof(int) });
        static MethodInfo _getFloat = typeof(IDataRecord).GetMethod("GetFloat", new Type[] { typeof(int) });
        static MethodInfo _getGuid = typeof(IDataRecord).GetMethod("GetGuid", new Type[] { typeof(int) });
        static MethodInfo _getInt16 = typeof(IDataRecord).GetMethod("GetInt16", new Type[] { typeof(int) });
        static MethodInfo _getInt32 = typeof(IDataRecord).GetMethod("GetInt32", new Type[] { typeof(int) });
        static MethodInfo _getInt64 = typeof(IDataRecord).GetMethod("GetInt64", new Type[] { typeof(int) });
        static MethodInfo _getString = typeof(IDataRecord).GetMethod("GetString", new Type[] { typeof(int) });
        static MethodInfo _getValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });

        private IDataRecord _reader = null;
        private CommandDefine_Select _define = null;
        private IDictionary<string, Func<IDataRecord, object>> _funcCache = null;
        private Func<IDataRecord, object> _topLevel = null;

        public TypeDeserializer(IDataRecord reader, CommandDefine_Select define)
        {
            _define = define;
            _reader = reader;
            _funcCache = new Dictionary<string, Func<IDataRecord, object>>(8);
        }

        /// <summary>
        /// 将 <see cref="IDataRecord"/> 上的当前行反序列化为实体
        /// </summary>
        /// <returns></returns>
        public T Deserialize()
        {
            #region 基元类型

            if (Reflection.TypeUtils.IsPrimitive(typeof(T)))
            {
                if (_reader.IsDBNull(0)) return default(T);

                var obj = _reader.GetValue(0);
                if (obj.GetType() != typeof(T))
                {
                    obj = Convert.ChangeType(obj, typeof(T));
                }

                return (T)obj;
            }

            #endregion

            #region 匿名类型

            TypeRuntimeInfo runtime = TypeRuntimeInfoCache.GetRuntimeInfo<T>();
            Inte.XFramework.Reflection.Emit.ConstructorInvoker ctor = runtime.ConstructInvoker;
            if (runtime.IsAnonymousType)
            {
                object[] values = new object[_reader.FieldCount];
                _reader.GetValues(values);
                for (int index = 0; index < values.Length; ++index)
                {
                    if (values[index] is DBNull) values[index] = null;
                }
                return (T)ctor.Invoke(values);
            }

            #endregion

            #region 实体类型

            T model = default(T);
            if (_define == null || (_define.NavDescriptors != null && _define.NavDescriptors.Count == 0))
            {
                // 直接跑SQL,则不解析导航属性
                if (_topLevel == null) _topLevel = GetDeserializer(typeof(T), _reader);
                model = (T)_topLevel(_reader);
            }
            else
            {
                // 第一层
                if (_topLevel == null) _topLevel = GetDeserializer(typeof(T), _reader, _define.Columns, 0, _define.NavDescriptors.MinIndex);
                model = (T)_topLevel(_reader);

                // 递归导航属性
                this.Deserialize_Navigation(model, string.Empty);
            }

            #endregion


            return model;
        }

        // 导航属性
        private void Deserialize_Navigation(object model, string typeName)
        {
            // CRM_SaleOrder.Client
            Type pType = model.GetType();
            TypeRuntimeInfo runtime = TypeRuntimeInfoCache.GetRuntimeInfo(pType);
            if (string.IsNullOrEmpty(typeName)) typeName = pType.Name;

            foreach (var kvp in _define.NavDescriptors)
            {
                var descriptor = kvp.Value;
                if (descriptor.Count == 0) continue;

                string keyName = typeName + "." + descriptor.Name;
                if (keyName != kvp.Key) continue;

                var navWrapper = runtime.GetWrapper(descriptor.Name);
                if (navWrapper == null) continue;

                Type navType = navWrapper.DataType;
                Func<IDataRecord, object> func = null;
                if (!_funcCache.TryGetValue(keyName, out func))
                {
                    func = GetDeserializer(navType, _reader, _define.Columns, descriptor.Start, descriptor.Start + descriptor.Count);
                    _funcCache[keyName] = func;
                }

                object navModel = func(_reader);
                navWrapper.Set(model, navModel);

                TypeRuntimeInfo navRuntime = TypeRuntimeInfoCache.GetRuntimeInfo(navType);
                if (navRuntime.NavWrappers.Count > 0) Deserialize_Navigation(navModel, keyName);
            }
        }

        private static Func<IDataRecord, object> GetDeserializer(Type modelType, IDataRecord reader, IDictionary<string, Column> columns = null, int start = 0, int? end = null)
        {
            string methodName = Guid.NewGuid().ToString();
            DynamicMethod dm = new DynamicMethod(methodName, typeof(object), new[] { typeof(IDataRecord) }, true);
            ILGenerator g = dm.GetILGenerator();
            TypeRuntimeInfo runtime = TypeRuntimeInfoCache.GetRuntimeInfo(modelType);

            var model = g.DeclareLocal(modelType);
            g.Emit(OpCodes.Newobj, runtime.ConstructInvoker.Constructor);
            g.Emit(OpCodes.Stloc, model);

            if (end == null) end = reader.FieldCount;
            for (int index = start; index < end; index++)
            {
                string keyName = reader.GetName(index);
                if (columns != null)
                {
                    Column column = null;
                    columns.TryGetValue(keyName, out column);
                    keyName = column != null ? column.Name : string.Empty;
                }

                var wrapper = runtime.GetWrapper(keyName);// as MemberAccessWrapper;
                if (wrapper == null) continue;

                var isDBNullLabel = g.DefineLabel();
                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Ldc_I4, index);
                g.Emit(OpCodes.Callvirt, _isDBNull);
                g.Emit(OpCodes.Brtrue, isDBNullLabel);

                var m_method = GetReaderMethod(wrapper.DataType);
                g.Emit(OpCodes.Ldloc, model);
                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Ldc_I4, index);
                g.Emit(OpCodes.Callvirt, m_method);

                Type dataType = wrapper.DataType;
                Type nullUnderlyingType = dataType.IsGenericType ? Nullable.GetUnderlyingType(dataType) : null;
                Type unboxType = nullUnderlyingType != null ? nullUnderlyingType : dataType;

                if (unboxType == typeof(byte[]) || (m_method == _getValue && dataType != typeof(object)))
                {
                    g.Emit(OpCodes.Castclass, dataType);
                }
                if (nullUnderlyingType != null) g.Emit(OpCodes.Newobj, dataType.GetConstructor(new[] { nullUnderlyingType }));

                if (wrapper.Member.MemberType == MemberTypes.Field)
                {
                    g.Emit(OpCodes.Stfld, wrapper.FieldInfo);
                }
                else
                {
                    g.Emit(OpCodes.Callvirt, wrapper.SetMethod);
                }

                g.MarkLabel(isDBNullLabel);
            }


            g.Emit(OpCodes.Ldloc, model);
            g.Emit(OpCodes.Ret);

            var func = (Func<IDataRecord, object>)dm.CreateDelegate(typeof(Func<IDataRecord, object>));
            return func;
        }

        private static MethodInfo GetReaderMethod(Type fieldType)
        {
            if (fieldType == typeof(char)) return _getChar;
            if (fieldType == typeof(string)) return _getString;
            if (fieldType == typeof(bool) || fieldType == typeof(bool?)) return _getBoolean;
            if (fieldType == typeof(byte) || fieldType == typeof(byte?)) return _getByte;
            if (fieldType == typeof(DateTime) || fieldType == typeof(DateTime?)) return _getDateTime;
            if (fieldType == typeof(decimal) || fieldType == typeof(decimal?)) return _getDecimal;
            if (fieldType == typeof(double) || fieldType == typeof(double?)) return _getDouble;
            if (fieldType == typeof(float) || fieldType == typeof(float?)) return _getFloat;
            if (fieldType == typeof(Guid) || fieldType == typeof(Guid?)) return _getGuid;
            if (fieldType == typeof(short) || fieldType == typeof(short?)) return _getInt16;
            if (fieldType == typeof(int) || fieldType == typeof(int?)) return _getInt32;
            if (fieldType == typeof(long) || fieldType == typeof(long?)) return _getInt64;

            return _getValue;

            //bit	Boolean
            //tinyint	Byte
            //smallint	Int16
            //int	Int32
            //bigint	Int64
            //smallmoney	Decimal
            //money	Decimal
            //numeric	Decimal
            //decimal	Decimal
            //float	Double
            //real	Single
            //smalldatetime	DateTime
            //datetime	DateTime
            //timestamp	DateTime
            //char	String
            //text	String
            //varchar	String
            //nchar	String
            //ntext	String
            //nvarchar	String
            //binary	Byte[]
            //varbinary	Byte[]
            //image	Byte[]
            //uniqueidentifier	Guid
            //Variant	Object
        }
    }
}
