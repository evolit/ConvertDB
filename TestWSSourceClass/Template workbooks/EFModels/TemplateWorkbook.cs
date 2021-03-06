﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Converter.Template_workbooks.EFModels
{
    public class TemplateWorkbook
    {
        public TemplateWorkbook()
        {
            Columns = new List<TemplateColumn>();
        }

        public int Id { get; set; }

        [NotMapped]
        public string Name
        {
            get { return "SuperName"; }
        }

        public XlTemplateWorkbookType WorkbookType { get; set; }
        public virtual List<TemplateColumn> Columns { get; set; }
    }

    public class TemplateColumn
    {
        public TemplateColumn()
        {
            SearchCritetias = new List<SearchCritetia>();
            BindedColumns = new List<BindedColumn>();
            TemplateWorkbooks = new List<TemplateWorkbook>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string CodeName { get; set; }
        public int ColumnIndex { get; set; }
        public virtual List<TemplateWorkbook> TemplateWorkbooks { get; set; }

        /// <summary>
        ///     Паттерны поиска для подходящих колонок
        /// </summary>
        public virtual List<SearchCritetia> SearchCritetias { get; set; }
        /// <summary>
        ///     Полное название сопоставимых колонок
        /// </summary>
        public virtual List<BindedColumn> BindedColumns { get; set; }
    }

    public class BindedColumn
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SearchCritetia
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }
}