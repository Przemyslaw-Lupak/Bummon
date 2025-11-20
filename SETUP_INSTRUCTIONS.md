# Fix WASD Movement - Setup Instructions

## The Problem
The ActiveRagdoll prefab is missing a **PlayerInput** component, which is required for Unity's Input System to send input events to your InputModule.

## Step-by-Step Fix

### 1. Open Your ActiveRagdoll Prefab
- In Unity Project window, navigate to: `Assets/active-ragdolls-master/active-ragdolls/Assets/Prefabs/`
- Double-click **ActiveRagdoll.prefab** to open it in Prefab Mode

### 2. Add PlayerInput Component
- Select the **root GameObject** (ActiveRagdoll)
- Click **Add Component** button in Inspector
- Search for and add: **Player Input**

### 3. Configure PlayerInput Component
Set these EXACT settings:

**Actions:**
- Click the circle icon next to "Actions"
- Select: `Assets/active-ragdolls-master/active-ragdolls/Assets/Input/ActiveRagdollActions`

**Default Map:**
- Set to: `Player`

**Behavior:**
- **CRITICAL**: Set to **Send Messages** (NOT Invoke Unity Events!)
- This is how Unity calls the Move(), LeftArm(), RightArm() methods in InputModule

**Camera:**
- Leave empty/None

### 4. Save and Test
- Click **Save** in the Prefab editing mode
- Enter Play mode
- Press **W** and watch the Console

## What Should Happen

When you press W, you should see these debug logs:
```
[InputModule] Move called! Input: (0.0, 1.0)
[DefaultBehaviour] MovementInput received: (0.0, 1.0), IsOwner: True
_movement: (0.0, 1.0)
```

## If Still Not Working

### Problem A: No "[InputModule] Move called!" log
- **Cause**: PlayerInput not configured correctly
- **Fix**: Make sure Behavior is set to "Send Messages" (NOT "Invoke Unity Events")
- **Fix**: Make sure Actions is set to the correct InputActions asset

### Problem B: "[InputModule] logs show but IsOwner: False"
- **Cause**: Network ownership issue
- **Fix**: You need a NetworkManager in your scene that spawns the player with ownership
- **Fix**: When spawning, use: `GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId)`

### Problem C: Input works but character doesn't move
- Check that the character has all required modules: PhysicsModule, AnimationModule, DefaultBehaviour
- Check that Movement Speed and Movement Force are > 0 in DefaultBehaviour

## Network Setup Checklist

If testing multiplayer:
1. ✅ NetworkManager in scene
2. ✅ ActiveRagdoll prefab added to NetworkManager's NetworkPrefabs list
3. ✅ Player spawned with SpawnAsPlayerObject() or ChangeOwnership()
4. ✅ Each client gets their own instance with IsOwner = true
