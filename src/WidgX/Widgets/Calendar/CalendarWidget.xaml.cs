using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WidgX.Models;

namespace WidgX.Widgets.Calendar;

public partial class CalendarWidget : System.Windows.Controls.UserControl, IWidget
{
    private readonly DispatcherTimer _timer;
    private System.Windows.Media.Color _accentColor = System.Windows.Media.Colors.White;

    public CalendarWidget()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromHours(1) };
        _timer.Tick += (_, _) => Refresh();
    }

    System.Windows.Controls.UserControl IWidget.View => this;

    public static List<List<int?>> BuildMonthGrid(DateTime today)
    {
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
        var startColumn = (int)firstOfMonth.DayOfWeek; // Sunday = 0

        var grid = new List<List<int?>>();
        var day = 1;

        for (var row = 0; row < 6 && day <= daysInMonth; row++)
        {
            var rowCells = new List<int?>();
            for (var col = 0; col < 7; col++)
            {
                if (row == 0 && col < startColumn)
                {
                    rowCells.Add(null);
                }
                else if (day <= daysInMonth)
                {
                    rowCells.Add(day);
                    day++;
                }
                else
                {
                    rowCells.Add(null);
                }
            }
            grid.Add(rowCells);
        }

        return grid;
    }

    public void Configure(WidgetInstance config)
    {
        Width = config.Width;
        Height = config.Height;
        WidgetChrome.ApplyBackgroundOpacity(this, config.Opacity);
        WidgetChrome.ApplyFont(this, config.FontFamily);
        WidgetChrome.ApplyTextShadow(this, config.TextShadow);
        FontSize = config.FontSize;

        if (System.Windows.Media.ColorConverter.ConvertFromString(config.AccentColorHex) is System.Windows.Media.Color color)
        {
            _accentColor = color;
        }

        Refresh();
    }

    public void StartUpdates() => _timer.Start();

    public void StopUpdates() => _timer.Stop();

    private void Refresh()
    {
        var today = DateTime.Now;
        var enCulture = CultureInfo.GetCultureInfo("en-US");
        MonthText.Text = today.ToString("MMMM yyyy", enCulture);
        MonthText.Foreground = new SolidColorBrush(_accentColor);

        var grid = BuildMonthGrid(today);

        DaysGrid.Children.Clear();
        DaysGrid.RowDefinitions.Clear();
        DaysGrid.ColumnDefinitions.Clear();

        for (var c = 0; c < 7; c++) DaysGrid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var r = 0; r < grid.Count; r++) DaysGrid.RowDefinitions.Add(new RowDefinition());

        for (var r = 0; r < grid.Count; r++)
        {
            for (var c = 0; c < 7; c++)
            {
                var dayValue = grid[r][c];
                var text = new TextBlock
                {
                    Text = dayValue?.ToString() ?? string.Empty,
                    Foreground = new SolidColorBrush(_accentColor),
                    FontWeight = dayValue == today.Day ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center
                };
                Grid.SetRow(text, r);
                Grid.SetColumn(text, c);
                DaysGrid.Children.Add(text);
            }
        }
    }
}
