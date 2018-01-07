using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Inte.XFramework.Reflection.Emit
{
    internal static class Helper
    {
        public static void EmitBoxIfNeeded(this ILGenerator g, Type type)
        {
            if (!type.IsValueType)
                return;
            g.Emit(OpCodes.Box, type);
        }

        public static void EmitCast(this ILGenerator g, Type type)
        {
            if (type.IsValueType)
                g.Emit(OpCodes.Unbox_Any, type);
            else
                g.Emit(OpCodes.Castclass, type);
        }

        /// <summary>
        /// 取指定类型的默认初始值
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GetNullValue(Type type)
        {
            if (type.IsValueType)
            {
                if (type.IsEnum)
                {
                    return GetNullValue(Enum.GetUnderlyingType(type));
                }

                if (type.IsPrimitive)
                {
                    if (type == typeof(Int32)) { return 0; }
                    if (type == typeof(Double)) { return (Double)0; }
                    if (type == typeof(Int16)) { return (Int16)0; }
                    if (type == typeof(SByte)) { return (SByte)0; }
                    if (type == typeof(Int64)) { return (Int64)0; }
                    if (type == typeof(Byte)) { return (Byte)0; }
                    if (type == typeof(UInt16)) { return (UInt16)0; }
                    if (type == typeof(UInt32)) { return (UInt32)0; }
                    if (type == typeof(UInt64)) { return (UInt64)0; }
                    if (type == typeof(UInt64)) { return (UInt64)0; }
                    if (type == typeof(Single)) { return (Single)0; }
                    if (type == typeof(Boolean)) { return false; }
                    if (type == typeof(char)) { return '\0'; }
                }
                else
                {
                    //DateTime : 01/01/0001 00:00:00
                    //TimeSpan : 00:00:00
                    //Guid : 00000000-0000-0000-0000-000000000000
                    //Decimal : 0

                    if (type == typeof(DateTime)) { return DateTime.MinValue; }
                    if (type == typeof(Decimal)) { return 0m; }
                    if (type == typeof(Guid)) { return Guid.Empty; }
                    if (type == typeof(TimeSpan)) { return new TimeSpan(0, 0, 0); }
                }
            }

            return null;
        }

        /// <summary>
        /// 将整形数值推送到计算堆栈上。
        /// </summary>
        /// <param name="g">(MSIL)指令</param>
        /// <param name="value">整形数值</param>
        public static void EmitInt(this ILGenerator g, int value)
        {
            switch (value)
            {
                case -1:
                    g.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case 0:
                    g.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    g.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    g.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    g.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    g.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    g.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    g.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    g.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    g.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value > -129 && value < 128)
                    {
                        g.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                        break;
                    }
                    g.Emit(OpCodes.Ldc_I4, value);
                    break;
            }
        }
    }
}
