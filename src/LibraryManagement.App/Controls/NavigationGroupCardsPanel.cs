using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Controls;

public sealed class NavigationGroupCardsPanel : Panel
{
    public static readonly DependencyProperty IsPaneOpenProperty =
        DependencyProperty.Register(
            nameof(IsPaneOpen),
            typeof(bool),
            typeof(NavigationGroupCardsPanel),
            new FrameworkPropertyMetadata(
                true,
                FrameworkPropertyMetadataOptions.AffectsMeasure
                    | FrameworkPropertyMetadataOptions.AffectsArrange
                    | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CardBackgroundProperty =
        DependencyProperty.Register(
            nameof(CardBackground),
            typeof(Brush),
            typeof(NavigationGroupCardsPanel),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CardBorderBrushProperty =
        DependencyProperty.Register(
            nameof(CardBorderBrush),
            typeof(Brush),
            typeof(NavigationGroupCardsPanel),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CardCornerRadiusProperty =
        DependencyProperty.Register(
            nameof(CardCornerRadius),
            typeof(CornerRadius),
            typeof(NavigationGroupCardsPanel),
            new FrameworkPropertyMetadata(
                new CornerRadius(12),
                FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CardPaddingProperty =
        DependencyProperty.Register(
            nameof(CardPadding),
            typeof(Thickness),
            typeof(NavigationGroupCardsPanel),
            new FrameworkPropertyMetadata(
                new Thickness(4),
                FrameworkPropertyMetadataOptions.AffectsMeasure
                    | FrameworkPropertyMetadataOptions.AffectsArrange
                    | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CardSpacingProperty =
        DependencyProperty.Register(
            nameof(CardSpacing),
            typeof(double),
            typeof(NavigationGroupCardsPanel),
            new FrameworkPropertyMetadata(
                8D,
                FrameworkPropertyMetadataOptions.AffectsMeasure
                    | FrameworkPropertyMetadataOptions.AffectsArrange
                    | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty CardBorderThicknessProperty =
        DependencyProperty.Register(
            nameof(CardBorderThickness),
            typeof(Thickness),
            typeof(NavigationGroupCardsPanel),
            new FrameworkPropertyMetadata(
                new Thickness(1),
                FrameworkPropertyMetadataOptions.AffectsMeasure
                    | FrameworkPropertyMetadataOptions.AffectsArrange
                    | FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly List<Rect> _cardBounds = [];

    public bool IsPaneOpen
    {
        get => (bool)GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    public Brush? CardBackground
    {
        get => (Brush?)GetValue(CardBackgroundProperty);
        set => SetValue(CardBackgroundProperty, value);
    }

    public Brush? CardBorderBrush
    {
        get => (Brush?)GetValue(CardBorderBrushProperty);
        set => SetValue(CardBorderBrushProperty, value);
    }

    public CornerRadius CardCornerRadius
    {
        get => (CornerRadius)GetValue(CardCornerRadiusProperty);
        set => SetValue(CardCornerRadiusProperty, value);
    }

    public Thickness CardPadding
    {
        get => (Thickness)GetValue(CardPaddingProperty);
        set => SetValue(CardPaddingProperty, value);
    }

    public double CardSpacing
    {
        get => (double)GetValue(CardSpacingProperty);
        set => SetValue(CardSpacingProperty, value);
    }

    public Thickness CardBorderThickness
    {
        get => (Thickness)GetValue(CardBorderThicknessProperty);
        set => SetValue(CardBorderThicknessProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return IsPaneOpen
            ? MeasureExpanded(availableSize)
            : MeasureCompact(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _cardBounds.Clear();

        foreach (UIElement child in InternalChildren)
        {
            child.Arrange(new Rect(0, 0, 0, 0));
        }

        if (IsPaneOpen)
        {
            ArrangeExpanded(finalSize);
        }
        else
        {
            ArrangeCompact(finalSize);
        }

        InvalidateVisual();
        return finalSize;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (!IsPaneOpen || _cardBounds.Count == 0)
        {
            return;
        }

        double borderThickness = GetUniformThickness(CardBorderThickness);
        Pen? borderPen = CardBorderBrush is null || borderThickness <= 0
            ? null
            : new Pen(CardBorderBrush, borderThickness);
        double radius = GetUniformRadius(CardCornerRadius);

        foreach (Rect bounds in _cardBounds)
        {
            Rect drawingBounds = DeflateForStroke(bounds, borderThickness);
            if (drawingBounds.Width <= 0 || drawingBounds.Height <= 0)
            {
                continue;
            }

            drawingContext.DrawRoundedRectangle(
                CardBackground,
                borderPen,
                drawingBounds,
                radius,
                radius);
        }
    }

    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        // The panel's rendered card surface is decorative; child controls retain hit testing.
        return null;
    }

    private Size MeasureExpanded(Size availableSize)
    {
        IReadOnlyList<IReadOnlyList<UIElement>> groups = BuildVisibleGroups();
        if (groups.Count == 0)
        {
            return new Size();
        }

        Thickness padding = NormalizeThickness(CardPadding);
        double innerAvailableWidth = double.IsInfinity(availableSize.Width)
            ? double.PositiveInfinity
            : Math.Max(0, availableSize.Width - padding.Left - padding.Right);
        double desiredWidth = 0;
        double desiredHeight = 0;

        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            if (groupIndex > 0)
            {
                desiredHeight += Math.Max(0, CardSpacing);
            }

            desiredHeight += padding.Top + padding.Bottom;

            foreach (UIElement child in groups[groupIndex])
            {
                child.Measure(new Size(innerAvailableWidth, double.PositiveInfinity));
                desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
                desiredHeight += child.DesiredSize.Height;
            }
        }

        return new Size(
            desiredWidth + padding.Left + padding.Right,
            desiredHeight);
    }

    private Size MeasureCompact(Size availableSize)
    {
        double desiredWidth = 0;
        double desiredHeight = 0;

        foreach (UIElement child in GetVisibleCompactItems())
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
            desiredHeight += child.DesiredSize.Height;
        }

        return new Size(desiredWidth, desiredHeight);
    }

    private void ArrangeExpanded(Size finalSize)
    {
        IReadOnlyList<IReadOnlyList<UIElement>> groups = BuildVisibleGroups();
        Thickness padding = NormalizeThickness(CardPadding);
        double cardSpacing = Math.Max(0, CardSpacing);
        double innerWidth = Math.Max(0, finalSize.Width - padding.Left - padding.Right);
        double currentY = 0;

        for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            if (groupIndex > 0)
            {
                currentY += cardSpacing;
            }

            double cardTop = currentY;
            currentY += padding.Top;

            foreach (UIElement child in groups[groupIndex])
            {
                double childHeight = child.DesiredSize.Height;
                child.Arrange(new Rect(padding.Left, currentY, innerWidth, childHeight));
                currentY += childHeight;
            }

            currentY += padding.Bottom;
            _cardBounds.Add(new Rect(0, cardTop, finalSize.Width, currentY - cardTop));
        }
    }

    private void ArrangeCompact(Size finalSize)
    {
        double currentY = 0;

        foreach (UIElement child in GetVisibleCompactItems())
        {
            double childHeight = child.DesiredSize.Height;
            child.Arrange(new Rect(0, currentY, finalSize.Width, childHeight));
            currentY += childHeight;
        }
    }

    private IReadOnlyList<IReadOnlyList<UIElement>> BuildVisibleGroups()
    {
        List<IReadOnlyList<UIElement>> groups = [];
        List<UIElement> currentGroup = [];
        bool currentGroupHasItem = false;

        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            if (child is NavigationViewItemHeader)
            {
                AddGroupIfItHasItems(groups, currentGroup, currentGroupHasItem);
                currentGroup = [child];
                currentGroupHasItem = false;
                continue;
            }

            currentGroup.Add(child);
            currentGroupHasItem = true;
        }

        AddGroupIfItHasItems(groups, currentGroup, currentGroupHasItem);
        return groups;
    }

    private IEnumerable<UIElement> GetVisibleCompactItems()
    {
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility != Visibility.Collapsed
                && child is not NavigationViewItemHeader)
            {
                yield return child;
            }
        }
    }

    private static void AddGroupIfItHasItems(
        ICollection<IReadOnlyList<UIElement>> groups,
        IReadOnlyList<UIElement> group,
        bool hasItem)
    {
        if (hasItem)
        {
            groups.Add(group);
        }
    }

    private static Thickness NormalizeThickness(Thickness thickness)
    {
        return new Thickness(
            Math.Max(0, thickness.Left),
            Math.Max(0, thickness.Top),
            Math.Max(0, thickness.Right),
            Math.Max(0, thickness.Bottom));
    }

    private static double GetUniformThickness(Thickness thickness)
    {
        return Math.Max(
            0,
            Math.Min(
                Math.Min(thickness.Left, thickness.Top),
                Math.Min(thickness.Right, thickness.Bottom)));
    }

    private static double GetUniformRadius(CornerRadius cornerRadius)
    {
        return Math.Max(
            0,
            Math.Min(
                Math.Min(cornerRadius.TopLeft, cornerRadius.TopRight),
                Math.Min(cornerRadius.BottomRight, cornerRadius.BottomLeft)));
    }

    private static Rect DeflateForStroke(Rect bounds, double strokeThickness)
    {
        double inset = strokeThickness / 2;
        return new Rect(
            bounds.X + inset,
            bounds.Y + inset,
            Math.Max(0, bounds.Width - strokeThickness),
            Math.Max(0, bounds.Height - strokeThickness));
    }
}
