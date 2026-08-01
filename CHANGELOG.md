## [2.72.8]
### Changes
- Fixed uneven header spacing under fractional DPI scaling.
- Added Top Bar Button registration point, letting packages build on this UI add their own Top Bar Buttons without an assembly needing to reference them.
- Ensure the Rendering Presets dropdown property name is properly recognized (affects Grab Pass, Triplanar Projection, etc.).

## [2.72.7]
### Changes
- Fixed an issue where Global Linking failed when there are 2 properties with the same name.

## [2.72.6]
### Changes
- `ShaderOptimizer`: Anchor the regex to line beginning as well. This fixes an issue where renaming `_Metallic` also rewrites the tail of `_LTCGI_Metallic` as well, as an example.
- Added some new decorative drawers for future usage, including `[ThryHeader]` and `[ThryDescription]`.

## [2.72.5]
### Changes
- Installing Poiyomi Shaders through VCC or ALCOM should now prompt you to install ThryEditor as a dependency. Updates for ThryEditor and Poiyomi Shaders are now separate.
- Fixed auto-collapsing Global Linked slots.
- Fixed VRCFallback tag carrying over on upgraded materials.
- Fixed Rendering Presets handling.
- Fixed inline slider can't be marked as animatable.
- Fixed headers can't be marked as animatable.
- Added a 'Clear' button for inline RGBA packer.
- Turned off random debugging log prints.
