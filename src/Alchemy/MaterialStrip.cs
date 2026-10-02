using Alchemy.Core;
using Godot;
using MegaCrit.Sts2.Core.Nodes;
using Material = Alchemy.Core.Material;

namespace Alchemy;

/// <summary>
/// The materials always in sight (user, 2026-10-03, option A): under the material button, one row of the four normal
/// materials as their icons with counts (none held: dim), and a second row with the rare materials held, only while
/// there are any. Hovering an icon names the material and what it is for. Shown wherever the button is.
/// </summary>
public static class MaterialStrip
{
    private const float IconSize = 34, CellWidth = 72, RowHeight = 40;
    private static Control? strip;
    private static string drawn = "";

    public static Control? Node => GodotObject.IsInstanceValid(strip) ? strip : null;

    /// Bottom edge of the strip in screen units, for what is laid out under it (the phase dial).
    public static float Bottom => Node is { } s ? s.Position.Y + s.Size.Y : WorkshopUi.LauncherPosition.Y + 76;

    public static void Update(NRun run, MaterialBox? box, Control? launcher)
    {
        if (box is null || launcher is null || !GodotObject.IsInstanceValid(launcher))
        {
            if (Node is { } hidden) hidden.Visible = false;
            return;
        }
        if (Node is null || strip!.GetParent() != launcher.GetParent())
        {
            if (Node is { } old) old.QueueFree();
            strip = new Control { Name = "AlchemyMaterialStrip", MouseFilter = Control.MouseFilterEnum.Ignore };
            launcher.GetParent().AddChild(strip);
            launcher.GetParent().MoveChild(strip, launcher.GetIndex() + 1);
            drawn = "";
        }
        strip!.Visible = launcher.Visible;
        strip.Position = launcher.Position + new Vector2(0, launcher.Size.Y + 4);
        var inv = box.Inventory;
        string key = string.Join(",", inv.Counts) + "|" + string.Join(",", inv.RareCounts);
        if (key == drawn) return;
        drawn = key;
        foreach (var child in strip.GetChildren()) { strip.RemoveChild(child); child.QueueFree(); }
        var normal = Enum.GetValues<Material>().Select(m => (MaterialChoice.Normal(m), inv.Counts[(int)m])).ToList();
        var rare = Enum.GetValues<RareMaterial>().Where(r => inv.RareCounts[(int)r] > 0)
            .Select(r => (MaterialChoice.Rare(r), inv.RareCounts[(int)r])).ToList();
        Row(normal, 0, dimZero: true);
        if (rare.Count > 0) Row(rare, 1, dimZero: false);
        strip.Size = new Vector2(CellWidth * 4, RowHeight * (rare.Count > 0 ? 2 : 1));
    }

    private static void Row(List<(MaterialChoice Material, int Count)> cells, int row, bool dimZero)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            var (material, count) = cells[i];
            var cell = new Control
            {
                Position = new Vector2(i * CellWidth, row * RowHeight), Size = new Vector2(CellWidth - 4, RowHeight - 4),
                MouseFilter = Control.MouseFilterEnum.Pass,
                TooltipText = $"{RareMaterials.Name(material)}：{Hint(material)}\n所持 {count}個",
                Modulate = dimZero && count == 0 ? new Color(1, 1, 1, 0.38f) : Colors.White,
            };
            var back = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };
            back.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.05f, 0.07f, 0.09f, 0.78f),
                BorderColor = material.Class == MaterialClass.Rare ? new Color("b48ce0") : new Color("5a4a30"),
            };
            style.SetBorderWidthAll(1);
            style.SetCornerRadiusAll(6);
            back.AddThemeStyleboxOverride("panel", style);
            cell.AddChild(back);
            if (IconArt.Big(IconArt.MaterialSlug(material)) is { } path && ResourceLoader.Load<Texture2D>(path) is { } icon)
            {
                // Size after ExpandMode: set first, the texture's own 128 px would win.
                var image = new TextureRect
                {
                    Texture = icon, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore,
                };
                image.Position = new Vector2(2, 1);
                image.Size = new Vector2(IconSize, IconSize);
                image.CustomMinimumSize = Vector2.Zero;
                cell.AddChild(image);
            }
            var label = new Label
            {
                Text = count.ToString(), Position = new Vector2(IconSize + 2, 2), Size = new Vector2(CellWidth - IconSize - 8, IconSize),
                HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            label.AddThemeFontSizeOverride("font_size", 22);
            label.AddThemeColorOverride("font_color", new Color("f2e6c8"));
            label.AddThemeColorOverride("font_outline_color", Colors.Black);
            label.AddThemeConstantOverride("outline_size", 4);
            cell.AddChild(label);
            strip!.AddChild(cell);
        }
    }

    private static string Hint(MaterialChoice material) => material.Class == MaterialClass.Rare
        ? RareMaterials.Get(material.RareMaterial).Description
        : material.NormalMaterial switch
        {
            Material.Iron => "物理・防御", Material.Herb => "毒・弱体",
            Material.Powder => "高火力・全体", _ => "ドロー・循環",
        };
}
