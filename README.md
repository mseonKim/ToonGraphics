# ToonGraphics

This package contains multiple graphics features for toon shading for PC/Console Platforms, Unity URP projects.


## 1. Character ShadowMap
![CharacterShadow](./Documentation~/Images/CharShadow.png)
Character Shadowmap is designed to improve character lighting quality by adding shadow for a single character. There's a room for improvement to cover more than one character, however, currently unsupported without customizing script.

### Additional ShadowMap
It also supports additional local shadow if `useBrightestLight` enabled.
Automatically calculates the most effective lights to the character among all spot lights matching the `FollowLayerMask` in the scene each frame. If the intensity of the brightest spot light is stronger than MainLight(Directional Light), use it for `CharacterShadowMap` instead of the MainLight.

![AdditionalShadow](./Documentation~/Images/CharShadow_Additional.png)


### Transparent Shadow
Transparent shadow is supported as well. However, it uses more memory so enable this when you do need this feature.
![TransparentShadow](./Documentation~/Images/TransparentShadow.png)


## 2. Order Independent Transparency
[Reference] https://github.com/happy-turtle/oit-unity

OIT in this package uses a linked-list method, currently support upto 8 layers for each pixel.
![OIT](./Documentation~/Images/OIT.png)

## 3. Material Transformer
Material Transformer helps you to implement cloth change effect or mesh transition for a single character. You can control linked materials with each channel seperately, or together.

![MaterialTransformer](./Documentation~/Images/MaterialTransformer.gif)
![MaterialTransformerInspector](./Documentation~/Images/MaterialTransformer_Inspector.png)