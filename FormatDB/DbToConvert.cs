﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Template_workbooks;
using Converter.Template_workbooks.EFModels;
using ExcelRLibrary;
using Formater.SupportWorksheetsClasses;
using Microsoft.Office.Interop.Excel;
using NLog;
using OfficeOpenXml;
using Action = System.Action;
using DataTable = System.Data.DataTable;

namespace Formater
{
    public partial class DbToConvert
    {
        readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string NoInfoString = "не указано";
        private static int _lastUsedRow;

        private readonly List<long> rowsToDelete; //Будет использоваться, но позже
        private readonly XlTemplateWorkbookType wbType;
        private ExcelWorksheet worksheet;
        private readonly IFormatDbParams dbParams;
        private Task initWbTask;
        private SupportWorksheets supportWorksheets;
        private List<TemplateColumn> columns;


        public DbToConvert(IFormatDbParams dbParams) : this()
        {
            this.dbParams = dbParams;
            wbType = dbParams.WorkbookType;
            ReadSupportWorksheets();
            InitWorkbookAndColumns(dbParams.Path);
            CheckWorkbookHead();
        }


        public List<string> ColumnsToReserve { get; set; }
        public int HeadSize { get; set; }
        public ExcelPackage ExcelPackage { get; private set; }
        public bool DoDescription { get; set; }


        /// <summary>
        ///     Сверяет шапку рабочей книги с шаблонным порядком столбцов
        ///     Крайне важно для работы
        ///  </summary>
        /// <exception cref="ArgumentException">Когда шапка книги не совпала с</exception>
        private void CheckWorkbookHead()
        {
            Task.WaitAll(initWbTask);
            
            var i = 1;
            foreach (var templateCode in columns.Select(c => c.CodeName))
            {
                if (worksheet.Cells[1, i].Value.ToString() != templateCode)
                {
                    logger.Error("Колонка #{0} должна содержать значение \"{1}\"",i,templateCode);
                    MessageBox.Show(string.Format("Табличная шапка в листе {0} не соотвествует стандарту",
                        worksheet.Name));
                    throw new ArgumentException("Шапка переданной книги не соответствует стандарту");
                }
                i++;
            }
        }

        private void ReadSupportWorksheets()
        {
            logger.Info("Чтение вспомогательных книг");

            var supportDataSetsDict = ReadPaths();
            if (supportDataSetsDict.Any(ds => ds.Equals(null))) throw new ArgumentException("Одна или несколько книг не читаемы");

            try
            {
                var klard = new KladrRepository();

                var oktmoWs =
                    new OKTMORepository(
                        supportDataSetsDict[dbParams.OktmoSupportWorkbook.Path].Tables.Cast<DataTable>()
                            .First(t => t.TableName.Equals(dbParams.OktmoSupportWorkbook.SelectedWorksheet)), klard,
                        supportDataSetsDict[dbParams.OktmoSupportWorkbook.Path].Tables.Cast<DataTable>()
                            .FirstOrDefault(t => t.TableName.EqualNoCase("РегЦентры")));

                var soubjectSourceWorksheet =
                    new SubjectSourcesRepository(
                        supportDataSetsDict[dbParams.SubjectSourceSupportWorkbook.Path]
                            .Tables.Cast<DataTable>()
                            .First(t => t.TableName.Equals(dbParams.SubjectSourceSupportWorkbook.SelectedWorksheet)));

                var vgtWorksheet = new VGTRepository(supportDataSetsDict[dbParams.VgtCatalogSupportWorkbook.Path]
                    .Tables.Cast<DataTable>()
                    .First(t => t.TableName.Equals(dbParams.VgtCatalogSupportWorkbook.SelectedWorksheet)));

                var catalogWs = new CatalogueRepository(supportDataSetsDict[dbParams.CatalogSupportWorkbook.Path]
                    .Tables.Cast<DataTable>()
                    .First(t => t.TableName.Equals(dbParams.CatalogSupportWorkbook.SelectedWorksheet)));

                supportWorksheets = new SupportWorksheets(catalogWs, oktmoWs, soubjectSourceWorksheet, vgtWorksheet, klard);
            }
            catch (Exception)
            {
                logger.Error("Ошибка при чтении вспомогательных книг");
                throw new ArgumentException("Одна или несколько книг не читаемы");
            }

            logger.Info("Чтение прошло успешно");

            foreach (var pair in supportDataSetsDict)
            {
                pair.Value.Dispose();
            }

            GC.Collect();
        }



        /// <summary>
        ///     Общий метод, обрабатывающий все доступные для обработки столбцы
        /// </summary>
        /// <returns></returns>
        public bool FormatWorksheet()
        {
            Task.WaitAll(initWbTask);

            if (worksheet == null || supportWorksheets.OKTMOWs == null || supportWorksheets.CatalogWs == null) return false;

            FormatClassification();
            

            FormatCommunications();
            FormatAreaLot();
            FormatPrice();

            FormatOfferDeal();
            FormatOperation();
            FormatLandLaw();
            FormatSaleType();

            FormatLandCategory();

            FormatDate("DATE_RESEARCH");
            FormatDate("DATE_PARSING");
            FormatDate("DATE_IN_BASE");
            FormatBuildings();
            FormatLastUpdateDate();

            FormatSurface();
            FormatRoad();
            FormatRelief();

            FormatDistToRegCenter();

            return true;
        }

        /// <summary>
        ///     Показывает диалоговое окно для сохранения проделанной работы
        /// </summary>
        public void SaveResult()
        {
            supportWorksheets.Dispose();
            ExcelPackage.SaveWithDialog();
        }



        #region Format Methods

        /// <summary>
        /// Разбирает столбцы местоположения
        /// </summary>
        private void FormatClassification()
        {
            Stopwatch sw = null;
            var mainSw = Stopwatch.StartNew();
            var currRow = 0;
            var rows = Enumerable.Range(HeadSize + 1, _lastUsedRow);

            rows.ForEach(row =>
            {
                if (sw == null)
                    sw = Stopwatch.StartNew();
                if (currRow % 1000 == 0)
                {
                    logger.Info("1000 объектов за {0}", sw.Elapsed.ToString(@"hh\:mm\:ss"));
                    sw = Stopwatch.StartNew();
                }

                using (var dataRow = new ExcelLocationRow(worksheet, row, wbType, supportWorksheets))
                {
                    dataRow.DoDescription = DoDescription;
                    dataRow.CheckRowForLocations();
                }

                Interlocked.Increment(ref currRow);
            });

            logger.Info("Обрабочка местоположения прошла успешно.");
            logger.Info("На {0} объектов было затрачено {1}", _lastUsedRow - HeadSize, mainSw.Elapsed.ToString(@"hh\:mm\:ss"));
        }

        private void FormatRelief()
        {
            var columnIndex = GetColumnIndex("RELIEF");
            if (columnIndex == 0) return;

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null || cell.Value.ToString() == string.Empty)
                {
                    cell.Value = NoInfoString;
                    continue;
                }

                var val = cell.Value.ToString();

                if (Regex.IsMatch(val, "ровн", RegexOptions.IgnoreCase))
                {
                    cell.Value = "ровный";
                    continue;
                }
                if (Regex.IsMatch(val, @"не\sзнач", RegexOptions.IgnoreCase))
                {
                    cell.Value = "небольшой уклон";
                    continue;
                }
                if (Regex.IsMatch(val, "склон", RegexOptions.IgnoreCase))
                {
                    cell.Value = "склон";
                    continue;
                }
                if (Regex.IsMatch(val, "знач", RegexOptions.IgnoreCase))
                    cell.Value = "значительные перепады высот";
            }
        }

        private void FormatRoad()
        {
            var columnIndex = GetColumnIndex("ROAD");
            if (columnIndex == 0) return;

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null || cell.Value.ToString() == string.Empty)
                {
                    cell.Value = NoInfoString;
                    continue;
                }

                if (Regex.IsMatch(cell.Value.ToString(), "асф", RegexOptions.IgnoreCase))
                {
                    cell.Value = "асфальтовая дорога";
                    continue;
                }
                if (Regex.IsMatch(cell.Value.ToString(), "бетон", RegexOptions.IgnoreCase))
                {
                    cell.Value = "бетонка";
                    continue;
                }
                if (Regex.IsMatch(cell.Value.ToString(), "грун", RegexOptions.IgnoreCase))
                {
                    cell.Value = "грунтовая дорога";
                    continue;
                }
                if (Regex.IsMatch(cell.Value.ToString(), "грав", RegexOptions.IgnoreCase))
                    cell.Value = "гравийная дорога";
                else
                    cell.Value = NoInfoString;
            }
        }

        private void FormatSurface()
        {
            var columnIndex = GetColumnIndex("SURFACE");
            if (columnIndex == 0) return;

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null || cell.Value.ToString() == string.Empty)
                {
                    cell.Value = NoInfoString;
                    continue;
                }

                if (Regex.IsMatch(cell.Value.ToString(), "асф", RegexOptions.IgnoreCase))
                {
                    cell.Value = "асфальт";
                    continue;
                }
                if (Regex.IsMatch(cell.Value.ToString(), "бетон", RegexOptions.IgnoreCase))
                {
                    cell.Value = "бетонные плиты";
                    continue;
                }
                if (Regex.IsMatch(cell.Value.ToString(), "грун", RegexOptions.IgnoreCase))
                    cell.Value = "грунт";
                else
                    cell.Value = NoInfoString;
            }
        }

        private void FormatBuildings()
        {
            var columnIndex = GetColumnIndex("OBJECT");
            if (columnIndex == 0) return;
            var regex = new Regex("дом", RegexOptions.IgnoreCase);
            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null || cell.Value.ToString() == string.Empty)
                {
                    continue;
                }

                //Участки с домами удаляем
                if (regex.IsMatch(cell.Value.ToString()))
                {
                    rowsToDelete.Add(cell.Start.Row);
                }
                else
                    cell.Value = "да";
            }
        }

        private void FormatSaleType()
        {
            var columnIndex = GetColumnIndex("SALE_TYPE");
            if (columnIndex == 0) return;
            var lawNowColumnIndex = GetColumnIndex("LAW_NOW");
            if (lawNowColumnIndex == 0) return;

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                cell.Value = worksheet.Cells[cell.Start.Row, lawNowColumnIndex].Value.ToString() == "аренда"
                    ? "переуступка прав аренды"
                    : "продажа";
            }
        }

        private void FormatOperation()
        {
            const string columnCode = "OPERATION";
            var columnIndex = GetColumnIndex(columnCode);
            if (columnIndex == 0) return;
            var v = supportWorksheets.CatalogWs.GetContentByCode(columnCode);

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null) continue;
                if (v.Contains(cell.Value.ToString())) continue;
                cell.Value = "продажа";
            }
        }

        private void FormatOfferDeal()
        {
            var columnCode = "OFFER_DEAL";
            var columnIndex = GetColumnIndex(columnCode);
            if (columnIndex == 0) return;
            var v = supportWorksheets.CatalogWs.GetContentByCode(columnCode);

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null) continue;
                if (v.Contains(cell.Value.ToString())) continue;

                cell.Value = "предложение";
            }
        }

        private void FormatLastUpdateDate()
        {

            const string columnCode = "DATE_IN_BASE";
            var parsingColumn = GetColumnIndex("DATE_PARSING");
            var columnIndex = GetColumnIndex(columnCode);
            if (columnIndex == 0) return;

            var dateRegex = new Regex("(сегодн|(поза)?вчер)");

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null || cell.Value.ToString() == string.Empty)
                    if (cell.Value == null || cell.Value.ToString() == string.Empty)
                    {
                        cell.Value = NoInfoString;
                    }

                //Если есть дата
                var value = cell.Value.ToString();

                DateTime dt;
                if (cell.Value is DateTime || DateTime.TryParse(value, out dt)) continue;


                double u = 0;
                if (double.TryParse(value, out u))
                    dt = DateTime.FromOADate(u);
                else
                    DateTime.TryParse(value, out dt);

                Match match;
                //Когда не удалось конвертиорвать в дату
                if (dt < new DateTime(2000, 01, 01))
                {
                    var regex = new Regex(@"\d\d\.\d\d\.\d{2,4}");
                    match = regex.Match(value);
                    if (match.Success)
                    {
                        value = match.Value;
                        DateTime.TryParse(value, out dt);
                    }
                }

                //Есть всё хорошо и мы нашли нормальную дату
                if (dt > new DateTime(2000, 01, 01))
                {
                    cell.Value = dt;
                    cell.Style.Numberformat.Format = "dd.mm.yyyy";
                    continue;
                }

                //Если не нашли колонку с датой парсинга
                if (parsingColumn == 0) continue;

                //Если нет даты
                match = dateRegex.Match(value);
                if (!match.Success) continue;

                var days = 0;
                if (match.Value == "позавчер")
                    days = -2;
                if (match.Value == "сегодн")
                    days = 0;
                if (match.Value == "вчер")
                    days = -1;


                if (worksheet.Cells[cell.Start.Row, parsingColumn].Value == null) continue;

                value = worksheet.Cells[cell.Start.Row, parsingColumn].Value.ToString();
                DateTime.TryParse(value, out dt);

                if (dt < new DateTime(2000, 01, 01)) continue;
                dt = dt.AddDays(days);
                cell.Value = dt;
                cell.Style.Numberformat.Format = "dd.mm.yyyy";
            }
        }

        private void FormatDate(string columnCode)
        {
            var columnIndex = GetColumnIndex(columnCode);
            if (columnIndex == 0) return;

            var pattern = @"\d{1,2}\.\d{2}\.\d{4}";
            var reg = new Regex(pattern);

            var daysAgoPattrn = "";
            var daysAgoRegex = new Regex(daysAgoPattrn);

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null || cell.Value.ToString() == string.Empty)
                {
                    continue;
                }

                if (cell.Style.Numberformat.Format == "dd.mm.yyyy") continue;

                var value = cell.Value.ToString();
    
                //215 дней назад
                var m = daysAgoRegex.Match(value);
                if (m.Success)
                {
                    var parsingDateCell = worksheet.Cells[i, GetColumnIndex("DATE_PARSING")];
                    var parsingDate = parsingDateCell.Value as DateTime? ?? DateTime.MinValue;
                    if (parsingDate != DateTime.MinValue)
                    {
                        double days;
                        if (double.TryParse(m.Value, out days))
                        {
                            var newDate = parsingDate.AddDays(-days);
                            cell.Value = newDate;
                            continue;
                        }
                    }
                }



                m = reg.Match(value);
                if (m.Success)
                {
                    value = m.Value;
                }

                DateTime dt;
                if (DateTime.TryParse(value, out dt))
                {
                    cell.Value = dt;
                    cell.Style.Numberformat.Format = "dd.mm.yyyy";
                    continue;
                }


                double b;
                if (double.TryParse(value, out b))
                {
                    dt = DateTime.FromOADate(b);
                }
                else
                {
                    DateTime.TryParse(value, out dt);
                }
                if (dt <= new DateTime(2000, 01, 01))
                {
                    var regex = new Regex(@"\d\d\.\d\d\.\d{2,4}");
                    var match = regex.Match(cell.Value.ToString());
                    value = match.Value;
                    DateTime.TryParse(value, out dt);
                    if (dt < new DateTime(2000, 01, 01))
                    {
                        dt = TryPasreDate(cell.Value.ToString());
                        if (dt == DateTime.MinValue) continue;
                    }
                    ;
                }
                cell.Value = dt < new DateTime(2000, 01, 01) ? (dynamic) string.Empty : dt;
                cell.Style.Numberformat.Format = "dd.mm.yyyy";
            }
        }

        private DateTime TryPasreDate(string text)
        {
            var dict = new Dictionary<string, string>
            {
                {"янв", "01"},
                {"февр", "02"},
                {"март", "03"},
                {"апр", "04"},
                {"ма(й|я)", "05"},
                {"июн", "06"},
                {"июл", "07"},
                {"авг", "08"},
                {"сент", "09"},
                {"окт", "10"},
                {"нояб", "11"},
                {"дек", "12"}
            };

            foreach (var keyPair in dict)
            {
                var reg = new Regex("\\b" + keyPair.Key + "\\w*");
                if (!reg.IsMatch(text)) continue;

                text = reg.Replace(text, "." + keyPair.Value + ".");
                break;
            }

            text = text.Replace(" ", "");

            var dateReg = new Regex(@"\d\d\.\d\d\.\d{2,4}");

            var match = dateReg.Match(text);
            if (!match.Success) return DateTime.MinValue;

            return DateTime.Parse(match.Value);
        }

        private void FormatLandCategory()
        {
            var columnIndex = GetColumnIndex("LAND_CATEGORY");
            if (columnIndex == 0) return;

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null || cell.Value.ToString() == string.Empty)
                {
                    cell.Value = NoInfoString;
                    continue;
                }

                if (Regex.IsMatch(cell.Value.ToString(), "сельхо", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(cell.Value.ToString(), "с.х", RegexOptions.IgnoreCase))
                {
                    cell.Value = "Земли сельскохозяйственного назначения";
                    continue;
                }
                if (Regex.IsMatch(cell.Value.ToString(), "пром", RegexOptions.IgnoreCase))
                {
                    cell.Value = "Земли промышленности и иного назначения";
                    continue;
                }
                if (Regex.IsMatch(cell.Value.ToString(), "селен", RegexOptions.IgnoreCase))
                {
                    cell.Value = "Земли населенных пунктов";
                    continue;
                }

                //Последняя проверка и запись дефолтного значения
                var regex = new Regex(@"(охран|лесн|водн|запас)", RegexOptions.IgnoreCase);
                if (regex.IsMatch(cell.Value.ToString()))
                    rowsToDelete.Add(cell.Start.Row);
                else
                {
                    cell.Value = "Земли населенных пунктов";
                }
            }
        }

        private void FormatLandLaw()
        {
            const string code = "LAW_NOW";
            var columnIndex = GetColumnIndex(code);
            if (columnIndex == 0) return;

            var v = supportWorksheets.CatalogWs.GetContentByCode(code);
            var rentalPeriodColumnIndex = GetColumnIndex("RENTAL_PERIOD");

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];

                if (cell.Value == null || cell.Value.ToString() == string.Empty)
                {
                    cell.Value = "собственность";
                    continue;
                }

                var value = cell.Value.ToString();

                if (v.Contains(value)) continue;

                if (Regex.IsMatch(value, "аренд", RegexOptions.IgnoreCase))
                {
                    var match = new Regex(@"\d+").Match(value);
                    if (match.Success && rentalPeriodColumnIndex != 0)
                        worksheet.Cells[cell.Start.Row, rentalPeriodColumnIndex].Value =
                            string.Format("на {0} лет",
                                match.Value);
                    cell.Value = "аренда";
                }
                else
                    cell.Value = "собственность";
            }
        }

        private void FormatCommunications()
        {
            var firstColumnIndex = GetColumnIndex("SYSTEM_GAS");
            if (firstColumnIndex == 0) return;
            
            var lastColumnIndex = GetColumnIndex("HEAT_SUPPLY");
            if (lastColumnIndex == 0) return;

            var usingRange = worksheet.Cells[2, firstColumnIndex, _lastUsedRow, lastColumnIndex];

            var columnsCodeList = new List<string>
            {
                "SYSTEM_GAS",
                "SYSTEM_WATER",
                "SYSTEM_SEWERAGE",
                "SYSTEM_ELECTRICITY",
                "HEAT_SUPPLY"
            };

            var columnIndex = firstColumnIndex;
            var v = supportWorksheets.CatalogWs.GetContentByCode(columnsCodeList[0]);

            //Проверяем первый столбец(Газ) на предмет информации для соседних столбцов
            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null || cell.Value.ToString() == string.Empty)
                {
                    cell.Value = NoInfoString;
                    continue;
                }

                if (v.Contains(cell.Value.ToString())) continue;

                //IDEA а что если в первой ячейке есть "Вод" а в столбце "водоснабжение" есть "родниковая вода"
                //Прочие элементарные вараинты для Газа
                if (Regex.IsMatch(cell.Value.ToString(), @"Нет", RegexOptions.IgnoreCase) &&
                    cell.Value.ToString().Length < 6)
                {
                    cell.Value = "отсутствует, возможность подключения неизвестна";
                    continue;
                }
                if (Regex.IsMatch(cell.Value.ToString(), @"есть", RegexOptions.IgnoreCase) &&
                    cell.Value.ToString().Length < 6)
                {
                    cell.Value = "есть, но не указано какое";
                    continue;
                }


                if (Regex.IsMatch(cell.Value.ToString(), "электр", RegexOptions.IgnoreCase))
                    worksheet.Cells[
                        cell.Start.Row, GetColumnIndex("SYSTEM_ELECTRICITY")
                        ].Value =
                        "есть, выделенная мощность неизвестна";
                if (Regex.IsMatch(cell.Value.ToString(), "вод", RegexOptions.IgnoreCase))
                    worksheet.Cells[cell.Start.Row, GetColumnIndex("SYSTEM_WATER")]
                        .Value =
                        "есть, но не указано какое";
                //просто пишем поверх
                if (Regex.IsMatch(cell.Value.ToString(), "скваж", RegexOptions.IgnoreCase))
                    worksheet.Cells[cell.Start.Row, GetColumnIndex("SYSTEM_WATER")]
                        .Value =
                        "скважина";
                if (Regex.IsMatch(cell.Value.ToString(), "канализ", RegexOptions.IgnoreCase))
                    worksheet.Cells[cell.Start.Row, GetColumnIndex("SYSTEM_SEWERAGE")]
                        .Value =
                        "есть, но не указано какое";
                if (Regex.IsMatch(cell.Value.ToString(), "отопл", RegexOptions.IgnoreCase))
                    worksheet.Cells[cell.Start.Row, GetColumnIndex("HEAT_SUPPLY")]
                        .Value =
                        "есть, но не указано какое";
                //В самом конце можем перезаписать ячейку
                cell.Value = Regex.IsMatch(cell.Value.ToString(), "газ", RegexOptions.IgnoreCase)
                    ? "есть, но не указано какое"
                    : NoInfoString;
            }

            //Проходимся по остальным столбцам (Вода, электр-во, канализация, отопление)
            //Варианты :Есть, Нет, Пусто

            //По всем столбцам
            for (var n = 2; n <= columnsCodeList.Count; n++)
            {
                var columnName = columnsCodeList[n - 1];
                //Значения по справочнику
                v = supportWorksheets.CatalogWs.GetContentByCode(columnName);

                columnIndex = GetColumnIndex(columnName);
                //Далее по всем ячейкам в столбце
                for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
                {
                    var cell = worksheet.Cells[i, columnIndex];
                    if (cell.Value == null || cell.Value.ToString() == string.Empty)
                    {
                        cell.Value = NoInfoString;
                        continue;
                    }

                    if (v.Contains(cell.Value.ToString())) continue;
                    //когда "10 вкт"
                    if (Regex.IsMatch(cell.Value.ToString(), @"\bквт\b", RegexOptions.IgnoreCase))
                        continue;

                    if (Regex.IsMatch(cell.Value.ToString(), @"нет", RegexOptions.IgnoreCase) &&
                        cell.Value.ToString().Length < 6)
                        cell.Value = "отсутствует, возможность подключения неизвестна";
                    if (Regex.IsMatch(cell.Value.ToString(), @"есть", RegexOptions.IgnoreCase) &&
                        cell.Value.ToString().Length < 6)
                    {
                        cell.Value = n == 4 ? "есть, выделенная мощность неизвестна" : "есть, но не указано какое";
                    }
                }
            }
        }

        private void FormatPrice()
        {
            const string columnCode = "PRICE";
            var columnIndex = GetColumnIndex(columnCode);
            if (columnIndex == 0) return;

            var numericRegex = new Regex(@"(\d|\s|\.|\,)+");
            var multiplierRegex = new Regex(@"(г(ект)?а|сот|(/)?(кв\\s*\\.?\\s*м\\b|м2|м\\s*\\.\\s*кв\b|м\b))",
                RegexOptions.IgnoreCase);

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null) return;

                double u;
                //Цифровые ячейки пропускаем
                if (cell.Value is double || double.TryParse(cell.Value.ToString(), out u))
                {
                    if (cell.Value.ToString() == 0.ToString(CultureInfo.InvariantCulture))
                        cell.Value = string.Empty;
                    continue;
                }

                var x = 1; //Множитель для млн и тысячи в числа
                var multReg = new Regex(@"(\b(млн|тыс))", RegexOptions.IgnoreCase);

                Match convertMatch;
                try
                {
                    convertMatch = multReg.Match(cell.Value.ToString());
                }
                catch (Exception)
                {
                    continue;
                }

                if (convertMatch.Success)
                {
                    if (convertMatch.Value.ToLower() == "млн")
                        x = 1000000;
                    else if (convertMatch.Value.ToLower() == "тыс")
                        x = 1000;
                }

                var y = 1; //Множитель для сотка-гектар
                double multiplier = 1; //множитель для "за сотку-метр-гектар". Содержит площадь земельного участка

                var cellValue = cell.Value.ToString();

                //Проверяем строку на предмет "100 000рублей за сотку-метр-гектар"
                var match = multiplierRegex.Match(cellValue);
                if (match.Success)
                {
                    switch (match.Value.ToLower())
                    {
                        case "гекта":
                        case "га":
                            y = 1/10000;
                            break;
                        case "сот":
                            y = 1/100;
                            break;
                    }

                    if (
                        worksheet.Cells[cell.Start.Row, GetColumnIndex("AREA_LOT")].Value is
                            double)
                    {
                        multiplier =
                            (double) worksheet.Cells[cell.Start.Row, GetColumnIndex("AREA_LOT")].Value;
                    }
                    else
                    {
                        //Проверка на 85.000
                        var val2 = Regex.IsMatch(cell.Value.ToString(), @"\d+\.\d{3,}")
                            ? cell.Value.ToString().Replace(".", string.Empty)
                            : cellValue;

                        match = numericRegex.Match(val2);
                        if (!match.Success)
                        {
                            cell.Value = string.Empty;
                            continue;
                        }
                    }
                }

                //Проверка на 85.000
                var val = Regex.IsMatch(cell.Value.ToString(), @"\d+\.\d{3,}")
                    ? cell.Value.ToString().Replace(".", string.Empty)
                    : cellValue;

                match = numericRegex.Match(val);
                if (!match.Success)
                {
                    cell.Value = string.Empty;
                    continue;
                }

                double s;

                //Убераем пробемы, заменяем точку на запятую и конвертирует в double
                double.TryParse(match.Value.Trim().Replace(" ", string.Empty).Replace(".", ","), out s);

                cell.Value = s*y*multiplier*x;
                cell.Style.Numberformat.Format = "#";
            }
        }

        private void FormatAreaLot()
        {
            const string columnCode = "AREA_LOT";

            var columnIndex = GetColumnIndex(columnCode);
            if (columnIndex == 0) return;

            //10 000,89 руб / 9 000.80 рубсотк
            var numericRegex = new Regex(@"(\d|\s|\.|\,)+");
            var multiplieRegex = new Regex(@"(га|сот)", RegexOptions.IgnoreCase);

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (cell.Value == null) return;

                double u;
                //Цифровые ячейки пропускаем
                if (cell.Value is double || double.TryParse(cell.Value.ToString(), out u))
                {
                    if (cell.Value.ToString() == 0.ToString(CultureInfo.InvariantCulture))
                        cell.Value = string.Empty;
                    continue;
                }

                //когда в ячейке площадь дома, мы не обрабатываем участки с домом
                if (Regex.IsMatch(cell.Value.ToString(), "дом", RegexOptions.IgnoreCase))
                {
                    rowsToDelete.Add(cell.Start.Row);
                    continue;
                }

                //Дефолтный мнжитель
                var y = 1;
                Match match;

                //При наличие модификатора Га или Сот
                //Берём первый встречный и меняем множитель
                if (multiplieRegex.IsMatch(cell.Value.ToString()))
                {
                    match = multiplieRegex.Match(cell.Value.ToString());
                    y = match.Value.ToLower() == "га" ? 10000 : 100;
                }

                //Вычленяем цифры с запятыми, точками и пробелом (которые сразу и режем)
                match = null;
                match = numericRegex.Match(cell.Value.ToString());
                if (!match.Success)
                {
                    cell.Value = string.Empty;
                    continue;
                }
                double s;
                double.TryParse(match.Value.Trim().Replace(" ", string.Empty).Replace(".", ","), out s);

                cell.Value = s*y;
                cell.Style.Numberformat.Format = @"#";
            }
        }

        /// <summary>
        ///     Обрабатывает информаци об удаленности от регионального центра
        ///     !Метод запускается после последней проверки всех ячеек на населенный пункт
        /// </summary>
        private void FormatDistToRegCenter()
        {
            const string code = "DIST_REG_CENTER";
            var columnIndex = GetColumnIndex(code);
            var nearCColumnIndex = GetColumnIndex("DIST_NEAR_CITY");
            if (columnIndex == 0) return;

            //Для проверки
            var distToNeadCity =
                new Regex(
                    @"(?<dist>\d(?:\d|\s|\,|\.)+)\s?км\.?\s*(?<incity>\b(?:от|до|за)\b\s(?<cityType>[а-я]+\.?\s?)?(?<cityName>[А-Я]\w+)?)?");

            for (var i = HeadSize + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var cell = worksheet.Cells[i, columnIndex];
                if (string.IsNullOrEmpty(cell.Value as string))
                    continue;

                var inCityCell = worksheet.Cells[cell.Start.Row, GetColumnIndex("IN_CITY")];
                var nearCityCell = worksheet.Cells[cell.Start.Row, GetColumnIndex("NEAR_CITY")];

                var distValue = cell.Value.ToString();
                if (distValue == "0")
                {
                    inCityCell.Value = "да";
                    continue;
                }
                if (Regex.IsMatch(distValue, @"^(\d|\.|,)+$")) continue;
                Match match = LocatonRegexpHandler.Init().DistToRegCenteRegex.Match(distValue);
                if (match.Success)
                {
                    if (Regex.IsMatch(match.Value, @"\bв\b\s", RegexOptions.IgnoreCase))
                    {
                        inCityCell.Value = "да";
                        cell.Value = string.Empty;
                    }
                    else if (Regex.IsMatch(match.Value, @"\bза\b\s", RegexOptions.IgnoreCase))
                    {
                        inCityCell.Value = "нет";
                        cell.Value = string.Empty;
                    }
                    else
                    {
                        inCityCell.Value = "нет";
                        if (nearCityCell.IsEmpty() == false &&
                            nearCityCell.Value.ToString() == match.Groups["Name"].Value)
                        {
                            worksheet.Cells[cell.Start.Row, GetColumnIndex("DIST_NEAR_CITY")].Value =
                                match.Groups["num"].Value;
                            cell.Value = string.Empty;
                        }
                        else
                        {
                            cell.Value = match.Groups["num"].Value;
                        }
                    }
                }
                else
                    cell.Value = string.Empty;
            }
        }

        #endregion


        /// <summary>
        /// Метод ищет номер колонки по переданному её названию
        /// </summary>
        /// <param name="columnCode">Кодовое название колонки</param>
        /// <returns>Порядоковый номер колонки. <c>0</c> Когда колонка не найдена</returns>
        private int GetColumnIndex(string columnCode)
        {
            var firstOrDefault = columns.FirstOrDefault(c => c.CodeName.EqualNoCase(columnCode));
            return firstOrDefault != null ? firstOrDefault.ColumnIndex : 0;
        }

        /// <summary>
        ///     Производит чтение всех дополнительных вниг
        /// </summary>
        /// <exception cref="ArgumentException">Когда одну из книг не получилось прочитать</exception>
        /// <returns></returns>
        private Dictionary<string, DataSet> ReadPaths()
        {
            
            var paths = new[]
            {
                dbParams.OktmoSupportWorkbook.Path,
                dbParams.CatalogSupportWorkbook.Path,
                dbParams.SubjectSourceSupportWorkbook.Path,
                dbParams.VgtCatalogSupportWorkbook.Path,
            }.Distinct();


            var result = new Dictionary<string, DataSet>();
            var reader = new ExcelReader();

            foreach (var path in paths)
            {
                var ds = reader.ReadExcelFile(path);
                if (ds == null) throw new ArgumentException("Одну или несколько книг не возмонжо прочитать");
                result.Add(path, ds);
            }

            return result;

        }

        /// <summary>
        ///     
        /// </summary>
        /// <exception cref="ArgumentException">Выбрасывает при ошибки чтения файла или чтения бд</exception>
        /// <param name="path">Пусть в рабочей книге</param>
        private void InitWorkbookAndColumns(string path)
        {
            initWbTask = Task.Run(() =>
            {
                logger.Info("Чтение главной книги по адресу {0}", path);
                try
                {
                    ExcelPackage = new ExcelPackage(new FileInfo(path));
                    worksheet = ExcelPackage.Workbook.Worksheets.First();
                }
                catch (Exception)
                {
                    logger.Error("Ошибка при чтении главного файла. Возможно файл слишком большой");
                    throw new ArgumentException();
                }
                _lastUsedRow = worksheet.Dimension.Rows;

                try
                {
                    columns = UnitOfWorkSingleton.UnitOfWork.TemplateWbsRespository.GetTypedWorkbook(wbType).Columns;
                }
                catch (Exception)
                {
                    throw new ArgumentException();
                }
            });           
        }

        private DbToConvert()
        {
            HeadSize = 2;
            rowsToDelete = new List<long>();
        }
    }
}