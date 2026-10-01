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
- 錬金術師本人は**`assets/concepts/alchemist-character-v3.png`（決定版・2026-09-29）**に合わせる：金縁の炭色のフードとローブ、顔は金色に光る錬成陣（二重の輪・三角・内円・放射線）、前に四相（地の三角・水のしずく・火の炎・風の渦）の縦帯、腰に小瓶2本。炉は持たせない。`generate-art.py`はこの画像を参考画像として毎回渡す。旧v2は破棄。
- 1枚ずつの題材を指定したいときは`assets/art/subjects.json`に`{"fire_spark": "…"}`の形で書く（なければカード名と効果から描かせる）。

## キービジュアル（錬金術師のデザイン）の案づくり

- 参考画像：`python scripts/extract-sts2-refs.py`がゲームの`SlayTheSpire2.pck`から、キャラ選択の肖像5枚（`lineup_small.png`）と選択画面の絵（`characterselect_<名前>.png`。1枚絵はサイレントだけで、他はアニメーションの部品）を`.research/sts2-ref/`へ取り出す。**ゲームの素材なので、MODやリポジトリには入れない**（`.research`はGit管理外）。生成時の参考として渡すだけ。
- 生成：`python scripts/keyvisual.py [--only flask,beak] [--note "追加の指示"] [--tag v4]`。共通の条件（`BRIEF`）と方向性（`DIRECTIONS`）はスクリプト内。結果は`assets/concepts/keyvisual/kv-<tag>-<案>.png`。
- 参考画像を渡す理由（ユーザー）：渡さないと、線のはっきりした顔のあるイケメンを描かれた。StS2のキャラは顔を出さず、線は最小限で、大きな色の面で描かれている。
- 2026-09-29の案（v3）：flask・beak・furnace・homunculus・sigil → ユーザーがsigilを選び「線をシンプルに」（v4：a・b・c）→ bを選び「炉なし、ローブの柄と顔の魔法陣に線を足す」（v5：b2・b3・b4）→ **b2に決定**（`kv-v5-sigil-b2.png`＝`alchemist-character-v3.png`）。

## 生成：Codex CLIで一括（2026-09-29 確認済み）

- Codex CLI（`npm install -g @openai/codex`、0.158.0）はChatGPTのログインで画像生成（`image_generation`機能）を使える。1枚あたり60〜85秒、3並列でも同程度。
- `python scripts/generate-art.py [--only a,b] [--kind レア] [--limit 10] [--jobs 3] [--note "追加の指示"] [--force]`：絵のないカードを1枚ずつ`codex exec`で生成し、`assets/art/incoming/<カード名>.png`に保存する。ログは`artifacts/art-gen/<カード名>.log`。
- `python scripts/art-sheet.py incoming --cols 4`：生成した画像をラベル付きで1枚に並べた`artifacts/art-gen/sheet.png`を作る。Claude（司令塔）はこれを見て、ダメなものを`--only`・`--note`で作り直させてから取り込む。
- 手作業でもよい：`docs/art-prompts.md`のプロンプトをChatGPTのアプリに貼り、ファイル名どおりに保存する。

## 生成：4枚まとめて（2026-09-29〜、使用量の節約）

- `python scripts/generate-art-grid.py [--only a,b,c,d | --limit 4] [--reasoning low]`：4枚を**1回の画像生成**で2×2のシートに描かせ、切り分けて`assets/art/incoming/<カード名>.png`に保存する。シートは`artifacts/art-gen/grid-<先頭のカード名>.png`。Codexの推論は`model_reasoning_effort="low"`。
- 理由：ユーザーのChatGPT使用量が、1枚1回の生成（`generate-art.py`）と案出しのやり直しで半分近く減った。カード絵は250×190なので、1536×1024のシートの4分の1（768×512）で足りる。
- 試作（岩盤ほか）は1枚1回、この方式の最初の4枚（地鳴り・激流・爆縮・突風）は73秒で1回。従来は同じ枚数で約4回・各60〜85秒。使用量の減り方はユーザーの画面で確認する。
- 使用量：4枚1回で「5時間枠の4%」（ユーザー確認）。日付が変わるたびに数回ずつ進めればよい。
- 傾向：最初の4枚は錬金術師が左に大きく写り、構図が似た。対策として、1枚のシートで人物を出してよいのは1コマだけ（手か後ろ姿、小さく）、他は人物なし、コマごとに構図（引き・接写・俯瞰・左右対称・斜め）を割り当てる。`STYLE`にも「錬金術師は毎回は出ない」を追加。効果を確認済み（灼熱・浸食・補強・風の衣：人物は補強の小さな後ろ姿だけ、構図は引き・接写・エンブレムでばらけた）。

## アイコン：12個まとめて（2026-10-01〜）

- `python scripts/generate-icons.py <グループ>`：遺物・パワー・素材・工房のアイコンを**1回の画像生成で12個**（4×3、マゼンタ単色の背景）描かせ、背景を抜いて`assets/art/incoming/icons/<名前>.png`に切り出す。描くものは同スクリプトの`GROUPS`。シートは`artifacts/art-gen/icons-<グループ>.png`。`--cut-only`で生成せずに切り直す。
- 作風の参考に、バニラのアイコンを並べた`artifacts/art-gen/vanilla-icons-ref.png`（pckから取り出したもの。リポジトリ・配布物には入れない）を渡す。
- `python scripts/import-icons.py`：バニラと同じ大きさの.ctexを`assets/art/icons/`に書く。遺物＝256（大）・85（小）・85の白い輪郭、平面（パワー・改造）＝256・64、マップ記号＝128（絵は72px）。C#は`IconArt`（CardArt.cs）が絶対パスを返し、ファイルがなければバニラの流用に戻る。遺物のファイル名はクラス名のスネークケース（`PhaseCompass`→`phase_compass`）。
- 切り抜き：マゼンタは縁からの塗りつぶしでなく全画素を抜く（線で囲まれた内側も背景のため）。マップ記号はインクがマゼンタ寄りに描かれるので、暗さから不透明度を出して色はインクの茶色1色に塗り直す。
- 試作1回目（遺物10・工房2）：113秒、12個とも使える出来で作り直しなし。バニラより描き込みが多く硬い質感だが、小さく表示しても判別できる。
