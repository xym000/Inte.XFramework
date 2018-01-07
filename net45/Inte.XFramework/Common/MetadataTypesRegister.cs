
using System;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Inte.XFramework.Reflection.Emit;

namespace Inte.XFramework
{
    /// <summary>
    /// 类型元数据注册器
    /// </summary>
    public static class MetadataTypesRegister
    {
        /// <summary>
        /// 注册类型元数据说明
        /// </summary>
        public static void Register(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                foreach (MetadataTypeAttribute attribute in type.GetCustomAttributes(typeof(MetadataTypeAttribute), true))
                {
                    TypeDescriptor.AddProviderTransparent(
                        new AssociatedMetadataTypeTypeDescriptionProvider(type, attribute.MetadataClassType), type);
                }
            }
        }
    }

    /// <summary>
    /// 定义一个帮助器类，在与对象、属性和方法关联的特性中包含此类时，可使用此类来验证这些项
    /// </summary>
    public static class XValidator
    {
        static MethodInfo _mi = null;
        static MemberAccess_Method _ma_Method = null;

        static PropertyInfo _pi = null;
        static MemberAccess_Method _ma_Property = null;


        static XValidator()
        {
            _mi = typeof(Validator).GetMethod("GetObjectValidationErrors", BindingFlags.Static | BindingFlags.NonPublic);
            _ma_Method = new MemberAccess_Method(_mi);
        }

        /// <summary>
        /// 通过使用验证上下文、验证结果集合和用于指定是否验证所有属性的值，确定指定的对象是否有效。
        /// </summary>
        /// <param name="instance">要验证的对象</param>
        /// <param name="validationContext">用于描述要验证的对象的上下文</param>
        /// <param name="validationResults">用于包含每个失败的验证的集合</param>
        /// <param name="validateAllProperties">若要验证所有属性</param>
        /// <param name="breakOnFirstError">当第一个错误产生时，是否不再进行后续验证</param>
        /// <returns></returns>
        public static bool TryValidateObject(object instance, ValidationContext validationContext, ICollection<ValidationResult> validationResults, bool validateAllProperties, bool breakOnFirstError)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            // 如果验证所有的错误，则使用微软默认的校验
            if (!breakOnFirstError) return Validator.TryValidateObject(instance, validationContext, validationResults, validateAllProperties);

            // 当第一个错误产生时，是否不再进行后续验证
            // ValidateObject 会验证所有，故可能需要重写
            IEnumerable collection = _ma_Method.Invoke(null, instance, validationContext, validateAllProperties, true) as IEnumerable;
            if (collection == null) return true;

            bool isValid = true;
            IEnumerator iterator = collection.GetEnumerator();
            while (iterator.MoveNext())
            {
                isValid = false;
                Type type = iterator.Current.GetType();

                if (_pi == null)
                {
                    _pi = type.GetProperty("ValidationResult", BindingFlags.Instance | BindingFlags.NonPublic);
                    _ma_Property = new MemberAccess_Method(_pi.GetGetMethod(true));
                }

                object obj = iterator.Current;
                ValidationResult result = (ValidationResult)_ma_Property.Invoke(iterator.Current);
                if (validationResults != null) validationResults.Add(result);

                break;
            }

            return isValid;
        }
    }
}
