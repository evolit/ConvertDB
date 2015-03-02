﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DataTable = System.Data.DataTable;
using Excel = Microsoft.Office.Interop.Excel;

namespace Converter
{
    class FillDataTable
    {
        [Obsolete("Метод не готов", true)]
        public static DataTable GetDataTable(string fileName, string sheetName)
        {
            DataTable table =GetDataTable(fileName,sheetName as object);

            return table;
        }
        [Obsolete("Метод не готов", true)]
        public static DataTable GetDataTable(string fileName, int sheetIndex)
        {
            DataTable table = GetDataTable(fileName, sheetIndex as object);

            return table;
        }

        public static DataTable GetDataTable(string fileName, string sheetName,int takeFirstItemsQuantity)
        {

            DataTable newTable = null; //new DataTable();
            var toClose = false;
            
            Excel.Application xlApplication = ExcelApp.GetExcelApplication();
            var workbookName = Path.GetFileName(fileName);
            Excel.Workbook workbook = xlApplication.Workbooks.Cast<Excel.Workbook>()
                .FirstOrDefault(wb => wb.Name == workbookName);
            if (workbook==null)
            {
                Process.Start(fileName);
                toClose = true;
                workbook = xlApplication.Workbooks.Cast<Excel.Workbook>()
                .FirstOrDefault(wb => wb.Name == workbookName);
                if (workbook == null)
                    throw new Exception("Конфликт запущенных процессов Excel. Закройте все процессы Excel и повторите попытку");
            }
            Excel.Worksheet worksheet = workbook.Worksheets[sheetName];

            newTable = worksheet.GetDataTableFromWorksheet();

            if (toClose)
            {
                workbook.Close(0);
                Marshal.ReleaseComObject(workbook);
                Marshal.ReleaseComObject(worksheet);
            }
            return newTable;
        }

        [Obsolete("Метод не готов", true)]
        public static DataTable GetDataTable(string fileName, int sheetIndex, int takeFirstItemsQuantity)
        {

            DataTable dataTable = GetDataTable(fileName, sheetIndex as object);

            DataTable newTable = dataTable.Clone();
            for (int i = 1; i < takeFirstItemsQuantity; i++)
            {
                newTable.ImportRow(dataTable.Rows[i - 1]);
            }

            return newTable;
        }
        [Obsolete("Метод не готов",false)]
        private static DataTable GetDataTable(string fileName, object sheetIndexOrName)
        {


            const string csvExtension = ".csv";
            DataTable dataTable;

//            var xlApplivation = (Excel.Application)Marshal.GetActiveObject("Excel.Application");


//            //Get from *.csv
//            if (1 != 1 && Path.GetExtension(fileName) == csvExtension)
//                dataTable = CSVReader.GetDataTableFromCsvFile(fileName);
//            else
//            {
//                //OR get from *.xls
//                var wbName = Path.GetFileName(fileName);
//                Process xlProcess = ExcelExtensions.GetExcelProcess(xlApplivation);
//                xlProcess.StartInfo = new ProcessStartInfo(fileName);
//                xlProcess.Start();
////                Process.Start(fileName);
//                xlProcess.StartInfo = new ProcessStartInfo("d:\\3.csv");
//                xlProcess.Start();
//                Excel.Workbook workbook = xlApplivation.Workbooks[wbName]; //xlApplivation.Workbooks.Open(fileName);
//                Excel.Worksheet workSheet;
//                if (sheetIndexOrName == null)
//                    return null;
//
//                if (sheetIndexOrName is string)
//                {
//                    var path = (sheetIndexOrName as string);
//                    workSheet = workbook.Worksheets[path];
//                }
//                else if (sheetIndexOrName is sbyte
//                         || sheetIndexOrName is byte
//                         || sheetIndexOrName is short
//                         || sheetIndexOrName is ushort
//                         || sheetIndexOrName is int
//                         || sheetIndexOrName is uint
//                         || sheetIndexOrName is long
//                         || sheetIndexOrName is ulong
//                         || sheetIndexOrName is float
//                         || sheetIndexOrName is double
//                         || sheetIndexOrName is decimal)
//                {
//                    var index = 0;
//                    int.TryParse(sheetIndexOrName.ToString(), out index);
//                    workSheet = workbook.Worksheets[index];
//                }
//                else return null;
//
//                dataTable = workSheet.GetDataTableFromWorksheet();
//
//                workbook.Close();
////                xlApplivation.Quit();
//            }
            return null;// dataTable;
        }

        [Obsolete("Метод не готов", true)]
        static string[] GetExcelSheetNames(string connectionString)
        {
            var xlApplivation = new Excel.Application();

            Process.Start(connectionString);

            Excel.Workbook workbook = xlApplivation.Workbooks[Path.GetFileName(connectionString)];
            var excelSheetNames = workbook.Worksheets.Cast<Excel.Worksheet>().Select(ws => ws.Name).ToArray();
            workbook.Close();
            xlApplivation.Quit();

            return excelSheetNames;
        }

        public static string GetConnectionString(string filePath,bool excelIsAbove2003 = false)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();

            if (excelIsAbove2003)
            {
                // XLSX - Excel 2007, 2010, 2012, 2013
                if (IntPtr.Size == 4) //x32
                    props["Provider"] = "Microsoft.ACE.OLEDB.12.0";
                else if (IntPtr.Size == 8) //x64
                    props["Provider"] = "Microsoft.Jet.OLEDB.4.0";
                else 
                    throw new Exception("Неизвестная платфора, драйрера для OleDBConnection не будут найдены");

                props["Provider"] = "Microsoft.ACE.OLEDB.12.0";
                props["Extended Properties"] = "\"" + "Excel 12.0 XML;HDR=YES;" + "\"";
                props["Data Source"] = "\"" + filePath + "\"";
 
            }
            else
            {
                // XLS - Excel 2003 and Older
                props["Provider"] = "Microsoft.Jet.OLEDB.4.0";
                props["Extended Properties"] = "\"" + "Excel 8.0;\""; //HDR=YES;" + "\"";
                props["Data Source"] = filePath;    
            }
            

            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, string> prop in props)
            {
                sb.Append(prop.Key);
                sb.Append('=');
                sb.Append(prop.Value);
                sb.Append(';');
            }

            return sb.ToString();
        }
    }
}
