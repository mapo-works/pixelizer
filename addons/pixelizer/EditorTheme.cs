#nullable enable // the addon builds the same in projects that leave <Nullable> off

using Godot;

namespace Pixelizer;

/// <summary>
/// The editor's look, built in code: flat dark panels with a one-pixel rim, flat buttons, Godot's default font. No
/// art files, so the project carries no UI pack. Sizes are in "art pixels" (<c>a</c>): one art pixel is
/// <see cref="ArtPixel"/> screen pixels, a whole number, so rims and gaps stay crisp at any window size.
///
/// The API (<see cref="ScaleFor"/>, <see cref="ArtPixel"/>, <see cref="Build"/>, the <c>TitleLabel</c>, <c>ListPanel</c> and <c>FlatButton</c> variations)
/// mirrors the pixel UI skin of the game this editor came from, so a skinned theme can replace this one later.
/// </summary>
public static class EditorTheme
{
    /// <summary>The game's low-res frame (see <see cref="PixelViewport.BaseResolution"/>).</summary>
    public static readonly Vector2I GameResolution = new(640, 360);

    public static readonly Color Panel = new(0.11f, 0.1f, 0.14f, 0.94f);
    public static readonly Color Rim = new(0.03f, 0.02f, 0.05f);
    public static readonly Color ButtonFace = new(0.22f, 0.2f, 0.27f);
    public static readonly Color ButtonHover = new(0.3f, 0.27f, 0.36f);
    public static readonly Color ButtonPressed = new(0.78f, 0.6f, 0.3f);
    public static readonly Color Field = new(0.06f, 0.05f, 0.08f);
    public static readonly Color Ink = new(0.93f, 0.9f, 0.84f);
    public static readonly Color InkDark = new(0.12f, 0.08f, 0.05f);
    public static readonly Color InkDim = new(0.6f, 0.58f, 0.62f);
    public static readonly Color Accent = new(0.95f, 0.78f, 0.42f);

    /// <summary>
    /// UI scale for a window: the same whole-number upscale the 3D view uses (window height / 360, rounded, see
    /// <see cref="PixelViewport"/>), capped so 640 art pixels still fit across. 720p 2, 1080p 3, 1440p 4,
    /// 4K 6, so the UI grows with the window (and in fullscreen) in step with the game picture.
    /// </summary>
    public static int ScaleFor(Vector2 window)
    {
        int byHeight = Mathf.RoundToInt(window.Y / GameResolution.Y);
        int byWidth = Mathf.FloorToInt(window.X / GameResolution.X);
        return Mathf.Max(1, Mathf.Min(byHeight, byWidth));
    }

    /// <summary>Screen pixels per art pixel at UI <paramref name="scale"/>.</summary>
    public static int ArtPixel(int scale) => Mathf.Max(1, scale);

    public static Theme Build(int scale)
    {
        int a = ArtPixel(scale);
        int text = 8 * a;
        var theme = new Theme { DefaultFontSize = text };

        // Panels.
        var panel = Box(Panel, Rim, a, 4 * a, 3 * a);
        theme.SetStylebox("panel", "PanelContainer", panel);
        theme.SetStylebox("panel", "TooltipPanel", Box(Field, Accent, a, 3 * a, 2 * a));
        theme.SetColor("font_color", "TooltipLabel", Ink);
        // Lists (the Scatter tab's layers): a sunken field-coloured box.
        theme.SetTypeVariation("ListPanel", "PanelContainer");
        theme.SetStylebox("panel", "ListPanel", Box(Field, Rim, a, 2 * a, 2 * a));
        theme.SetFontSize("font_size", "TooltipLabel", text);

        // Buttons: flat face, dark rim; toggled ones turn amber with dark text.
        theme.SetStylebox("normal", "Button", Box(ButtonFace, Rim, a, 3 * a, a));
        theme.SetStylebox("hover", "Button", Box(ButtonHover, Rim, a, 3 * a, a));
        theme.SetStylebox("pressed", "Button", Box(ButtonPressed, Rim, a, 3 * a, a));
        theme.SetStylebox("hover_pressed", "Button", Box(ButtonPressed.Lightened(0.1f), Rim, a, 3 * a, a));
        theme.SetStylebox("disabled", "Button", Box(ButtonFace.Darkened(0.35f), Rim, a, 3 * a, a));
        theme.SetStylebox("focus", "Button", new StyleBoxEmpty());
        theme.SetFontSize("font_size", "Button", text);
        theme.SetColor("font_color", "Button", Ink);
        theme.SetColor("font_focus_color", "Button", Ink);
        theme.SetColor("font_hover_color", "Button", Colors.White);
        theme.SetColor("font_pressed_color", "Button", InkDark);
        theme.SetColor("font_hover_pressed_color", "Button", InkDark);
        theme.SetColor("font_disabled_color", "Button", InkDim);
        theme.SetConstant("h_separation", "Button", 2 * a);

        // Flat buttons (a panel header's fold and close): no face until hovered, then their icons turn white.
        theme.SetTypeVariation("FlatButton", "Button");
        var flatFace = new StyleBoxEmpty { ContentMarginLeft = a, ContentMarginRight = a, ContentMarginTop = a, ContentMarginBottom = a };
        theme.SetStylebox("normal", "FlatButton", flatFace);
        theme.SetStylebox("disabled", "FlatButton", flatFace);
        theme.SetStylebox("hover", "FlatButton", Box(ButtonHover, ButtonHover, 0, a, a));
        theme.SetStylebox("pressed", "FlatButton", Box(ButtonFace, ButtonFace, 0, a, a));
        theme.SetStylebox("hover_pressed", "FlatButton", Box(ButtonHover, ButtonHover, 0, a, a));
        theme.SetColor("icon_normal_color", "FlatButton", Ink);
        theme.SetColor("icon_focus_color", "FlatButton", Ink);
        theme.SetColor("icon_hover_color", "FlatButton", Colors.White);
        theme.SetColor("icon_pressed_color", "FlatButton", Ink);
        theme.SetColor("icon_hover_pressed_color", "FlatButton", Colors.White);

        // Labels.
        theme.SetFontSize("font_size", "Label", text);
        theme.SetColor("font_color", "Label", Ink);
        theme.SetTypeVariation("TitleLabel", "Label");
        theme.SetFontSize("font_size", "TitleLabel", 10 * a);
        theme.SetColor("font_color", "TitleLabel", Accent);

        // Text fields.
        var field = Box(Field, Rim, a, 3 * a, a);
        var focused = Box(Field, Accent, a, 3 * a, a);
        theme.SetStylebox("normal", "LineEdit", field);
        theme.SetStylebox("focus", "LineEdit", focused);
        theme.SetFontSize("font_size", "LineEdit", text);
        theme.SetColor("font_color", "LineEdit", Ink);
        theme.SetColor("font_placeholder_color", "LineEdit", InkDim);
        theme.SetColor("caret_color", "LineEdit", Accent);

        // Progress bars.
        theme.SetStylebox("background", "ProgressBar", Box(Field, Rim, a, 0, 0));
        theme.SetStylebox("fill", "ProgressBar", Box(Accent, Accent.Darkened(0.4f), a, 0, 0));

        // Scroll bars: thin, flat.
        var grabber = Box(ButtonFace, ButtonFace, 0, 0, 0);
        grabber.ContentMarginLeft = grabber.ContentMarginRight = 2 * a;
        theme.SetStylebox("grabber", "VScrollBar", grabber);
        theme.SetStylebox("grabber_highlight", "VScrollBar", Box(ButtonHover, ButtonHover, 0, 2 * a, 0));
        theme.SetStylebox("grabber_pressed", "VScrollBar", Box(ButtonPressed, ButtonPressed, 0, 2 * a, 0));
        theme.SetStylebox("scroll", "VScrollBar", Box(Field, Field, 0, 2 * a, 0));

        // Bars along the window's edges (menu bar, status bar): opaque, a rim on the side facing the view.
        theme.SetTypeVariation("BarPanel", "PanelContainer");
        var bar = Box(new Color(Panel, 1f), Rim, 0, 3 * a, a);
        bar.BorderWidthBottom = bar.BorderWidthTop = a;
        theme.SetStylebox("panel", "BarPanel", bar);

        // The menu bar's titles: flat until hovered or open.
        var flat = new StyleBoxEmpty { ContentMarginLeft = 4 * a, ContentMarginRight = 4 * a, ContentMarginTop = a, ContentMarginBottom = a };
        theme.SetStylebox("normal", "MenuBar", flat);
        theme.SetStylebox("disabled", "MenuBar", flat);
        theme.SetStylebox("focus", "MenuBar", new StyleBoxEmpty());
        theme.SetStylebox("hover", "MenuBar", Box(ButtonHover, ButtonHover, 0, 4 * a, a));
        theme.SetStylebox("pressed", "MenuBar", Box(ButtonPressed, ButtonPressed, 0, 4 * a, a));
        theme.SetStylebox("hover_pressed", "MenuBar", Box(ButtonPressed, ButtonPressed, 0, 4 * a, a));
        theme.SetFontSize("font_size", "MenuBar", text);
        theme.SetColor("font_color", "MenuBar", Ink);
        theme.SetColor("font_hover_color", "MenuBar", Colors.White);
        theme.SetColor("font_pressed_color", "MenuBar", InkDark);
        theme.SetColor("font_hover_pressed_color", "MenuBar", InkDark);
        theme.SetConstant("h_separation", "MenuBar", 0);

        // Menus.
        theme.SetStylebox("panel", "PopupMenu", Box(new Color(Panel, 1f), Rim, a, 2 * a, 2 * a));
        theme.SetStylebox("hover", "PopupMenu", Box(ButtonHover, ButtonHover, 0, 0, 0));
        var line = new StyleBoxLine { Color = ButtonFace, Thickness = a, GrowBegin = -2 * a, GrowEnd = -2 * a };
        theme.SetStylebox("separator", "PopupMenu", line);
        theme.SetStylebox("labeled_separator_left", "PopupMenu", line);
        theme.SetStylebox("labeled_separator_right", "PopupMenu", line);
        theme.SetFontSize("font_size", "PopupMenu", text);
        theme.SetColor("font_color", "PopupMenu", Ink);
        theme.SetColor("font_hover_color", "PopupMenu", Colors.White);
        theme.SetColor("font_disabled_color", "PopupMenu", InkDim.Darkened(0.2f));
        theme.SetColor("font_accelerator_color", "PopupMenu", InkDim);
        theme.SetConstant("v_separation", "PopupMenu", 3 * a);
        theme.SetConstant("h_separation", "PopupMenu", 4 * a);
        theme.SetConstant("item_start_padding", "PopupMenu", 3 * a);
        theme.SetConstant("item_end_padding", "PopupMenu", 3 * a);

        // Separators in panels and bars.
        var hline = new StyleBoxLine { Color = Rim, Thickness = a };
        var vline = new StyleBoxLine { Color = Rim, Thickness = a, Vertical = true };
        theme.SetStylebox("separator", "HSeparator", hline);
        theme.SetStylebox("separator", "VSeparator", vline);
        theme.SetConstant("separation", "HSeparator", 3 * a);
        theme.SetConstant("separation", "VSeparator", 3 * a);

        // Sliders (the blueprint's opacity).
        theme.SetStylebox("slider", "HSlider", Box(Field, Rim, a, 0, a));
        theme.SetStylebox("grabber_area", "HSlider", Box(ButtonPressed, ButtonPressed, 0, 0, a));
        theme.SetStylebox("grabber_area_highlight", "HSlider", Box(ButtonPressed.Lightened(0.1f), ButtonPressed, 0, 0, a));

        theme.SetConstant("separation", "VBoxContainer", 2 * a);
        theme.SetConstant("separation", "HBoxContainer", 2 * a);
        theme.SetConstant("h_separation", "GridContainer", a);
        theme.SetConstant("v_separation", "GridContainer", a);
        foreach (string side in new[] { "margin_left", "margin_top", "margin_right", "margin_bottom" })
            theme.SetConstant(side, "MarginContainer", 4 * a);
        return theme;
    }

    /// <summary>A flat box with square corners, a <paramref name="rim"/>-wide border and content margins.</summary>
    private static StyleBoxFlat Box(Color fill, Color border, int rim, int marginX, int marginY)
    {
        var box = new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            ContentMarginLeft = marginX,
            ContentMarginRight = marginX,
            ContentMarginTop = marginY,
            ContentMarginBottom = marginY,
            AntiAliasing = false,
        };
        box.SetBorderWidthAll(rim);
        return box;
    }
}
