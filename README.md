# Welcome to ROSE Revolution

<p align="center">
  <img src="https://i.imgur.com/QorK0mp.png" alt="ROSE Revolution Logo" width="200" />
</p>

<p align="center">
  A modern reimplementation of <b>ROSE Online</b> built with Unity and modern C#.
</p>

<p align="center">
  <img src="https://i.imgur.com/7zmTjLY.png" alt="ROSE Revolution Preview" width="1000" />
</p>

- [Discord Server](https://discord.gg/2SxQWtMC3X)
- [Website](https://baptistefran.github.io/rose-revolution/)
- [Server Repository](https://github.com/Akrelia/Rose-Revolution-Server/tree/master)

---

# About the Project

**ROSE Revolution** is an open-source project aiming to recreate the MMORPG **ROSE Online** (*Rush On Seven Episodes Online*) from scratch.

The project features:

- A completely rebuilt Unity client.
- A brand-new server architecture written in modern C#.
- A complete asset conversion pipeline replacing the original legacy formats.
- A modular and data-driven design.

The goal is not simply to make the game run again, but to build a clean and modern foundation for the future of ROSE Online. Also we hope that it will help people to make their own vision of ROSE Online.

---

# Who We Are

We are former ROSE Online players who have always been passionate about the game and its community.

Some of us were also involved in the ROSE private server scene over the years, giving us a unique perspective on both the game itself and the technical challenges behind it.

Today, we want to give ROSE Online a fresh start with modern tools while preserving what made the game special.

<p align="center">
  <img width="400" height="298" alt="Image" src="https://github.com/user-attachments/assets/f3361465-00f5-4658-a8ff-c21be1b8508c" />
</p>

---

# Project Vision

ROSE Online was originally developed more than 20 years ago using technologies that are now outdated. The original client and server were written in C++ and relied on many custom formats and tools.

ROSE Revolution takes a different approach:

- Legacy client formats are only used during import.
- All game content is converted into native Unity assets.
- Data is stored in clean and reusable databases.
- The client uses Unity Addressables for efficient loading and memory management.
- The server uses a modern C# architecture designed for scalability.
- Every important data can be exported as plain JSON.

The result is a cleaner, easier-to-maintain, and more accessible project.

---

# Which Version of ROSE Online?

The project currently targets a mostly vanilla **iROSE (International ROSE)** experience.

However, thanks to the modular architecture of both the client and server, adapting the project to other ROSE versions is possible, but we won't provide any help.

![Image](https://i.imgur.com/QmtNKjQ.png)

---

# Modern Development Workflow

One of the biggest changes compared to the original game is the complete replacement of the old asset workflow.

Instead of working directly with legacy files at runtime:

Everything is imported into Unity:

- Maps
- Monsters
- Items
- Skills
- Animations
- Textures
- Effects

And so on.

Developers can then work with standard Unity tools instead of relying on outdated external utilities.

---

# Setup

We aim to keep the project as simple and portable as possible.

To run the project, you will need:

- The client repository.
- The server repository.
- Extracted ROSE client data (`3DDATA` and related files) to import everything.
- An optionnal running PostgreSQL installation.

The original game assets are not included in this repository.

If you want to, we provide a very light 3DDATA folder on our Discord, including some fixes for known issues with the original assets.

Once imported, the assets are converted into proper Unity resources. The original files are only required once during the import process. Once the import done, you won't need to do it again.

---

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
