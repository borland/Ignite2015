using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication
{
    public static class TableHelpers
    {
        class TableBuilder<T>
        {
            struct ColumnDefinition
            {
                public string Title;
                public Func<T, IHtmlString> Generator;
            }

            List<ColumnDefinition> columns = new List<ColumnDefinition>();

            public void Column(string title, Func<T, string> contents)
            {
                columns.Add(new ColumnDefinition {
                    Title = title,
                    Generator = contents;
                });
            }

            public void Column(string title, Func<T, IHtmlString> unsafeContents)
            {
                columns.Add(new ColumnDefinition
                {
                    Title = title,
                    Generator = unsafeContents
                });
            }
        }

        public static IHtmlString DataTable<T>(IEnumerable<T> model, Action<TableBuilder<T>> builder)
        {

        }
    }
}