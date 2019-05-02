using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GetPicture
{
    class Program
    {
        private static string[] args;
        private static string Url { get; set; }
        private static string FilePath { get; set; }
        private static int Size { get; set; } = 10;
        public static string[] Args
        {
            get => args;
            set
            {
                args = value;
                Regex regex = new Regex(@"^([a-zA-Z]+://)([\w-\.]+)(\.[a-zA-Z0-9]+)(:\d{0,5})?/?([\w-/]*)\.?([a-zA-Z]*)\??(([\w-]*=[\w%]*&?)*)$");
                if (regex.IsMatch(value[0]))
                {
                    Url = value[0];
                }

                if (args.Length == 2)
                {

                    FilePath = FormatPath(value[1]);

                }
                if (args.Length == 3)
                {

                    Size = TryParesInt(value[1]);
                    FilePath = FormatPath(value[2]);
                }
            }

        }


        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                HelpMe();
                return;
            }
            Args = args;
            switch (Args.Length)
            {
                case 1:
                    SingleArgs(Args[0]);
                    break;
                case 2:
                    await Begin();
                    break;
                case 3:
                    await Begin();
                    break;
                default:
                    ArgsError();
                    break;
            }
        }
        public static async Task Begin()
        {
            int i = 1;
            while (i <= Size)
            {
                switch (await GetPicture())
                {
                    case 0:
                        Console.WriteLine("获取失败");
                        break;
                    case 1:
                        Console.WriteLine("成功获取" + i + "个图片");
                        i++;
                        break;
                    case 2:
                        Console.WriteLine("重复");
                        break;
                    case 3:
                        Console.WriteLine("连接超时");
                        break;
                    case 4:
                        Console.WriteLine("目标地址有误,请核对");
                        break;
                }
            }
        }
        public static async Task<int> GetPicture()
        {

            var uri = new Uri(Url);
            HttpClient httpClient = new HttpClient();

            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();
            try
            {
                httpResponseMessage = await httpClient.GetAsync(uri);
            }
            catch
            {
                return 3;
            }

            var requestUrl = httpResponseMessage.RequestMessage.RequestUri.ToString();
            var fileName = requestUrl.Substring(requestUrl.LastIndexOf('/') + 1);
            if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                return 0;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(FilePath);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            var fileNameList = new List<string>();
            foreach (var item in fileInfos)
            {
                fileNameList.Add(item.Name);
            }
            if (!fileNameList.Contains(fileName))
            {
                if (string.IsNullOrEmpty(fileName))
                    fileName = Guid.NewGuid().ToString();
                var buffer = await httpResponseMessage.Content.ReadAsByteArrayAsync();
                var fileExpansion = IntegrateImageFileName(buffer, fileName);
                using (FileStream fs = new FileStream(FilePath + fileExpansion, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(buffer, 0, buffer.Length);
                    fs.Close();
                }
                return 1;
            }
            else
            {
                return 2;
            }

        }
        private static void HelpMe()
        {
            string example = string.Format("用法：[{0}] [{1}] [{2}]", "地址", "下载量", "保存路径");
            Console.WriteLine(example);
        }
        /// <summary>
        /// 获取版本
        /// </summary>
        /// <returns></returns>
        private static string GetVersion()
        {
            return Configs.GetVersion();
        }
        /// <summary>
        /// 单一命令处理
        /// </summary>
        /// <param name="args">命令参数</param>
        private static void SingleArgs(string args)
        {

            switch (args)
            {
                case "-help":
                    HelpMe();
                    break;
                case "-version":
                    GetVersion();
                    break;
                default:
                    HelpMe();
                    break;
            }

        }

        private static void ArgsError()
        {
            string argsError = "您输入的命令有误，请输入-help查看帮助";
            Console.WriteLine(argsError);
        }

        private static int TryParesInt(string tryNumber)
        {
            if (int.TryParse(tryNumber, out int result))
                return result;
            else
                return 0;
        }
        private static string FormatPath(string path)
        {
            path = path.Trim();
            if (path[path.Length - 1] != '\\')
                return path + '\\';
            else
                return path;
        }
        /// <summary>
        /// 检查文件是否为图片
        /// </summary>
        /// <param name="buffer">16进制文件编码字节组</param>
        /// <param name="fileName">文件名</param>
        /// <returns></returns>
        private static string IntegrateImageFileName(byte[] buffer, string fileName)
        {
            List<string> pictureExpansion = new List<string>() { "jpg", "png", "gif" };
            if (fileName.Contains('.') || pictureExpansion.Contains(fileName.Split('.')[1]))
            {
                return fileName;
            }
            var hexStart = BitConverter.ToString(buffer).Substring(0, 8);
            string result = null;
            switch (hexStart)
            {
                case "FF-D8-FF":
                    result = pictureExpansion[0];
                    break;
                case "89-50-4E":
                    result = pictureExpansion[1];
                    break;
                case "47-49-46":
                    result = pictureExpansion[2];
                    break;
            }
            result = fileName + "." + result;
            return result;
        }
    }
}

