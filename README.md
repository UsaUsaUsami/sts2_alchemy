# 錬金術師MOD（Alchemist）— Slay the Spire 2向け試作版

素材採取・カード錬成・ポーション調合を軸とする錬金術師キャラクターのMOD。現状は試作段階（AGENTS.mdのM0〜M1範囲）。詳細な仕様方針は`AGENTS.md`を参照。

## 対応バージョン

- Slay the Spire 2: v0.111.0
- BaseLib-StS2: v3.4.5（[Alchyr/BaseLib-StS2](https://github.com/Alchyr/BaseLib-StS2)）

異なるゲーム版・BaseLib版での動作は未確認。`scripts/install.ps1`がバージョン不一致を検出すると導入を停止する。

## ビルド

前提：`.tools/dotnet`にSDKが配置済み（`scripts/env.ps1`が参照）。未配置の場合は`scripts/env.ps1`の参照先を確認し、.NET SDK 9.0.318相当を用意する。

```powershell
.\scripts\build.ps1
# GameRoot既定値は "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
# 別の場所にインストールしている場合は: .\scripts\build.ps1 -GameRoot "D:\Games\...\Slay the Spire 2"
```

内部で以下を順に行う：
1. `tests/Alchemist.Core.Tests`のルール検証テストを実行（失敗時はビルドを中断）
2. `src/Alchemist`をReleaseビルド
3. `dist/Alchemist/`に`Alchemist.dll`・`Alchemist.json`を配置

## 導入

```powershell
.\scripts\install.ps1
```

- StS2が起動中の場合はエラーで停止する。事前に終了しておくこと。
- ゲーム本体のバージョン、`mods/BaseLib/BaseLib.json`のバージョンが対応版と異なる場合はエラーで停止する。
- `mods/Alchemist`が既に存在する場合は停止する。更新にはゲームを終了して `scripts/update.ps1` を実行する。旧DLL・マニフェストを `artifacts/backups/` に保存してから更新する。
- 導入後、ゲーム内のMOD設定でBaseLibとAlchemistを有効化する必要がある（初回導入時はMOD一覧に追加されるだけで、既定では無効の場合がある）。

## v0.2.0：工房でデッキの主役を作る

通常素材2個で作れるカードを5種から12種に拡張。4素材の順不同の全10組み合わせに作成先があり、鉄＋鉄と薬草＋薬草は2種類から選べる。工房には役割・使い方・強化後の効果・不足素材を表示し、「作れるものだけ」の絞り込みを追加した。

| 目指すデッキ | 主役のレシピ・未強化時の効果 |
|---|---|
| 大きな一撃 | 鉄＋鉄《錬鉄の大剣》：2コスト、28ダメージ＋8ブロック、保留 |
| 毒で押し切る | 薬草＋薬草《猛毒培養槽》：1コストのパワー、毎ターン敵全体に毒4 |
| 廃棄を戦力に | 鉄＋エーテル《循環錬成炉》：2コストのパワー、廃棄のたび5ブロック＋1ドロー |
| 集団戦の切り札 | 火薬＋エーテル《エーテル爆縮》：1コスト、全体24ダメージ＋2ドロー、廃棄 |

このほか徹甲榴弾・毒霧爆弾・濃縮毒液を追加し、既存5種も強化。数値は強めの試作値で、通しプレイによるバランス検証はこれから。既存カードのIDとラン保存形式は維持するが、新カードを含むセーブを旧MODへ戻すことは非対応。

## 既知の制限

- 工房は専用マップノードが未実装。試作では戦闘勝利直後の限定的な入口からのみアクセスできる（AGENTS.md M1範囲）。
- レシピは12種。ベース3種を含む30入力のハイブリッド合成と、希少素材の装着/分解は未実装（M3範囲）。
- マルチプレイ・他キャラクターとの共存は未検証。

## アンインストール時の注意

`mods/Alchemist`フォルダを削除するだけでよいが、Alchemist使用中のランのセーブは復元できなくなる可能性がある。アンインストール前に該当ランを終了しておくことを推奨する。

## 開発記録

- `docs/research.md`：環境・API調査記録
- `docs/design-decisions.md`：採用した判断・未決事項
- `docs/test-results.md`：自動検証・実機検証の結果
