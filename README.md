# Tabnado
> A sophisticated tab-targeting enhancement plugin for Final Fantasy XIV using Dalamud

![Tabnado Icon](https://raw.githubusercontent.com/Paparogue/Tabnado/2579f4200a6ba0e60bd12eb6acd31be341e08490/tabnado.png)

Tabnado enhances FFXIV's default tab-targeting system through intelligent algorithms, advanced visibility checks, and customizable targeting preferences for both PvE and PvP.

## Features

### Core Targeting
- **Line of Sight**: Advanced raycast calculations with configurable precision and visibility thresholds ensure targets are actually visible
- **Distance Management**: 
  - Configurable maximum range (up to 55 yalms)
  - Adjustable camera radius for selection area (1-1000)
  - Dynamic filtering based on screen center distance
- **Smart Selection**:
  - Camera-centric targeting priority
  - Optional reset based on camera rotation percentage
  - New combatant detection reset
  - Nearest target change detection

### Advanced Visibility System
- **Raycast Configuration**:
  - Adjustable visibility percentage threshold
  - Customizable raycast transformation percentage
  - Configurable raycast multiplier (1-16x)
- **Performance Options**:
  - Adjustable draw refresh rate
  - Camera depth settings
  - Debug visualization toggles

## Configuration

![Tabnado Config](https://raw.github.com/Paparogue/Tabnado/f015b95d9023a7109a9a20c5ec14edcb1245ef82/tabnado_1.2.5.png)

### Targeting Settings
- **Key Binding**: Customizable targeting key with conflict warnings
- **Range Settings**:
  - Maximum target distance (1-55 yalms)
  - Camera search radius (1-1000)
  - Visibility percentage requirements
- **Reset Options**:
  - Camera rotation threshold
  - New combatant detection
  - Nearest target changes

### Target Filters
- **PvP Mode**: Option to target only hostile players
- **PvE Mode**: Battle NPC-only targeting
- **Visibility**: Configurable minimum visibility requirements

### Debug Features
- Target table reset intervals
- Raycast visualization
- Selection process information
- Performance monitoring
- Camera depth adjustment

## Installation & Usage
1. Install via FFXIV Dalamud plugin installer
2. Type `/tabnado` to access configuration
3. Set your preferred targeting key
4. If using Tab, unbind FFXIV's default target key to avoid conflicts
5. Adjust settings to match your performance needs and targeting preferences
