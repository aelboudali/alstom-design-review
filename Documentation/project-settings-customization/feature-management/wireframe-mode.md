# Wireframe mode

The Industry Viewer Template includes a **Wireframe mode** feature that allows users to visualize a wireframe of 3D assets. Wireframe mode is useful for detailed inspections of the geometry and structure of complex models.

**Note:** Wireframe mode consumes additional rendering resources. Unity loads a separate mesh in the background, which is hidden until **Wireframe** is enabled from the Settings menu.

## Enable wireframe mode in your project

Use the following steps to enable wireframe mode in your Industry Viewer Template project:

1. In the Unity Editor, in the **Project** window, navigate to Assets > Scenes.
1. Open the streaming scene you want to enable wireframe mode for. For example, `Streaming.unity` or `Streaming VR.unity`.
1. In the **Hierarchy** window, select the **Streaming Model Controller** GameObject.
1. In the **Inspector** window, locate the **Streaming Model Controller (Script)** component.
1. Enable the **Enable Wireframe** property.

## Using wireframe mode in the built Player

When wireframe mode is enabled in the project, users can toggle a wireframe view in the built Player by using the **Wireframe** option in the **Settings** (Cog) menu.

## Additional resources

* [Feature management](feature-management.md)
* [In-app controls and navigation](../../get-started/in-app-controls-and-navigation.md)