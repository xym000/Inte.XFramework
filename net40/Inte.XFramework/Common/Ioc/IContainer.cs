﻿using System;

namespace Inte.XFramework
{
    public interface IContainer
    {
        /// <summary>
        /// 向容器注册类型
        /// </summary>
        void Register<T>(Func<T> func) where T : class;

        /// <summary>
        /// 向容器注册类型
        /// </summary>
        void Register<T>(Func<T> func, bool isSingleton) where T : class;

        /// <summary>
        /// 向容器注册类型
        /// </summary>
        void Register(Type type, Func<object> func, bool isSingleton);

        /// <summary>
        /// 从窗口中解析指定类型
        /// </summary>
        T Resolve<T>() where T : class;
    }
}
