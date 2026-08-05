# [2.73.0]

**THIS IS A MAJOR UPDATE!**

### Shader Optimizer Improvements

This update introduces improvements to our Shader Optimizer pipeline by further improving it's behavior over shared samplers.

For the longest time, Materials that are locked with the same settings share the same Locked hash. However, this has never always been the case. ThryEditor 2.73.0 goes further by making sure this is done more often whenever possible. By doing so, we can eliminate messy variants for each independent material that use the same set of features enabled.

In order to support these new changes, newly-locked materials will now have all their optimized shader files placed centrally in `Assets/_LockedShaderCache`. Please carefully read the Changelog below for info.

**Please, PLEASE report any bugs to us! We greatly appreciate any feedback you have on this!**

## Changes
- Overhauled the Shader Optimizer system.
  - Each material now caches/shares it's own samplers in a central `Assets/_LockedShaderCache` folder, separated by each Shader. Locked Materials no longer have their own separate `OptimizedShaders` file.
  - Unlocking no longer deletes cache-resident shaders, saving greatly on CPU. Installs get cleaned on Unlock rather than leaving orphans scattered around. This should effectively make Unlocking twice as faster than before.
  - By default, the Locked Shader Cache is capped at 2048 MB. When it reaches the limit, older unused samplers get deleted.
    - *This is configurable in ThryEditor Settings, but change that value at your own risk!*
  - **CREATORS:** As locked materials now go to `Assets/_LockedShaderCache`, this folder will be auto-generated when locking materials for the first time starting with this version.
    - *As this folder contains all locked samplers, deleting this folder will require materials to be recompiled. However, it is still advised to ignore it from your exported Avatar assets (as you shouldn't export Avatars with locked materials anyways)!*
    - *A README.txt file is inserted as a reminder about the folder's importance and instructions on how to manage it.*
    - *As a side note: A script is also included to automatically handle missing locked shaders, if for some reason the `_LockedShaderCache` folder is missing. This should hopefully reduce the occurrence of Pink Materials from missing locked shader files.*
- Fixed Inspector Rebuild occurring often in some usage scenarios, especially during Lock/Unlock.
- Fixed multiple ShaderProperty name collision bugs.
- The Levenshtein sweep over `GuessShader` function no longer runs constantly and only executes when absolutely needed.
- Fixed parser parity.
- Fixed `A`/`RA` dot indicator on Headers not showing if a collapsed section has a tagged `A`/`RA` in it's context.
- Fixed dead URL on `Right-Click -> Locking Explanation` context menu option.
- Fixed a NullReferenceException on the Render Queue property when Right-Clicked.

# [2.72.8]
## Changes
- Fixed uneven header spacing under fractional DPI scaling.
- Added Top Bar Button registration point, letting packages build on this UI add their own Top Bar Buttons without an assembly needing to reference them.
- Ensure the Rendering Presets dropdown property name is properly recognized (affects Grab Pass, Triplanar Projection, etc.).

# [2.72.7]
## Changes
- Fixed an issue where Global Linking failed when there are 2 properties with the same name.

# [2.72.6]
## Changes
- `ShaderOptimizer`: Anchor the regex to line beginning as well. This fixes an issue where renaming `_Metallic` also rewrites the tail of `_LTCGI_Metallic` as well, as an example.
- Added some new decorative drawers for future usage, including `[ThryHeader]` and `[ThryDescription]`.

# [2.72.5]
## Changes
- Installing Poiyomi Shaders through VCC or ALCOM should now prompt you to install ThryEditor as a dependency. Updates for ThryEditor and Poiyomi Shaders are now separate.
- Fixed auto-collapsing Global Linked slots.
- Fixed VRCFallback tag carrying over on upgraded materials.
- Fixed Rendering Presets handling.
- Fixed inline slider can't be marked as animatable.
- Fixed headers can't be marked as animatable.
- Added a 'Clear' button for inline RGBA packer.
- Turned off random debugging log prints.
