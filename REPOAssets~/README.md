# REPOAssets override

The patcher downloads the game's original shaders and localization files from
[REPOAssets](https://github.com/Semiwork-Studios-AB/REPOAssets).

To test files that aren't pushed there yet, put them in this folder using the same layout as the repository:

- `Shaders/`: `.shader` files, matched to the ripped shaders by shader name, so file names don't matter.
  Applied by `Tools > R.E.P.O. Project Patcher > Patch Assets > Shaders`.
- `Localization/`: the contents of the game project's `Assets/Localization` folder, including `.meta` files.
  Applied by `Tools > R.E.P.O. Project Patcher > Patch Assets > Localization`.

Each folder replaces the download only while it contains files, so you can override one without the other.
Everything in here except this README is git-ignored.
