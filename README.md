# Ascended ezWondrousTails

A Dalamud plugin for Final Fantasy XIV that adds Wondrous Tails row probabilities, exact shuffle averages, and shuffle advice to the in-game journal.

This is a maintained fork of MidoriKami's archived EzWondrousTails plugin, updated for Dalamud API 15 with added shuffle averages, shuffle advice, and a standalone plugin window.

![Ascended ezWondrousTails demo](res/demo.png)

## Features

* Shows the chance of finishing with one, two, or three Wondrous Tails lines from the current board.
* Injects probability output directly into the Wondrous Tails journal.
* Calculates the exact shuffle average for valid shuffle states, based on every board with the same sticker count.
* Adds shuffle advice that compares the current three-line chance against the exact average.
* Displays the same information in a standalone plugin window when the journal is not open.
* Shows sticker count, Second Chance points, and a simple board state preview in the standalone window.
* Provides settings for journal injection, visible probability sections, colored journal text, and percentage precision.

Shuffle averages and advice are available while the board has 3 through 7 stickers, matching the range where Wondrous Tails shuffle can be used. If the character has fewer than 2 Second Chance points, the advice reports that requirement instead of recommending a shuffle.

## Installation

Add the custom plugin repository in Dalamud:

```text
https://raw.githubusercontent.com/jkleinne/ascended-plugins/master/pluginmaster.json
```

Then install **Ascended ezWondrousTails** from the plugin installer.

## Usage

Open a Wondrous Tails journal in game. The plugin appends three lines to the journal text:

* `Line Chances`, the current probability of ending with one, two, or three lines.
* `Shuffle Average`, the exact average line chances for all shuffled boards with the same number of stickers.
* `Shuffle Advice`, a keep, neutral, or shuffle recommendation based on the current three-line chance compared with the shuffle average.

You can also open the plugin's main window from Dalamud's plugin UI. The settings window lets you enable or disable the journal overlay, choose which probability sections are shown, turn colored journal text on or off, change percentage precision, and view the current journal status.

## Development

The solution root is `WondrousTailsSolver.sln`. Plugin source lives in `WondrousTailsSolver/`, with ImGui windows under `WondrousTailsSolver/Ui/`. The Dalamud manifest is `WondrousTailsSolver/ascended-ezwondroustails.json`.

Build with a compatible .NET SDK and local Dalamud development files:

```sh
dotnet restore WondrousTailsSolver/WondrousTailsSolver.csproj
dotnet build --no-restore -c Release WondrousTailsSolver/WondrousTailsSolver.csproj
```

The GitHub Actions build uses .NET `10.0.x`, `Dalamud.NET.Sdk/15.0.0`, and downloads the latest Dalamud distribution into the XIVLauncher development hook path before building.

There is not a dedicated test project yet. When tests are added, run:

```sh
dotnet test WondrousTailsSolver.sln
```

## Release

Version tags matching `*.*.*` trigger the publish workflow. The workflow builds the plugin, uploads `latest.zip` and `pluginmaster.json` to the GitHub release, then syncs the entry to `jkleinne/ascended-plugins`.
