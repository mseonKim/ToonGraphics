# ToonGraphics

This package contains multiple graphics features for toon shading for PC/Console Platforms, Unity URP projects.


## 1. Character ShadowMap
![CharacterShadow](./Documentation~/Images/CharShadow.png)
Character Shadowmap is designed to improve character lighting quality by adding shadow for a single character. There's a room for improvement to cover more than one character, however, currently unsupported without customizing script.

### Additional ShadowMap
It also supports additional local shadows as well, upto 3 spot lights.
Automatically calculates the most effective lights to the character among all spot lights in the scene each frame, and then draw additional shadows.
![AdditionalShadow](./Documentation~/Images/CharShadow_Additional.png)


### Transparent Shadow
Transparent shadow is supported as well. However, it uses more memory so enable this when you do need this feature.
![TransparentShadow](./Documentation~/Images/TransparentShadow.png)


## 2. Order Independent Transparency
[Reference] https://github.com/happy-turtle/oit-unity

OIT in this package uses a linked-list method, currently support upto 8 layers for each pixel.

