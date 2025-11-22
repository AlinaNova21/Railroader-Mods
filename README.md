# Alinas Railroader Mods

This is a collection of my mods for Railroader.

All mods are installable through [Railloader](https://railroader.stelltis.ch/)

## Installation

### Requirements:
- [Railloader](https://railroader.stelltis.ch/) 1.6
- [Strange Customs](https://railroader.stelltis.ch/mods/strange-customs) 1.6+ (Beta Version)

### Steps:

1. Download the latest release from the [website](https://railroader.alinanova.dev)
2. Drop onto Railloader.exe
3. Done!

### Troubleshooting

If it doesn't work, check in Preferences => Features => Mod Settings
Alina's Map Mod should show loaded, if not an error will show. If it says Strange Customs did not load, and you use UMM, goto Railloader at the top and click the big magic button.

## Project Setup

In order to get going with this, follow the following steps:

1. Clone the repo
2. Copy the `Paths.user.example` to `Paths.user`, open the new `Paths.user` and set the `<GameDir>` to your game's directory.
3. Open the Solution
4. You're ready!

### Reference Assemblies (Recommended)

For faster compilation and legal distribution, setup RefasMer reference assemblies:

**Windows:**
```batch
setup-refasmer.bat
```

**Linux/macOS:**
```bash
./setup-refasmer.sh
```

This creates metadata-only assemblies in `ref-assemblies/` that provide:
- ✅ Faster compilation (metadata-only assemblies)
- ✅ Legal distribution (no game code included)
- ✅ Smaller repository size
- ✅ Cross-platform compatibility

The build system automatically uses reference assemblies when available and falls back to game assemblies if needed.

### During Development
Make sure you're using the _Debug_ configuration. Every time you build your project, the files will be copied to your Mods folder and you can immediately start the game to test it.

### Publishing
Make sure you're using the _Release_ configuration. The build pipeline will then automatically do a few things:

1. Makes sure it's a proper release build without debug symbols
1. Replaces `$(AssemblyVersion)` in the `Definition.json` with the actual assembly version.
1. Copies all build outputs into a zip file inside `bin` with a ready-to-extract structure inside, named like the project they belonged to and the version of it.