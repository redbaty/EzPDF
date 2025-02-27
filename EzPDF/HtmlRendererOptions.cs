using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace EzPDF
{
    public class HtmlRendererOptions
    {
        public Func<IPage, Task>? RunBeforePageLoad { get; set; }

        public Func<IPage, Task>? RunBeforePdf { get; set; }

        public Func<IPage, Task>? RunAfterPdf { get; set; }
        
        public Dictionary<string, string>? RequestHeaders { get; set; }
    }
}