using System;
using System.Globalization;
using System.Windows;
using WidgX.Widgets.Calendar;
using Brush = System.Windows.Media.Brush;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using TextBlock = System.Windows.Controls.TextBlock;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace WidgX.Controls;

/// <summary>
/// A compact, dark-themed date picker: a field button that opens a custom month
/// calendar popup. Exposes a two-way bindable <see cref="SelectedDate"/>.
/// </summary>
public partial class DatePickerLite : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register(
        nameof(SelectedDate), typeof(DateTime?), typeof(DatePickerLite),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

    private DateTime _viewMonth = DateTime.Today;

    public DatePickerLite()
    {
        InitializeComponent();
        BuildDayOfWeekHeaders();
        ToggleButton.Checked += (_, _) => OpenCalendar();
        UpdateLabel();
    }

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DatePickerLite)d).UpdateLabel();

    private void UpdateLabel()
    {
        if (SelectedDate is { } date)
        {
            ToggleButton.Content = date.ToString("MMM d, yyyy", CultureInfo.CurrentCulture);
            ToggleButton.SetResourceReference(ForegroundProperty, "TextPrimaryBrush");
        }
        else
        {
            ToggleButton.Content = "Due date";
            ToggleButton.SetResourceReference(ForegroundProperty, "TextSecondaryBrush");
        }
    }

    private void BuildDayOfWeekHeaders()
    {
        var names = CultureInfo.GetCultureInfo("en-US").DateTimeFormat.AbbreviatedDayNames; // Sun..Sat
        var secondary = (Brush)FindResource("TextSecondaryBrush");
        foreach (var name in names)
        {
            DowHeader.Children.Add(new TextBlock
            {
                Text = name[..2],
                Foreground = secondary,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
        }
    }

    private void OpenCalendar()
    {
        _viewMonth = SelectedDate ?? DateTime.Today;
        RenderMonth();
    }

    private void RenderMonth()
    {
        MonthLabel.Text = _viewMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
        DaysGrid.Children.Clear();

        var accent = (Brush)FindResource("AccentBrush");
        var onAccent = new SolidColorBrush(Color.FromRgb(0x0B, 0x0B, 0x10));
        var today = DateTime.Today;

        foreach (var week in CalendarWidget.BuildMonthGrid(_viewMonth))
        {
            foreach (var day in week)
            {
                if (day is null)
                {
                    DaysGrid.Children.Add(new System.Windows.Controls.Border());
                    continue;
                }

                var date = new DateTime(_viewMonth.Year, _viewMonth.Month, day.Value);
                var button = new Button
                {
                    Style = (Style)Resources["DayButtonStyle"],
                    Content = day.Value,
                    Tag = date,
                    Background = System.Windows.Media.Brushes.Transparent
                };
                button.Click += OnDayClick;

                if (SelectedDate is { } sel && sel.Date == date)
                {
                    button.Background = accent;
                    button.Foreground = onAccent;
                }
                else if (date == today)
                {
                    button.Foreground = accent;
                    button.FontWeight = FontWeights.Bold;
                }

                DaysGrid.Children.Add(button);
            }
        }
    }

    private void OnDayClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DateTime date })
        {
            SelectedDate = date;
            Close();
        }
    }

    private void OnPrevMonth(object sender, RoutedEventArgs e)
    {
        _viewMonth = _viewMonth.AddMonths(-1);
        RenderMonth();
    }

    private void OnNextMonth(object sender, RoutedEventArgs e)
    {
        _viewMonth = _viewMonth.AddMonths(1);
        RenderMonth();
    }

    private void OnToday(object sender, RoutedEventArgs e)
    {
        SelectedDate = DateTime.Today;
        Close();
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        SelectedDate = null;
        Close();
    }

    private void Close() => ToggleButton.IsChecked = false;
}
