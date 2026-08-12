using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Controls;

public sealed class LibraryNavigationView : NavigationView
{
    private const string MenuItemsPartName = "PART_MenuItemsItemsControl";
    private const string FooterMenuItemsPartName = "PART_FooterMenuItemsItemsControl";
    private const string GroupCardsItemsPanelResourceKey =
        "NavigationGroupCardsItemsPanelTemplate";

    public bool CanGoForward =>
        NavigationViewContentPresenter?.CanGoForward == true;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // These part names are verified against the WPF-UI 4.3.0 NavigationView template.
        ApplyGroupCardsPanel(MenuItemsPartName);
        ApplyGroupCardsPanel(FooterMenuItemsPartName);
    }

    private void ApplyGroupCardsPanel(string partName)
    {
        if (GetTemplateChild(partName) is not ItemsControl itemsControl)
        {
            Trace.TraceWarning(
                "LibraryNavigationView could not find template part '{0}'. "
                + "The default WPF-UI navigation panel remains active.",
                partName);
            return;
        }

        if (TryFindResource(GroupCardsItemsPanelResourceKey)
            is not ItemsPanelTemplate itemsPanelTemplate)
        {
            Trace.TraceWarning(
                "LibraryNavigationView could not find resource '{0}'. "
                + "The default WPF-UI navigation panel remains active.",
                GroupCardsItemsPanelResourceKey);
            return;
        }

        itemsControl.ItemsPanel = itemsPanelTemplate;
    }
}
