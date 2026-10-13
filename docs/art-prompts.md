# カード絵のプロンプト一覧

`scripts/art-prompts.py`が生成（手で編集しない。画風は同スクリプトの`STYLE`、題材は`assets/art/subjects.json`で変える）。生成した画像は`assets/art/incoming/`に**ファイル名どおり**保存し、`python scripts/import-art.py`で取り込む。手順は`docs/art-pipeline.md`。

- 対象：**5枚**（絵がまだないカード。--allで全カード）
- サイズ：通常250×190、エンシェント250×351（横長。大きめに作ってよい。取り込み時に中央を切り抜いて縮小する）

## 共通の画風（すべてのプロンプトの先頭に入っている）

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a small companion golem (assets/concepts/homunculus-v1.png: a chunky, rounded chibi golem of smooth ochre stones, a glowing gold circle-and-triangle sigil on its body as its only eye, thin element-coloured lines along the seams). When the alchemist appears, draw them as in the attached key visual (assets/concepts/alchemist-character-v3.png): faceless, a deep charcoal hood with gold trim, the face a softly glowing gold alchemical sigil (double ring, triangle, inner circle, thin rays), a long charcoal robe with gold border lines and a vertical band of the four element marks (earth triangle, water drop, fire flame, air swirl), a brown belt with two small vials, dark leather gloves and boots. Flat colour masses and clean graphic line ornament, not noisy texture. The alchemist is NOT in every card: unless the panel says the alchemist may appear, show only the effect, an object, a creature or the environment, with no person at all.
```

## 元素の奔流 → `alch_elemental_torrent.png`

- レア・Skill・無相　効果：相をX回進める（地→水→火→風→地）。無相なら、1回目で地相へ移る。
- 題材：a roaring spiral vortex of four streams, ochre stone, blue water, red fire and pale cyan wind, chasing each other around a glowing transmutation circle

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a small companion golem (assets/concepts/homunculus-v1.png: a chunky, rounded chibi golem of smooth ochre stones, a glowing gold circle-and-triangle sigil on its body as its only eye, thin element-coloured lines along the seams). When the alchemist appears, draw them as in the attached key visual (assets/concepts/alchemist-character-v3.png): faceless, a deep charcoal hood with gold trim, the face a softly glowing gold alchemical sigil (double ring, triangle, inner circle, thin rays), a long charcoal robe with gold border lines and a vertical band of the four element marks (earth triangle, water drop, fire flame, air swirl), a brown belt with two small vials, dark leather gloves and boots. Flat colour masses and clean graphic line ornament, not noisy texture. The alchemist is NOT in every card: unless the panel says the alchemist may appear, show only the effect, an object, a creature or the environment, with no person at all.

Card: 「元素の奔流」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 相をX回進める（地→水→火→風→地）。無相なら、1回目で地相へ移る。
Subject: a roaring spiral vortex of four streams, ochre stone, blue water, red fire and pale cyan wind, chasing each other around a glowing transmutation circle
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 黄金化 → `alch_golden_form.png`

- レア・Power・無相　効果：カードが素材を消費するとき、素材は減らない（その素材を持っている必要はある）。エナジーを得るカードは除く。
- 題材：the alchemist's hand held up, slowly turning to gleaming gold from the fingertips, iron ore and herbs in the palm also turning to gold, warm golden light

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a small companion golem (assets/concepts/homunculus-v1.png: a chunky, rounded chibi golem of smooth ochre stones, a glowing gold circle-and-triangle sigil on its body as its only eye, thin element-coloured lines along the seams). When the alchemist appears, draw them as in the attached key visual (assets/concepts/alchemist-character-v3.png): faceless, a deep charcoal hood with gold trim, the face a softly glowing gold alchemical sigil (double ring, triangle, inner circle, thin rays), a long charcoal robe with gold border lines and a vertical band of the four element marks (earth triangle, water drop, fire flame, air swirl), a brown belt with two small vials, dark leather gloves and boots. Flat colour masses and clean graphic line ornament, not noisy texture. The alchemist is NOT in every card: unless the panel says the alchemist may appear, show only the effect, an object, a creature or the environment, with no person at all.

Card: 「黄金化」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): カードが素材を消費するとき、素材は減らない（その素材を持っている必要はある）。エナジーを得るカードは除く。
Subject: the alchemist's hand held up, slowly turning to gleaming gold from the fingertips, iron ore and herbs in the palm also turning to gold, warm golden light
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 地脈の共鳴 → `earth_resonance.png`

- レア・Skill・地　効果：このターンに起きた相転移1回につき1回、地相への相転移の効果を発動する（相は変わらない）。 地相
- 題材：a ring of standing stones pulsing with ochre light, concentric shock rings rising from the ground and hardening into a stone shield

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a small companion golem (assets/concepts/homunculus-v1.png: a chunky, rounded chibi golem of smooth ochre stones, a glowing gold circle-and-triangle sigil on its body as its only eye, thin element-coloured lines along the seams). When the alchemist appears, draw them as in the attached key visual (assets/concepts/alchemist-character-v3.png): faceless, a deep charcoal hood with gold trim, the face a softly glowing gold alchemical sigil (double ring, triangle, inner circle, thin rays), a long charcoal robe with gold border lines and a vertical band of the four element marks (earth triangle, water drop, fire flame, air swirl), a brown belt with two small vials, dark leather gloves and boots. Flat colour masses and clean graphic line ornament, not noisy texture. The alchemist is NOT in every card: unless the panel says the alchemist may appear, show only the effect, an object, a creature or the environment, with no person at all.

Card: 「地脈の共鳴」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): このターンに起きた相転移1回につき1回、地相への相転移の効果を発動する（相は変わらない）。 地相
Subject: a ring of standing stones pulsing with ochre light, concentric shock rings rising from the ground and hardening into a stone shield
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 地殻変動 → `earth_upheaval.png`

- アンコモン・Power・地　効果：地相へ転移するたび、敵全体に3ダメージを与える。 地相
- 題材：the ground splitting open as jagged ochre rock slabs burst upward in a line, sharp stone shards flying toward silhouetted enemies

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a small companion golem (assets/concepts/homunculus-v1.png: a chunky, rounded chibi golem of smooth ochre stones, a glowing gold circle-and-triangle sigil on its body as its only eye, thin element-coloured lines along the seams). When the alchemist appears, draw them as in the attached key visual (assets/concepts/alchemist-character-v3.png): faceless, a deep charcoal hood with gold trim, the face a softly glowing gold alchemical sigil (double ring, triangle, inner circle, thin rays), a long charcoal robe with gold border lines and a vertical band of the four element marks (earth triangle, water drop, fire flame, air swirl), a brown belt with two small vials, dark leather gloves and boots. Flat colour masses and clean graphic line ornament, not noisy texture. The alchemist is NOT in every card: unless the panel says the alchemist may appear, show only the effect, an object, a creature or the environment, with no person at all.

Card: 「地殻変動」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): 地相へ転移するたび、敵全体に3ダメージを与える。 地相
Subject: the ground splitting open as jagged ochre rock slabs burst upward in a line, sharp stone shards flying toward silhouetted enemies
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 連鎖爆破 → `fire_chain_blast.png`

- アンコモン・Attack・火　効果：敵全体に5ダメージをX回与える。このカードで相転移が起きるなら、さらに1回。 火相
- 題材：a row of small black powder flasks exploding one after another in a chain of red-orange blasts across a dark battlefield

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a small companion golem (assets/concepts/homunculus-v1.png: a chunky, rounded chibi golem of smooth ochre stones, a glowing gold circle-and-triangle sigil on its body as its only eye, thin element-coloured lines along the seams). When the alchemist appears, draw them as in the attached key visual (assets/concepts/alchemist-character-v3.png): faceless, a deep charcoal hood with gold trim, the face a softly glowing gold alchemical sigil (double ring, triangle, inner circle, thin rays), a long charcoal robe with gold border lines and a vertical band of the four element marks (earth triangle, water drop, fire flame, air swirl), a brown belt with two small vials, dark leather gloves and boots. Flat colour masses and clean graphic line ornament, not noisy texture. The alchemist is NOT in every card: unless the panel says the alchemist may appear, show only the effect, an object, a creature or the environment, with no person at all.

Card: 「連鎖爆破」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 敵全体に5ダメージをX回与える。このカードで相転移が起きるなら、さらに1回。 火相
Subject: a row of small black powder flasks exploding one after another in a chain of red-orange blasts across a dark battlefield
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```
