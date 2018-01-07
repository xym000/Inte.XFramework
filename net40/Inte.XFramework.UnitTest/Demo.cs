using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

using Inte.XFramework;
using Inte.XFramework.Data;

namespace Inte.XFramework.UnitTest
{
    public class Demo
    {
        static string _demoName = "002";
        static int[] _demoIdList = new int[] { 2, 3 };

        public static void Run()
        {
            //
            var context = new DataContext();
            var q1 = context.GetTable<Inte_CRM.Client>().Where(x => x.ClientId == 1);
            var q2 = context.GetTable<Inte_CRM.Client>().Where(x => x.ClientId == 1);
            var q3 = context.GetTable<Inte_CRM.Client>().Where(x => x.ClientId == 1);
            var q4 = q1.Union(q2).Union(q3);
            var r = q4.ToList();

            //var query3 =
            //    context
            //    .GetTable<Inte_CRM.CRM_SaleOrder>()
            //    .SelectMany(a => context.GetTable<Inte_CRM.Client>(), (a, b) => new { Sale = a, Buyer = b, ClientId = b.ClientId });
            //var query =
            //    from a in query3
            //    join c in context.GetTable<Inte_CRM.CloudServer>() on a.Buyer.CloudServerId equals c.CloudServerId into u_c
            //    from d in u_c.DefaultIfEmpty()
            //    where d.CloudServerId > 0 && a.Buyer.ActiveDate == DateTime.Now
            //    select new
            //    {
            //        Sale = a.Sale,
            //        Buyer = a.Buyer,
            //        Server = d
            //    };
            //query = query.Where(x => x.Buyer.CloudServerId == 1);
            //var q =
            //    query.Select(x => new { Sale = x.Sale, Buyer = x.Buyer });
            //var result = q.ToList();

            Query();
            Join();
            Other();
            Rabbit();
        }

        // 单表查询
        static void Query()
        {
            var context = new DataContext();

            // 查询所有
            var query = from a in context.GetTable<Inte_CRM.Demo>()
                        select a;
            var r1 = query.ToList();
            // Lambda表达式形式
            query = context.GetTable<Inte_CRM.Demo>();
            r1 = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong] AS [DemoLong],
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0

            // 构造函数
            query =
                from a in context.GetTable<Inte_CRM.Demo>()
                select new Inte_CRM.Demo(a);
            r1 = query.ToList();
            query =
               from a in context.GetTable<Inte_CRM.Demo>()
               select new Inte_CRM.Demo(a.DemoId, a.DemoName);
            r1 = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoName] AS [DemoName]
            //FROM [Sys_Demo] t0 

            // 指定字段
            query = from a in context.GetTable<Inte_CRM.Demo>()
                    select new Inte_CRM.Demo
                    {
                        DemoCode = a.DemoCode,
                        DemoDateTime_Nullable = a.DemoDateTime_Nullable
                    };
            r1 = query.ToList();
            // Lambda表达式形式
            query = context
                .GetTable<Inte_CRM.Demo>()
                .Select(a => new Inte_CRM.Demo
                {
                    DemoCode = a.DemoCode,
                    DemoDateTime_Nullable = a.DemoDateTime_Nullable
                });
            r1 = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoDateTime_Nullable] AS [DemoDateTime_Nullable]
            //FROM [Sys_Demo] t0 

            // 匿名类
            var query_dm = from a in context.GetTable<Inte_CRM.Demo>()
                           select new
                           {
                               DemoCode = a.DemoCode,
                               DemoDateTime_Nullable = a.DemoDateTime_Nullable
                           };
            var r2 = query_dm.ToList();
            // Lambda表达式形式
            query_dm = context
                .GetTable<Inte_CRM.Demo>()
                .Select(a => new
                {
                    DemoCode = a.DemoCode,
                    DemoDateTime_Nullable = a.DemoDateTime_Nullable
                });
            r2 = query_dm.ToList();
            //SQL=> 
            //SELECT 
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoDateTime_Nullable] AS [DemoDateTime_Nullable]
            //FROM [Sys_Demo] t0 

            //分页查询（非微软api）
            query = from a in context.GetTable<Inte_CRM.Demo>()
                    select a;
            var r3 = query.ToPagedList(1, 20);
            //SQL=>
            //SELECT TOP(20)
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            query = context.GetTable<Inte_CRM.Demo>();
            r3 = query.OrderBy(a => a.DemoDecimal).ToPagedList(2, 20);
            //SQL=>
            //SELECT TOP(20)
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 

            // 分页查询
            // 1.不是查询第一页的内容时，必须先OrderBy再分页，OFFSET ... Fetch Next 分页语句要求有 OrderBy
            // 2.OrderBy表达式里边的参数必须跟query里边的变量名一致，如此例里的 a。SQL解析时根据此变更生成表别名
            query = from a in context.GetTable<Inte_CRM.Demo>()
                    orderby a.DemoCode
                    select a;
            query = query.Skip(1).Take(18);
            r1 = query.ToList();
            // Lambda表达式形式
            query = context
                .GetTable<Inte_CRM.Demo>()
                .OrderBy(a => a.DemoCode)
                .Skip(1)
                .Take(18);
            r1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //ORDER BY t0.[DemoCode]
            //OFFSET 1 ROWS FETCH NEXT 18 ROWS ONLY

            query =
                from a in context.GetTable<Inte_CRM.Demo>()
                orderby a.DemoCode
                select a;
            query = query.Skip(1);
            r1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //ORDER BY t0.[DemoCode]
            //OFFSET 1 ROWS

            query =
                from a in context.GetTable<Inte_CRM.Demo>()
                orderby a.DemoCode
                select a;
            query = query.Take(1);
            r1 = query.ToList();
            //SQL=>
            //SELECT TOP(1)
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //ORDER BY t0.[DemoCode]

            query =
                from a in context.GetTable<Inte_CRM.Demo>()
                orderby a.DemoCode
                select a;
            query = query.Skip(1);
            query = query.Where(a => a.DemoId > 0);
            query = query.OrderBy(a => a.DemoCode).Skip(1).Take(1);
            r1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM (
            //    SELECT 
            //    t0.[DemoId] AS [DemoId],
            //    t0.[DemoCode] AS [DemoCode],
            //    t0.[DemoName] AS [DemoName],
            //    ...
            //    t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //    FROM [Sys_Demo] t0 
            //    ORDER BY t0.[DemoCode]
            //    OFFSET 1 ROWS
            //) t0 
            //WHERE t0.[DemoId] > 0
            //ORDER BY t0.[DemoCode]
            //OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY 

            // 过滤条件
            query = from a in context.GetTable<Inte_CRM.Demo>()
                    where a.DemoName == "002"
                    select a;
            r1 = query.ToList();
            // Lambda表达式形式
            query = context.GetTable<Inte_CRM.Demo>().Where(a => a.DemoName == "002");
            r1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //WHERE t0.[DemoName] = N'002'

            // 更多条件
            int m_byte = 9;
            query = from a in context.GetTable<Inte_CRM.Demo>()
                    where
                        a.DemoName == "002" &&
                        a.DemoCode.Contains("TAN") &&                                   // LIKE '%%'
                        a.DemoCode.StartsWith("TAN") &&                                 // LIKE 'K%'
                        a.DemoCode.EndsWith("TAN") &&                                   // LIKE '%K' 
                        a.DemoCode.Length == 12 &&
                        //支持的字符串操作=> Trim | TrimStart | TrimEnd | ToString | Length
                        a.DemoDateTime == DateTime.Now &&
                        a.DemoName == (
                            a.DemoDateTime_Nullable == null ? "NULL" : "NOT NULL") &&   // 三元表达式
                        a.DemoName == (a.DemoName ?? a.DemoCode) &&                     // 二元表达式
                        new[] { 1, 2, 3 }.Contains(a.DemoId) &&
                        Demo._demoIdList.Contains(a.DemoId) &&
                        a.DemoName == Demo._demoName &&
                        a.DemoByte == (byte)m_byte &&
                        a.DemoByte == (byte)State.OK ||                                 // IN(1,2,3)
                        (a.DemoName == "STATE" && a.DemoName == "REMARK")               // OR 查询
                    select a;
            r1 = query.ToList();
            // Lambda表达式形式
            query = context.GetTable<Inte_CRM.Demo>().Where(a =>
                        a.DemoName == "002" &&
                        a.DemoCode.Contains("TAN") &&                                   // LIKE '%%'
                        a.DemoCode.StartsWith("TAN") &&                                 // LIKE 'K%'
                        a.DemoCode.EndsWith("TAN") &&                                   // LIKE '%K' 
                        a.DemoCode.Length == 12 &&
                            //支持的字符串操作=> Trim | TrimStart | TrimEnd | ToString | Length
                        a.DemoDateTime == DateTime.Now &&
                        a.DemoName == (
                            a.DemoDateTime_Nullable == null ? "NULL" : "NOT NULL") &&   // 三元表达式
                        a.DemoName == (a.DemoName ?? a.DemoCode) &&                     // 二元表达式
                        new[] { 1, 2, 3 }.Contains(a.DemoId) &&
                        !Demo._demoIdList.Contains(a.DemoId) &&
                        a.DemoName == Demo._demoName &&
                        a.DemoByte == (byte)m_byte &&
                        a.DemoByte == (byte)State.OK ||                                 // IN(1,2,3)
                        (a.DemoName == "STATE" && a.DemoName == "REMARK")               // OR 查询
                );
            //SQL=>            
            //SELECT 
            //t0.[DemoId] AS [DemoId],
            //t0.[DemoCode] AS [DemoCode],
            //t0.[DemoName] AS [DemoName],
            //...
            //t0.[DemoLong_Nullable] AS [DemoLong_Nullable]
            //FROM [Sys_Demo] t0 
            //WHERE (t0.[DemoName] = N'002' AND t0.[DemoCode] LIKE N'%TAN%' AND t0.[DemoCode] LIKE N'TAN%' AND t0.[DemoCode] LIKE N'%TAN' AND LEN(t0.[DemoCode]) = 12 AND t0.[DemoDateTime] = '2017/11/25 11:53:37' AND t0.[DemoName] = (CASE WHEN t0.[DemoDateTime_Nullable] = NULL THEN N'NULL' ELSE N'NOT NULL' END) AND t0.[DemoName] = ISNULL(t0.[DemoName],t0.[DemoCode]) AND t0.[DemoId] IN(1,2,3) AND t0.[DemoId] IN(2,3) AND t0.[DemoName] = N'002' AND t0.[DemoByte] = 9 AND t0.[DemoByte] = 1) OR (t0.[DemoName] = N'STATE' AND t0.[DemoName] = N'REMARK')
        }

        // 多表查询
        static void Join()
        {
            var context = new DataContext();

            // INNER JOIN
            var query =
                from a in context.GetTable<Inte_CRM.CRM_SaleOrder>()
                join b in context.GetTable<Inte_CRM.Client>() on a.ClientId equals b.ClientId
                join c in context.GetTable<Inte_CRM.CloudServer>() on b.CloudServerId equals c.CloudServerId
                where a.ClientId > 0
                select a;
            var r1 = query.ToList();
            // Lambda表达式形式
            query = context
                .GetTable<Inte_CRM.CRM_SaleOrder>()
                .Join(context.GetTable<Inte_CRM.Client>(), a => a.ClientId, b => b.ClientId, (a, b) => new { Sale = a, Buyer = b })
                .Join(context.GetTable<Inte_CRM.CloudServer>(), b => b.Buyer.CloudServerId, c => c.CloudServerId, (a, c) => new Inte_CRM.CRM_SaleOrder { })
                .Where(a => a.ClientId > 0);
            //r1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[OrderId] AS [OrderId],
            //t0.[OrderNo] AS [OrderNo],
            //t0.[Remark] AS [Remark],
            //t0.[ClientId] AS [ClientId]
            //FROM [CRM_SaleOrder] t0 
            //INNER JOIN [Bas_Client] t1 ON t0.[ClientId] = t1.[ClientId]
            //INNER JOIN [Sys_CloudServer] t2 ON t1.[CloudServerId] = t2.[CloudServerId]
            //WHERE t0.[ClientId] > 0


            // 更简单的赋值方式 
            // 适用场景：在显示列表时只想显示外键表的一两个字段
            query =
                from a in context.GetTable<Inte_CRM.CRM_SaleOrder>()
                select new Inte_CRM.CRM_SaleOrder(a)
                {
                    Client = new Inte_CRM.Client(a.Client)
                    {
                        CloudServer = new Inte_CRM.CloudServer
                        {
                            CloudServerId = a.Client.CloudServer.CloudServerId,
                            CloudServerName = a.Client.CloudServer.CloudServerName
                        }
                    },
                    HeavyBuyer = new Inte_CRM.Client
                    {
                        ClientId = a.Client.ClientId + 10,
                        ClientName = a.Client.ClientName + "_heavy",
                        CloudServer = new Inte_CRM.CloudServer
                        {
                            CloudServerId = a.Client.CloudServer.CloudServerId + 10,
                            CloudServerName = a.Client.CloudServer.CloudServerName + "_heavy",
                        }
                    }
                };
            r1 = query.ToList();
            //SQL=>
            //SELECT 
            //t0.[OrderId] AS [OrderId],
            //t0.[OrderNo] AS [OrderNo],
            //t0.[Remark] AS [Remark],
            //t0.[ClientId] AS [ClientId],
            //t1.[ClientId] AS [ClientId1],
            //t1.[ClientCode] AS [ClientCode],
            //t1.[ClientName] AS [ClientName],
            //t1.[State] AS [State],
            //t1.[ActiveDate] AS [ActiveDate],
            //t1.[CloudServerId] AS [CloudServerId],
            //t2.[CloudServerId] AS [CloudServerId1],
            //t2.[CloudServerName] AS [CloudServerName],
            //t1.[ClientId] + 10 AS [ClientId2],
            //t1.[ClientName] + N'_heavy' AS [ClientName1],
            //t2.[CloudServerId] + 10 AS [CloudServerId2],
            //t2.[CloudServerName] + N'_heavy' AS [CloudServerName1]
            //FROM [CRM_SaleOrder] t0 
            //LEFT JOIN [Bas_Client] t1 ON t0.[ClientId] = t1.[ClientId]
            //LEFT JOIN [Sys_CloudServer] t2 ON t1.[CloudServerId] = t2.[CloudServerId]

            query =
                from a in context.GetTable<Inte_CRM.CRM_SaleOrder>()
                select new Inte_CRM.CRM_SaleOrder(a)
                {
                    Client = a.Client
                };
            r1 = query.ToList();

            // CROSS JOIN
            var query3 =
                context
                .GetTable<Inte_CRM.CRM_SaleOrder>()
                .SelectMany(a => context.GetTable<Inte_CRM.Client>(), (a, b) => new
                {
                    a.ClientId,
                    b.ClientName
                });
            var r3 = query3.ToList();
            //SQL=>
            //SELECT
            //t0.[ClientId] AS[ClientId],
            //t1.[ClientName] AS[ClientName]
            //FROM[CRM_SaleOrder] t0
            //CROSS JOIN[Bas_Client] t1

            // LEFT JOIN
            query =
                from a in context.GetTable<Inte_CRM.CRM_SaleOrder>()
                join b in context.GetTable<Inte_CRM.Client>() on a.ClientId equals b.ClientId into u_b
                from b in u_b
                select a;
            r1 = query.ToList();
            //SQL=>
            //SELECT
            //t0.[OrderId] AS[OrderId],
            //t0.[OrderNo] AS[OrderNo],
            //t0.[Remark] AS[Remark],
            //t0.[ClientId] AS[ClientId]
            //FROM[CRM_SaleOrder] t0
            //LEFT JOIN[Bas_Client] t1 ON t0.[ClientId] = t1.[ClientId]

            /// 未解决*****************
            //var cJoin =
            //    context
            //    .GetTable<Inte_CRM.CRM_SaleOrder>()
            //    .SelectMany(a => context.GetTable<Inte_CRM.Client>(), (a, b) => new { Sale = a, Buyer = b });
            /// 未解决*****************
            

            var query5 =
                 from a in context.GetTable<Inte_CRM.Client>()
                 join b in context.GetTable<Inte_CRM.CloudServer>() on a.CloudServerId equals b.CloudServerId into u_c
                 from b in u_c.DefaultIfEmpty()
                 select a;
            var query6 =
                query5.SelectMany(c => context.GetTable<Inte_CRM.CRM_SaleOrder>(), (a, c) => new
                {
                    OrderId = c.OrderId,
                    ClientName = a.ClientName,
                    ServerName = a.CloudServer.CloudServerName
                });
            var r2 = query6.ToList();
            //SQL=>
            //SELECT 
            //t2.[OrderId] AS [OrderId],
            //t0.[ClientName] AS [ClientName],
            //t1.[CloudServerName] AS [ServerName]
            //FROM [Bas_Client] t0 
            //LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //CROSS JOIN [CRM_SaleOrder] t2 

            // uion
            var q1 = context.GetTable<Inte_CRM.Client>().Where(x => x.ClientId == 1);
            var q2 = context.GetTable<Inte_CRM.Client>().Where(x => x.ClientId == 1);
            var q3 = context.GetTable<Inte_CRM.Client>().Where(x => x.ClientId == 1);
            var q4 = q1.Union(q2).Union(q3);
            var r = q4.ToList();
        }

        // 其它说明
        static void Other()
        {
            var context = new DataContext();

            // Any
            var any = context.GetTable<Inte_CRM.Client>().Any(a => a.ActiveDate == DateTime.Now);
            //SQL=> 
            //IF EXISTS(
            //    SELECT TOP 1 1
            //    FROM[Bas_Client] t0
            //    WHERE t0.[ActiveDate] = '2017/11/26 22:43:52'
            //) SELECT 1 ELSE SELECT 0

            // FirstOrDefault
            var f = context.GetTable<Inte_CRM.Client>().FirstOrDefault();
            //SQL=> 
            //SELECT TOP(1)
            //t0.[ClientId] AS[ClientId],
            //t0.[ClientCode] AS[ClientCode],
            //t0.[ClientName] AS[ClientName],
            //t0.[State] AS[State],
            //t0.[ActiveDate] AS[ActiveDate],
            //t0.[CloudServerId] AS[CloudServerId]
            //FROM[Bas_Client] t0

            // Max
            var max = context.GetTable<Inte_CRM.Client>().Where(a => a.ClientId < -9).Max(a => a.ClientId);
            //SQL=> 
            //SELECT
            //MAX(t0.[ClientId])
            //FROM[Bas_Client] t0
            //WHERE t0.[ClientId] < -9

            // GROUP BY
            var query =
                from a in context.GetTable<Inte_CRM.Client>()
                where a.ClientName == "TAN"
                group a by new { a.ClientId, a.ClientName } into g
                where g.Key.ClientId > 0
                orderby g.Key.ClientName
                select new
                {
                    Id = g.Key.ClientId,
                    Name = g.Min(a => a.ClientId)
                };
            var r1 = query.ToList();
            //SQL=> 
            //SELECT
            //t0.[ClientId] AS[Id],
            //MIN(t0.[ClientId]) AS[Name]
            //FROM[Bas_Client] t0
            //WHERE t0.[ClientName] = N'TAN'
            //GROUP BY t0.[ClientId],t0.[ClientName]
            //Having t0.[ClientId] > 0
            //ORDER BY t0.[ClientName]

            // 分组后再分页
            query =
                from a in context.GetTable<Inte_CRM.Client>()
                where a.ClientName == "TAN"
                group a by new { a.ClientId, a.ClientName } into g
                where g.Key.ClientId > 0
                orderby new { g.Key.ClientName, g.Key.ClientId }
                select new
                {
                    Id = g.Key.ClientId,
                    Name = g.Min(a => a.ClientId)
                };
            query = query.Skip(2).Take(3);
            r1 = query.ToList();
            //SQL=> 
            //SELECT 
            //t0.[Id],
            //t0.[Name]
            //FROM ( 
            //    SELECT 
            //    t0.[ClientId] AS [Id],
            //    MIN(t0.[ClientId]) AS [Name],
            //    t0.[ClientName] AS [ClientName]
            //    FROM [Bas_Client] t0 
            //    WHERE t0.[ClientName] = N'TAN'
            //    GROUP BY t0.[ClientId],t0.[ClientName]
            //    Having t0.[ClientId] > 0
            // ) t0
            //ORDER BY t0.[ClientName]
            //OFFSET 2 ROWS FETCH NEXT 3 ROWS ONLY 

            // DISTINCT 分组
            var query2 =
                context.GetTable<Inte_CRM.Client>().Distinct().Select(a => new Inte_CRM.Client { ClientId = a.ClientId, ClientName = a.ClientName });
            var min = query2.Min(a => a.CloudServerId);
            //SQL=> 
            //SELECT 
            //MIN(t0.[CloudServerId])
            //FROM ( 
            //    SELECT DISTINCT 
            //    t0.[ClientId] AS [ClientId],
            //    t0.[ClientName] AS [ClientName],
            //    t0.[CloudServerId] AS [CloudServerId]
            //    FROM [Bas_Client] t0 
            // ) t0

            var thin = new Inte_CRM.Thin { ThinId = 1, ThinName = "001" };
            var thinIdentity = new Inte_CRM.ThinIdentity { ThinName = "001" };
            List<Inte_CRM.Thin> collection = new List<Inte_CRM.Thin> 
            {
                 new Inte_CRM.Thin { ThinId = 2, ThinName = "002" },
                 new Inte_CRM.Thin { ThinId = 3, ThinName = "003" }
            };
            List<Inte_CRM.ThinIdentity> identirys = new List<Inte_CRM.ThinIdentity> 
            {
                 new Inte_CRM.ThinIdentity { ThinName = "002" },
                 new Inte_CRM.ThinIdentity { ThinName = "003" }
            };

            // 删除记录
            context.Delete(thin);
            context.Delete(thinIdentity);
            ///context.SubmitChanges();

            context.Delete<Inte_CRM.Thin>(a => a.ThinId == 2 || a.ThinId == 3);
            context.Delete<Inte_CRM.ThinIdentity>(a => a.ThinName == "001" || a.ThinName == "002" || a.ThinName == "003");
            ///context.SubmitChanges();
            var qeury4 = context.GetTable<Inte_CRM.Thin>().Where(a => a.ThinId == 2 || a.ThinId == 3);
            context.Delete<Inte_CRM.Thin>(qeury4);
            context.SubmitChanges(); // 一次性提交
            //SQL=> 
            //DELETE t0 FROM [Sys_Thin] t0 
            //WHERE t0.[ThinId] = 1
            //DELETE t0 FROM [Sys_Thin] t0 
            //WHERE (t0.[ThinId] = 2) OR (t0.[ThinId] = 3)
            //DELETE t0 FROM [Sys_Thin] t0 
            //WHERE (t0.[ThinId] = 2) OR (t0.[ThinId] = 3)

            // 增加记录
            context.Insert(thin);
            context.SubmitChanges();
            //SQL=> 
            //INSERT INTO [Sys_Thin]
            //([ThinId],[ThinName])
            //VALUES
            //(1,N'001')


            context.Insert<Inte_CRM.Thin>(collection);
            context.SubmitChanges();

            // 自增列添加记录
            context.Insert(thinIdentity);
            context.Insert<Inte_CRM.ThinIdentity>(identirys);
            context.SubmitChanges();
            //SQL=> 
            //INSERT INTO [Sys_ThinIdentity]
            //([ThinName])
            //VALUES
            //(N'001')
            //SELECT CAST(SCOPE_IDENTITY() AS INT)
            //...


            thin.ThinName = "001.N";
            context.Update(thin);
            context.SubmitChanges();
            //SQL=> 
            //UPDATE t0 SET
            //t0.[ThinId] = 1,
            //t0.[ThinName] = N'001.N'
            //FROM [Sys_Thin] t0
            //WHERE t0.[ThinId] = 1

            // 更新记录
            context.Update<Inte_CRM.Thin>(a => new Inte_CRM.Thin { ThinName = "001.TAN" }, a => a.ThinId != 3);
            context.SubmitChanges();
            //SQL=> 
            //UPDATE t0 SET
            //t0.[ThinName] = N'001.TAN'
            //FROM [Sys_Thin] AS [t0]
            //WHERE t0.[ThinId] <> 3

            var query3 =
                from a in context.GetTable<Inte_CRM.Client>()
                where a.CloudServer.CloudServerId != 0
                select a;
            context.Update<Inte_CRM.Client>(a => new Inte_CRM.Client { Remark = "001.TAN" }, query3);
            context.SubmitChanges();
            //SQL=> 
            //UPDATE t0 SET
            //t0.[Remark] = N'001.TAN'
            //FROM [Bas_Client] AS [t0]
            //LEFT JOIN [Sys_CloudServer] t1 ON t0.[CloudServerId] = t1.[CloudServerId]
            //WHERE t1.[CloudServerId] <> 0
        }

        // 性能测试
        static void Rabbit()
        {
            string connString = "Server=192.168.32.170;Database=ProductCenter;uid=testuser;pwd=123456";
            var context = new DataContext(new Inte.XFramework.Data.SqlClient.DbQueryProvider(connString));


            Stopwatch stop = new Stopwatch();
            stop.Start();

            for (int i = 0; i < 1000; i++)
            {
                //var query =
                //    from a in context.GetTable<Prd_Center.Product>()
                //    where a.Client.ClientID == 1
                //    select new Prd_Center.Product(a)
                //    {
                //        Client = a.Client
                //    };
                var query =
                    from a in context.GetTable<Prd_Center.Product>()
                    where a.ClientID == 1
                    select new Prd_Center.Product
                    {
                        ClientID = a.ClientID,
                        ProductID = a.ProductID,
                        Title = a.Title
                    };
                var result = query.ToList();
            }

            stop.Stop();
            Console.WriteLine(stop.ElapsedMilliseconds);
            Console.WriteLine(stop.Elapsed);
            Console.ReadLine();
            // Elapsed = {00:04:52.2977968}
            // Elapsed = {00:06:30.9521988} EF

            context = new DataContext();
            stop = new Stopwatch();
            stop.Start();
            for (int i = 0; i < 1000000; i++)
            {
                var query = context.GetTable<Inte_CRM.Client>().Where(a => a.ClientId != 0);
                var result = query.ToList();
            }

            stop.Stop();
            Console.WriteLine(stop.ElapsedMilliseconds);
            Console.WriteLine(stop.Elapsed);
            Console.ReadLine();

        }
    }
}
