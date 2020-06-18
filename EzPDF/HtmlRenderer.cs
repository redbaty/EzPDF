using System;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace EzPDF
{
    public class HtmlRenderer
    {
        public HtmlRenderer()
        {
        }

        public HtmlRenderer(Func<LaunchOptions, Task> configureLaunchOptions)
        {
            ConfigureLaunchOptions = configureLaunchOptions;
        }

        private Browser Browser { get; set; }

        private Func<LaunchOptions, Task> ConfigureLaunchOptions { get; }

        private async Task InitializeBrowser()
        {
            if (Browser == null)
            {
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                var launchOptions = new LaunchOptions
                {
                    Headless = true
                };

                if (ConfigureLaunchOptions != null) await ConfigureLaunchOptions.Invoke(launchOptions);
                Browser = await Puppeteer.LaunchAsync(launchOptions);
            }
        }

        public async Task<byte[]> RenderHtml(string html, HtmlRendererOptions htmlOptions = null,
                                             PdfOptions pdfOptions = null)
        {
            await InitializeBrowser();
            var page = await Browser.NewPageAsync();
            await (htmlOptions?.RunBeforePageLoad?.Invoke(page) ?? Task.CompletedTask);
            await page.SetContentAsync(html);
            await (htmlOptions?.RunBeforePdf?.Invoke(page) ?? Task.CompletedTask);
            var pdfData = pdfOptions == null ? await page.PdfDataAsync() : await page.PdfDataAsync(pdfOptions);
            await (htmlOptions?.RunAfterPdf?.Invoke(page) ?? Task.CompletedTask);
            await page.CloseAsync();
            await page.DisposeAsync();
            return pdfData;
        }

        public async Task<byte[]> RenderUrl(string url, HtmlRendererOptions htmlOptions = null,
                                            PdfOptions pdfOptions = null)
        {
            void OnPageRequest(object sender, RequestEventArgs args)
            {
                if (args.Request.IsNavigationRequest)
                {
                    foreach (var (key, value) in htmlOptions.RequestHeaders)
                    {
                        args.Request.Headers.Add(key, value);
                    }
                }
            }

            await InitializeBrowser();
            var page = await Browser.NewPageAsync();

            if (htmlOptions?.RequestHeaders != null && htmlOptions.RequestHeaders.Count > 0)
            {
                await page.SetRequestInterceptionAsync(true);
                page.Request += OnPageRequest;
            }

            var pdfData = await RunInternal(url, htmlOptions, pdfOptions, page).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    return (object) t.Result;
                }

                return t.Exception;
            });

            await page.CloseAsync();
            await page.DisposeAsync();

            if (htmlOptions?.RequestHeaders != null && htmlOptions.RequestHeaders.Count > 0)
            {
                page.Request -= OnPageRequest;
            }

            if (pdfData is byte[] result)
                return result;

            if (pdfData is Exception ex)
                throw ex;
            
            throw new InvalidOperationException("Something went terribly wrong.");
        }

        private static async Task<byte[]> RunInternal(string url, HtmlRendererOptions htmlOptions,
                                                      PdfOptions pdfOptions, Page page)
        {
            await (htmlOptions?.RunBeforePageLoad?.Invoke(page) ?? Task.CompletedTask);
            await page.GoToAsync(url);
            await (htmlOptions?.RunBeforePdf?.Invoke(page) ?? Task.CompletedTask);
            var pdfData = pdfOptions == null ? await page.PdfDataAsync() : await page.PdfDataAsync(pdfOptions);
            await (htmlOptions?.RunAfterPdf?.Invoke(page) ?? Task.CompletedTask);
            return pdfData;
        }
    }
}