﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Inte.XFramework.Caching
{
    /// <summary>
    /// 可读写缓存器
    /// </summary>
    public class ReaderWriterCache<TKey, TValue> : ICache<TKey, TValue>, IDisposable
    {
        protected readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        protected readonly IDictionary<TKey, TValue> _cache;

        /// <summary>
        /// 缓存内容
        /// </summary>
        protected IDictionary<TKey, TValue> Cache
        {
            get
            {
                return this._cache;
            }
        }

        /// <summary>
        /// 缓存项目计数
        /// </summary>
        public virtual int Count
        {
            get
            {
                this._rwLock.EnterReadLock();
                try
                {
                    return this._cache.Count;
                }
                finally
                {
                    this._rwLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// 实例化 <see cref="ReaderWriterCache"/> 类的新实例
        /// </summary>
        public ReaderWriterCache()
            : this(null)
        {
        }

        /// <summary>
        /// 实例化 <see cref="ReaderWriterCache"/> 类的新实例
        /// </summary>
        public ReaderWriterCache(IEqualityComparer<TKey> comparer)
        {
            this._cache = new Dictionary<TKey, TValue>(comparer);
        }

        /// <summary>
        /// 尝试获取指定键值的缓存项，若缓存项不存在，则使用指定委托创建
        /// </summary>
        public virtual TValue GetOrAdd(TKey key, Func<TKey, TValue> creator = null)
        {
            this._rwLock.EnterReadLock();
            try
            {
                TValue obj;
                if (this._cache.TryGetValue(key, out obj)) 
                    return obj;
            }
            finally
            {
                this._rwLock.ExitReadLock();
            }

            if (creator == null) 
                return default(TValue);

            this._rwLock.EnterWriteLock();
            try
            {
                TValue obj1;
                if (this._cache.TryGetValue(key, out obj1)) 
                    return obj1;
                
                TValue obj2 = creator(key);
                this._cache[key] = obj2;
                return obj2;
            }
            finally
            {
                this._rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 若指定的键值存在，则使用指定委托更新，否则使用指定委托创建
        /// </summary>
        public virtual TValue AddOrUpdate(TKey key, Func<TKey, TValue> creator, Func<TKey, TValue> updator = null)
        {
            this._rwLock.EnterWriteLock();
            try
            {
                TValue value;
                if (!this._cache.TryGetValue(key, out value))
                {
                    if (creator == null)
                        return default(TValue);
                    
                    TValue obj1 = creator(key);
                    this._cache[key] = obj1;
                    return obj1;
                }
                else
                {
                    if (updator == null)
                        return default(TValue);
                    
                    TValue obj2 = updator(key);
                    this._cache[key] = obj2;
                    return obj2;
                }
            }
            finally
            {
                this._rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 尝试获取指定键值的缓存项
        /// </summary>
        public virtual bool TryGet(TKey key, out TValue value)
        {
            this._rwLock.EnterReadLock();
            try
            {
                if (this._cache.TryGetValue(key, out value))
                    return true;
            }
            finally
            {
                this._rwLock.ExitReadLock();
            }
            return false;
        }

        /// <summary>
        /// 移除指定键值的缓存项
        /// </summary>
        public virtual void Remove(TKey key)
        {
            this._rwLock.EnterWriteLock();
            try
            {
                this._cache.Remove(key);
            }
            finally
            {
                this._rwLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 执行与释放或重置非托管资源相关的应用程序定义的任务。
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 执行与释放或重置非托管资源相关的应用程序定义的任务。
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._rwLock.EnterWriteLock();
                try
                {
                    this._cache.Clear();
                }
                finally
                {
                    this._rwLock.ExitWriteLock();
                }

                this._rwLock.Dispose();
            }
        }
    }
}
