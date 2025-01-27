using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CLASSIC.ViewModels;

namespace CLASSIC;

/// <summary>
/// The <see cref="ViewLocator"/> class is responsible for resolving the appropriate view
/// for a given view model. It implements the <see cref="IDataTemplate"/> interface, enabling
/// dynamic view resolution in an Avalonia application.
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Builds and returns the appropriate view associated with the given view model.
    /// </summary>
    /// <param name="param">
    /// An object representing the view model for which the corresponding view should be created.
    /// If the provided parameter is null, the method returns null.
    /// </param>
    /// <returns>
    /// Returns an instance of the <see cref="Control"/> associated with the specified view model.
    /// If the view model cannot be resolved to a corresponding view, returns a <see cref="TextBlock"/>
    /// with a "Not Found" message.
    /// </returns>
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    /// <summary>
    /// Determines whether the specified data object is a type that can be associated with a view.
    /// </summary>
    /// <param name="data">
    /// The object to evaluate. Expected to be a view model derived from <see cref="ViewModelBase"/>.
    /// </param>
    /// <returns>
    /// Returns true if the specified data object is of type <see cref="ViewModelBase"/>; otherwise, false.
    /// </returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}