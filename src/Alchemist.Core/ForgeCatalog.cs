namespace Alchemist.Core;

public enum ForgeKind { Attack, Skill, Power }
public enum ForgeTarget { Self, Enemy, AllEnemies }

// Stable formula IDs are saved on one card model; names and display text never act as keys.
public sealed record ForgeFormula(string Id, string Name, Material First, Material Second,
    string Role, string Plan, int Cost, ForgeKind Kind, ForgeTarget Target,
    string Text, IReadOnlyDictionary<string,int> Values, IReadOnlyDictionary<string,int> Upgrade,
    bool Exhaust = false, bool Retain = false)
{
    public string Describe(bool upgraded = false)
    {
        string result = Text;
        foreach (var (key,value) in Values)
            result = result.Replace("{"+key+":diff()}", (value + (upgraded ? Upgrade.GetValueOrDefault(key) : 0)).ToString());
        return result;
    }
    public string Preview => $"{Cost}コスト / {Kind switch { ForgeKind.Attack => "アタック", ForgeKind.Skill => "スキル", _ => "パワー" }} / {Target switch { ForgeTarget.Enemy => "敵1体", ForgeTarget.AllEnemies => "敵全体", _ => "自身" }}\n"
        + Describe() + (Retain ? "\n保留。" : "") + (Exhaust ? "\n廃棄。" : "") + "\n強化後：" + Describe(true);
}

public static class ForgeCatalog
{
    public static readonly IReadOnlyList<ForgeFormula> All = Array.AsReadOnly(new ForgeFormula[] {
        new("greatblade.v1","錬鉄の大剣",Material.Iron,Material.Iron,"切り札","保留して弱体を待ち、大きな一撃と防御を両立。",2,ForgeKind.Attack,ForgeTarget.Enemy,
            "{Damage:diff()}ダメージ。{Block:diff()}ブロックを得る。",
            new Dictionary<string,int>{{"Damage",28},{"Block",8}}, new Dictionary<string,int>{{"Damage",8},{"Block",4}},Retain:true),
        new("siege_shell.v1","徹甲榴弾",Material.Iron,Material.Powder,"切り札","先に弱体を入れてから命中。次の攻撃も通しやすくする。",2,ForgeKind.Attack,ForgeTarget.Enemy,
            "弱体{VulnerablePower:diff()}を与え、その後{Damage:diff()}ダメージ。",
            new Dictionary<string,int>{{"Damage",22},{"VulnerablePower",2}}, new Dictionary<string,int>{{"Damage",6},{"VulnerablePower",1}}),
        new("recycling_reactor.v1","循環錬成炉",Material.Iron,Material.Ether,"デッキの軸","携帯錬金炉や廃棄カードを、ドローと防御に変える。",2,ForgeKind.Power,ForgeTarget.Self,
            "カードを廃棄するたび、{ExhaustBlock:diff()}ブロックを得て、カードを{ExhaustDraw:diff()}枚引く。",
            new Dictionary<string,int>{{"ExhaustBlock",5},{"ExhaustDraw",1}}, new Dictionary<string,int>{{"ExhaustBlock",3}}),
        new("virulent_culture.v1","猛毒培養槽",Material.Herb,Material.Herb,"デッキの軸","守っている間も毒を重ねる、長期戦の勝ち筋。",1,ForgeKind.Power,ForgeTarget.Self,
            "自分のターン開始時、敵全体に毒{Fumes:diff()}を与える。",
            new Dictionary<string,int>{{"Fumes",4}}, new Dictionary<string,int>{{"Fumes",2}}),
        new("toxic_blast.v1","毒霧爆弾",Material.Herb,Material.Powder,"集団戦","全体攻撃と毒を一枚で。培養槽と合わせて継続ダメージ。",1,ForgeKind.Attack,ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージと毒{PoisonPower:diff()}を与える。",
            new Dictionary<string,int>{{"Damage",10},{"PoisonPower",5}}, new Dictionary<string,int>{{"Damage",4},{"PoisonPower",2}}),
        new("distilled_venom.v1","濃縮毒液",Material.Herb,Material.Ether,"切り札","保留できる大きな毒。ボスへ投入し、防御に専念する。",1,ForgeKind.Skill,ForgeTarget.Enemy,
            "毒{PoisonPower:diff()}を与える。カードを{Cards:diff()}枚引く。",
            new Dictionary<string,int>{{"PoisonPower",16},{"Cards",1}}, new Dictionary<string,int>{{"PoisonPower",6}},Exhaust:true,Retain:true),
        new("aether_nova.v1","エーテル爆縮",Material.Powder,Material.Ether,"切り札","危険な集団戦で大きく削り、引いたカードで攻勢を続ける。",1,ForgeKind.Attack,ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージ。カードを{Cards:diff()}枚引く。",
            new Dictionary<string,int>{{"Damage",24},{"Cards",2}}, new Dictionary<string,int>{{"Damage",8}},Exhaust:true)
    });
    public static ForgeFormula Get(string id) => All.SingleOrDefault(f=>f.Id==id)
        ?? throw new InvalidDataException($"未対応の錬成定義です：{id}。カードを初期化しません。");
}
