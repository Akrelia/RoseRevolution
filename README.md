# Welcome to ROSE Revolution

<p align="center">
  <img src="https://i.imgur.com/QorK0mp.png" alt="ROSE Revolution Logo" width="200" />
</p>

<p align="center">
  A modern reimplementation of <b>ROSE Online</b> built with Unity and modern C#.
</p>

<p align="center">
  <img src="https://i.imgur.com/KP9QGqJ.png" alt="ROSE Revolution Preview" width="1000" />
</p>

- [Discord Server](https://discord.gg/2SxQWtMC3X)
- [Website](https://baptistefran.github.io/rose-revolution/)
- [Server Repository](https://github.com/Akrelia/Rose-Revolution-Server/tree/master)
- [Basic Client](https://drive.google.com/file/d/1vjBFy7oBV2MYX-qSKDiMrT8FR0IWCam5/view?usp=sharing)

# Introduction

**ROSE Revolution** is an open-source project aiming to recreate the MMORPG **ROSE Online** (*Rush On Seven Episodes Online*) from scratch.

The project features:

- A completely rebuilt Unity client.
- A brand-new server  written in modern C#.
- A complete asset conversion pipeline replacing the original legacy formats.
- A modular and data-driven design.

The goal is not simply to make the game run again, but to build a clean and modern foundation for the future of ROSE Online. Also we hope that it will help people to make their own vision of ROSE Online.

We use Unity **6000.0.49f1**

<p align="center">
<img width="640" height="358" alt="Image" src="https://github.com/user-attachments/assets/c4e7f20c-ddc0-4da4-be9e-6939eea76a8f" />
</p>

# Our Goal

ROSE Online was originally developed more than 20 years ago using technologies that are now outdated. The original client and server were written in good old C++ and relied on many custom formats and tools.

ROSE Revolution takes a different approach :

- Legacy client formats are only used during the import process (once).
- All game content is converted into native Unity assets.
- Data is stored in clean and reusable databases.
- The client uses Unity Addressables for efficient loading and memory management.
- The server uses a modern C# architecture designed for scalability.
- Every important data can be exported as plain JSON.

The result is a cleaner, easier-to-maintain, and more accessible project.

<p align="center">
  <img src="https://i.imgur.com/oXR6h05.png" alt="ROSE Revolution Preview" width="1000" />
</p>

# Setup

We aim to keep the project as simple and portable as possible.

To run the project, you will need:

- The client repository.
- The server repository (Rose Revolution only works with its own server).
- Extracted ROSE client data (`3DDATA` and related files) to import everything.
- An optional running PostgreSQL installation.

The original game assets are not included in this repository.

If you want to, we provide a very light 3DDATA folder on our Discord, including some fixes for known issues with the original assets.

Once imported, the assets are converted into proper Unity resources. The original files are only required once during the import process. Once the import done, you won't need to do it again.

Note that we provide a basic client to test the project without having to setup everything, and we are also hosting a test server. 

# Which Version of ROSE Online?

The project currently targets a mostly vanilla **iROSE (International ROSE)** experience.

However, thanks to the modular architecture of both the client and server, adapting the project to other ROSE versions is possible, but we won't provide any help.

But you can write your own importer if you want.

# Contributing

Want to help ? Please join us and share ideas, improve the code, or simply join the discussion. 

Whether you are interested in:

- Unity development
- C# server development
- Reverse engineering
- Tools
- Documentation
- Game design

Everyone is welcome, whatever you are a skilled programmer or just a ROSE fan.

Join our [Discord Server](https://discord.gg/2SxQWtMC3X) to get started and find guides, discussions, and development updates.
