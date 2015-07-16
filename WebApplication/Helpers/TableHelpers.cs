using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WebApplication
{
    public static class TableHelpers
    {
        public class TableBuilder<T>
        {
            readonly HtmlHelper html;
            public TableBuilder(HtmlHelper html)
            {
                this.html = html;
            }

            internal struct ColumnDefinition
            {
                public string Title;
                public Func<T, IHtmlString> Generator;
            }

            internal List<ColumnDefinition> Columns { get; } = new List<ColumnDefinition>();

            public void Column(string title, Func<T, string> contents) => 
                Column(title, x => new MvcHtmlString(html.Encode(contents(x))));

            public void Column(string title, Func<T, IHtmlString> unsafeContents)
            {
                Columns.Add(new ColumnDefinition {
                    Title = title,
                    Generator = unsafeContents
                });
            }
        }

        public static IHtmlString DataTable<T>(this HtmlHelper html, IEnumerable<T> model, Action<TableBuilder<T>> builder)
        {
            var tb = new TableBuilder<T>(html);
            builder(tb);

            var header = new TagBuilder("tr") {
                InnerHtml = String.Join("",
                    tb.Columns.Select(c => new TagBuilder("th") { InnerHtml = html.Encode(c.Title) }))
            }.ToString();

            var rows = String.Join("", model.Select(m => 
                new TagBuilder("tr") {
                    InnerHtml = String.Join("",
                        tb.Columns.Select(c => new TagBuilder("td") { InnerHtml = c.Generator(m).ToString() }.ToString()))
                }.ToString()));

            return MvcHtmlString.Create(new TagBuilder("table") {
                Attributes = { ["border"] = "1" },
                InnerHtml = string.Join("", header, rows)
            }.ToString());
        }
    }
}