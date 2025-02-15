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

### Combat Modes
- **PvP Optimization**:
  - Hostile player targeting
  - Alliance/party member filtering
- **PvE Enhancements**:
  - Battle NPC filtering
  - Configurable reset conditions

### Technical Systems
- **Raycast Configuration**:
  - Adjustable multiplier for precision vs performance
  - Multiple collision detection points
  - Camera rotation threshold options
- **Performance Features**:
  - Efficient target table management
  - Customizable refresh rates
  - Performance-based scaling

## Configuration

![Tabnado Config](https://raw.github.com/Paparogue/Tabnado/038c96a4cd18140322f988d9e22e661121ee9515/version1.2.png)

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
  - Dead target management

### Debug Tools
- Target selection preview
- Line-of-sight visualization
- Selection process information

## Installation & Usage

1. Install via FFXIV Dalamud plugin installer
2. Type `/tabnado` to configure
3. Set targeting key (default: Tab)
4. Unbind FFXIV's default target key if using Tab
