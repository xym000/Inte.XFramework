using System;
using System.Configuration;
using System.Drawing;

namespace Inte.XFramework
{
    public class XfwCommon
    {
        #region 静态属性

        /// <summary>
        /// 连接字符串
        /// </summary>
        public static string ConnString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["XFrameworkConnString"].ConnectionString;
            }
        }

        /// <summary>
        /// 验证方式
        /// </summary>
        public static string AuthScheme = "Basic";

        /// <summary>
        /// 系统最小时间
        /// </summary>
        public static DateTime MinDateTime = Convert.ToDateTime("2010-01-01");
        
        /// <summary>
        /// 日期格式
        /// </summary>
        public static string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// 时间格式
        /// </summary>
        public static string TimeFormat = "yyyy-MM-dd HH:mm";

        /// <summary>
        /// 长时间格式
        /// </summary>
        public static string LongTimeFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// 金额格式
        /// </summary>
        public static string MoneyFormat = "0.00";

        #endregion

        #region 其它方法

        /// <summary>
        /// 获取 App Setting 节的配置信息
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        /// <summary>
        /// 将时间戳(以秒为单位)转成时间
        /// </summary>
        /// <param name="sec">是已秒做单位的时间戳</param>
        /// <returns></returns>
        public static DateTime ConvertToDateTime(string sec)
        {
            
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));

            long lTime = long.Parse(sec + "0000000");
            TimeSpan toNow = new TimeSpan(lTime);
            return dtStart.Add(toNow);
        }

        /// <summary>
        /// 将时间戳(以刻度数为单位)转成时间
        /// </summary>
        /// <returns></returns>
        public static DateTime ConvertToDateTime(long d)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long lTime = long.Parse(d + "0000");
            TimeSpan toNow = new TimeSpan(lTime);
            DateTime dtResult = dtStart.Add(toNow);
            return dtResult;
        }

        /// <summary>
        /// 日期转 long 型(以刻度数为单位)
        /// </summary>
        /// <returns></returns>
        public static long ConvertToLong(DateTime time)
        {
            DateTime dtStart = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            TimeSpan toNow = time.Subtract(dtStart);
            long timeStamp = toNow.Ticks;
            timeStamp = long.Parse(timeStamp.ToString().Substring(0, timeStamp.ToString().Length - 4));
            return timeStamp;
        }

        /// <summary>
        /// 日期转long型(以秒为单位)
        /// </summary>
        public static long ConvertToLong(string date)
        {
            System.DateTime time = DateTime.Parse(date);
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            return (long)(time - startTime).TotalSeconds;
        }

        static char[] _chars = 
        { 
            '0','1','2','3','4','5','6','7','8','9', 
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z' 
        };
        /// <summary>
        /// 生成0-9, a-b,A-B 之间指定的数量的随机数
        /// </summary>
        /// <param name="length">指定返回多少个数.</param>
        /// <returns></returns>
        public static string GenerateRandom(int length)
        {
            System.Text.StringBuilder newRandom = new System.Text.StringBuilder(36);
            Random rd = new Random();
            for (int i = 0; i < length; i++)
            {
                newRandom.Append(_chars[rd.Next(36)]);
            }
            return newRandom.ToString();
        }

        #endregion

        ///// <summary>
        ///// 全屏截图
        ///// </summary>
        ///// <returns></returns>
        //public static Image FullScreenShot()
        //{
        //    Image image = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
        //    using (Graphics g = Graphics.FromImage(image))
        //    {
        //        g.CopyFromScreen(new Point(0, 0), new Point(0, 0), System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size);
        //    }

        //    return image;
        //}

        /// <summary>
        /// @从视频文件截图,生成在视频文件所在文件夹
        /// 在Web.Config 中需要两个前置配置项:
        /// 1.ffmpeg.exe文件的路径
        /// <add key="ffmpeg" value="E:\ffmpeg\ffmpeg.exe" />
        /// 2.截图的尺寸大小
        /// <add key="CatchFlvImgSize" value="240x180" />
        /// 3.视频处理程序ffmpeg.exe
        /// </summary>
        /// <param name="vFileName">视频文件地址,如:/Web/FlvFile/User1/00001.Flv</param>
        /// <returns>成功:返回图片虚拟地址; 失败:返回空字符串</returns>
        //public string CatchImg(string vFileName)
        // {
        //    //取得ffmpeg.exe的路径,路径配置在Web.Config中,如:<add key="ffmpeg" value="E:\ffmpeg\ffmpeg.exe" />
        //    string ffmpeg=System.Configuration.ConfigurationSettings.AppSettings["ffmpeg"];
        //    if ( (!System.IO.File.Exists(ffmpeg)) || (!System.IO.File.Exists(vFileName)) )
        //     {
        //    return "";
        //     }
        //    //获得图片相对路径/最后存储到数据库的路径,如:/Web/FlvFile/User1/00001.jpg
        //    string flv_img = System.IO.Path.ChangeExtension(vFileName,".jpg") ;
        //    //图片绝对路径,如:D:\Video\Web\FlvFile\User1\0001.jpg
        //    string flv_img_p = HttpContext.Current.Server.MapPath(flv_img);
        //    //截图的尺寸大小,配置在Web.Config中,如:<add key="CatchFlvImgSize" value="240x180" />
        //    string FlvImgSize=System.Configuration.ConfigurationSettings.AppSettings["CatchFlvImgSize"];
        //     System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(ffmpeg);
        //     startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden; 
        //    //此处组合成ffmpeg.exe文件需要的参数即可,此处命令在ffmpeg 0.4.9调试通过
        //    startInfo.Arguments = " -i " vFileName " -y -f image2 -t 0.001 -s " FlvImgSize " " flv_img_p ;
        //    try 
        //     {
        //     System.Diagnostics.Process.Start(startInfo);
        //     }
        //    catch
        //     {
        //    return "";
        //     }
        //    ///注意:图片截取成功后,数据由内存缓存写到磁盘需要时间较长,大概在3,4秒甚至更长;
        //    ///这儿需要延时后再检测,我服务器延时8秒,即如果超过8秒图片仍不存在,认为截图失败;
        //    ///此处略去延时代码.如有那位知道如何捕捉ffmpeg.exe截图失败消息,请告知,先谢过!
        //    if ( System.IO.File.Exists(flv_img_p))
        //     {
        //    return flv_img; 
        //     }//51aspx
        //    return "";
        //}

    }
}
