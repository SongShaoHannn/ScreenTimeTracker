using System.Windows;
using System.Windows.Controls;

namespace ScreenTimeTracker.Views.Controls;

public partial class GlassPanel : UserControl
{
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GlassPanel),
            new PropertyMetadata(new CornerRadius(12), OnCornerRadiusChanged));

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public static readonly DependencyProperty ChildProperty =
        DependencyProperty.Register(nameof(Child), typeof(UIElement), typeof(GlassPanel),
            new PropertyMetadata(null, OnChildChanged));

    public UIElement? Child
    {
        get => (UIElement?)GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public GlassPanel()
    {
        InitializeComponent();
    }

    private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassPanel panel && panel.FindName("GlassBorder") is Border border)
        {
            border.CornerRadius = (CornerRadius)e.NewValue;
        }
    }

    private static void OnChildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GlassPanel panel && panel.FindName("ContentHost") is ContentPresenter presenter)
        {
            presenter.Content = e.NewValue;
        }
    }
}
