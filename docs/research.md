# 調査記録

## 対象環境（2026-09-21 確認）

- ゲーム本体：Slay the Spire 2 v0.111.0（commit `41cef1ea`、`release_info.json`のdateは2026-08-13、branch `v0.111.0`）
  - インストール先：`C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2`
- ローダー／Modding API：BaseLib-StS2（作者 Alchyr）。ソース: https://github.com/Alchyr/BaseLib-StS2
  - `.research/BaseLib-StS2` に参照用リポジトリをshallow clone済み（当時のHEADはタグ`v3.2.0`相当。バージョン把握用の参照であり、実装はビルド時点の`.tools/BaseLib-3.4.5`バイナリに追従する）
  - `.tools/BaseLib-3.4.5/` にビルド用バイナリ（BaseLib.dll/.json/.pck）を配置し、`Directory.Build.props`の`BaseLibPath`から参照
  - GitHub Releases（2026-09-21確認、REST API直接参照。WebFetch経由の要約は日付を誤って報告したため、APIレスポンスで裏取りした）：
    - 最新: v3.4.7（2026-09-11）
    - v3.4.6（2026-09-10）、v3.4.5（2026-08-14）、v3.4.4（2026-08-06）……
  - v3.4.5のリリースノートに破壊的変更の記載なし。v3.4.6/3.4.7も"fixes"中心で、本MODが使うAPI面（CustomCharacterModel, CustomCardModel, CustomRelicModel, CardPileCmd, CardCmd, DamageCmd, PowerCmd等）への影響は確認できなかった（詳細diffは未調査）。
- 開発環境：.NET SDK 9.0.318（`global.json`固定）、`.tools/dotnet`にローカル配置。ビルドは`scripts/build.ps1`経由。

## 発見した環境不整合（2026-09-21）

- 実機にインストールされていたBaseLibは**v3.2.0**（2026-06-04リリース）だったが、プロジェクトのビルド参照は**v3.4.5**（2026-08-14リリース）で、3ヶ月分のバージョン差があった。
- `scripts/install.ps1`のバージョンチェックも`v3.2.0`のまま残っており、ビルド設定・マニフェスト（`v3.4.5`要求）と矛盾していた。
- 対応：実機の`mods/BaseLib`を`.tools/BaseLib-3.4.5`の内容で上書きし、`install.ps1`のチェックを`v3.4.5`に修正（[[design-decisions]]参照）。旧v3.2.0一式はプロジェクト外のスクラッチ領域にバックアップ。
- 最新のv3.4.7へは今回追従していない。次回作業時に追従するかは未決（採用しても実装ロジック側の変更は不要見込みだが未検証）。

## Godotログの場所

- `%APPDATA%\SlayTheSpire2\logs\godot.log`（最新セッション）およびタイムスタンプ付きの過去ログ。
- MOD有効化状態は`%APPDATA%\SlayTheSpire2\steam\<SteamID>\settings.save`の`mod_settings.mod_list[].is_enabled`で管理される。新規導入したMODはこのリストに手動で追加しないと（またはゲーム側UIで有効化しないと）ロードされない。

## 未検証項目

- BaseLib v3.4.6/v3.4.7への追従可否（API diffの直接確認はしていない）。
- 工房のマウス操作感・日本語レイアウトと、Act通しでの素材経済（自動UI経路は確認済み。詳細は[[test-results]]）。
- マルチプレイ・他キャラクターへの副作用確認。

## 2026-09-21: v0.4.0 マップ・強化API確認

- 対象は実機v0.111.0 / BaseLib v3.4.5。ローカル対象版DLLと隔離実行で `RunManager.GenerateMap` 後の `RunState.Map`、`MapPoint.PointType`、`NMapScreen.SetMap`、`NNormalMapPoint._Ready` を確認した。生成後に選択済み座標を適用し、マップノードへ専用ラベルを追加できる。
- 工房の安定した入退室経路として `RestSiteRoom.EnterInternal` と `MapRoom` を確認した。工房座標だけ部屋ロールを休憩所へ固定し、通常の休憩所にはパッチを適用しない。
- 既存カード強化は `CardCmd.Upgrade(card, EventLayout)` でマスターデッキのカードへ適用できる。カード型は `CardType.Attack / Skill / Power` から取得でき、強化済みカードは候補から除外できる。
- `MaterialBox` のシリアライズ対象状態で次戦闘の開始相とActごとの工房座標が保存・復元されることを確認した。

## 2026-09-21: v0.2.0 カード生成APIの再確認

- 対象は引き続き実機v0.111.0（41cef1ea、Steam buildid24724944）、BaseLib v3.4.5、.NET SDK9.0.318。最新一般版への追従ではなく利用中の版に対するローカル検証。
- ローカルゲームDLLの逆コンパイル参照 `.research/game` と実行検証で、CardModelのSavedProperty文字列、FromSerializableのプロパティ復元後の強化、DynamicVars、EnergyCost.SetCustomBaseCostを確認。
- DowngradeInternalはcanonicalの数値とキーワードへ戻すため、ForgedCard.AfterDowngradedで保存済みformulaの基礎値・キーワードを再適用する。
- 毒の継続と廃棄連動にはゲーム標準NoxiousFumesPower、FeelNoPainPower、DarkEmbracePowerを使用。カード説明はForgedCardに限定したDescription getterパッチと既存の動的変数フォーマットで表示する。
- 参照元はローカル対象版DLLおよび既記録のBaseLib配布元 https://github.com/Alchyr/BaseLib-StS2 。実ゲームを使う隔離テストの結果はtest-results.md参照。ゲーム本体や有料アセットはdistに同梱しない。
