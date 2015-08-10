// This is free and unencumbered software released into the public domain.
// Anyone is free to copy, modify, publish, use, compile, sell, or
// distribute this software, either in source code form or as a compiled
// binary, for any purpose, commercial or non-commercial, and by any
// means.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WebApplication
{
    static class TapExt
    {
        public static T Tap<T>(this T obj, Action<T> action)
        {
            action(obj);
            return obj;
        }
    }

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

            public TableBuilder<T> Column(string title, Func<T, string> contents) => 
                Column(title, x => new MvcHtmlString(html.Encode(contents(x))));

            public TableBuilder<T> Column(string title, Func<T, IHtmlString> unsafeContents)
            {
                Columns.Add(new ColumnDefinition {
                    Title = title,
                    Generator = unsafeContents
                });
                return this;
            }
        }

        public static IHtmlString DataTable<T>(this HtmlHelper html, IEnumerable<T> model, Action<TableBuilder<T>> builder, object htmlAttributes = null)
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


            var attrs = htmlAttributes.Unwrap(h => HtmlHelper.AnonymousObjectToHtmlAttributes(h));

            return MvcHtmlString.Create(new TagBuilder("table") {
                InnerHtml = string.Join("", header, rows)
            }.Tap(tag => attrs.Unwrap(a => tag.MergeAttributes(a))).ToString());
        }
    }
}