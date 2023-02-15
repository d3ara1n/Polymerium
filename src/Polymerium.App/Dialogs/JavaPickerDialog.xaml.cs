// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Dialogs;

public sealed partial class JavaPickerDialog : ContentDialog
{
    // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty SelectedJavaProperty = DependencyProperty.Register(
        nameof(SelectedJava),
        typeof(JavaInstallationModel),
        typeof(JavaPickerDialog),
        new PropertyMetadata(null)
    );

    // Using a DependencyProperty as the backing store for JavaInstallations.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty JavaInstallationsProperty =
        DependencyProperty.Register(
            nameof(JavaInstallations),
            typeof(IEnumerable<JavaInstallationModel>),
            typeof(JavaPickerDialog),
            new PropertyMetadata(null)
        );

    public JavaPickerDialog()
    {
        InitializeComponent();
    }

    public JavaInstallationModel SelectedJava
    {
        get => (JavaInstallationModel)GetValue(SelectedJavaProperty);
        set => SetValue(SelectedJavaProperty, value);
    }

    public IEnumerable<JavaInstallationModel> JavaInstallations
    {
        get => (IEnumerable<JavaInstallationModel>)GetValue(JavaInstallationsProperty);
        set => SetValue(JavaInstallationsProperty, value);
    }
}