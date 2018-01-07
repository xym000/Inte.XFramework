using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 导航属性描述集合
    /// </summary>
    public class ColumnNavDescriptorCollection : IEnumerable<KeyValuePair<string,ColumnNavDescriptor>>
    {
        private IDictionary<string, ColumnNavDescriptor> _navDescriptors = null;
        private int? _minIndex;

        /// <summary>
        /// 所有导航属性的最小开始索引
        /// </summary>
        public int MinIndex { get { return _minIndex == null ? 0 : _minIndex.Value; } }

        /// <summary>
        /// 包含的元素数
        /// </summary>
        public int Count { get { return _navDescriptors.Count; } }

        /// <summary>
        /// 实例化<see cref="ColumnNavDescriptorCollection"/>类的新实例
        /// </summary>
        public ColumnNavDescriptorCollection()
        {
            _navDescriptors = new Dictionary<string, ColumnNavDescriptor>(8);
        }
        
        /// <summary>
        /// 返回一个循环访问集合的枚举数。
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, ColumnNavDescriptor>> GetEnumerator()
        {
            return _navDescriptors.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _navDescriptors.GetEnumerator();
        }

        /// <summary>
        /// 添加一个带有所提供的键和值的元素。
        /// </summary>
        public void Add(string key, ColumnNavDescriptor descriptor)
        {
            _navDescriptors.Add(key, descriptor);
            if (descriptor != null && descriptor.Count > 0)
            {
                if (_minIndex == null)
                {
                    _minIndex = descriptor.Start;
                }
                else
                {
                    if (descriptor.Start < _minIndex.Value) _minIndex = descriptor.Start;
                } 
            }
        }
        
        /// <summary>
        /// 是否包含具有指定键的元素
        /// </summary>
        public bool ContainsKey(string key)
        {
            return _navDescriptors.ContainsKey(key);
        }
        
        /// <summary>
        /// 获取与指定的键相关联的值。
        /// </summary>
        public bool TryGetValue(string key,out ColumnNavDescriptor descriptor)
        {
            return _navDescriptors.TryGetValue(key, out descriptor);
        }
    }

    /// <summary>
    /// 导航属性描述信息，包括：导航属性名称，字段在 DataReader 中的取值范围
    /// </summary>
    public class ColumnNavDescriptor
    {
        /// <summary>
        /// 导航属性名称
        /// </summary>
        public string Name
        {
            get
            {
                return this.Member.Name;
            }
        }

        private string _keyName;
        /// <summary>
        /// 全名称
        /// </summary>
        public string KeyName { get { return _keyName; } }

        private MemberInfo _navMember = null;
        /// <summary>
        /// 导航属性对应
        /// </summary>
        public MemberInfo Member
        {
            get { return _navMember; }
        }

        /// <summary>
        /// 对应 DataReader 的索引，表示从这个位置开始到 End 位置的所有字段都是属于该导航属性
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// 对应 DataReader 的索引，表示从 Start 位置开始到该位置的所有字段都是属于该导航属性
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 实例化<see cref="ColumnNavDescriptor"/>类的新实例
        /// </summary>
        public ColumnNavDescriptor(string keyName, MemberInfo member)
        {
            _keyName = keyName;
            _navMember = member;
        }
    }
}
