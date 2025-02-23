# Tabnado

![Tabnado Icon](https://raw.githubusercontent.com/Paparogue/Tabnado/2579f4200a6ba0e60bd12eb6acd31be341e08490/tabnado.png)

Tabnado is a targeting plugin that lets you customize how you target enemies in FFXIV. It includes options for visibility checks, distance limitations, and advanced targeting behavior controls.

## Features

### Core Functionality
- Customizable targeting key
- Distance-based targeting (up to 55 yalms)
- Visibility-based target selection
- Camera-based targeting area
- Dynamic target point adjustment based on camera zoom and distance

### Target Reset Features
- Camera rotation-based reset
- New entity detection reset
- Proximity-based reset
- Combinable reset conditions

## Configuration Guide

![Tabnado Config](https://raw.github.com/Paparogue/Tabnado/1d8bd06165db514748ca9d5c11c7c0c6a6793d54/tabnado_1.4.1.png)

### Targeting Settings

#### Target Key
- Select your preferred targeting key
- Special note for Tab key users regarding game keybind conflicts

#### Range Settings
- **Target Point Position**: Adjustable X and Y coordinates for targeting center
- **Max Target Distance**: Sets targeting range from 1-55 yalms
- **Camera Search Radius**: Defines circular search area around target point
- **Visibility Settings** (when "Target Only Visible Objects" is enabled):
  - Minimum Visibility Percentage
  - Raycast Transformation
  - Raycast Multiplier for detection accuracy

### Dynamic Targeting Adjustments

#### Camera Lerp
- Automatically adjusts target point height based on camera zoom
- Customizable lerp speed for zoom adjustment

#### Distance Lerp
- Raises target point for closer enemies
- Adjustable lerp speed for distance-based corrections

### Target Reset Options

#### Camera Rotation Reset
- Resets to nearest target when camera movement exceeds threshold
- Combinable with other reset conditions
- Adjustable rotation threshold

#### New Entity Reset
- Resets when new enemies appear in targeting range
- Combinable with other reset conditions
- Optional camera rotation threshold

#### Proximity Reset
- Resets when closer entities become available
- Combinable with other reset conditions
- Optional camera rotation threshold

### Target Filtering

- **Sticky Target On Reset**: Maintains current target if it matches reset conditions
- **Target Only Attackable Objects**: Filters for attackable targets only
- **Target Only Visible Objects**: Enables visibility checking system

### Debug Options

- **Reset Target Table**: Clears and rebuilds target list periodically
  - Adjustable refresh rate in milliseconds
- **Show Raycast Info**: Displays visibility check data
- **Show Selection Info**: Shows targeting information
- **Draw Refresh Rate**: Controls debug visualization updates (1-100)
- **Camera Depth**: Adjusts visibility check depth (1.0-10.0)

## Important Notes

- If your selected targeting key conflicts with game keybinds, unbind it in the game's settings
- Debug options may significantly impact performance when enabled
- Visibility checks use raycast system for accurate target detection
