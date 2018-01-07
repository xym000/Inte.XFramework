using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inte.XFramework.Reflection
{
    /// <summary>
    /// 运行时类型工具类
    /// </summary>
    public class TypeUtils
    {
        static HashSet<Type> _primitiveTypes = new HashSet<Type>();

        static TypeUtils()
        {
            _primitiveTypes.Add(typeof(string));
            _primitiveTypes.Add(typeof(byte[]));
            _primitiveTypes.Add(typeof(bool));
            _primitiveTypes.Add(typeof(Nullable<bool>));
            _primitiveTypes.Add(typeof(byte));
            _primitiveTypes.Add(typeof(Nullable<byte>));
            _primitiveTypes.Add(typeof(DateTime));
            _primitiveTypes.Add(typeof(Nullable<DateTime>));
            _primitiveTypes.Add(typeof(decimal));
            _primitiveTypes.Add(typeof(Nullable<decimal>));
            _primitiveTypes.Add(typeof(double));
            _primitiveTypes.Add(typeof(Nullable<double>));
            _primitiveTypes.Add(typeof(Guid));
            _primitiveTypes.Add(typeof(Nullable<Guid>));
            _primitiveTypes.Add(typeof(short));
            _primitiveTypes.Add(typeof(Nullable<short>));
            _primitiveTypes.Add(typeof(int));
            _primitiveTypes.Add(typeof(Nullable<int>));
            _primitiveTypes.Add(typeof(long));
            _primitiveTypes.Add(typeof(Nullable<long>));
            _primitiveTypes.Add(typeof(sbyte));
            _primitiveTypes.Add(typeof(Nullable<sbyte>));
            _primitiveTypes.Add(typeof(float));
            _primitiveTypes.Add(typeof(Nullable<float>));
            _primitiveTypes.Add(typeof(ushort));
            _primitiveTypes.Add(typeof(Nullable<ushort>));
            _primitiveTypes.Add(typeof(uint));
            _primitiveTypes.Add(typeof(Nullable<uint>));
            _primitiveTypes.Add(typeof(ulong));
            _primitiveTypes.Add(typeof(Nullable<ulong>));
        }

        /// <summary>
        /// 判断给定类型是否是ORM支持的基元类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsPrimitive(Type type)
        {
            return _primitiveTypes.Contains(type);
        }

        /// <summary>
        /// CRL类型 转 DbType
        /// </summary>
        public static DbType ConvertClrTypeToDbType(Type clrType)
        {
            switch (Type.GetTypeCode(clrType))
            {
                case TypeCode.Empty:
                    throw new ArgumentException(TypeCode.Empty.ToString());

                case TypeCode.Object:
                    if (clrType == typeof(Byte[]))
                    {
                        return DbType.Binary;
                    }
                    if (clrType == typeof(Char[]))
                    {
                        // Always treat char and char[] as string
                        return DbType.String;
                    }
                    else if (clrType == typeof(Guid))
                    {
                        return DbType.Guid;
                    }
                    else if (clrType == typeof(TimeSpan))
                    {
                        return DbType.Time;
                    }
                    else if (clrType == typeof(DateTimeOffset))
                    {
                        return DbType.DateTimeOffset;
                    }

                    return DbType.Object;

                case TypeCode.DBNull:
                    return DbType.Object;
                case TypeCode.Boolean:
                    return DbType.Boolean;
                case TypeCode.SByte:
                    return DbType.SByte;
                case TypeCode.Byte:
                    return DbType.Byte;
                case TypeCode.Char:
                    // Always treat char and char[] as string
                    return DbType.String;
                case TypeCode.Int16:
                    return DbType.Int16;
                case TypeCode.UInt16:
                    return DbType.UInt16;
                case TypeCode.Int32:
                    return DbType.Int32;
                case TypeCode.UInt32:
                    return DbType.UInt32;
                case TypeCode.Int64:
                    return DbType.Int64;
                case TypeCode.UInt64:
                    return DbType.UInt64;
                case TypeCode.Single:
                    return DbType.Single;
                case TypeCode.Double:
                    return DbType.Double;
                case TypeCode.Decimal:
                    return DbType.Decimal;
                case TypeCode.DateTime:
                    return DbType.DateTime;
                case TypeCode.String:
                    return DbType.String;
                default:
                    throw new XfwException("Unkown type ", clrType.FullName);
            }
        }

        //public static string ConvertDbTypeToSqlType(DbType dbType)
        //{
        //    switch (dbType)
        //    {
        //        //case TypeCode.Object:
        //        //    if (clrType == typeof(Byte[]))
        //        //    {
        //        //        return DbType.Binary;
        //        //    }
        //        //    if (clrType == typeof(Char[]))
        //        //    {
        //        //        // Always treat char and char[] as string
        //        //        return DbType.String;
        //        //    }
        //        //    else if (clrType == typeof(Guid))
        //        //    {
        //        //        return DbType.Guid;
        //        //    }
        //        //    else if (clrType == typeof(TimeSpan))
        //        //    {
        //        //        return DbType.Time;
        //        //    }
        //        //    else if (clrType == typeof(DateTimeOffset))
        //        //    {
        //        //        return DbType.DateTimeOffset;
        //        //    }

        //        //    return DbType.Object;

        //        case DbType.Binary: return "BINARY";
        //        case DbType.String: return "BINARY";

        //        case TypeCode.DBNull:
        //            return DbType.Object;
        //        case TypeCode.Boolean:
        //            return DbType.Boolean;
        //        case TypeCode.SByte:
        //            return DbType.SByte;
        //        case TypeCode.Byte:
        //            return DbType.Byte;
        //        case TypeCode.Char:
        //            // Always treat char and char[] as string
        //            return DbType.String;
        //        case TypeCode.Int16:
        //            return DbType.Int16;
        //        case TypeCode.UInt16:
        //            return DbType.UInt16;
        //        case TypeCode.Int32:
        //            return DbType.Int32;
        //        case TypeCode.UInt32:
        //            return DbType.UInt32;
        //        case TypeCode.Int64:
        //            return DbType.Int64;
        //        case TypeCode.UInt64:
        //            return DbType.UInt64;
        //        case TypeCode.Single:
        //            return DbType.Single;
        //        case TypeCode.Double:
        //            return DbType.Double;
        //        case TypeCode.Decimal:
        //            return DbType.Decimal;
        //        case TypeCode.DateTime:
        //            return DbType.DateTime;
        //        case TypeCode.String:
        //            return DbType.String;
        //        default:
        //            throw new XfwException("Unkown type ", clrType.FullName);
        //    }
        //}

        ///// <summary>
        ///// Finds best constructor
        ///// </summary>
        ///// <param name="names">DataReader column names</param>
        ///// <param name="types">DataReader column types</param>
        ///// <returns>Matching constructor or default one</returns>
        //public ConstructorInfo FindConstructor(string[] names, Type[] types)
        //{
        //    var constructors = _type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        //    foreach (ConstructorInfo ctor in constructors.OrderBy(c => c.IsPublic ? 0 : (c.IsPrivate ? 2 : 1)).ThenBy(c => c.GetParameters().Length))
        //    {
        //        ParameterInfo[] ctorParameters = ctor.GetParameters();
        //        if (ctorParameters.Length == 0)
        //            return ctor;

        //        if (ctorParameters.Length != types.Length)
        //            continue;

        //        int i = 0;
        //        for (; i < ctorParameters.Length; i++)
        //        {
        //            if (!String.Equals(ctorParameters[i].Name, names[i], StringComparison.OrdinalIgnoreCase))
        //                break;
        //            if (types[i] == typeof(byte[]) && ctorParameters[i].ParameterType.FullName == SqlMapper.LinqBinary)
        //                continue;
        //            var unboxedType = Nullable.GetUnderlyingType(ctorParameters[i].ParameterType) ?? ctorParameters[i].ParameterType;
        //            if (unboxedType != types[i]
        //                && !(unboxedType.IsEnum && Enum.GetUnderlyingType(unboxedType) == types[i])
        //                && !(unboxedType == typeof(char) && types[i] == typeof(string)))
        //                break;
        //        }

        //        if (i == ctorParameters.Length)
        //            return ctor;
        //    }

        //    return null;
        //}

        //        typeMap = new Dictionary<Type, DbType>();
        //typeMap[typeof(byte)] = DbType.Byte;
        //typeMap[typeof(sbyte)] = DbType.SByte;
        //typeMap[typeof(short)] = DbType.Int16;
        //typeMap[typeof(ushort)] = DbType.UInt16;
        //typeMap[typeof(int)] = DbType.Int32;
        //typeMap[typeof(uint)] = DbType.UInt32;
        //typeMap[typeof(long)] = DbType.Int64;
        //typeMap[typeof(ulong)] = DbType.UInt64;
        //typeMap[typeof(float)] = DbType.Single;
        //typeMap[typeof(double)] = DbType.Double;
        //typeMap[typeof(decimal)] = DbType.Decimal;
        //typeMap[typeof(bool)] = DbType.Boolean;
        //typeMap[typeof(string)] = DbType.String;
        //typeMap[typeof(char)] = DbType.StringFixedLength;
        //typeMap[typeof(Guid)] = DbType.Guid;
        //typeMap[typeof(DateTime)] = DbType.DateTime;
        //typeMap[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
        //typeMap[typeof(TimeSpan)] = DbType.Time;
        //typeMap[typeof(byte[])] = DbType.Binary;
        //typeMap[typeof(byte?)] = DbType.Byte;
        //typeMap[typeof(sbyte?)] = DbType.SByte;
        //typeMap[typeof(short?)] = DbType.Int16;
        //typeMap[typeof(ushort?)] = DbType.UInt16;
        //typeMap[typeof(int?)] = DbType.Int32;
        //typeMap[typeof(uint?)] = DbType.UInt32;
        //typeMap[typeof(long?)] = DbType.Int64;
        //typeMap[typeof(ulong?)] = DbType.UInt64;
        //typeMap[typeof(float?)] = DbType.Single;
        //typeMap[typeof(double?)] = DbType.Double;
        //typeMap[typeof(decimal?)] = DbType.Decimal;
        //typeMap[typeof(bool?)] = DbType.Boolean;
        //typeMap[typeof(char?)] = DbType.StringFixedLength;
        //typeMap[typeof(Guid?)] = DbType.Guid;
        //typeMap[typeof(DateTime?)] = DbType.DateTime;
        //typeMap[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;
        //typeMap[typeof(TimeSpan?)] = DbType.Time;
        //typeMap[typeof(object)] = DbType.Object;
    }
}
