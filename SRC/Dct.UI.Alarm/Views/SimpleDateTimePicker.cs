using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows;

namespace Dct.UI.Alarm.Views
{
    public class SimpleDateTimePicker : Control
    {
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(SimpleDateTimePicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SimpleDateTimePicker picker && picker._textBox != null)
            {
                picker._textBox.Text = picker.SelectedDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            }
        }

        private TextBox _textBox;
        private Popup _popup;
        private Calendar _calendar;

        static SimpleDateTimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SimpleDateTimePicker), new FrameworkPropertyMetadata(typeof(SimpleDateTimePicker)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _textBox = GetTemplateChild("PART_TextBox") as TextBox;
            _popup = GetTemplateChild("PART_Popup") as Popup;
            _calendar = GetTemplateChild("PART_Calendar") as Calendar;

            if (_textBox != null)
            {
                _textBox.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    if (_popup != null)
                    {
                        _popup.IsOpen = true;
                    }
                };
            }

            if (_calendar != null)
            {
                _calendar.SelectedDatesChanged += (s, e) =>
                {
                    SelectedDate = _calendar.SelectedDate;
                    if (_popup != null)
                    {
                        _popup.IsOpen = false;
                    }
                };
            }
        }
    }

    public class CustomDateTimePicker : Control
    {
        public static readonly DependencyProperty SelectedDateTimeProperty =
            DependencyProperty.Register(nameof(SelectedDateTime), typeof(DateTime?), typeof(CustomDateTimePicker),
                new FrameworkPropertyMetadata(DateTime.Now, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateTimeChanged));

        public DateTime? SelectedDateTime
        {
            get => (DateTime?)GetValue(SelectedDateTimeProperty);
            set => SetValue(SelectedDateTimeProperty, value);
        }

        private TextBox _textBox;
        private Popup _popup;
        private Calendar _calendar;
        private ComboBox _hourPicker;
        private ComboBox _minutePicker;

        static CustomDateTimePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomDateTimePicker), new FrameworkPropertyMetadata(typeof(CustomDateTimePicker)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 获取控件模板中的元素
            _textBox = GetTemplateChild("PART_TextBox") as TextBox;
            _popup = GetTemplateChild("PART_Popup") as Popup;
            _calendar = GetTemplateChild("PART_Calendar") as Calendar;
            _hourPicker = GetTemplateChild("PART_HourPicker") as ComboBox;
            _minutePicker = GetTemplateChild("PART_MinutePicker") as ComboBox;
            var confirmButton = GetTemplateChild("PART_ConfirmButton") as Button;

            // 添加事件
            if (_textBox != null)
            {
                _textBox.PreviewMouseLeftButtonDown += (s, e) => TogglePopup();
                UpdateTextBox();
            }

            if (_popup != null)
            {
                _popup.StaysOpen = true;
                _popup.Closed += (s, e) => UpdateTextBox();
            }

            if (_calendar != null)
            {
                _calendar.SelectedDatesChanged += OnDateChanged;
                if (SelectedDateTime != null)
                    _calendar.SelectedDate = SelectedDateTime.Value.Date;
            }

            if (_hourPicker != null && _minutePicker != null)
            {
                InitializeTimePickers();
                _hourPicker.SelectionChanged += OnTimeChanged;
                _minutePicker.SelectionChanged += OnTimeChanged;
            }

            if (confirmButton != null)
            {
                confirmButton.Click += (s, e) =>
                {
                    if (_popup != null)
                    {
                        _popup.IsOpen = false; // 关闭弹出框
                    }
                };
            }
        }

        private void TogglePopup()
        {
            if (_popup != null)
            {
                _popup.IsOpen = !_popup.IsOpen;
            }
        }

        private void OnDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_calendar.SelectedDate != null)
            {
                var currentTime = SelectedDateTime?.TimeOfDay ?? TimeSpan.Zero;
                SelectedDateTime = _calendar.SelectedDate.Value.Date + currentTime;
            }
        }

        private void OnTimeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_hourPicker.SelectedItem != null && _minutePicker.SelectedItem != null)
            {
                var currentDate = SelectedDateTime?.Date ?? DateTime.Today;
                var hours = int.Parse(_hourPicker.SelectedItem.ToString());
                var minutes = int.Parse(_minutePicker.SelectedItem.ToString());
                SelectedDateTime = currentDate + new TimeSpan(hours, minutes, 0);
            }
        }

        private void InitializeTimePickers()
        {
            _hourPicker.ItemsSource = CreateRange(0, 23);
            _minutePicker.ItemsSource = CreateRange(0, 59);

            if (SelectedDateTime != null)
            {
                _hourPicker.SelectedItem = SelectedDateTime.Value.Hour;
                _minutePicker.SelectedItem = SelectedDateTime.Value.Minute;
            }
        }

        private void UpdateTextBox()
        {
            if (_textBox != null && SelectedDateTime != null)
            {
                _textBox.Text = SelectedDateTime.Value.ToString("yyyy-MM-dd HH:mm");
            }
        }

        private static int[] CreateRange(int start, int end)
        {
            var range = new int[end - start + 1];
            for (int i = start; i <= end; i++)
                range[i - start] = i;
            return range;
        }

        private static void OnSelectedDateTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (CustomDateTimePicker)d;
            picker.UpdateTextBox();
        }
    }
}
