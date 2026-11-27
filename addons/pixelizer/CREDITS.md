# Credits

Pixelizer is made by [mapo works](https://mapo.works) and released under the MIT license (`LICENSE`). It got to where
it is thanks to people who shared what they know. Thank you all.

## The look

- **[t3ssel8r](https://www.youtube.com/@t3ssel8r)**: the reason this exists. Their videos of 3D scenes rendered as
  pixel art showed that it can be done and how it should feel: a small render shown in whole pixels, a camera that
  moves in whole pixels and hands the leftover sub-pixel shift to the upscaled image, light in a few flat bands,
  and one-pixel silhouette and crease lines. Pixelizer is our Godot take on that approach. No code of theirs is in
  it; the ideas are theirs.
- **[Eduardo Schildt](https://www.youtube.com/@eduardoschildt)**: the
  [Godot 3D PixelArt Vegetation Demo](https://pixelagegames.itch.io/3d-pixelart-vegetation) and its video tutorial
  [Stylized Grass & Trees for Pixel Art 3D in Godot 4](https://www.youtube.com/watch?v=iPbYzFWECz4): 3D pixel art
  in Godot 4 with an edge-detection outline pass, which we studied while building ours.

## Along the way

- **[JorisAR](https://github.com/JorisAR)**: [GDVoxelTerrain](https://github.com/JorisAR/GDVoxelTerrain) (MIT), the
  editable SDF voxel terrain our games are built on and the ground this look grew up on. Code from it lives on in
  our terrain tools; it is not part of this addon.
- **[Godot Engine contributors](https://godotengine.org/)**: the engine, and the pieces this addon stands on:
  SubViewports, the normal-roughness buffer, stencils in spatial shaders, shader includes, C# support.

## How it was written

Pixelizer was written by mapo works together with Claude, Anthropic's AI model, used as a pair programmer: the
commits that carry a `Co-Authored-By: Claude` line were made that way. We say so because the Godot Asset Store asks
for it, and because you should know.

## Contributing

Found a bug, or made the look better? Issues and pull requests are welcome. You'll be listed here.
