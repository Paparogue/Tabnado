# Tabnado
> Because tab targeting is hard.

![Tabnado Icon](https://raw.githubusercontent.com/Paparogue/Tabnado/2579f4200a6ba0e60bd12eb6acd31be341e08490/tabnado.png)

A Dalamud plugin for Final Fantasy XIV that enhances the default tab-targeting system by applying more intelligent filtering and visibility checks.

---

## Table of Contents
- [Features](#features)
- [How It Works](#how-it-works)
- [Configurations](#configurations)

---

## Features

1. **Intelligent Targeting**  
   - Only target enemies or players that are actually in your line of sight (using a configurable collision multiplier).

2. **Distance Threshold**  
   - Prevents targeting enemies or objects that are too far away (configurable up to 55 yalms by default).

3. **Camera-Based Selection**  
   - Chooses targets based on what's closest to the center of your screen, making it easier to switch quickly in hectic fights.

4. **Configurable Keys**  
   - Bind any key (e.g., `Tab`, `F1`, etc.) to trigger Tabnado’s targeting logic.

5. **Debug Views**  
   - Turn on debug displays to visualize how Tabnado processes targets and see line-of-sight checks in real-time.

---

## How It Works

Tabnado operates by:
1. **Scanning Nearby Objects**  
   It looks for enemies or players within a certain radius.
2. **Performing Visibility Checks**  
   It uses collision raycasts to see if a potential target is actually visible, ignoring those behind walls or large obstacles.
3. **Prioritizing**  
   Among all valid targets, it picks the one closest to your camera center or within the configured radius distance.
   
---

## Configurations

![Tabnado Config](https://raw.github.com/Paparogue/Tabnado/e086b975161d4844f0a097a50d38e28b772a73b9/config.png)
