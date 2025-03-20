﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dct.UI.Alarm.Views.Alarm
{
    /// <summary>
    /// FingerEngagement.xaml 的交互逻辑
    /// </summary>
    public partial class FingerEngagement : UserControl
    {
        private List<string> _originalItems;
        public FingerEngagement()
        {
            InitializeComponent();

            _originalItems = new List<string>() { "Shutter01", "Shutter02" };

            FilteredComboBox.ItemsSource = _originalItems;
        }
    }
}
