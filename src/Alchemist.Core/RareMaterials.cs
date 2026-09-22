using System.Text.Json.Serialization;

namespace Alchemist.Core;

public enum RareMaterial { Mercury = 0, Stardust = 1, VoidCrystal = 2 }
public enum MaterialClass { Normal = 0, Rare = 1 }

/// A stable, serializable reference used by reward slots and pending receipts.
public readonly record struct MaterialChoice(MaterialClass Class, int Value)
{
    public static MaterialChoice Normal(Material material) => new(MaterialClass.Normal, (int)material);
    public static MaterialChoice Rare(RareMaterial material) => new(MaterialClass.Rare, (int)material);
    [JsonIgnore] public bool IsValid => Class switch
    {
        MaterialClass.Normal => Enum.IsDefined((Material)Value),
        MaterialClass.Rare => Enum.IsDefined((RareMaterial)Value),
        _ => false
    };
    [JsonIgnore] public Material NormalMaterial => Class == MaterialClass.Normal && IsValid
        ? (Material)Value : throw new InvalidOperationException("通常素材ではありません。");
    [JsonIgnore] public RareMaterial RareMaterial => Class == MaterialClass.Rare && IsValid
        ? (RareMaterial)Value : throw new InvalidOperationException("希少素材ではありません。");
}

public sealed record RareMaterialDefinition(RareMaterial Material, string Id, string Name,
    string EffectName, string Description);

public static class RareMaterials
{
    public static readonly IReadOnlyList<RareMaterialDefinition> All = [
        new(RareMaterial.Mercury, "rare.mercury", "水銀", "流動化", "このカードは保留を得る。"),
        new(RareMaterial.Stardust, "rare.stardust", "星砂", "リプレイ", "このカードをプレイした時、効果をもう1回発動する。"),
        new(RareMaterial.VoidCrystal, "rare.void_crystal", "虚無結晶", "凝縮", "コストが1減り、廃棄を得る。")
    ];

    public static RareMaterialDefinition Get(RareMaterial material) => All.Single(x => x.Material == material);
    public static RareMaterialDefinition Get(string id) => All.SingleOrDefault(x => x.Id == id)
        ?? throw new InvalidDataException($"未知のレア素材IDです: {id}");
    public static string Name(MaterialChoice choice) => choice.Class == MaterialClass.Normal
        ? Recipes.Name(choice.NormalMaterial) : Get(choice.RareMaterial).Name;
}
