namespace Alchemist.Core;

public enum ForgeKind { Attack, Skill, Power }
public enum ForgeTarget { Self, Enemy, AllEnemies }

// Stable formula IDs are saved on one card model; names and display text never act as keys.
// Materials is unordered and allows repeats; commons use 2, uncommons 3, rares 4 (AlchemyState.Recipes).
public sealed record ForgeFormula(string Id, string Name, IReadOnlyList<Material> Materials,
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
        new("greatblade.v1","錬鉄の大剣",[Material.Iron,Material.Iron],"切り札","保留して弱体を待ち、大きな一撃と防御を両立。",2,ForgeKind.Attack,ForgeTarget.Enemy,
            "{Damage:diff()}ダメージ。{Block:diff()}ブロックを得る。",
            new Dictionary<string,int>{{"Damage",28},{"Block",8}}, new Dictionary<string,int>{{"Damage",8},{"Block",4}},Retain:true),
        new("siege_shell.v1","徹甲榴弾",[Material.Iron,Material.Powder],"切り札","先に弱体を入れてから命中。次の攻撃も通しやすくする。",2,ForgeKind.Attack,ForgeTarget.Enemy,
            "弱体{VulnerablePower:diff()}を与え、その後{Damage:diff()}ダメージ。",
            new Dictionary<string,int>{{"Damage",22},{"VulnerablePower",2}}, new Dictionary<string,int>{{"Damage",6},{"VulnerablePower",1}}),
        new("recycling_reactor.v1","循環錬成炉",[Material.Iron,Material.Ether],"デッキの軸","携帯錬金炉や廃棄カードを、ドローと防御に変える。",2,ForgeKind.Power,ForgeTarget.Self,
            "カードを廃棄するたび、{ExhaustBlock:diff()}ブロックを得て、カードを{ExhaustDraw:diff()}枚引く。",
            new Dictionary<string,int>{{"ExhaustBlock",5},{"ExhaustDraw",1}}, new Dictionary<string,int>{{"ExhaustBlock",3}}),
        new("virulent_culture.v1","猛毒培養槽",[Material.Herb,Material.Herb],"デッキの軸","守っている間も毒を重ねる、長期戦の勝ち筋。",1,ForgeKind.Power,ForgeTarget.Self,
            "自分のターン開始時、敵全体に毒{Fumes:diff()}を与える。",
            new Dictionary<string,int>{{"Fumes",4}}, new Dictionary<string,int>{{"Fumes",2}}),
        new("toxic_blast.v1","毒霧爆弾",[Material.Herb,Material.Powder],"集団戦","全体攻撃と毒を一枚で。培養槽と合わせて継続ダメージ。",1,ForgeKind.Attack,ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージと毒{PoisonPower:diff()}を与える。",
            new Dictionary<string,int>{{"Damage",10},{"PoisonPower",5}}, new Dictionary<string,int>{{"Damage",4},{"PoisonPower",2}}),
        new("distilled_venom.v1","濃縮毒液",[Material.Herb,Material.Ether],"切り札","保留できる大きな毒。ボスへ投入し、防御に専念する。",1,ForgeKind.Skill,ForgeTarget.Enemy,
            "毒{PoisonPower:diff()}を与える。カードを{Cards:diff()}枚引く。",
            new Dictionary<string,int>{{"PoisonPower",16},{"Cards",1}}, new Dictionary<string,int>{{"PoisonPower",6}},Exhaust:true,Retain:true),
        new("aether_nova.v1","エーテル爆縮",[Material.Powder,Material.Ether],"切り札","危険な集団戦で大きく削り、引いたカードで攻勢を続ける。",1,ForgeKind.Attack,ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージ。カードを{Cards:diff()}枚引く。",
            new Dictionary<string,int>{{"Damage",24},{"Cards",2}}, new Dictionary<string,int>{{"Damage",8}},Exhaust:true),

        new("alloy_flurry.v1","合金乱舞",[Material.Iron,Material.Iron],"多段攻撃","筋力を得てから使う主役。低い打点を何度も叩き込む。",2,ForgeKind.Attack,ForgeTarget.Enemy,
            "{Damage:diff()}ダメージを{Hits:diff()}回与える。",
            new Dictionary<string,int>{{"Damage",6},{"Hits",4}}, new Dictionary<string,int>{{"Damage",2}}),
        new("adamant_tonic.v1","金剛の霊薬",[Material.Iron,Material.Iron],"自己強化","筋力を恒久的に高め、多段攻撃を育てる。",1,ForgeKind.Power,ForgeTarget.Self,
            "筋力{StrengthPower:diff()}を得る。",
            new Dictionary<string,int>{{"StrengthPower",3}}, new Dictionary<string,int>{{"StrengthPower",1}}),
        new("war_elixir.v1","戦支度の霊薬",[Material.Iron,Material.Herb],"自己強化","攻撃と防御を同時に底上げし、デッキ全体を主役にする。",2,ForgeKind.Power,ForgeTarget.Self,
            "筋力{StrengthPower:diff()}と敏捷{DexterityPower:diff()}を得る。",
            new Dictionary<string,int>{{"StrengthPower",2},{"DexterityPower",2}}, new Dictionary<string,int>{{"StrengthPower",1},{"DexterityPower",1}}),
        new("serrated_barrage.v1","鋸刃連射",[Material.Iron,Material.Herb],"多段攻撃","先に弱体を付け、続く連撃と後続の攻撃を通す。",1,ForgeKind.Attack,ForgeTarget.Enemy,
            "弱体{VulnerablePower:diff()}を与え、{Damage:diff()}ダメージを{Hits:diff()}回与える。",
            new Dictionary<string,int>{{"VulnerablePower",1},{"Damage",4},{"Hits",4}}, new Dictionary<string,int>{{"Damage",1},{"Hits",1}}),
        new("rotary_cannon.v1","回転式錬金砲",[Material.Iron,Material.Powder],"多段攻撃","筋力を一発ごとに乗せる六連射。単体の敵を削り切る。",1,ForgeKind.Attack,ForgeTarget.Enemy,
            "{Damage:diff()}ダメージを{Hits:diff()}回与える。",
            new Dictionary<string,int>{{"Damage",3},{"Hits",6}}, new Dictionary<string,int>{{"Damage",1}}),
        new("blast_plating.v1","爆圧装甲",[Material.Iron,Material.Powder],"攻防一体","その場を守りながら、以降の攻撃を強化する。",2,ForgeKind.Skill,ForgeTarget.Self,
            "{Block:diff()}ブロックを得る。筋力{StrengthPower:diff()}を得る。",
            new Dictionary<string,int>{{"Block",14},{"StrengthPower",2}}, new Dictionary<string,int>{{"Block",5},{"StrengthPower",1}},Exhaust:true),
        new("reinforcement_matrix.v1","補強行列",[Material.Iron,Material.Ether],"自己強化","敏捷を高め、軽い防御カードにも終盤の役割を持たせる。",1,ForgeKind.Power,ForgeTarget.Self,
            "敏捷{DexterityPower:diff()}を得る。",
            new Dictionary<string,int>{{"DexterityPower",3}}, new Dictionary<string,int>{{"DexterityPower",1}}),
        new("kinetic_loop.v1","運動連鎖",[Material.Iron,Material.Ether],"多段攻撃","三連撃から次のカードへ繋ぎ、強化した筋力を回転させる。",1,ForgeKind.Attack,ForgeTarget.Enemy,
            "{Damage:diff()}ダメージを{Hits:diff()}回与える。カードを{Cards:diff()}枚引く。",
            new Dictionary<string,int>{{"Damage",6},{"Hits",3},{"Cards",1}}, new Dictionary<string,int>{{"Damage",2}}),
        new("footwork_distillate.v1","軽業の蒸留液",[Material.Herb,Material.Herb],"自己強化","敏捷に特化し、毒が敵を倒すまでの守りを固める。",1,ForgeKind.Power,ForgeTarget.Self,
            "敏捷{DexterityPower:diff()}を得る。",
            new Dictionary<string,int>{{"DexterityPower",3}}, new Dictionary<string,int>{{"DexterityPower",2}}),
        new("venom_needles.v1","毒針斉射",[Material.Herb,Material.Powder],"多段攻撃","連撃で筋力を活かしながら、毒の勝ち筋も進める。",1,ForgeKind.Attack,ForgeTarget.Enemy,
            "{Damage:diff()}ダメージを{Hits:diff()}回与える。毒{PoisonPower:diff()}を与える。",
            new Dictionary<string,int>{{"Damage",2},{"Hits",5},{"PoisonPower",6}}, new Dictionary<string,int>{{"Damage",1},{"PoisonPower",3}}),
        new("berserker_catalyst.v1","闘争触媒",[Material.Herb,Material.Powder],"自己強化","筋力を得て手札を繋ぐ、攻撃型デッキの始動薬。",1,ForgeKind.Power,ForgeTarget.Self,
            "筋力{StrengthPower:diff()}を得る。カードを{Cards:diff()}枚引く。",
            new Dictionary<string,int>{{"StrengthPower",2},{"Cards",1}}, new Dictionary<string,int>{{"StrengthPower",1},{"Cards",1}}),
        new("quicksilver_draft.v1","水銀の早飲み",[Material.Herb,Material.Ether],"即応強化","敏捷と手札を一度に整え、守りへ素早く移る。",1,ForgeKind.Skill,ForgeTarget.Self,
            "敏捷{DexterityPower:diff()}を得る。カードを{Cards:diff()}枚引く。",
            new Dictionary<string,int>{{"DexterityPower",2},{"Cards",2}}, new Dictionary<string,int>{{"DexterityPower",1},{"Cards",1}},Exhaust:true),
        new("chain_detonation.v1","連鎖爆轟",[Material.Powder,Material.Powder],"多段全体攻撃","敵全体へ三度爆発を浴びせ、筋力を全体火力に変える。",2,ForgeKind.Attack,ForgeTarget.AllEnemies,
            "敵全体に{Damage:diff()}ダメージを{Hits:diff()}回与える。",
            new Dictionary<string,int>{{"Damage",7},{"Hits",3}}, new Dictionary<string,int>{{"Damage",2}},Exhaust:true),
        new("gunpowder_rhythm.v1","火薬律動",[Material.Powder,Material.Powder],"自己強化","攻撃を重くし、連射・爆轟の全段を強化する。",1,ForgeKind.Power,ForgeTarget.Self,
            "筋力{StrengthPower:diff()}を得る。",
            new Dictionary<string,int>{{"StrengthPower",2}}, new Dictionary<string,int>{{"StrengthPower",2}}),
        new("star_barrage.v1","星屑弾幕",[Material.Powder,Material.Ether],"多段攻撃","五連撃の後も手札を補充し、攻勢を止めない。",2,ForgeKind.Attack,ForgeTarget.Enemy,
            "{Damage:diff()}ダメージを{Hits:diff()}回与える。カードを{Cards:diff()}枚引く。",
            new Dictionary<string,int>{{"Damage",5},{"Hits",5},{"Cards",1}}, new Dictionary<string,int>{{"Damage",1},{"Cards",1}}),
        new("aetheric_form.v1","霊質変成",[Material.Ether,Material.Ether],"自己強化","筋力と敏捷を同時に得る、攻守両用の変成。",2,ForgeKind.Power,ForgeTarget.Self,
            "筋力{StrengthPower:diff()}と敏捷{DexterityPower:diff()}を得る。",
            new Dictionary<string,int>{{"StrengthPower",2},{"DexterityPower",2}}, new Dictionary<string,int>{{"StrengthPower",1},{"DexterityPower",1}})
    });
    public static ForgeFormula Get(string id) => All.SingleOrDefault(f=>f.Id==id)
        ?? throw new InvalidDataException($"未対応の錬成定義です：{id}。カードを初期化しません。");
}
