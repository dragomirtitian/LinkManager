using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkManager
{
    public class HtmlHelper
    {
        public static string ParseHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return html;
        }
    }
}
