# Tabnado

> A sophisticated tab-targeting enhancement plugin for Final Fantasy XIV using Dalamud

![Tabnado Icon](https://raw.githubusercontent.com/Paparogue/Tabnado/2579f4200a6ba0e60bd12eb6acd31be341e08490/tabnado.png)

Tabnado enhances FFXIV's default tab-targeting system through intelligent algorithms, advanced visibility checks, and customizable targeting preferences for both PvE and PvP.

## Features

### Core Targeting
- **Line of Sight**: Advanced raycast calculations ensure targets are actually visible
- **Distance Management**: 
  - Configurable maximum range (up to 55 yalms)
  - Adjustable camera radius for selection
  - Dynamic filtering based on screen center distance
- **Smart Selection**:
  - Camera-centric targeting priority
  - Optional reset based on camera rotation
  - Configurable target table cleanup

## Configuration

![Tabnado Config](https://raw.github.com/Paparogue/Tabnado/345c70ed176ac83051a922e59002944206b4d55b/tabnado_config.png)

### Key Settings
- **Distance**: Maximum targeting range (1-55 yalms)
- **Camera**: Search radius from screen center
- **Raycast**: Precision multiplier (higher = more accurate but more demanding)
- **Reset Triggers**:
  - Camera rotation threshold
  - New combatant detection
  - Nearest target changes
- **Filters**:
  - PvP: Hostile players only
  - PvE: Battle NPCs only
  - Visibility requirements

### Others
- Dead target clearing
- Line-of-sight visualization
- Selection process information

## Installation & Usage

1. Install via FFXIV Dalamud plugin installer
2. Type `/tabnado` to configure
3. Set targeting key (default: Tab)
4. Unbind FFXIV's default target key if using Tab
