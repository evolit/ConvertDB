using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using OfficeOpenXml;

namespace Formater.SupportWorksheetsClasses
{
    public  class CatalogueRepository:IDisposable
    {
        private System.Data.DataTable table;
        private const byte CodeColumnIndex = 2;
        private const byte NameColumnIndex = 3;
        private const byte ContentColumnIndex = 5;

        public CatalogueRepository(System.Data.DataTable table)
        {
            this.table = table;
        }

        public List<string> GetContentByCode(string code)
        {
            var result = table.Rows.Cast<DataRow>().Where(r => Equals(r[CodeColumnIndex - 1], code))
                .Select(r => (r[ContentColumnIndex - 1]??"").ToString()).ToList();

            return result;
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed) return;
            table.Dispose();
            disposed = true;
        }
    }
}