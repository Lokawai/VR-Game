# Netcode for GameObjects

[![Forums](https://img.shields.io/badge/unity--forums-multiplayer-blue)](https://forum.unity.com/forums/multiplayer.26/) [![Discord](https://img.shields.io/discord/449263083769036810.svg?label=discord&logo=discord&color=informational)](https://discord.gg/FM8SE9E)
[![Manual](https://img.shields.io/badge/docs-manual-informational.svg)](https://docs-multiplayer.unity3d.com/netcode/current/about) [![API](https://img.shields.io/badge/docs-api-informational.svg)](https://docs-multiplayer.unity3d.com/netcode/current/api/introduction)

[![GitHub Release](https://img.shields.io/github/release/Unity-Technologies/com.unity.netcode.gameobjects.svg?logo=github)](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/releases/latest)

### Welcome!

Welcome to the Netcode for GameObjects repository.

Netcode for GameObjects is a Unity package that provides networking capabilities to GameObject & MonoBehaviour workflows. The framework is interoperable with many low-level transports, including the official [Unity Transport Package](https://docs-multiplayer.unity3d.com/transport/current/about).

### Getting Started

Visit the [Multiplayer Docs Site](https://docs-multiplayer.unity3d.com/) for package & API documentation, as well as information about several samples which leverage the Netcode for GameObjects package.

You can also jump right into our [Hello World](https://docs-multiplayer.unity3d.com/netcode/current/tutorials/helloworld/helloworldintro) guide for a taste of how to use the framework for basic networked tasks.

### Community and Feedback

For general questions, networking advice or discussions about Netcode for GameObjects, please join our [Discord Community](https://discord.gg/FM8SE9E) or create a post in the [Unity Multiplayer Forum](https://forum.unity.com/forums/multiplayer.26/).

### Compatibility

Netcode for GameObjects targets the following Unity versions:
- Unity 2020.3, 2021.1, 2021.2 and 2021.3

On the following runtime platforms:
- Windows, MacOS, and Linux
- iOS and Android
- Most closed platforms, such as consoles. Contact us for more information about specific closed platforms.

### Development

This repository is broken into multiple components, each one implemented as a Unity Package.
```
    .
    ├── com.unity.netcode.gameobjects           # The core netcode SDK unity package (source + tests)
    └── testproject                             # A Unity project with various test implementations & scenes which exercise the features in the above packages.
```

### Contributing

We are an open-source project and we encourage and welcome contributions. If you wish to contribute, please be sure to review our [contribution guidelines](CONTRIBUTING.md).

#### Issues and missing features

If you have an issue, bug or feature request, please follow the information in our [contribution guidelines](CONTRIBUTING.md) to submit an issue.

You can also check out our public [roadmap](https://unity.com/roadmap/unity-platform/multiplayer-networking) to get an idea for what we might be working on next!
