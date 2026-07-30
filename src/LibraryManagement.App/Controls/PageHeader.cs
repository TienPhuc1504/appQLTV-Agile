using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Controls;

public sealed class PageHeader : Control
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(PageHeader),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(PageHeader),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(IconElement),
            typeof(PageHeader),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TrailingContentProperty =
        DependencyProperty.Register(
            nameof(TrailingContent),
            typeof(object),
            typeof(PageHeader),
            new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public IconElement? Icon
    {
        get => (IconElement?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }
}
