using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DailyTaskManager.Core.Localization;

namespace DailyTaskManager.Avalonia.Services;

/// <summary>
/// Shows the interface in the chosen language without touching every view: whenever a text (text block, button or
/// label content, watermark, tooltip, window title) is set, its English wording is looked up in the catalog and the
/// translation is displayed instead. The English original is remembered per control, so changing the language
/// translates what is already on screen. Texts without a translation (user data, names) stay as they are.
/// </summary>
internal static class UiLocalizer
{
    private static readonly ConditionalWeakTable<AvaloniaObject, Dictionary<AvaloniaProperty, string>> Originals = new();
    private static bool _initialized;

    [ThreadStatic]
    private static bool _applying;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        TextBlock.TextProperty.Changed.AddClassHandler<TextBlock>((t, e) => OnChanged(t, TextBlock.TextProperty, e));
        ContentControl.ContentProperty.Changed.AddClassHandler<ContentControl>((t, e) => OnChanged(t, ContentControl.ContentProperty, e));
        TextBox.WatermarkProperty.Changed.AddClassHandler<TextBox>((t, e) => OnChanged(t, TextBox.WatermarkProperty, e));
        ComboBox.PlaceholderTextProperty.Changed.AddClassHandler<ComboBox>((t, e) => OnChanged(t, ComboBox.PlaceholderTextProperty, e));
        Window.TitleProperty.Changed.AddClassHandler<Window>((t, e) => OnChanged(t, Window.TitleProperty, e));
        ToolTip.TipProperty.Changed.AddClassHandler<Control>((t, e) => OnChanged(t, ToolTip.TipProperty, e));

        Loc.LanguageChanged += () => Dispatcher.UIThread.Post(RefreshAll);
    }

    private static void OnChanged(AvaloniaObject target, AvaloniaProperty property, AvaloniaPropertyChangedEventArgs e)
    {
        if (_applying || e.NewValue is not string text || text.Length == 0) return;

        // This is the English text as written in the view or produced by a view model
        Originals.GetOrCreateValue(target)[property] = text;

        var translated = Loc.Translate(text);
        if (!ReferenceEquals(translated, text) && translated != text) Apply(target, property, translated);
    }

    private static void Apply(AvaloniaObject target, AvaloniaProperty property, string text)
    {
        _applying = true;
        try
        {
            target.SetCurrentValue(property, text);
        }
        finally
        {
            _applying = false;
        }
    }

    /// <summary>Re-translates everything already on screen after the language changed</summary>
    private static void RefreshAll()
    {
        foreach (var pair in Originals)
        {
            foreach (var entry in new List<KeyValuePair<AvaloniaProperty, string>>(pair.Value))
            {
                var translated = Loc.Translate(entry.Value);
                if (pair.Key.GetValue(entry.Key) is string current && current == translated) continue;
                Apply(pair.Key, entry.Key, translated);
            }
        }
    }
}
