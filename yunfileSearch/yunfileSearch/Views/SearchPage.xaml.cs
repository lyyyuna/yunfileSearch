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
using Microsoft.Toolkit.Uwp;
using System.Threading;
using System.Text.RegularExpressions;
using Windows.UI.Popups;

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

        private async Task getQingZhou(string SearchKeyword, int pageno)
        {
            string keyword = Uri.EscapeDataString(SearchKeyword);

            HttpClient client = new HttpClient();
            //client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
            //client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");
            //client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "zh-CN,zh;q=0.8");
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
                var h = hnode.Descendants("p")
                            .Where(p => p.GetAttributeValue("class", "") == "result-path").ToList();
                if (h.Count() == 0)
                    continue;
                var path = h[0].InnerText;

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
                file.path = path;

                Files.Add(file);
            }
        }

        private async void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var file = e.ClickedItem as YunRes;
            var uri = new Uri(file.detail.Replace("detail", "redirect"));

            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");
                var result = await client.GetAsync(uri);
                var html = await result.Content.ReadAsStringAsync();
                var match = Regex.Match(html, "\"(http.+)\"");
                var RawUrl = match.Groups[1].Value;
                RawUrl = RawUrl.Replace("\\x26", "&");
                RawUrl = RawUrl.Replace("\\", "");
                
                await Windows.System.Launcher.LaunchUriAsync(new Uri(RawUrl));
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("发生网络错误。若重复出现，联系作者修复。");
                await dialog.ShowAsync();
            }

        }



        private async void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var Keyword = SearchAutoSuggestBox.Text;
            Files.Clear();

            string keywordUpload = Uri.EscapeDataString(Keyword);
            HttpClient client = new HttpClient();
            try
            {
                var result = await client.GetAsync("http://hhuui.lihulab.net/" + keywordUpload);
                await result.Content.ReadAsStringAsync();
            }
            catch
            {

            }

            LoadingControl.IsLoading = true;
            try { 
                await getQingZhou(Keyword, 1);
            }
            catch (Exception ex)
            {

            }
            await Task.Delay(1000);
            LoadingControl.IsLoading = false;

            try
            {
                await getQingZhou(Keyword, 2);
            }
            catch (Exception ex)
            {

            }
            await Task.Delay(1000);

            try
            {
                await getQingZhou(Keyword, 3);
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog("发生网络错误。若重复出现，联系作者修复。");
                await dialog.ShowAsync();
            }

            if (Files.Count() == 0)
            {
                var dialog = new MessageDialog("没有搜索结果，请更换搜索词。");
                await dialog.ShowAsync();
            }
        }

    }

    public class YunRes
    {
        public string title { get; set; }
        public string detail { get; set; }
        public string time { get; set; }
        public string format { get; set; }
        public string size { get; set; }
        public string path { get; set; }
    }
}
