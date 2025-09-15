# Tabnado
![Tabnado Icon](https://raw.githubusercontent.com/Paparogue/Tabnado/2579f4200a6ba0e60bd12eb6acd31be341e08490/tabnado.png)

A customizable camera-based targeting plugin for FFXIV.

## Installation
Add the following URL to your third-party repository list:
```
https://raw.githubusercontent.com/Paparogue/PaparogueRepo/refs/heads/main/repo.json
```

## Configuration Guide
![Tabnado Config](https://raw.github.com/Paparogue/Tabnado/4e136700e6277247c241a4c26b142613224cd55f/Tabnado_v1.6.4.png)

### Targeting Key
- No key needs to be set - Tabnado overrides the in-game targeting system automatically.

### Targeting Settings

#### Target Center Position
- **Target Center X (%)**: Horizontal position of your targeting center (0-100%)
- **Target Center Y (%)**: Vertical position of your targeting center (0-100%)
- **Max Target Distance**: Range limit for targeting (1-55 yalms)

#### Selection Area
- **Use Rectangle Selection**: Switch between circular and rectangular targeting areas
  - When disabled: Uses **Camera Search Radius** (1-1000) for circular detection
  - When enabled: Configure rectangular area with:
    - **Rectangle Left (%)**: Extends selection area left from center
    - **Rectangle Right (%)**: Extends selection area right from center
    - **Rectangle Top (%)**: Extends selection area up from center
    - **Rectangle Bottom (%)**: Extends selection area down from center

#### Visibility Options (when "Target Only Visible Objects" is enabled)
- **Minimum Visibility (%)**: Required visibility percentage for valid targets
- **Raycast Transformation (%)**: Adjusts raycast points toward camera center
- **Raycast Multiplier**: Increases raycast density for better detection accuracy (1-16)

#### Dynamic Adjustments
- **Camera Lerp**: Automatically adjusts target center based on camera zoom
  - Targeting shifts upward when zoomed out for better accuracy
  - **Camera Zoom Lerp**: Controls adjustment rate (0.001-0.1)
  
- **Distance Lerp**: Raises object target point when closer
  - Makes nearby targets easier to select
  - **Distance Lerp**: Controls adjustment rate (0.01-10.0)
  
- **Alternative Targeting Mode**: First selects closest target to camera center
  - After initial selection, cycles through targets based on proximity to current selection

### Target Reset Options

#### No Target Reset
- Resets to nearest target when nothing is selected

#### Camera Rotation Reset
- Resets to nearest target when camera moves beyond rotation threshold
- **Rotation threshold (% movement)**: Percentage of movement required to trigger reset (1-100%)
- Can be combined with:
  - New Target Reset
  - New Closest Target Reset

#### New Target Reset
- Resets when new entities appear or leave targeting range
- Can be combined with:
  - Camera Rotation Reset (with adjustable threshold)
  - New Closest Target Reset

#### New Closest Target Reset
- Resets when a new closer entity becomes available
- Can be combined with:
  - Camera Rotation Reset (with adjustable threshold)
  - New Target Reset

### Target Filtering Options
- **Sticky Target On Reset**: Maintains current target after reset if still closest
  - When disabled, resets to the second closest enemy
- **Target Only Attackable Objects**: Only targets attackable entities
- **Target Only Visible Objects**: Enables visibility checking system using raycasts

### Debug Options
- **Show Debug Options**: Toggles visibility of advanced debugging features
- **Show Selection Info**: Shows targeting diagnostic information (⚠️ Performance impact)
  ![Visibility](https://raw.github.com/Paparogue/Tabnado/2de89869b1076fefbba66c799fed0e4ca5bc3add/Selection_Information.jpg)
- **Show Raycast Info**: Displays visibility raycast information (⚠️ Performance impact)
  ![Raycast](https://raw.githubusercontent.com/Paparogue/Tabnado/2de89869b1076fefbba66c799fed0e4ca5bc3add/Visibility_information.jpg)
- **Camera Depth**: Adjusts visibility check depth (1.0-10.0) - Advanced setting

## Important Notes
- Debug options may significantly impact performance when enabled
- Higher raycast multiplier values improve accuracy but may affect performance
