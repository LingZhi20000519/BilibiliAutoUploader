using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace BilibiliAutoUploader
{
    // 用来存放Cookie信息的类
    public class CookieClass
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Domain { get; set; }
        public string Path { get; set; }
        public DateTime? Expiry { get; set; }
        public bool Secure { get; set; }
        public bool IsHttpOnly { get; set; }
        public string SameSite { get; set; }
    }

    public class BilibiliUploadVideoClass
    {
        public string VideoName { get; set; }
        public string VideoFilePath { get; set; }
        public string VideoCategory { get; set; }
        public string VideoTag { get; set; }
        public string VideoSpecialTag { get; set; }
        public DateTime VideoPublishTime { get; set; }
        public string VideoCollection { get; set; }
    }


    internal class Program
    {
        static void Main(string[] args)
        {
            //SaveBilibiliCookies();
            CreateUploadJson();
            UploadVideos();
        }

        // 手动登录bilibili并保存Cookies
        public static void SaveBilibiliCookies()
        {
            IWebDriver driver = new EdgeDriver();
            try
            {
                // 导航到B站登录页面
                driver.Navigate().GoToUrl("https://passport.bilibili.com/login");

                // 等待用户手动登录（可以添加显式等待逻辑）
                Console.WriteLine("请手动登录，登录成功后按回车继续...");
                Console.ReadLine();

                // 获取所有Cookies
                var cookies = driver.Manage().Cookies.AllCookies;

                // 转换为 CookiesClass 并保存
                var cookiesClass = new List<CookieClass>();
                foreach (var cookie in cookies)
                {
                    cookiesClass.Add(new CookieClass
                    {
                        Name = cookie.Name,
                        Value = cookie.Value,
                        Domain = cookie.Domain,
                        Path = cookie.Path,
                        Expiry = cookie.Expiry,
                        Secure = cookie.Secure,
                        IsHttpOnly = cookie.IsHttpOnly,
                        SameSite = cookie.SameSite
                    });
                }

                // 序列化并保存Cookies到文件
                var json = JsonSerializer.Serialize(cookiesClass);
                File.WriteAllText("bilibili_cookies.json", json);
            }
            finally
            {
                driver.Quit();
            }
        }

        public static void CreateUploadJson()
        {
            string videoFolderPath = @"D:\File\米哈喵呀\视频\尚未发布\";
            string[] videoFilePaths = Directory.GetFiles(videoFolderPath, "*.mp4", SearchOption.TopDirectoryOnly);
            DateTime startDate = DateTime.Today;
            DateTime startDateTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, 19, 0, 0);

            List<BilibiliUploadVideoClass> bilibiliUploadVideoClassList = new List<BilibiliUploadVideoClass>();

            foreach (var videoFilePath in videoFilePaths) 
            {
                // 例如 @"D:\File\米哈喵呀\视频\尚未发布\鸣潮\秧秧美图_10_20250705_142840.mp4"; 取得最后一个'\'前，第一个'_'后的秧秧美图作为视频标题
                string videoName = videoFilePath.Substring(videoFilePath.LastIndexOf('\\') + 1).Substring(0, videoFilePath.Substring(videoFilePath.LastIndexOf('\\') + 1).IndexOf('_'));
                videoName = videoName + "(" + startDateTime.ToString("yyyyMMdd") + ")" + startDateTime.Hour.ToString();

                BilibiliUploadVideoClass bilibiliUploadVideoClass = new BilibiliUploadVideoClass();
                bilibiliUploadVideoClass.VideoName = videoName;
                bilibiliUploadVideoClass.VideoFilePath = videoFilePath;
                bilibiliUploadVideoClass.VideoCategory = "游戏";
                bilibiliUploadVideoClass.VideoTag = "鸣潮;AI绘图;二次元;美女";
                bilibiliUploadVideoClass.VideoSpecialTag = "鸣潮2.5版本二创";
                bilibiliUploadVideoClass.VideoPublishTime = startDateTime;
                bilibiliUploadVideoClass.VideoCollection = "鸣潮";

                bilibiliUploadVideoClassList.Add(bilibiliUploadVideoClass);

                startDateTime = startDateTime.AddDays(5);
            }

            // 序列化并保存Cookies到文件
            var json = JsonSerializer.Serialize(bilibiliUploadVideoClassList);
            File.WriteAllText("bilibiliUploadVideoClass.json", json);
        }

        public static void UploadVideos()
        {
            string bilibiliUploadVideoClassJson = File.ReadAllText("bilibiliUploadVideoClass.json");
            List<BilibiliUploadVideoClass> bilibiliUploadVideoClassList =JsonSerializer.Deserialize<List<BilibiliUploadVideoClass>>(bilibiliUploadVideoClassJson);

            foreach (BilibiliUploadVideoClass bilibiliUploadVideoClass in bilibiliUploadVideoClassList)
            {
                IWebDriver driver = new EdgeDriver();
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
                try
                {
                    // 从文件加载Cookies
                    var json = File.ReadAllText("bilibili_cookies.json");
                    var cookieClass = JsonSerializer.Deserialize<List<CookieClass>>(json);

                    // 先导航到任何B站页面以设置正确的域名上下文
                    driver.Navigate().GoToUrl("https://www.bilibili.com");

                    foreach (var dto in cookieClass)
                    {
                        Cookie cookie = new Cookie(dto.Name, dto.Value, dto.Domain, dto.Path, dto.Expiry, dto.IsHttpOnly, dto.Secure, dto.SameSite);
                        driver.Manage().Cookies.AddCookie(cookie);
                    }

                    // 刷新页面或导航到需要登录才能访问的页面
                    driver.Navigate().Refresh(); // 或者直接导航到特定页面
                    driver.Navigate().GoToUrl("https://member.bilibili.com/platform/upload/video");

                    Thread.Sleep(5000);
                    // 找到input标签，上传视频路径
                    IWebElement fileInput = wait.Until(d =>d.FindElement(By.CssSelector("input[type='file']")));
                    string videoFilePath = bilibiliUploadVideoClass.VideoFilePath;
                    fileInput.SendKeys(videoFilePath);
                    Thread.Sleep(5000);

                    // 输入标题
                    IWebElement titleInput = driver.FindElement(By.XPath("//input[@placeholder='请输入稿件标题']"));
                    titleInput.Clear();
                    string newTitle = bilibiliUploadVideoClass.VideoName;
                    titleInput.SendKeys(newTitle);

                    // 选择分区
                    IWebElement CategoryChoose = driver.FindElement(By.CssSelector("div.select-controller"));
                    CategoryChoose.Click();
                    IWebElement CategoryInput = driver.FindElements(By.CssSelector($"div.drop-list-v2-item[title='{bilibiliUploadVideoClass.VideoCategory}']")).FirstOrDefault();
                    CategoryInput.Click();

                    // 输入活动话题
                    Thread.Sleep(5000);
                    IWebElement tagMore = wait.Until(driver => driver.FindElement(By.CssSelector("div.tag-more"))); ;
                    tagMore.Click();
                    IWebElement tagSearch = driver.FindElement(By.XPath("//input[@placeholder='请输入']"));
                    tagSearch.SendKeys(bilibiliUploadVideoClass.VideoSpecialTag);
                    Thread.Sleep(1000);
                    IWebElement tagSearchInput = driver.FindElement(By.XPath($"//div[contains(@class, 'topic-tag-name') and normalize-space(text())='{bilibiliUploadVideoClass.VideoSpecialTag}']/ancestor::div[contains(@class, 'dialog-item')]"));
                    tagSearchInput.Click();
                    IWebElement buttonSpecialTagInput = driver.FindElement(By.CssSelector($"button.bcc-button.submit-add"));
                    buttonSpecialTagInput.Click();
                    Thread.Sleep(1000);

                    // 输入标签
                    IWebElement tagInput = driver.FindElement(By.XPath("//input[@placeholder='按回车键Enter创建标签']"));
                    List<string> tags = bilibiliUploadVideoClass.VideoTag.Split(';').ToList();
                    foreach (var tag in tags)
                    {
                        tagInput.SendKeys(tag + '\n');
                        Thread.Sleep(500);
                    }

                    // 点击定时发布
                    IWebElement switchInput = driver.FindElement(By.CssSelector("div.switch-container"));
                    switchInput.Click();

                    // 输入日期
                    IWebElement datePick = driver.FindElement(By.CssSelector("div.date-picker-date"));
                    datePick.Click();
                    // 获得默认年月例如"2025年7月"
                    IWebElement yearMonth = driver.FindElement(By.CssSelector("p.date-picker-nav-title"));
                    string yearMonthText = yearMonth.Text;
                    // 提取年份
                    int yearIndex = yearMonthText.IndexOf("年");
                    int monthIndex = yearMonthText.IndexOf("月");
                    string yearStr = yearMonthText.Substring(0, yearIndex);
                    string monthStr = yearMonthText.Substring(yearIndex + 1, monthIndex - (yearIndex + 1));
                    int year = int.Parse(yearStr);
                    int month = int.Parse(monthStr);
                    if (bilibiliUploadVideoClass.VideoPublishTime.Year > year ||bilibiliUploadVideoClass.VideoPublishTime.Month > month)
                    {
                        IWebElement nextMouth = driver.FindElement(By.CssSelector(".next-btn-month"));
                        nextMouth.Click();
                    }
                    // 选择日子
                    IWebElement dateInput = driver.FindElement(By.XPath($"//div[contains(@class, 'date-picker-body-item') and contains(@class, 'date-item') and text()=' {bilibiliUploadVideoClass.VideoPublishTime.Day.ToString()} ']"));
                    dateInput.Click();

                    // 输入时间
                    IWebElement timePick = driver.FindElement(By.CssSelector(".date-picker-timer"));
                    timePick.Click();
                    // 输入小时
                    IWebElement hourInput = driver.FindElements(By.XPath($"//span[@class='time-picker-panel-select-item' and text()='{bilibiliUploadVideoClass.VideoPublishTime.Hour.ToString("00")}']")).FirstOrDefault();
                    hourInput.Click();
                    // 输入分钟
                    IWebElement minuteInput = driver.FindElements(By.XPath($"//span[@class='time-picker-panel-select-item' and text()='{bilibiliUploadVideoClass.VideoPublishTime.Minute.ToString("00")}']")).LastOrDefault();
                    minuteInput.Click();

                    // 输入合集
                    IWebElement collectionPick = driver.FindElement(By.CssSelector("div.season-enter"));
                    collectionPick.Click();
                    //Thread.Sleep(500);
                    IWebElement collectionInput = driver.FindElement(By.XPath($"//p[contains(@class, 'season-item-title') and text()='{bilibiliUploadVideoClass.VideoCollection}']/.."));
                    ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", collectionInput);
                    //collectionInput.Click();

                    // 点击投稿
                    IWebElement submitButton = driver.FindElement(By.CssSelector("span.submit-add"));
                    submitButton.Click();
                    Thread.Sleep(1000);

                    Console.WriteLine($"{bilibiliUploadVideoClass.VideoName}已经成功上传！");

                }
                finally
                {
                    // 关闭浏览器
                    driver.Quit();
                }
            }


        }
    }
}
