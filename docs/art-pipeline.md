# カード絵の作り方（v0.24〜）

カードの絵を、借り物のバニラの絵から自前の絵へ差し替えるための手順。画像生成はMODの外（ChatGPT・Codexなど）で行い、MOD側は「何を描くか」の一覧と「取り込み・読み込み」を受け持つ。

## 流れ

1. **一覧を作る**：`.\scripts\smoke.ps1 -Name card-dump` → `python scripts/art-prompts.py artifacts/smoke/card-dump.log`
   - `assets/art/prompts.json`（機械向け）と`docs/art-prompts.md`（人・チャット向け）ができる。
   - 絵がまだないカードだけが載る。全カードを出すなら`--all`。
2. **生成する**：各カードのプロンプトで画像を作り、`assets/art/incoming/`に**指定のファイル名**（例：`fire_spark.png`）で保存する。
   - 名前の後ろに`_v2`・` (1)`・`-alt`などが付いていてもよい（`fire_spark_v2.png`→`fire_spark`）。
   - サイズ・縦横比は気にしなくてよい（大きめの横長が理想）。取り込み時に中央を切り抜いて縮小する。
3. **取り込む**：`python scripts/import-art.py`
   - `assets/art/cards/<カード名>.png`（250×190、エンシェントは250×351）として保存し、元の画像は`assets/art/incoming/done/`へ移す。
   - 対応するカードがない名前は取り込まずに表示する。
4. **反映する**：`.\scripts\build.ps1` → `.\scripts\update.ps1`（ゲームは終了しておく）。

## 仕組み

- `src/Alchemy/CardArt.cs`：MODのDLLの隣の`art/cards/<カード名>.png`を起動時に読み込み、カードの絵（BaseLibの`CustomPortrait`）にする。Godotの.pckは使わない。ファイルがないカードは借り物の絵のまま。
- カード名（ファイル名）はカードIDから`ALCHEMY-`を除いた小文字（`ALCHEMY-FIRE_SPARK`→`fire_spark`）。バニラの`strike_ironclad.png`と同じ形。
- サイズはバニラのカード絵に合わせた（隔離実機で計測：通常250×190、エンシェント250×351。拡大表示でも同じ絵が使われる）。`import-art.py --scale 2`で倍の解像度でも保存できるが、ゲーム内でどう縮小されるかは未確認。
- `assets/art/cards/`はGitで管理する（取り込み済みの完成品）。`assets/art/incoming/`は管理しない。
- スモークは、テスト用の画像（`tests/Alchemy.Smoke/fixtures/art/cards/furnace_activation.png`）が絵として使われることと、画像がないカードが借り物の絵のままであることを確かめる。

## 画風

- 共通の画風は`scripts/art-prompts.py`の`STYLE`（全プロンプトの先頭に入る）。相ごとの色は`PALETTE`、カードの種類ごとの構図は`TYPE_HINT`。
- 錬金術師本人は`assets/concepts/alchemist-character-v2.png`（フードと逆さフラスコ型の真鍮の仮面、顔は見えない）に合わせる。
- 1枚ずつの題材を指定したいときは`assets/art/subjects.json`に`{"fire_spark": "…"}`の形で書く（なければカード名と効果から描かせる）。

## 生成：Codex CLIで一括（2026-09-29 確認済み）

- Codex CLI（`npm install -g @openai/codex`、0.158.0）はChatGPTのログインで画像生成（`image_generation`機能）を使える。1枚あたり60〜85秒、3並列でも同程度。
- `python scripts/generate-art.py [--only a,b] [--kind レア] [--limit 10] [--jobs 3] [--note "追加の指示"] [--force]`：絵のないカードを1枚ずつ`codex exec`で生成し、`assets/art/incoming/<カード名>.png`に保存する。ログは`artifacts/art-gen/<カード名>.log`。
- `python scripts/art-sheet.py incoming --cols 4`：生成した画像をラベル付きで1枚に並べた`artifacts/art-gen/sheet.png`を作る。Claude（司令塔）はこれを見て、ダメなものを`--only`・`--note`で作り直させてから取り込む。
- 手作業でもよい：`docs/art-prompts.md`のプロンプトをChatGPTのアプリに貼り、ファイル名どおりに保存する。
