using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace EzPDF
{
    public class HtmlRendererOptions
    {
        public Func<Page, Task> RunBeforePageLoad { get; set; }

        public Func<Page, Task> RunBeforePdf { get; set; }

        public Func<Page, Task> RunAfterPdf { get; set; }
        
        public Dictionary<string, string> RequestHeaders { get; set; }
    }
}