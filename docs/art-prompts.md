# カード絵のプロンプト一覧

`scripts/art-prompts.py`が生成（手で編集しない。画風は同スクリプトの`STYLE`、題材は`assets/art/subjects.json`で変える）。生成した画像は`assets/art/incoming/`に**ファイル名どおり**保存し、`python scripts/import-art.py`で取り込む。手順は`docs/art-pipeline.md`。

- 対象：**0枚**（絵がまだないカード。--allで全カード）
- サイズ：通常250×190、エンシェント250×351（横長。大きめに作ってよい。取り込み時に中央を切り抜いて縮小する）

## 共通の画風（すべてのプロンプトの先頭に入っている）

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a small companion golem (assets/concepts/homunculus-v1.png: a chunky, rounded chibi golem of smooth ochre stones, a glowing gold circle-and-triangle sigil on its body as its only eye, thin element-coloured lines along the seams). When the alchemist appears, draw them as in the attached key visual (assets/concepts/alchemist-character-v3.png): faceless, a deep charcoal hood with gold trim, the face a softly glowing gold alchemical sigil (double ring, triangle, inner circle, thin rays), a long charcoal robe with gold border lines and a vertical band of the four element marks (earth triangle, water drop, fire flame, air swirl), a brown belt with two small vials, dark leather gloves and boots. Flat colour masses and clean graphic line ornament, not noisy texture. The alchemist is NOT in every card: unless the panel says the alchemist may appear, show only the effect, an object, a creature or the environment, with no person at all.
```
