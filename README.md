# Tabnado
![Tabnado Icon](https://raw.githubusercontent.com/Paparogue/Tabnado/2579f4200a6ba0e60bd12eb6acd31be341e08490/tabnado.png)

A customizable camera-based targeting plugin for FFXIV.

## Installation
Add the following URL to your third-party repository list:
```
https://raw.githubusercontent.com/Paparogue/PaparogueRepo/refs/heads/main/repo.json
```

## Configuration Guide
![Tabnado Config](https://raw.github.com/Paparogue/Tabnado/master/tabnado_v1.4.7.png)

### Targeting Key
- Select your preferred key for targeting
- **Warning**: If using Tab, you may need to unbind it in game settings to avoid conflicts

### Targeting Settings

#### Target Center Position
- **Target Center X (%)**: Horizontal position of your targeting center (0-100%)
- **Target Center Y (%)**: Vertical position of your targeting center (0-100%)
- **Max Target Distance**: Range limit for targeting (1-55 yalms)
- **Camera Search Radius**: Size of circular detection area (1-1000)

#### Visibility Options (when "Target Only Visible Objects" is enabled)
- **Minimum Visibility (%)**: Required visibility percentage for valid targets
- **Raycast Transformation (%)**: Adjusts raycast points toward camera center
- **Raycast Multiplier**: Increases raycast density for better detection accuracy

#### Dynamic Adjustments
- **Camera Lerp**: Automatically adjusts target center based on camera zoom
  - Targeting shifts upward when zoomed out for better accuracy
  - **Camera Lerp Speed**: Controls adjustment rate (0.001-1.0)
- **Distance Lerp**: Raises object target point when closer
  - Makes nearby targets easier to select
  - **Distance Lerp Speed**: Controls adjustment rate (0.01-10.0)
- **Alternative Targeting Mode**: First selects closest target to camera center
  - After initial selection, cycles through targets based on proximity

### Target Reset Options

#### No Target Reset
- Resets to nearest target when nothing is selected

#### Camera Rotation Reset
- Resets to nearest target when camera moves beyond rotation threshold
- **Rotation Threshold**: Percentage of movement required to trigger reset
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

### Filtering Options
- **Sticky Target On Reset**: Maintains current target after reset if still closest
- **Target Only Attackable Objects**: Only targets attackable entities
- **Target Only Visible Objects**: Enables visibility checking system

### Debug Options
- **Reset Target Table**: Clears the target list periodically
  - **Reset Timer**: Millisecond interval between resets (1-2000ms)
- **Show Raycast Info**: Displays visibility check information
- **Show Selection Info**: Shows targeting diagnostic information
- **Draw Refresh Rate**: Controls debug visualization update frequency (1-100)
- **Camera Depth**: Adjusts visibility check depth (1.0-10.0)

## Important Notes
- If your selected targeting key conflicts with game keybinds, unbind it in the game's settings
- Debug options may significantly impact performance when enabled
- Higher raycast multiplier values improve accuracy but may affect performance
