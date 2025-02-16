# Tabnado

> A tab-targeting plugin for Final Fantasy XIV that provides customizable targeting options

![Tabnado Icon](https://raw.githubusercontent.com/Paparogue/Tabnado/2579f4200a6ba0e60bd12eb6acd31be341e08490/tabnado.png)

Tabnado is a targeting plugin that lets you customize how you target enemies in FFXIV. It includes options for visibility checks, distance limitations, and specific target filtering for both PvE and PvP situations.

## Features

### Core Functionality
- Customizable targeting key
- Distance-based targeting (up to 55 yalms)
- Visibility-based target selection
- Camera-based targeting area

### PvP Features
- Option to target only hostile players
- Alliance and party member filtering

### PvE Features
- Option to target only battle NPCs
- Filters out non-combat NPCs and pets

## Configuration Guide

![Tabnado Config](https://raw.github.com/Paparogue/Tabnado/f015b95d9023a7109a9a20c5ec14edcb1245ef82/tabnado_1.2.5.png)

### Targeting Settings

#### Target Key
- Select your preferred targeting key

#### Range Settings
- **Max Target Distance (yalms)**: Sets targeting range from 1-55 yalms
  - Controls maximum distance for valid targets
  
- **Camera Search Radius**: Sets the targeting area size
  - Determines the radius around your camera's center where targets can be selected
  
- **Minimum Visibility (%)**: Required visibility percentage for targeting
  - Controls how visible an object must be to become a valid target

### Target Reset Options

- **Reset target on camera rotation**: Resets target when camera moves beyond threshold
  - Rotation threshold: Set percentage of camera movement that triggers reset
  
- **Reset target when a new combatant appears**: Resets when targets enter camera area
  
- **Reset target on new nearest entity**: Resets when a closer valid target is available

### Target Filtering

- **Target Only Hostile Players**: PvP targeting mode
  - Targets only enemy players
  - Excludes party members and alliance
  
- **Target Only Battle NPCs**: PvE targeting mode
  - Targets combat NPCs
  - Excludes vendors, quest NPCs, and pets
  
- **Target Only Visible Objects**: Enables visibility checking system
  - Uses raycast system to verify target visibility
  - Checks both head and feet positions of targets

### Debug Options

- **Reset Target Table**: Clears and rebuilds target list
  - WARNING: Performance impact when enabled
  - Adjustable refresh rate in milliseconds
  
- **Show Raycast Info**: Displays visibility check visualization
  - WARNING: High performance impact
  
- **Show Selection Info**: Shows targeting area and valid targets
  - WARNING: High performance impact
  
- **Draw Refresh Rate**: Controls debug visualization updates
  - Adjustable from 1-100
  
- **Camera Depth**: Controls visibility check depth
  - Adjustable from 1.0-10.0
