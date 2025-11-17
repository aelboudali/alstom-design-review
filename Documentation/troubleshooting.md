# Troubleshooting common issues with the Industry Viewer Template

This document provides solutions to common issues you might encounter when using the Industry Viewer Template.

## Xcode 26 throws an Assertion failed error when building for iOS with ARKit

If building for iOS using ARKit, it's recommended to use Xcode 16 or earlier versions to prevent a known issue with Xcode 26 that causes an `Assertion failed` error during the build process.


## Unity Cloud Common modifies Info.plist file when building for macOS

When building for macOS, Unity Cloud Common might modify the `Info.plist` file of your application, which can lead to macOS blocking access to the microphone.

To resolve, open Terminal on macOS and run the following commands to reset the microphone permissions and re-sign your application:

```
tccutil reset Microphone <replace bundle id here>
codesign --force --deep --sign - <path of your app>
```

For example:

```
tccutil reset Microphone com.unity.industry-viewer
codesign --force --deep --sign - /Users/<user>/IndustryViewer/Viewer.app
```

## XR controller is unresponsive when interacting with the Hierarchy view

When interacting with the Hierarchy view on an XR device, you might find that input from one of the controllers is unresponsive or ignored. To resolve, use the secondary controller to interact with the Hierarchy view.

**Note**: This is a known issue and a fix is planned for a future release.

## Issues when developing for Meta Quest devices

If you encounter issues when developing for Meta Quest devices, it's recommended to use version 2.1 of the [Unity OpenXR: Meta](https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.1/manual/index.html) package. If using a later version, consider downgrading to version 2.1 to resolve potential issues.

## Additional resources

* [Build and publish](build-and-publish.md)
* [Get started with the Industry Viewer Template](get-started/get-started.md)
