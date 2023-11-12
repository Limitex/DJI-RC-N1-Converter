# DJI-RC-N1-Converter
This application allows the DJI RC-N1 (RC231) transmitter to be connected to a Windows system and function as an Xbox controller. It resides in the Windows task tray and automatically starts simulating an Xbox 360 controller upon connection of the RC-N1. The software is developed using Windows Forms.

## How to use
### Important Installation Requirement
This application requires ViGEmBus, so ensure you install it from the official repository before running our application: [ViGEmBus Releases](https://github.com/nefarius/ViGEmBus/releases).

### Recommendation
This application starts minimized to the system tray, so we recommend registering it to start with Windows so that it starts when the system starts.
This feature allows you to use your RC-N1 (RC231) as a gamepad by simply connecting it to your PC.

### Steps
1. Ensure ViGEmBus is installed, then run the application.
2. Turn on the RC-N1 (RC231).
3. Connect it to your PC with a USB cable.

## How to build
This repository is created with Visual Studio 2022 and requires .NET 7.0 Windows.
1. Clone this repository.
2. Open DJI-RC-N1-Converter.sln and build the solution.

## Reference
This repository was inspired by and references the following projects:
- https://github.com/IvanYaky/DJI_RC-N1_SIMULATOR_FLY_DCL
- https://github.com/Matsemann/mDjiController
- https://github.com/nefarius/ViGEmBus
