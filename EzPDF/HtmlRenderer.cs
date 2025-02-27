using System;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace EzPDF
{
    public sealed class HtmlRenderer : IDisposable, IAsyncDisposable
    {
        public HtmlRenderer()
        {
        }

        public HtmlRenderer(Func<LaunchOptions, Task> configureLaunchOptions)
        {
            ConfigureLaunchOptions = configureLaunchOptions;
        }

        private IBrowser? Browser { get; set; }

        private Func<LaunchOptions, Task>? ConfigureLaunchOptions { get; }

        public async ValueTask<bool?> HealthCheck(Uri? testUri)
        {
            if (Browser == null)
            {
                return null;
            }

            try
            {
                await using var newPage = await Browser.NewPageAsync()
                    .WaitAsync(TimeSpan.FromSeconds(5));

                if (testUri != null)
                {
                    await newPage.GoToAsync(testUri.ToString());
                }

                await newPage.CloseAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async ValueTask InitializeBrowser()
        {
            if (Browser == null)
            {
                var launchOptions = new LaunchOptions();
                if (ConfigureLaunchOptions != null) await ConfigureLaunchOptions.Invoke(launchOptions);

                var browserFetcher = new BrowserFetcher
                {
                    Browser = SupportedBrowser.Chrome
                };
                await browserFetcher.DownloadAsync();
                Browser = await Puppeteer.LaunchAsync(launchOptions);
            }
        }

        public async ValueTask<byte[]> RenderHtml(string html, HtmlRendererOptions? htmlOptions = null,
            PdfOptions? pdfOptions = null)
        {
            await InitializeBrowser();

            if (Browser == null)
                throw new InvalidOperationException("Browser was not properly initialized.");

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

        public async ValueTask<byte[]> RenderUrl(string url, HtmlRendererOptions? htmlOptions = null,
            PdfOptions? pdfOptions = null)
        {
            async void OnPageRequest(object? sender, RequestEventArgs args)
            {
                if (args.Request.IsNavigationRequest)
                {
                    await args.Request.ContinueAsync(new Payload
                    {
                        Headers = htmlOptions.RequestHeaders
                    });
                }
                else
                {
                    await args.Request.ContinueAsync();
                }
            }

            await InitializeBrowser();
            
            if (Browser == null)
                throw new InvalidOperationException("Browser was not properly initialized.");
            
            var page = await Browser.NewPageAsync();

            if (htmlOptions?.RequestHeaders is { Count: > 0 })
            {
                await page.SetRequestInterceptionAsync(true);
                page.Request += OnPageRequest;
            }

            var pdfData = await RunInternal(url, htmlOptions, pdfOptions, page).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    return (object)t.Result;
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

        private static async Task<byte[]> RunInternal(string url, HtmlRendererOptions? htmlOptions,
            PdfOptions? pdfOptions, IPage page)
        {
            await (htmlOptions?.RunBeforePageLoad?.Invoke(page) ?? Task.CompletedTask);
            await page.GoToAsync(url);
            await (htmlOptions?.RunBeforePdf?.Invoke(page) ?? Task.CompletedTask);
            var pdfData = pdfOptions == null
                ? await page.PdfDataAsync()
                : await page.PdfDataAsync(pdfOptions);
            await (htmlOptions?.RunAfterPdf?.Invoke(page) ?? Task.CompletedTask);
            return pdfData;
        }

        public void Dispose()
        {
            Browser?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (Browser != null)
                await Browser.DisposeAsync();
        }
    }
}