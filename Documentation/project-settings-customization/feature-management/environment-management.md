# Manage environments

The Industry Viewer Template includes an **Environment Management** feature that allows you to manage and customize scene environments. This feature enables you to swap between different environments to view your asset in various lighting conditions and settings.

## Manage environments in your project

Use the following steps to manage environments in your Industry Viewer Template project:

1. In the Unity Editor, in the **Project** window, navigate to **Assets** > **Scenes** > **Environment Tool**.
1. Select the **Environment Settings** to open it in the **Inspector** window.
1. In the **Inspector** window, expand the **Scenes** section to view and manage the available environments.

You can add, remove, or modify environments using the options provided in the **Scenes** section. For more information about creating scenes in Unity, refer to [Scenes](https://docs.unity3d.com/Manual/working-with-scenes.html).

### Update scene information

You can update the following information for each scene environment:

| **Property** | **Description** |
| :---- | :---- |
| **Id** | A unique identifier for the scene environment.|
| **Scene Asset** | Select the scene asset you wish to associate with the environment.|
| **Scene Name** | The name of the scene environment.|
| **Display name** | The display name of the scene environment as it appears in the built Player. You can use localization keys here for different locales. For more information about localization, refer to [Localize your project](localization.md).|
| **Thumbnail** | An image representing the scene environment in the built Player.|

## Add scenes to the Player

You must add scenes to a build profile to make them available in the built Player. For more information refer to [Manage scenes in your build](https://docs.unity3d.com/Documentation/Manual/build-profile-scene-list.html).

## Using environments in the built Player

When environment management is enabled in the project, users can swap between different scene environments in the built Player by using the **Environment** option in the **Tool Options** (Briefcase) menu.

## Additional resources

* [Feature management](feature-management.md)
* [Explore the Tool Options menu](../../get-started/tool-options.md)