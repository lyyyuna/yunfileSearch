using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Net.Http;
using HtmlAgilityPack;
using System.Threading.Tasks;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace yunfileSearch.Views
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        public ObservableCollection<YunRes> Files;
        public SearchPage()
        {
            this.InitializeComponent();
            Files = new ObservableCollection<YunRes>();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var Keyword = SearchTextBox.Text;
            Files.Clear();
            await getQingZhou(Keyword, 1);
            await getQingZhou(Keyword, 2);
        }

        private async Task getQingZhou(string SearchKeyword, int pageno)
        {
            string keyword = Uri.EscapeDataString(SearchKeyword);

            HttpClient client = new HttpClient();
            var result = await client.GetAsync("https://www.qzhou.com.cn/search?ft=&stime=&size=&order=&p=" + pageno + "&keyword=" + keyword);
            var html = await result.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var ResultBlock = doc.DocumentNode.
                        Descendants("ul")
                        .Where(p => p.GetAttributeValue("class", "") == "result-list").First();
            var lists = ResultBlock.Descendants("li").ToList();

            foreach (var hnode in lists)
            {
                var s = hnode.Descendants("p")
                        .Where(p => p.GetAttributeValue("class", "") == "result-title").ToList();
                if (s.Count() == 0)
                    continue;
                var SearchItem = s[0].Descendants("a").First();
                //var SearchItem = hnode.SelectSingleNode(".//p[@class='result-title']/a");
                if (SearchItem == null)
                    continue;
                var title = SearchItem.InnerText;
                var url = SearchItem.Attributes["href"].Value;

                var spans = hnode.Descendants("span").ToList();
                if (spans == null)
                    continue;
                var time = spans[1].InnerText;
                var format = spans[2].InnerText;
                var size = spans[3].InnerText;

                var file = new YunRes();
                file.title = title;
                file.detail = url;
                file.time = time;
                file.format = format;
                file.size = size;

                Files.Add(file);
            }
        }

        private async void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var file = e.ClickedItem as YunRes;
            var uri = new Uri(file.detail);
            await Windows.System.Launcher.LaunchUriAsync(uri);

        }
    }

    public class YunRes
    {
        public string title { get; set; }
        public string detail { get; set; }
        public string time { get; set; }
        public string format { get; set; }
        public string size { get; set; }
    }
}
