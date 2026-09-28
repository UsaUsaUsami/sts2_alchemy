# カード絵のプロンプト一覧

`scripts/art-prompts.py`が生成（手で編集しない。画風は同スクリプトの`STYLE`、題材は`assets/art/subjects.json`で変える）。生成した画像は`assets/art/incoming/`に**ファイル名どおり**保存し、`python scripts/import-art.py`で取り込む。手順は`docs/art-pipeline.md`。

- 対象：**98枚**（絵がまだないカード。--allで全カード）
- サイズ：通常250×190、エンシェント250×351（横長。大きめに作ってよい。取り込み時に中央を切り抜いて縮小する）

## 共通の画風（すべてのプロンプトの先頭に入っている）

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.
```

## 残風の刃 → `air_afterimage.png`

- アンコモン・Power・風　効果：ターン終了時、このターンに起きた相転移1回につき、ランダムな敵に3ダメージを与える。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「残風の刃」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): ターン終了時、このターンに起きた相転移1回につき、ランダムな敵に3ダメージを与える。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## そよ風 → `air_breeze.png`

- コモン・Skill・風　効果：カードを1枚引く。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「そよ風」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): カードを1枚引く。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 疾風の心核 → `air_core.png`

- 工房の錬成・Power・風　効果：風相へ転移したとき、次の相転移の効果を1強化する。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「疾風の心核」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 風相へ転移したとき、次の相転移の効果を1強化する。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 気流 → `air_current.png`

- アンコモン・Skill・風　効果：次のターン、エナジーを1得て、カードを1枚追加で引く。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「気流」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 次のターン、エナジーを1得て、カードを1枚追加で引く。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 四元の刃 → `air_element_blade.png`

- アンコモン・Attack・風　効果：18ダメージ。所持している通常素材の種類数だけコストが下がる。希少素材を持っていれば、さらに1下がる。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「四元の刃」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 18ダメージ。所持している通常素材の種類数だけコストが下がる。希少素材を持っていれば、さらに1下がる。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 突風 → `air_gale.png`

- アンコモン・Attack・風　効果：敵全体に5ダメージ。カードを1枚引く。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「突風」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 敵全体に5ダメージ。カードを1枚引く。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 追い風 → `air_momentum.png`

- アンコモン・Skill・風　効果：カードを2枚引く。このカードで相転移が起きるなら、次のターン、エナジーを1得る。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「追い風」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): カードを2枚引く。このカードで相転移が起きるなら、次のターン、エナジーを1得る。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 風の知らせ → `air_refine.png`

- アンコモン・Skill・風　効果：カードを2枚引く。その後、手札1枚を廃棄する。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「風の知らせ」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): カードを2枚引く。その後、手札1枚を廃棄する。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## かまいたち → `air_sickle.png`

- コモン・Attack・風　効果：4ダメージを2回与える。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「かまいたち」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 4ダメージを2回与える。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## オーバードライブ → `air_sky.png`

- レア・Power・風　効果：ターン終了時、このターンに相転移が4回以上起きていれば、次のターン、エナジーを2得る。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「オーバードライブ」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): ターン終了時、このターンに相転移が4回以上起きていれば、次のターン、エナジーを2得る。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## ソードストリーム → `air_storm_blade.png`

- レア・Attack・風　効果：6ダメージを、この戦闘で風相へ相転移した回数だけ与える（最低1回）。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「ソードストリーム」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 6ダメージを、この戦闘で風相へ相転移した回数だけ与える（最低1回）。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 風の刃 → `air_wind_blade.png`

- アンコモン・Attack・風　効果：4ダメージ。このカードで相転移が起きるなら、3ブロックを得る。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「風の刃」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 4ダメージ。このカードで相転移が起きるなら、3ブロックを得る。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 風の衣 → `air_wind_veil.png`

- コモン・Skill・風　効果：6ブロックを得る。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「風の衣」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 6ブロックを得る。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## アルカヘスト → `alch_alkahest.png`

- レア・Attack・火　効果：通常素材を1〜10個選んで消費する。消費した素材の数×3×消費した素材の種類数のダメージを与える。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「アルカヘスト」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 通常素材を1〜10個選んで消費する。消費した素材の数×3×消費した素材の種類数のダメージを与える。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 触媒反応 → `alch_catalysis.png`

- アンコモン・Skill・無相　効果：エーテルを1個消費し、カードを2枚引いてエナジーを1得る。エーテルがなければ使えない。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「触媒反応」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): エーテルを1個消費し、カードを2枚引いてエナジーを1得る。エーテルがなければ使えない。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 連鎖反応 → `alch_chain_reaction.png`

- レア・Attack・無相　効果：このターンの3回目以降の相転移1回につき20ダメージを与える。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「連鎖反応」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): このターンの3回目以降の相転移1回につき20ダメージを与える。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## エコーストライク → `alch_echo_strike.png`

- コモン・Attack・無相　効果：6ダメージ。現在相の相転移効果をもう一度発動する（相は変わらない）。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「エコーストライク」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 6ダメージ。現在相の相転移効果をもう一度発動する（相は変わらない）。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 万象流転 → `alch_flux.png`

- レア・Skill・無相　効果：保留。 好きな相へ移る（素材は消費しない。鉄＝地、薬草＝水、火薬＝火、エーテル＝風）。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「万象流転」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 保留。 好きな相へ移る（素材は消費しない。鉄＝地、薬草＝水、火薬＝火、エーテル＝風）。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 素材投入 → `alch_infusion.png`

- コモン・Skill・無相　効果：通常素材を1個選んで消費する。エナジーを1得て、その素材の相へ移る。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「素材投入」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 通常素材を1個選んで消費する。エナジーを1得て、その素材の相へ移る。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 素材爆弾 → `alch_material_bomb.png`

- アンコモン・Attack・無相　効果：敵全体に5ダメージ。火薬を1個消費できれば、代わりに敵全体に16ダメージ。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「素材爆弾」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 敵全体に5ダメージ。火薬を1個消費できれば、代わりに敵全体に16ダメージ。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 四相輪転 → `alch_phase_wheel.png`

- レア・Power・無相　効果：カードを使うたび、そのカードで相転移が起きなかったなら、次の相へ進む（地→水→火→風→地）。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「四相輪転」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): カードを使うたび、そのカードで相転移が起きなかったなら、次の相へ進む（地→水→火→風→地）。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## チャージアップ → `alch_preparation.png`

- コモン・Skill・無相　効果：5ブロックを得る。次の相転移の効果を2強化する。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「チャージアップ」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 5ブロックを得る。次の相転移の効果を2強化する。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 逆相 → `alch_reversal.png`

- アンコモン・Skill・無相　効果：6ブロックを得る。ひとつ前の相へ戻る。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「逆相」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 6ブロックを得る。ひとつ前の相へ戻る。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## ダブルシフト → `alch_synergy.png`

- アンコモン・Skill・無相　効果：カードを1枚引く。次の相転移の効果が、追加で1回発動する。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「ダブルシフト」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): カードを1枚引く。次の相転移の効果が、追加で1回発動する。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 培養槽 → `craft_culture_vat.png`

- 工房の錬成・Power・水　効果：自分のターン開始時、ホムンクルスHPを3得る。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「培養槽」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 自分のターン開始時、ホムンクルスHPを3得る。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 精霊の触媒 → `craft_ether_catalyst.png`

- 工房の錬成・Skill・風　効果：カードを2枚引き、エナジーを1得る。 風相 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「精霊の触媒」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): カードを2枚引き、エナジーを1得る。 風相 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 血肉の鎧 → `craft_flesh_armor.png`

- 工房の錬成・Skill・地　効果：ホムンクルスHPの50%のブロックを得る。 地相 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「血肉の鎧」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): ホムンクルスHPの50%のブロックを得る。 地相 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 器の融合 → `craft_fusion.png`

- 工房の錬成・Attack・火　効果：ホムンクルスHPを10消費できれば、30ダメージを与える。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「器の融合」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): ホムンクルスHPを10消費できれば、30ダメージを与える。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 真理の扉 → `craft_gate_of_truth.png`

- 工房の錬成・Power・火　効果：自分のターン開始時、ホムンクルスHPが20以上なら、敵全体に8ダメージを与える。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「真理の扉」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 自分のターン開始時、ホムンクルスHPが20以上なら、敵全体に8ダメージを与える。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 鋼の砦 → `craft_iron_bastion.png`

- 工房の錬成・Skill・地　効果：12ブロックを得る。次のターン開始時、6ブロックを得る。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「鋼の砦」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): 12ブロックを得る。次のターン開始時、6ブロックを得る。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 分裂 → `craft_mitosis.png`

- 工房の錬成・Skill・風　効果：ホムンクルスHPを2倍にする。 風相 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「分裂」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): ホムンクルスHPを2倍にする。 風相 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 賢者の血 → `craft_philosophers_blood.png`

- 工房の錬成・Power・水　効果：ホムンクルスが攻撃を肩代わりするたび、攻撃した敵にドレイン2を与える。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「賢者の血」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): ホムンクルスが攻撃を肩代わりするたび、攻撃した敵にドレイン2を与える。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 爆裂フラスコ → `craft_powder_flask.png`

- 工房の錬成・Attack・火　効果：敵全体に12ダメージを与える。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「爆裂フラスコ」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 敵全体に12ダメージを与える。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## EW&F → `craft_three_phase_torrent.png`

- 工房の錬成・Attack・火　効果：8ダメージ。5ブロックを得る。 地相→風相→火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「EW&F」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 8ダメージ。5ブロックを得る。 地相→風相→火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## ダーヴの坩堝 → `darv_crucible_card.png`

- エンシェント・Power・無相　効果：自分のターン開始時、現在相に対応する素材を1個得る。素材ボックスが満杯なら得ない。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「ダーヴの坩堝」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 自分のターン開始時、現在相に対応する素材を1個得る。素材ボックスが満杯なら得ない。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:351 (landscape), keep the subject inside the centre.
```

## コペルニクスシフト → `debuff_transfer_card.png`

- レア・Skill・風　効果：通常素材を1個消費してもよい。消費したなら、自分と敵1体の対象デバフを入れ替える。消費しなければ、自分の対象デバフを敵1体にコピーする。 （対象：脱力・弱体・虚弱・筋力低下・敏捷性低下・毒・死亡・ドレイン） 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「コペルニクスシフト」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 通常素材を1個消費してもよい。消費したなら、自分と敵1体の対象デバフを入れ替える。消費しなければ、自分の対象デバフを敵1体にコピーする。 （対象：脱力・弱体・虚弱・筋力低下・敏捷性低下・毒・死亡・ドレイン） 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## ディフェンド → `defend_alchemist.png`

- 初期デッキ・Skill・無相　効果：5ブロックを得る。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「ディフェンド」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 5ブロックを得る。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## ドレインミスト → `drizzle.png`

- 工房の錬成・Skill・風　効果：ドレイン4を与える。カードを1枚引く。 水相→風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「ドレインミスト」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): ドレイン4を与える。カードを1枚引く。 水相→風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 大地の加護 → `earth_blessing.png`

- アンコモン・Skill・地　効果：このターンに起きた相転移1回につきプレート1を得る。 地相 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「大地の加護」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): このターンに起きた相転移1回につきプレート1を得る。 地相 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 城塞打ち → `earth_bulwark_bash.png`

- アンコモン・Attack・地　効果：現在のブロックと同じダメージを与える。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「城塞打ち」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): 現在のブロックと同じダメージを与える。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 大地の心核 → `earth_core.png`

- 工房の錬成・Power・地　効果：地相への相転移の効果を3強化する。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「大地の心核」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): 地相への相転移の効果を3強化する。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 補強 → `earth_fortify.png`

- アンコモン・Skill・地　効果：4ブロックを得る。次のターン開始時、8ブロックを得る。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「補強」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): 4ブロックを得る。次のターン開始時、8ブロックを得る。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 鉄壁錬成 → `earth_iron_bulwark.png`

- コモン・Skill・地　効果：5ブロックを得る。鉄を1個消費できれば、代わりに13ブロックを得る。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「鉄壁錬成」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): 5ブロックを得る。鉄を1個消費できれば、代わりに13ブロックを得る。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 大地の王 → `earth_king.png`

- レア・Power・地　効果：ターン開始時、ブロックがすべて失われる代わりに、20だけ失われる。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「大地の王」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): ターン開始時、ブロックがすべて失われる代わりに、20だけ失われる。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 大地の記憶 → `earth_memory.png`

- レア・Skill・地　効果：この戦闘で起きた相転移1回につき2ブロックを得る。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「大地の記憶」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): この戦闘で起きた相転移1回につき2ブロックを得る。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 棘の外殻 → `earth_thorn_shell.png`

- アンコモン・Power・地　効果：トゲ3を得る。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「棘の外殻」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): トゲ3を得る。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 地鳴り → `earth_tremor.png`

- アンコモン・Attack・地　効果：敵全体に5ダメージ。4ブロックを得る。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「地鳴り」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): 敵全体に5ダメージ。4ブロックを得る。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 土壁 → `earthen_guard.png`

- 初期デッキ・Skill・地　効果：8ブロックを得る。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「土壁」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): 8ブロックを得る。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 即席の霊風 → `ether_improvisation.png`

- トークン・Skill・風　効果：カードを2枚引く。 風相 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「即席の霊風」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): カードを2枚引く。 風相 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## エーテル → `ether_material_card.png`

- 素材（選択画面の表示用）・Skill・無相　効果：ドロー・循環を形作る基本素材。工房でカードへ錬成する。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「エーテル」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): ドロー・循環を形作る基本素材。工房でカードへ錬成する。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 炎の烙印 → `fire_brand.png`

- レア・Attack・火　効果：敵全体に10ダメージと弱体2を与える。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「炎の烙印」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 敵全体に10ダメージと弱体2を与える。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 劫火の心核 → `fire_core.png`

- 工房の錬成・Power・火　効果：火相への相転移の効果を3強化する。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「劫火の心核」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 火相への相転移の効果を3強化する。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 爆縮 → `fire_explosion.png`

- レア・Attack・火　効果：35ダメージ。対象をスタンさせる。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「爆縮」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 35ダメージ。対象をスタンさせる。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 焔の心 → `fire_heart.png`

- レア・Power・火　効果：火相へ転移するたび、筋力1を得る。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「焔の心」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 火相へ転移するたび、筋力1を得る。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## ヒートアップ → `fire_heat.png`

- アンコモン・Skill・火　効果：活力6を得る。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「ヒートアップ」 (Skill). a technique or alchemical action, defensive or utility. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 活力6を得る。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 業火 → `fire_hellfire.png`

- レア・Attack・火　効果：6ダメージ。この戦闘で起きた相転移1回につき、さらに2ダメージ。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「業火」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 6ダメージ。この戦闘で起きた相転移1回につき、さらに2ダメージ。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 灼熱 → `fire_scorch.png`

- アンコモン・Attack・火　効果：7ダメージ。弱体2を与える。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「灼熱」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 7ダメージ。弱体2を与える。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 火炎旋風 → `fire_whirl.png`

- 工房の錬成・Attack・風　効果：敵全体に4ダメージを2回与える。 火相→風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「火炎旋風」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 敵全体に4ダメージを2回与える。 火相→風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 閃光火薬 → `flash_powder.png`

- アンコモン・Attack・火　効果：敵全体に6ダメージ。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「閃光火薬」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 敵全体に6ダメージ。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 炉の起動 → `furnace_activation.png`

- トークン・Skill・無相　効果：保留。 手札1枚を廃棄し、現在相に対応する素材を2個得る。無相では使用できない。 地：鉄、水：薬草、火：火薬、風：エーテル。戦闘全体で2回まで（精錬された素材ボックスでは3回）。 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「炉の起動」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 保留。 手札1枚を廃棄し、現在相に対応する素材を2個得る。無相では使用できない。 地：鉄、水：薬草、火：火薬、風：エーテル。戦闘全体で2回まで（精錬された素材ボックスでは3回）。 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 即席の鎮静薬 → `herb_improvisation.png`

- トークン・Skill・水　効果：脱力1を与える。 水相 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「即席の鎮静薬」 (Skill). a technique or alchemical action, defensive or utility. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 脱力1を与える。 水相 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 薬草 → `herb_material_card.png`

- 素材（選択画面の表示用）・Skill・無相　効果：毒・弱体を形作る基本素材。工房でカードへ錬成する。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「薬草」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 毒・弱体を形作る基本素材。工房でカードへ錬成する。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 人体錬成 → `human_transmutation.png`

- レア・Attack・火　効果：敵1体にホムンクルスHP×3のダメージを与える。自分に死亡を付与する。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「人体錬成」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 敵1体にホムンクルスHP×3のダメージを与える。自分に死亡を付与する。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 点火 → `ignition.png`

- コモン・Attack・火　効果：弱体1を与え、6ダメージ。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「点火」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 弱体1を与え、6ダメージ。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 即席錬成 → `instant_alchemy.png`

- 初期デッキ・Skill・無相　効果：通常素材を1個選んで消費する。対応する0コストの一時カードを手札に加える。 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「即席錬成」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 通常素材を1個選んで消費する。対応する0コストの一時カードを手札に加える。 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 即席の鉄壁 → `iron_improvisation.png`

- トークン・Skill・地　効果：7ブロックを得る。 地相 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「即席の鉄壁」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): 7ブロックを得る。 地相 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 鉄 → `iron_material_card.png`

- 素材（選択画面の表示用）・Skill・無相　効果：物理攻撃・防御を形作る基本素材。工房でカードへ錬成する。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「鉄」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 物理攻撃・防御を形作る基本素材。工房でカードへ錬成する。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 溶岩弾 → `lava_shot.png`

- 工房の錬成・Attack・火　効果：9ダメージ。4ブロックを得る。 地相→火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「溶岩弾」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 9ダメージ。4ブロックを得る。 地相→火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 目覚めた器 → `life_awakened_vessel.png`

- レア・Power・風　効果：自分のターン開始時、ホムンクルスHPを5消費して、ランダムな敵に12ダメージを与える。 風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「目覚めた器」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 自分のターン開始時、ホムンクルスHPを5消費して、ランダムな敵に12ダメージを与える。 風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 命の弾丸 → `life_bullet.png`

- コモン・Attack・火　効果：7ダメージ。ホムンクルスHPを5消費できれば、代わりに15ダメージ。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「命の弾丸」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 7ダメージ。ホムンクルスHPを5消費できれば、代わりに15ダメージ。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 腐食の抱擁 → `life_corrosive_embrace.png`

- レア・Skill・水　効果：対象のドレインを50%増やす（端数切り捨て）。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「腐食の抱擁」 (Skill). a technique or alchemical action, defensive or utility. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 対象のドレインを50%増やす（端数切り捨て）。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 吸精の瘴気 → `life_drain_miasma.png`

- アンコモン・Power・水　効果：自分のターン開始時、敵全体にドレイン1を与える。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「吸精の瘴気」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 自分のターン開始時、敵全体にドレイン1を与える。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 肉の壁 → `life_flesh_wall.png`

- コモン・Skill・地　効果：ホムンクルスHPを6得る。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「肉の壁」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): ホムンクルスHPを6得る。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 大錬成 → `life_great_work.png`

- アンコモン・Attack・火　効果：50ダメージ。ホムンクルスHP10ごとに、コストが1下がる。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「大錬成」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 50ダメージ。ホムンクルスHP10ごとに、コストが1下がる。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 生命の収穫 → `life_harvest.png`

- レア・Skill・水　効果：すべての敵のドレインを即座に3回発動させる。発動ごとに通常どおり減る。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「生命の収穫」 (Skill). a technique or alchemical action, defensive or utility. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): すべての敵のドレインを即座に3回発動させる。発動ごとに通常どおり減る。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 養分 → `life_nourish.png`

- アンコモン・Power・水　効果：ドレインが発動するたび、ホムンクルスHPを追加で1得る。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「養分」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): ドレインが発動するたび、ホムンクルスHPを追加で1得る。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 生命の供物 → `life_offering.png`

- コモン・Skill・水　効果：HPを5失う。ホムンクルスHPを10得る。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「生命の供物」 (Skill). a technique or alchemical action, defensive or utility. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): HPを5失う。ホムンクルスHPを10得る。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 還元 → `life_reclaim.png`

- レア・Skill・地　効果：ホムンクルスHPをすべて消費し、その半分だけHPを回復する。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「還元」 (Skill). a technique or alchemical action, defensive or utility. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): ホムンクルスHPをすべて消費し、その半分だけHPを回復する。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 自己培養 → `life_self_cultivation.png`

- レア・Power・水　効果：自分のターン開始時、自分にドレイン2を付与する。自分に付いたドレインで得るホムンクルスHPは2倍になる。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「自己培養」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 自分のターン開始時、自分にドレイン2を付与する。自分に付いたドレインで得るホムンクルスHPは2倍になる。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 共生 → `life_symbiosis.png`

- アンコモン・Power・地　効果：ターン終了時、ホムンクルスが場にいれば4ブロックを得る。 地相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「共生」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: earth phase: ochre, moss green and stone grey, heavy and solid.
What the card does (Japanese, for mood only, do not write it): ターン終了時、ホムンクルスが場にいれば4ブロックを得る。 地相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 道連れ → `life_torrent.png`

- レア・Attack・火　効果：ホムンクルスHPをすべて消費し、その量のダメージを敵全体に与える。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「道連れ」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): ホムンクルスHPをすべて消費し、その量のダメージを敵全体に与える。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 命脈の一撃 → `life_vein_strike.png`

- アンコモン・Attack・火　効果：10ダメージ。ホムンクルスHPが20以上なら、2回与える。 火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「命脈の一撃」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 10ダメージ。ホムンクルスHPが20以上なら、2回与える。 火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 水銀 → `mercury_material_card.png`

- 素材（選択画面の表示用）・Skill・無相　効果：保留を刻む希少素材。工房で錬成カードへ恒久加工する。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「水銀」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 保留を刻む希少素材。工房で錬成カードへ恒久加工する。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 泥の城壁 → `mud_rampart.png`

- 工房の錬成・Skill・水　効果：7ブロックを得る。脱力1を与える。 地相→水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「泥の城壁」 (Skill). a technique or alchemical action, defensive or utility. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 7ブロックを得る。脱力1を与える。 地相→水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 即席の炸薬 → `powder_improvisation.png`

- トークン・Skill・火　効果：8ダメージ。 火相 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「即席の炸薬」 (Skill). a technique or alchemical action, defensive or utility. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 8ダメージ。 火相 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 火薬 → `powder_material_card.png`

- 素材（選択画面の表示用）・Skill・無相　効果：高火力・全体攻撃を形作る基本素材。工房でカードへ錬成する。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「火薬」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 高火力・全体攻撃を形作る基本素材。工房でカードへ錬成する。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 砂嵐 → `sandstorm.png`

- 工房の錬成・Skill・風　効果：6ブロックを得る。カードを1枚引く。 地相→風相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「砂嵐」 (Skill). a technique or alchemical action, defensive or utility. Colours: air phase: pale cyan and white, swirling wind, light and fast.
What the card does (Japanese, for mood only, do not write it): 6ブロックを得る。カードを1枚引く。 地相→風相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 鎮静の霧 → `soothing_mist.png`

- 初期デッキ・Skill・水　効果：脱力1を与える。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「鎮静の霧」 (Skill). a technique or alchemical action, defensive or utility. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 脱力1を与える。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 星砂 → `stardust_material_card.png`

- 素材（選択画面の表示用）・Skill・無相　効果：リプレイとコスト+1を刻む希少素材。工房で錬成カードへ恒久加工する。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「星砂」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): リプレイとコスト+1を刻む希少素材。工房で錬成カードへ恒久加工する。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 蒸気爆発 → `steam_burst.png`

- 工房の錬成・Attack・火　効果：敵全体に6ダメージと脱力1を与える。 水相→火相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「蒸気爆発」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: fire phase: crimson, orange and ember gold, explosive.
What the card does (Japanese, for mood only, do not write it): 敵全体に6ダメージと脱力1を与える。 水相→火相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## ストライク → `strike_alchemist.png`

- 初期デッキ・Attack・無相　効果：6ダメージを与える。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「ストライク」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): 6ダメージを与える。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 虚無結晶 → `void_crystal_material_card.png`

- 素材（選択画面の表示用）・Skill・無相　効果：コスト減少と廃棄を刻む希少素材。工房で錬成カードへ恒久加工する。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「虚無結晶」 (Skill). a technique or alchemical action, defensive or utility. Colours: neutral alchemy: brass, glass flasks, violet arcane light.
What the card does (Japanese, for mood only, do not write it): コスト減少と廃棄を刻む希少素材。工房で錬成カードへ恒久加工する。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## ライフストリーム → `water_clear_stream.png`

- レア・Skill・水　効果：ドレイン2を与える。さらに、この戦闘で起きた相転移1回につきドレイン1を与える。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「ライフストリーム」 (Skill). a technique or alchemical action, defensive or utility. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): ドレイン2を与える。さらに、この戦闘で起きた相転移1回につきドレイン1を与える。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 流水の心核 → `water_core.png`

- 工房の錬成・Power・水　効果：水相へ転移するたび、脱力に加えて弱体1を与える。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「流水の心核」 (Power). a lasting aura, sigil or transformation, emblem-like. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 水相へ転移するたび、脱力に加えて弱体1を与える。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 浸食 → `water_erosion.png`

- アンコモン・Attack・水　効果：8ダメージ。対象が脱力状態なら、さらに6ダメージ。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「浸食」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 8ダメージ。対象が脱力状態なら、さらに6ダメージ。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 薬草のしずく → `water_herb_drip.png`

- コモン・Skill・水　効果：ドレイン1を与える。薬草を1個消費できれば、代わりにドレイン4を与える。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「薬草のしずく」 (Skill). a technique or alchemical action, defensive or utility. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): ドレイン1を与える。薬草を1個消費できれば、代わりにドレイン4を与える。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 鎮魂 → `water_requiem.png`

- レア・Skill・水　効果：対象の筋力を3下げる。 水相 廃棄。

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「鎮魂」 (Skill). a technique or alchemical action, defensive or utility. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 対象の筋力を3下げる。 水相 廃棄。
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```

## 激流 → `water_torrent.png`

- アンコモン・Attack・水　効果：敵全体に4ダメージと脱力1を与える。 水相

```
Card illustration for a fantasy deck-building game in the style of Slay the Spire 2: hand-painted digital painting, bold readable shapes, strong silhouette, dramatic lighting, rich but limited palette, simple background, centered subject that still reads at small size. No text, no letters, no card frame, no border, no UI. The character is a travelling alchemist who fights by shifting between four elemental phases (earth, water, fire, air) and grows a homunculus. When the alchemist appears, draw them as in assets/concepts/alchemist-character-v2.png: faceless, deep charcoal hood, an inverted-flask brass mask with one cyan glass slit, worn grey cloak, leather gloves, a small brass furnace and reagent vessels (green herb, orange powder, cyan ether) at the belt. Show hands, tools and effects more often than the full figure.

Card: 「激流」 (Attack). an attack in motion, aimed at an unseen enemy. Colours: water phase: deep teal and aqua, flowing, misty.
What the card does (Japanese, for mood only, do not write it): 敵全体に4ダメージと脱力1を与える。 水相
Subject: illustrate the card's name and effect as one clear scene or object.
Aspect ratio 250:190 (landscape), keep the subject inside the centre.
```
