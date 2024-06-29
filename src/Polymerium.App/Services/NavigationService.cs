﻿using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Views;
using System;
using System.Collections.Generic;

namespace Polymerium.App.Services
{
    public class NavigationService(ILogger<NavigationService> logger)
    {
        private Action<Type, object?, NavigationTransitionInfo?, bool>? handler;

        public IEnumerable<NavItem> MainNavMenu =
        [
            new NavItem("Home", "/Assets/Icons/House.svg", typeof(HomeView)),
            new NavItem("Instances", "/Assets/Icons/Package.svg", typeof(DesktopView)),
            new NavItem("Accounts", "/Assets/Icons/Japanese dolls.svg", typeof(AccountView)),
            new NavItem("Market", "/Assets/Icons/Shopping bags.svg", typeof(MarketView))
        ];

        public IEnumerable<NavItem> SideNavMenu =
        [
            //new("Toolbox", "/Assets/Icons/Toolbox.svg", typeof(ToolboxView)),
            new NavItem("Settings", "/Assets/Icons/Gear.svg", typeof(SettingView))
        ];

        public void SetHandler(Action<Type, object?, NavigationTransitionInfo?, bool> action)
        {
            handler = action;
        }

        public void Navigate(Type view, object? parameter = null, NavigationTransitionInfo? info = null,
            bool isRoot = false)
        {
            logger.LogInformation("Navigating to {} with \"{}\" as {} in {}", view.Name, parameter,
                isRoot ? "root" : "subpage",
                info?.GetType().Name ?? "default transition");
            handler?.Invoke(view, parameter, info, isRoot);
        }
    }
}