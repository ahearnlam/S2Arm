// SPDX-License-Identifier: Apache-2.0
// Copyright 2026 S2Arm contributors
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace S2Arm.Launcher;

// 大版本首次打开的新功能大窗（11-13 维护者定：启动器首次进入默认弹出，图要大，说清是什么 / 怎么用 / 在哪用；iOS 不做）。同 Mac FeatureSpotlightView.swift。
// 内容随包：docs/site/feature.json + feature/ 里的图；弹不弹、每期只弹一次在 pack/jobs/updater.py feature_spotlight / inbox（popup.kind = feature），这里只显示。
// 关掉 / 跳过 / 去出包 = 这一期看过（inbox-seen --feature）。
public sealed partial class EnvironmentWindow
{
    private int featureIndex;
    // 大窗的图：只有几张、几百 KB，进程里留着不释放（换页重画时旧界面还引用着位图，当场释放会踩 LibraryPage.ClearCovers 记的那个崩溃）
    private readonly Dictionary<string, Bitmap> featureImages = new(StringComparer.OrdinalIgnoreCase);
    private static readonly IBrush FeatureGold = Brush.Parse("#E9B85C");
    private static readonly IBrush FeatureGold2 = Brush.Parse("#F5D48D");

    private bool FeatureOpen => updatePopup is { } p && p.TryGetProperty("kind", out var k) && k.GetString() == "feature";

    private string FeatureText(JsonElement owner, string key) =>
        owner.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Object ? NoteText(value) : "";

    /// <summary>「**…**」加粗，其余原样</summary>
    private static SelectableTextBlock Rich(string text, double size, IBrush color)
    {
        var block = new SelectableTextBlock { FontSize = size, Foreground = color, TextWrapping = TextWrapping.Wrap, LineHeight = size * 1.55 };
        var parts = Regex.Split(text, @"\*\*(.+?)\*\*");
        block.Inlines = [];
        for (var i = 0; i < parts.Length; i++)
            if (parts[i].Length > 0)
                block.Inlines.Add(new Run(parts[i]) { FontWeight = i % 2 == 1 ? FontWeight.Bold : FontWeight.Normal, Foreground = i % 2 == 1 ? Ink : color });
        return block;
    }

    private void CloseFeature(bool export = false)
    {
        var id = updatePopup is { } p && p.TryGetProperty("id", out var v) ? v.GetString() ?? "" : "";
        updatePopup = null; featureIndex = 0;
        if (id.Length > 0) Seen("--feature", id);
        if (!export) { Render(); return; }
        if (catalog?.Games.Any(g => !g.Hidden) == true) Navigate("library");
        else OpenWelcomeGames();   // 库里还没有游戏：直接打开「添加游戏」
    }

    private void RenderFeature(StackPanel body)
    {
        var popup = updatePopup!.Value;
        var pages = Items(popup, "pages").ToList();
        if (pages.Count == 0) { CloseFeature(); return; }
        featureIndex = Math.Clamp(featureIndex, 0, pages.Count - 1);
        var page = pages[featureIndex];
        var last = featureIndex == pages.Count - 1;

        // 上：大图 + 角标 + 关闭
        var art = new Grid { Height = 392, ClipToBounds = true };
        var path = page.TryGetProperty("image", out var img) ? img.GetString() ?? "" : "";
        if (path.Length > 0 && !featureImages.ContainsKey(path))
        {
            try { featureImages[path] = new Bitmap(path); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { }
        }
        if (featureImages.TryGetValue(path, out var bitmap))
            art.Children.Add(new Image { Source = bitmap, Stretch = Stretch.UniformToFill });
        else
            art.Background = Canvas;
        var badges = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, Margin = new Thickness(24, 18),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        badges.Children.Add(new Border { Child = new TextBlock { Text = FeatureText(popup, "badge"), FontSize = 12, FontWeight = FontWeight.Bold, Foreground = Accent },
            Background = new SolidColorBrush(Colors.Black, 0.45), BorderBrush = Brush.Parse("#5A5346"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(999), Padding = new Thickness(10, 4) });
        badges.Children.Add(new Border { Child = new TextBlock { Text = "BETA", FontSize = 12, FontWeight = FontWeight.Black, Foreground = Canvas },
            Background = FeatureGold2, CornerRadius = new CornerRadius(999), Padding = new Thickness(10, 4) });
        art.Children.Add(badges);
        var close = new Button { Content = "✕", Width = 32, Height = 32, CornerRadius = new CornerRadius(16), Padding = new Thickness(0),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Background = new SolidColorBrush(Colors.Black, 0.45), Foreground = Ink, Margin = new Thickness(16),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
        ToolTip.SetTip(close, T("feature.close"));
        AutomationId(close, "feature.close");
        close.Click += (_, _) => CloseFeature();
        art.Children.Add(close);

        // 下：标题 + 是什么 / 怎么用 / 在哪用
        var text = new StackPanel { Margin = new Thickness(32, 20, 32, 16) };
        text.Children.Add(new TextBlock { Text = FeatureText(page, "kicker"), FontSize = 12.5, FontWeight = FontWeight.Bold, Foreground = FeatureGold, LetterSpacing = 0.8 });
        text.Children.Add(new TextBlock { Text = FeatureText(page, "title"), FontSize = 26, FontWeight = FontWeight.Bold, Foreground = Ink, Margin = new Thickness(0, 6, 0, 12), TextWrapping = TextWrapping.Wrap });
        var cells = new Grid { ColumnDefinitions = new ColumnDefinitions("*,14,*,14,*") };
        var col = 0;
        foreach (var (label, key) in new[] { ("feature.what", "what"), ("feature.how", "how"), ("feature.where", "where") })
        {
            var cell = new StackPanel { Spacing = 4 };
            cell.Children.Add(new TextBlock { Text = T(label), FontSize = 12, FontWeight = FontWeight.Bold, Foreground = FeatureGold2 });
            cell.Children.Add(Rich(FeatureText(page, key), 13.5, Brush.Parse("#DAD6CE")));
            var box = new Border { Child = cell, Background = Brush.Parse("#202024"), BorderBrush = Brush.Parse("#2B2B30"), BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12), Padding = new Thickness(13, 11) };
            Grid.SetColumn(box, col); col += 2;
            cells.Children.Add(box);
        }
        text.Children.Add(cells);

        // 底栏：页点、了解更多、跳过 / 上一步、下一步 / 去出包
        var foot = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto,Auto"), Height = 72, Margin = new Thickness(32, 0) };
        var dots = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 7, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        for (var i = 0; i < pages.Count; i++)
        {
            var at = i;
            var dot = new Border { Width = i == featureIndex ? 24 : 8, Height = 8, CornerRadius = new CornerRadius(4),
                Background = i == featureIndex ? Accent : new SolidColorBrush(Colors.White, 0.2), Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) };
            dot.PointerPressed += (_, _) => { featureIndex = at; Render(); };
            dots.Children.Add(dot);
        }
        foot.Children.Add(dots);
        if (popup.TryGetProperty("url", out var u) && Uri.TryCreate(u.GetString(), UriKind.Absolute, out var more))
        {
            var link = new Button { Content = new TextBlock { Text = T("feature.more"), TextDecorations = TextDecorations.Underline, Foreground = Accent, FontSize = 13 },
                Background = Brushes.Transparent, Margin = new Thickness(10, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            AutomationId(link, "feature.more");
            link.Click += async (_, _) => await OpenOfficialAsync(more);
            Grid.SetColumn(link, 1); foot.Children.Add(link);
        }
        var back = new Button { Content = T(featureIndex == 0 ? "feature.skip" : "feature.back"), Background = Brushes.Transparent, Foreground = Brush.Parse("#A8A6A1"),
            FontSize = 14.5, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        AutomationId(back, "feature.back");
        back.Click += (_, _) => { if (featureIndex == 0) CloseFeature(); else { featureIndex--; Render(); } };
        Grid.SetColumn(back, 3); foot.Children.Add(back);
        var next = Primary(T(last ? "feature.go" : "feature.next"), () => { if (last) CloseFeature(export: true); else { featureIndex++; Render(); } });
        next.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
        AutomationId(next, "feature.next");
        Grid.SetColumn(next, 4); foot.Children.Add(next);

        var window = new StackPanel { Width = 1040 };
        window.Children.Add(new Border { Child = art, CornerRadius = new CornerRadius(22, 22, 0, 0), ClipToBounds = true });   // 图的方角别盖住窗口圆角
        window.Children.Add(text);
        window.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Colors.White, 0.08) });
        window.Children.Add(foot);
        var frame = new Border { Child = window, Background = Surface, CornerRadius = new CornerRadius(22), ClipToBounds = true,
            BorderBrush = Brush.Parse("#34343A"), BorderThickness = new Thickness(1), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
        AutomationId(frame, "feature.popup");
        body.Children.Add(frame);
    }
}
