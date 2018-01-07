
using Inte.XFramework.Data;

namespace Inte.XFramework.UnitTest
{
    public class Program
    {
        public static void Main()
        {
            XfwContainer.Default.Register<IDbQueryProvider>(() => new Inte.XFramework.Data.SqlClient.DbQueryProvider(XfwCommon.ConnString), true);
            Demo.Run();
        }
    }
}
