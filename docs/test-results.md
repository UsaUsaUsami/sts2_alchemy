# テスト結果記録

## 自動検証（2026-09-21）

- 実行コマンド：`scripts/build.ps1`（内部で`dotnet run --project tests/Alchemist.Core.Tests`→`dotnet build src/Alchemist/Alchemist.csproj`の順に実行）
- 結果：`tests/Alchemist.Core.Tests`の48チェックすべてPASS。`src/Alchemist`のビルドも警告0・エラー0で成功。
- 対象：`AlchemyState`（素材相循環、撃破採取、容量・満杯交換・辞退、レシピ照合の順序非依存、錬成のコミット/ロールバック、セーブの往復・スキーマ検証）と`HarvestCombat`（ターン進行、同時撃破、系譜による重複採取防止、上限、炉の起動回数共有）。
- 未実施：マップ生成の複数シード検証（工房配置ロジック自体が未実装のため対象外）。

## 実機テスト（2026-09-21、ユーザー実施・ログで裏付け確認）

- 対象版：Slay the Spire 2 v0.111.0 / BaseLib v3.4.5 / Alchemist v0.1.0
- ログ：`%APPDATA%\SlayTheSpire2\logs\godot.log`（該当セッション、シード`26F7DEM3XCSL`）

確認できたこと：
- MODマニフェストが検出され、`Alchemist.dll`のロードと`Alchemist.Main.Initialize`の実行、Harmonyパッチ（BaseLib全体で280件、失敗0件）が成功。
- キャラクター選択で「錬金術師 — 採取と工房（試作）」が一覧表示され（ログ25行目）、`ALCHEMIST-ALCHEMIST_CHARACTER`でシングルプレイランを開始。
- 初期デッキの`STRIKE_IRONCLAD`・`DEFEND_IRONCLAD`、専用カード`ALCHEMIST-PORTABLE_FURNACE`／`ALCHEMIST-FURNACE_ACTIVATION`が複数回プレイされ、クラッシュなし。
- 通常戦2回（SHRINKER_BEETLE_WEAK, FUZZY_WURM_CRAWLER_WEAK）、エリート1回（BYGONE_EFFIGY_ELITE）に勝利。休憩所・宝物庫・商人にも到達。最終的にVANTOMとの戦闘に敗北してラン終了（Game Over画面まで到達、ラン履歴保存も成功）。
- ログ全体（1231行）を走査した限り、Alchemist名前空間（`MaterialBox`, `AlchemyState`, `WorkshopUi`, カード各クラス等）由来の例外・`PushError`は0件。

確認できなかったこと（要追試）：
- 素材ボックスの実際の獲得表示・工房UIの開閉・カード錬成の実機操作。ログにはUI操作の直接的な痕跡が残らないため、クラッシュしなかったこと以上は確認できていない。次回は工房入場・錬成・満杯交換・辞退を実際に操作し、可能であればログに独自の`GD.Print`トレースを一時的に追加して裏付けを取ること。
- 素材相の実際の値・撃破時の付与（同様にUI確認が必要）。

ベースゲーム側で観測した無関係の例外（本MOD無効化時にも起こりうる可能性が高く、本MODのバグではないと判断）：
- `NTimelineScreen.get_Instance()`（アンロック画面のクローズアニメーション）でのNullReferenceException（1件）
- `NGameOverScreen.AnimateRunSummary`→`NRunSummary.AnimateInDiscoveries`でのNullReferenceException（1件）
- いずれもMegaCrit本体コードのスタックトレースのみで、Alchemist側のフレームを含まない。

## 結論

AGENTS.md M0の完了条件（実環境の起動証拠、最小MODが起動し錬金術師の登録と9枚デッキを確認）は**達成**。M1の中核ループ（採取→工房→錬成→次戦闘での使用）のうち、戦闘内の軸カード動作は実機で動作痕跡を確認したが、工房・素材ボックスのUI操作は未確認のまま。次セッションでの優先確認事項とする。

## 2026-09-21: v0.2.0 工房強化の検証

- ルール検証107項目成功。全素材ペアの順序対称性、候補の存在、安定ID、素材2個の消費、プレビュー、未知formula拒否を含む。Releaseビルドは警告0・エラー0。ログ: artifacts/workshop-build.txt。
- 実ゲームv0.111.0 / BaseLib3.4.5の隔離headlessランで97チェック成功。ログ: artifacts/smoke/workshop-v020-verified.log。ALCHEMIST_SMOKE_COMPLETEとALCHEMIST_LOOP_COMPLETEを確認し、MODのFAIL、[ERROR]、Exceptionはなし。
- 新7種について表示・コスト・キーワード、強化・複製・ゲーム保存復元・強化取消を検証。採取→工房消費→次戦闘で作成カード使用、および新7種のAutoPlay完了と毒/廃棄パワーの付与量を確認。
- 実ゲームAPIの自動テストであり、人による新UIの視認性・入力操作・通しプレイ・各戦闘効果の全状況を網羅する検証ではない。新バランスの人手試遊件数は0。自動ランのseedはALCHEMIST_SMOKE_01 / ALCHEMIST_LOOP_01。
- テスト用APPDATAをartifacts/smoke/appdataへ隔離し、Steam無効・FTUE無効のテスト設定で実行。実ユーザーのセーブは変更していない。終了時のheadless RID解放警告は残る。
- 一度目の最終テストは1800フレームの終了制限で途中終了したため未完了として扱い、4500フレームへ延長して上記の全完了を確認した。
- 実機mods/Alchemistへscripts/update.ps1でv0.2.0を更新済み。DLL・マニフェストのコピー後SHA256一致を確認。旧版はartifacts/backups/20260921-123411-209/Alchemistに保存。導入後の通常GUI起動・人手プレイは未実施。

## 2026-09-21: v0.2.1 工房UI検証

- ルール検証107項目成功、Releaseビルドは警告0・エラー0。
- 実ゲームv0.111.0 / BaseLib3.4.5の隔離headlessランで100チェック成功。レスポンシブな外枠、素材サイドバー、レシピブラウザ、レシピカードの生成を実UIノードから確認。新UIから錬成し、次戦闘でカードを使用する流れも成功。ALCHEMIST_SMOKE_COMPLETE / ALCHEMIST_LOOP_COMPLETE、MOD由来のFAIL・[ERROR]・Exceptionなし。ログ: artifacts/smoke/workshop-ui-v021.log。
- 自動検証はUIノード構造とテキスト・操作経路を対象とする。実画面での日本語の折返し、色の見え方、マウス操作感は人手確認が残る。
- 実機 `mods/Alchemist` へv0.2.1を更新し、DLL・マニフェストのコピー後SHA256一致を確認。旧v0.2.0は `artifacts/backups/20260921-132315-188/Alchemist` に保存した。

## 2026-09-21: v0.2.2 完成カード選択UI検証

- ルール検証107項目成功、ReleaseビルドとSmokeビルドは警告0・エラー0。
- 実ゲームv0.111.0 / BaseLib3.4.5の隔離headlessランで103チェック成功。完成カード12枚の標準カード表示、一覧選択、一枚の確認画面、「このカードを作る」操作、素材消費、次戦闘での使用を確認した。
- 表示用の12カードがRunStateへ登録されていないことを全件確認。工房を開いただけではラン状態・デッキ・保存対象を変更しない。
- ALCHEMIST_SMOKE_COMPLETE / ALCHEMIST_LOOP_COMPLETEを確認し、MOD由来のFAIL・[ERROR]・Exceptionなし。ログ: `artifacts/smoke/workshop-gallery-v022.log`。
- 自動検証では標準カードノードの生成と操作経路を確認した。実画面でのカード間隔、スクロール量、マウスでの選びやすさは人手確認が残る。
- 実機 `mods/Alchemist` へv0.2.2を更新し、DLL・マニフェストのコピー後SHA256一致を確認。旧v0.2.1は `artifacts/backups/20260921-184351-560/Alchemist` に保存した。
