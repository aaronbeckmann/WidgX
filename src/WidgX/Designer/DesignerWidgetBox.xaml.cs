using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WidgX.Models;
using WidgX.Widgets;

namespace WidgX.Designer;

public partial class DesignerWidgetBox : System.Windows.Controls.UserControl
{
    private readonly WidgetInstance _instance;
    private readonly System.Windows.FrameworkElement _view;
    private System.Windows.Point _dragStartMouse;
    private System.Windows.Point _dragStartPosition;
    private bool _isDragging;

    public event Action<WidgetInstance>? BoundsChanged;
    public event Action<WidgetInstance>? Selected;

    /// <summary>
    /// Given the dragged instance and its proposed top-left, returns the snapped
    /// top-left (and draws alignment guides as a side effect). Set by the host.
    /// </summary>
    public Func<WidgetInstance, double, double, (double X, double Y)>? SnapResolver;

    /// <summary>Raised when a drag finishes, so the host can clear guides.</summary>
    public event Action? DragEnded;

    public DesignerWidgetBox(WidgetInstance instance, IWidget widget)
    {
        InitializeComponent();
        _instance = instance;

        _view = widget.View;
        WidgetContent.Content = _view;
        Width = instance.Width;
        Height = instance.Height;

        Canvas.SetLeft(this, instance.X);
        Canvas.SetTop(this, instance.Y);

        OuterBorder.MouseLeftButtonDown += OnMouseDown;
        OuterBorder.MouseMove += OnMouseMove;
        OuterBorder.MouseLeftButtonUp += OnMouseUp;
        ResizeThumb.DragDelta += OnResizeDrag;
    }

    private void OnMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isDragging = true;
        _dragStartMouse = e.GetPosition(Parent as IInputElement);
        _dragStartPosition = new System.Windows.Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        OuterBorder.CaptureMouse();
        Selected?.Invoke(_instance);
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDragging) return;

        var current = e.GetPosition(Parent as IInputElement);
        var delta = current - _dragStartMouse;

        var newX = _dragStartPosition.X + delta.X;
        var newY = _dragStartPosition.Y + delta.Y;

        if (SnapResolver != null)
        {
            (newX, newY) = SnapResolver(_instance, newX, newY);
        }

        Canvas.SetLeft(this, newX);
        Canvas.SetTop(this, newY);

        _instance.X = newX;
        _instance.Y = newY;
        BoundsChanged?.Invoke(_instance);
    }

    private void OnMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        OuterBorder.ReleaseMouseCapture();
        DragEnded?.Invoke();
    }

    private void OnResizeDrag(object sender, DragDeltaEventArgs e)
    {
        var newWidth = Math.Max(40, Width + e.HorizontalChange);
        var newHeight = Math.Max(40, Height + e.VerticalChange);

        Width = newWidth;
        Height = newHeight;

        // Resize the hosted widget too, so the content scales with the box
        // rather than the selection border growing around a fixed-size widget.
        _view.Width = newWidth;
        _view.Height = newHeight;

        _instance.Width = newWidth;
        _instance.Height = newHeight;
        BoundsChanged?.Invoke(_instance);
    }
}
