
using System.Data;
using System.Collections.Generic;

namespace Inte.XFramework.Data
{
    /// <summary>
    /// 命令定义
    /// </summary>
    public class CommandDefine
    {
        private string _commandText;
        private IEnumerable<IDataParameter> _parameters;
        //private IDbTransaction _transaction;
        private CommandType? _commandType;

        /// <summary>
        /// 针对数据源运行的文本命令
        /// </summary>
        public virtual string CommandText
        {
            get { return _commandText; }
            set { _commandText = value; }
        }

        /// <summary>
        /// 命令参数
        /// </summary>
        public virtual IEnumerable<IDataParameter> Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        ///// <summary>
        ///// 命令对应的事务
        ///// </summary>
        //public virtual IDbTransaction Transaction
        //{
        //    get { return _transaction; }
        //    set { _transaction = value; }
        //}

        /// <summary>
        /// 命令类型
        /// </summary>
        public virtual CommandType? CommandType
        {
            get { return _commandType; }
            set { _commandType = value; }
        }

        /// <summary>
        /// 初始化 <see cref="CommandDefine"/> 类的新实例
        /// </summary>
        public CommandDefine(string commandText, IEnumerable<IDataParameter> parameters = null, CommandType? commandType = null)
        {
            this._commandText = commandText;
            this._parameters = parameters;
            //this._transaction = transaction;
            this._commandType = commandType;
        }
    }
}
