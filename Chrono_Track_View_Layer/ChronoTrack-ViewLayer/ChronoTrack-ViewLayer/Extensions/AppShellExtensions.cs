using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls;
using ChronoTrack_ViewLayer.Components;

namespace ChronoTrack_ViewLayer.Extensions
{
    public static class AppShellExtensions
    {
        /// <summary>
        /// Gets the SidebarComponent from the AppShell
        /// </summary>
        /// <param name="shell">The AppShell instance</param>
        /// <returns>The SidebarComponent if found, otherwise null</returns>
        public static SidebarComponent GetSidebarComponent(this AppShell shell)
        {
            if (shell == null)
                return null;
                
            // Try to find the sidebar in the current page first
            if (shell.CurrentPage != null)
            {
                var sidebar = FindSidebarInPage(shell.CurrentPage);
                if (sidebar != null)
                    return sidebar;
            }
            
            // If not found, try a simpler approach - search all pages within the app
            if (Application.Current != null)
            {
                foreach (var page in GetAllPages())
                {
                    if (page != null)
                    {
                        var sidebar = FindSidebarInPage(page);
                        if (sidebar != null)
                            return sidebar;
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets all ContentPage instances that are currently created in the application
        /// </summary>
        private static IEnumerable<Page> GetAllPages()
        {
            if (Application.Current == null)
                yield break;
                
            // Check the main page
            if (Application.Current.MainPage != null)
            {
                yield return Application.Current.MainPage;
                
                // If it's a navigation page, check its navigation stack
                if (Application.Current.MainPage is NavigationPage navPage)
                {
                    foreach (var page in navPage.Navigation.NavigationStack)
                    {
                        yield return page;
                    }
                    
                    // Also check the modal stack
                    foreach (var page in navPage.Navigation.ModalStack)
                    {
                        yield return page;
                    }
                }
                
                // If it's a TabbedPage, check its children
                if (Application.Current.MainPage is TabbedPage tabbedPage)
                {
                    foreach (var page in tabbedPage.Children)
                    {
                        yield return page;
                    }
                }
                
                // If it's a FlyoutPage, check its flyout and detail
                if (Application.Current.MainPage is FlyoutPage flyoutPage)
                {
                    if (flyoutPage.Flyout != null)
                        yield return flyoutPage.Flyout;
                        
                    if (flyoutPage.Detail != null)
                        yield return flyoutPage.Detail;
                }
            }
        }
        
        /// <summary>
        /// Finds a SidebarComponent within a given page
        /// </summary>
        private static SidebarComponent FindSidebarInPage(Page page)
        {
            if (page == null)
                return null;
                
            // Check if the page itself implements IFindSidebarProvider
            if (page is IFindSidebarProvider sidebarProvider)
                return sidebarProvider.GetSidebar();
            
            // Check if the page has a SidebarComponent as a field or property
            var sidebarField = page.GetType().GetFields(System.Reflection.BindingFlags.Public | 
                                                        System.Reflection.BindingFlags.NonPublic | 
                                                        System.Reflection.BindingFlags.Instance)
                                    .FirstOrDefault(f => f.FieldType == typeof(SidebarComponent));
            
            if (sidebarField != null)
            {
                var sidebar = sidebarField.GetValue(page) as SidebarComponent;
                if (sidebar != null)
                    return sidebar;
            }
            
            // Try to find it by name in the XAML
            var sidebarByName = page.FindByName<SidebarComponent>("SidebarComponent");
            if (sidebarByName != null)
                return sidebarByName;
                
            // If not found through direct references, search the visual tree
            if (page is ContentPage contentPage && contentPage.Content != null)
            {
                return FindSidebarComponent(contentPage.Content);
            }
            
            return null;
        }

        /// <summary>
        /// Recursively searches for a SidebarComponent in the visual tree
        /// </summary>
        private static SidebarComponent FindSidebarComponent(Element element)
        {
            // If this element is a SidebarComponent, return it
            if (element is SidebarComponent sidebar)
            {
                return sidebar;
            }

            // For elements that can contain other elements
            if (element is Layout layout)
            {
                foreach (var child in layout.Children)
                {
                    if (child is Element childElement)
                    {
                        var result = FindSidebarComponent(childElement);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            else if (element is ContentView contentView && contentView.Content != null)
            {
                if (contentView.Content is Element contentElement)
                {
                    return FindSidebarComponent(contentElement);
                }
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// Interface for pages that can provide their sidebar component directly
    /// </summary>
    public interface IFindSidebarProvider
    {
        SidebarComponent GetSidebar();
    }
} 