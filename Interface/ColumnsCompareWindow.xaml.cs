﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Converter;
using Converter.Template_workbooks;
using Telerik.Windows.Controls;

namespace UI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class ColumnsCompareWindow : Window
    {
        private CompareViewModel view;
        private ICollection<WorksheetInfo> wsInfos; 

        public ColumnsCompareWindow(Dictionary<string, List<string>> dict, List<WorksheetInfo> wsInfos)
        {
            InitializeComponent();
            this.wsInfos = wsInfos;
            view = new CompareViewModel(DitctToObservDict(dict), wsInfos);
            UnbindexListBox.ItemsSource = view.UnbindedColumns;
            BindedColumnsListBox.ItemsSource = view.BindedColumnsDictionary;
            ValuesExamplesListBox.ItemsSource = view.LastSelectedColumnValuesExamples;
        }

        private void ListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            view.LastSelectedItem = ((RadListBox) sender).SelectedItem as string;
            ValuesExamplesListBox.ItemsSource = view.LastSelectedColumnValuesExamples;
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dict = ObservDictToDict(view.BindedColumnsDictionary);
            var typifer = new WorkbookTypifier<LandPropertyTemplateWorkbook>(dict);
        }

        private Dictionary<string, ObservableCollection<string>> DitctToObservDict(
            Dictionary<string, List<string>> sourceDict)
        {
            return sourceDict.ToDictionary(k => k.Key, v => new ObservableCollection<string>(v.Value));
        }

        private Dictionary<string, List<string>> ObservDictToDict(
            Dictionary<string, ObservableCollection<string>> sourceDict)
        {
            return sourceDict.ToDictionary(k => k.Key, v => v.Value.ToList());
        }
    }
}
