using System.Threading.Tasks;
using PuppeteerSharp;

namespace EzPDF
{
    public class HtmlRenderer
    {
        private Browser Browser { get; set; }

        private async Task InitializeBrowser()
        {
            if (Browser == null)
            {
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                Browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true
                });
            }
        }
        
        public async Task<byte[]> Render(string html)
        {
            await InitializeBrowser();
            var page = await Browser.NewPageAsync();
            await page.SetContentAsync(html);
            var pdfData = await page.PdfDataAsync();
            await page.CloseAsync();
            return pdfData;
        }
    }
}
