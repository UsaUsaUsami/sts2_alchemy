namespace Alchemist.Core;

/// <summary>
/// The current workshop pool. Every unordered pair of normal materials has one result for each
/// base: weapon, remedy and device. Legacy formulas remain loadable through <see cref="All"/>, but
/// are deliberately excluded from <see cref="Craftable"/>.
/// </summary>
public static class ForgeCatalog
{
    private static Dictionary<string, int> V(params (string Key, int Value)[] values)
        => values.ToDictionary(x => x.Key, x => x.Value);

    public static IReadOnlyList<ForgeFormula> Craftable { get; } = Array.AsReadOnly(new[]
    {
        // Weapons: immediate attacks. Only Phase Needle is a multi-hit card.
        F("weapon.iron_iron.v2", "焼入れの剣", ForgeBase.Weapon, Material.Iron, Material.Iron,
            "単体攻撃", "余計な仕掛けを持たない、安定した主力武器。", 1, ForgeKind.Attack, ForgeTarget.Enemy,
            "{Damage:diff()}ダメージ。", V(("Damage", 11)), V(("Damage", 4))),
        F("weapon.iron_herb.v2", "腐蝕の刃", ForgeBase.Weapon, Material.Iron, Material.Herb,
            "毒攻撃", "直接打撃と毒を半分ずつ進める。", 1, ForgeKind.Attack, ForgeTarget.Enemy,
            "{Damage:diff()}ダメージ。毒{PoisonPower:diff()}を与える。", V(("Damage", 7), ("PoisonPower", 3)), V(("Damage", 2), ("PoisonPower", 1))),
        F("weapon.iron_powder.v2", "破城薬莢", ForgeBase.Weapon, Material.Iron, Material.Powder,
            "攻撃の準備", "弱体を先に刻み、この一撃と後続の攻撃を通す。", 1, ForgeKind.Attack, ForgeTarget.Enemy,
            "弱体{VulnerablePower:diff()}を与え、{Damage:diff()}ダメージ。", V(("VulnerablePower", 1), ("Damage", 9)), V(("Damage", 3))),
        F("weapon.iron_ether.v2", "帰還針", ForgeBase.Weapon, Material.Iron, Material.Ether,
            "手札調整", "小さく攻撃し、使い切りで次の札へ繋ぐ。", 0, ForgeKind.Attack, ForgeTarget.Enemy,
            "{Damage:diff()}ダメージ。カードを{Cards:diff()}枚引く。", V(("Damage", 5), ("Cards", 1)), V(("Damage", 3)), exhaust: true),
        F("weapon.herb_herb.v2", "毒穿ち", ForgeBase.Weapon, Material.Herb, Material.Herb,
            "毒攻撃", "小さな傷から濃い毒を送り込む。", 1, ForgeKind.Attack, ForgeTarget.Enemy,
            "{Damage:diff()}ダメージ。毒{PoisonPower:diff()}を与える。", V(("Damage", 4), ("PoisonPower", 6)), V(("PoisonPower", 3))),
        F("weapon.herb_powder.v2", "煙裂き", ForgeBase.Weapon, Material.Herb, Material.Powder,
            "集団戦", "敵全体へ傷と毒を浅く広げる。", 1, ForgeKind.Attack, ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージと毒{PoisonPower:diff()}を与える。", V(("Damage", 5), ("PoisonPower", 2)), V(("Damage", 2), ("PoisonPower", 1))),
        F("weapon.herb_ether.v2", "蒸留牙", ForgeBase.Weapon, Material.Herb, Material.Ether,
            "手札調整", "毒を仕込んで次の一手を探す使い切りの刃。", 1, ForgeKind.Attack, ForgeTarget.Enemy,
            "{Damage:diff()}ダメージ。毒{PoisonPower:diff()}を与える。カードを{Cards:diff()}枚引く。", V(("Damage", 6), ("PoisonPower", 3), ("Cards", 1)), V(("PoisonPower", 2)), exhaust: true),
        F("weapon.powder_powder.v2", "成形爆薬", ForgeBase.Weapon, Material.Powder, Material.Powder,
            "切り札", "一度だけ大きな単体打点を作る。", 2, ForgeKind.Attack, ForgeTarget.Enemy,
            "{Damage:diff()}ダメージ。", V(("Damage", 21)), V(("Damage", 7)), exhaust: true),
        F("weapon.powder_ether.v2", "閃光斉射", ForgeBase.Weapon, Material.Powder, Material.Ether,
            "集団戦", "盤面を削りながら次の札へ移る。", 1, ForgeKind.Attack, ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージ。カードを{Cards:diff()}枚引く。", V(("Damage", 7), ("Cards", 1)), V(("Damage", 3)), exhaust: true),
        F("weapon.ether_ether.v2", "位相針", ForgeBase.Weapon, Material.Ether, Material.Ether,
            "多段攻撃", "新プールで唯一の多段武器。手札を減らさず軽く刻む。", 1, ForgeKind.Attack, ForgeTarget.Enemy,
            "{Damage:diff()}ダメージを{Hits:diff()}回与える。カードを{Cards:diff()}枚引く。", V(("Damage", 4), ("Hits", 2), ("Cards", 1)), V(("Damage", 1))),

        // Remedies: defense, poison and debuffs. None grant permanent Strength or Dexterity.
        F("remedy.iron_iron.v2", "鉄皮膏", ForgeBase.Remedy, Material.Iron, Material.Iron,
            "防御", "そのターンを確実に守る基本薬。", 1, ForgeKind.Skill, ForgeTarget.Self,
            "{Block:diff()}ブロックを得る。", V(("Block", 10)), V(("Block", 3))),
        F("remedy.iron_herb.v2", "麻痺チンキ", ForgeBase.Remedy, Material.Iron, Material.Herb,
            "弱体防御", "敵の攻撃を弱めながら最低限の守りを得る。", 1, ForgeKind.Skill, ForgeTarget.Enemy,
            "弱体{WeakPower:diff()}を与える。{Block:diff()}ブロックを得る。", V(("WeakPower", 1), ("Block", 6)), V(("Block", 3))),
        F("remedy.iron_powder.v2", "焼灼剤", ForgeBase.Remedy, Material.Iron, Material.Powder,
            "緊急防御", "使い切りの厚い防御で危険な一手を越える。", 1, ForgeKind.Skill, ForgeTarget.Self,
            "{Block:diff()}ブロックを得る。", V(("Block", 14)), V(("Block", 5)), exhaust: true),
        F("remedy.iron_ether.v2", "整流薬", ForgeBase.Remedy, Material.Iron, Material.Ether,
            "守りと循環", "守りながら必要な札を探す。", 1, ForgeKind.Skill, ForgeTarget.Self,
            "{Block:diff()}ブロックを得る。カードを{Cards:diff()}枚引く。", V(("Block", 7), ("Cards", 1)), V(("Block", 3))),
        F("remedy.herb_herb.v2", "濃毒液", ForgeBase.Remedy, Material.Herb, Material.Herb,
            "毒", "攻撃せず、長期戦の勝ち筋だけを進める。", 1, ForgeKind.Skill, ForgeTarget.Enemy,
            "毒{PoisonPower:diff()}を与える。", V(("PoisonPower", 8)), V(("PoisonPower", 3))),
        F("remedy.herb_powder.v2", "窒息胞子", ForgeBase.Remedy, Material.Herb, Material.Powder,
            "集団毒", "敵全体へ毒を散布する。", 1, ForgeKind.Skill, ForgeTarget.AllEnemies,
            "敵全体に毒{PoisonPower:diff()}を与える。", V(("PoisonPower", 4)), V(("PoisonPower", 2))),
        F("remedy.herb_ether.v2", "澄明薬", ForgeBase.Remedy, Material.Herb, Material.Ether,
            "手札調整", "無料で守りと手札を少し整える使い切り薬。", 0, ForgeKind.Skill, ForgeTarget.Self,
            "{Block:diff()}ブロックを得る。カードを{Cards:diff()}枚引く。", V(("Block", 3), ("Cards", 1)), V(("Block", 3)), exhaust: true),
        F("remedy.powder_powder.v2", "爆薬瓶", ForgeBase.Remedy, Material.Powder, Material.Powder,
            "集団戦", "使い切りの瓶で敵全体を削る。", 1, ForgeKind.Attack, ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージ。", V(("Damage", 9)), V(("Damage", 3)), exhaust: true),
        F("remedy.powder_ether.v2", "火花薬", ForgeBase.Remedy, Material.Powder, Material.Ether,
            "攻撃と循環", "瞬間的な火力を出し、次の札へ繋ぐ。", 1, ForgeKind.Attack, ForgeTarget.Enemy,
            "{Damage:diff()}ダメージ。カードを{Cards:diff()}枚引く。", V(("Damage", 10), ("Cards", 1)), V(("Damage", 3)), exhaust: true),
        F("remedy.ether_ether.v2", "明晰蒸留液", ForgeBase.Remedy, Material.Ether, Material.Ether,
            "手札調整", "手札を入れ替えることだけに特化した使い切り薬。", 1, ForgeKind.Skill, ForgeTarget.Self,
            "カードを{Cards:diff()}枚引く。", V(("Cards", 2)), V(("Cards", 1)), exhaust: true),

        // Devices: persistent exhaust engines and phase tools, with a few defensive mechanisms.
        F("device.iron_iron.v2", "鋲留め機構", ForgeBase.Device, Material.Iron, Material.Iron,
            "持続防御", "このターンの守りを次のターンへ少し残す。", 1, ForgeKind.Skill, ForgeTarget.Self,
            "{Block:diff()}ブロックと不動{BlurPower:diff()}を得る。", V(("Block", 6), ("BlurPower", 1)), V(("Block", 3))),
        F("device.iron_herb.v2", "培養槽", ForgeBase.Device, Material.Iron, Material.Herb,
            "毒の軸", "毎ターン少量の毒を積み、守る時間を勝ち筋に変える。", 1, ForgeKind.Power, ForgeTarget.Self,
            "自分のターン開始時、敵全体に毒{Fumes:diff()}を与える。", V(("Fumes", 2)), V(("Fumes", 1))),
        F("device.iron_powder.v2", "反動板", ForgeBase.Device, Material.Iron, Material.Powder,
            "反撃防御", "守りながら接触した敵へ反撃する。", 1, ForgeKind.Skill, ForgeTarget.Self,
            "{Block:diff()}ブロックとトゲ{ThornsPower:diff()}を得る。", V(("Block", 7), ("ThornsPower", 2)), V(("Block", 3), ("ThornsPower", 1))),
        F("device.iron_ether.v2", "再生炉", ForgeBase.Device, Material.Iron, Material.Ether,
            "廃棄防御", "炉の起動と廃棄カードを防御へ変える。", 2, ForgeKind.Power, ForgeTarget.Self,
            "カードを廃棄するたび、{ExhaustBlock:diff()}ブロックを得る。", V(("ExhaustBlock", 3)), V(("ExhaustBlock", 1))),
        F("device.herb_herb.v2", "胞子ふいご", ForgeBase.Device, Material.Herb, Material.Herb,
            "毒の軸", "重い初期投資で、毎ターン濃い毒を散布する。", 2, ForgeKind.Power, ForgeTarget.Self,
            "自分のターン開始時、敵全体に毒{Fumes:diff()}を与える。", V(("Fumes", 3)), V(("Fumes", 1))),
        F("device.herb_powder.v2", "腐蝕噴霧器", ForgeBase.Device, Material.Herb, Material.Powder,
            "集団戦", "敵全体へ小さな打撃と毒を同時に撒く。", 1, ForgeKind.Attack, ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージと毒{PoisonPower:diff()}を与える。", V(("Damage", 3), ("PoisonPower", 3)), V(("Damage", 2), ("PoisonPower", 1))),
        F("device.herb_ether.v2", "記憶蒸留器", ForgeBase.Device, Material.Herb, Material.Ether,
            "廃棄循環", "炉の起動を含む廃棄を手札へ変える。", 2, ForgeKind.Power, ForgeTarget.Self,
            "カードを廃棄するたび、カードを{ExhaustDraw:diff()}枚引く。", V(("ExhaustDraw", 1)), V(("ExhaustDraw", 1))),
        F("device.powder_powder.v2", "爆圧室", ForgeBase.Device, Material.Powder, Material.Powder,
            "集団戦", "装置を使い切り、敵全体へ大きな爆発を起こす。", 2, ForgeKind.Attack, ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージ。", V(("Damage", 14)), V(("Damage", 5)), exhaust: true),
        F("device.powder_ether.v2", "位相蓄電器", ForgeBase.Device, Material.Powder, Material.Ether,
            "素材相制御", "手札を減らさず、狙う素材相を一つ先へ送る。", 0, ForgeKind.Skill, ForgeTarget.Self,
            "カードを{Cards:diff()}枚引く。素材相を{AdvancePhase:diff()}つ進める。", V(("Cards", 1), ("AdvancePhase", 1)), V(("Cards", 1)), exhaust: true),
        F("device.ether_ether.v2", "残響コイル", ForgeBase.Device, Material.Ether, Material.Ether,
            "持続防御", "防御を次のターンへ持ち越し、行動の余白を作る。", 1, ForgeKind.Skill, ForgeTarget.Self,
            "{Block:diff()}ブロックと不動{BlurPower:diff()}を得る。", V(("Block", 6), ("BlurPower", 1)), V(("Block", 3)))
    });

    public static IReadOnlyList<ForgeFormula> All { get; } = Craftable.Concat(LegacyForgeCatalog.All).ToArray();

    public static ForgeFormula Get(string id) => All.SingleOrDefault(f => f.Id == id)
        ?? throw new InvalidDataException($"未対応の錬成定義です：{id}。カードを初期化しません。");

    public static string BaseName(ForgeBase value) => value switch
    {
        ForgeBase.Weapon => "武器",
        ForgeBase.Remedy => "薬",
        ForgeBase.Device => "装置",
        _ => "旧式"
    };

    private static ForgeFormula F(string id, string name, ForgeBase @base, Material first, Material second,
        string role, string plan, int cost, ForgeKind kind, ForgeTarget target, string text,
        IReadOnlyDictionary<string, int> values, IReadOnlyDictionary<string, int> upgrade,
        bool exhaust = false, bool retain = false)
        => new(id, name, [first, second], role, plan, cost, kind, target, text, values, upgrade,
            exhaust, retain, @base);
}
