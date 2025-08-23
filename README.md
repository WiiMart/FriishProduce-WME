# FriishProduce-WME

This is a fork of [CatmanFan/FriishProduce](https://github.com/CatmanFan/FriishProduce) maintained by the [WiiMart](https://wiimart.org/) team.

## About This Fork
The purpose of this fork was initially to extend upon features, and to improve on the overall function and quality of FriishProduce. The main purpose of this fork is now to maintain and extend a unique build of FriishProduce, primarily focusing on features that will be uniquely benefitial to WiiMart alongside any other QOL improvements.

Some situationally benefitial updates include the 'Genre' field, output parameter and automatic filling/data retrieval for the 'Genre' field.

#### Maintenance and extensibility includes:
- New features for Wii VC injection workflows
- More customization and control features
- More online/offline compatibility features

- QOL updates:
  - [X] Genre field
  - [X] Optionally lowercase WAD file name parameters
  - [X] Optionally save downloaded thumbnails/banners locally
  - [X] Optionally save downloaded WADs locally

- Integrating full regional support per game and per emulator
  - [ ] Full support for 'MSX' injects in North America and Europe
  - [ ] Full support for 'Flash' injects in CJK regions

#

### Building
Clone this repository instead of the original:

```bash
git clone https://wiilab.wiimart.org/wiimart/friishproduce-wme.git
cd friishproduce-wme
```
Half-baked batch scripts and a VSCode 'tasks.json' are included in the forked source for running and building as well. You can reconfigure these for alternative means of development.

#

#### TODO
- FLASH
  - Fix strap reminder and config issues in Flash injects
  - Fix KirbyTV Flash injects (again)
  - Remove GENRE param *if* Flash inject
- MSX
  - Finish Operations Manual templates
  - Finish Regional support

#
<br>

# FriishProduce
<div align=center><a href=""><img src="https://github.com/CatmanFan/FriishProduce/blob/main/images/icon.png" /></a>

![GBAtemp thread](https://img.shields.io/badge/GBAtemp-thread-blue?link=https%3A%2F%2Fgbatemp.net%2Fthreads%2Ffriishproduce-multiplatform-wad-injector.632028%2F)
 ![Wiki](https://img.shields.io/badge/wiki-white?link=https%3A%2F%2Fcatmanfan.github.io%2FFriishProduce%2F)
</div>

**FriishProduce** is a WAD channel injector/creator for (v)Wii. It can be used to convert ROMs, disc images or other types of software to installable WADs for Wii/vWii (Wii U). This includes injectable Virtual Console (VC) games, as well as single ROM loaders (SRLs), and Adobe Flash files.
This application is designed to streamline the process to as few third-party programs as possible.

---

## Features
This injector bypasses other third-party assets (such as Common-Key.bin, HowardC's tools, and Autoinjectuwad/Devilken's VC) by handling many steps directly from the program's code. Some examples:
* Automatic WAD/U8/CCF handling
* VC ROM injection through hex writing and/or file replacement
* Automatic banner/icon editing
* Automatic editing of source WAD's savedata where available
* Additional content/emulator options for each platform where supported
* Replace WAD contents with forwarder to auto-load specific emulator core and ROM

### Platforms
The following platforms are currently supported:

* *Virtual Console*:
  * Nintendo Entertainment System (NES) / Famicom
  * Super Nintendo Entertainment System (SNES) / Super Famicom
  * Nintendo 64
  * SEGA Master System
  * SEGA Mega Drive / Genesis
  * NEC TurboGrafx-16 / PC Engine (HuCARD & CD-ROM)
  * SNK NEO-GEO
  * Commodore 64
  * Microsoft MSX / MSX2
* Others:
  * Adobe Flash
  * Sony PlayStation
  * RPG Maker 2000 / 2003

## Wiki
Please check the **[wiki](https://catmanfan.github.io/FriishProduce/)** for a tutorial on how to use the app, and more useful tips.

---

## To-Do

### Potential
- [ ] Merge separate components of ProjectForm into panels / Create UserControl for content options ?
- [ ] Restructuring, trimming features and cleaning code
- [ ] Add default WAD bases option
- [ ] Add title.png/xyz selector for RPG Maker games

---

### License
This application is distributed and licensed under the **GNU General Public License v3.0** ([view in full](https://github.com/CatmanFan/FriishProduce/blob/main/LICENSE)).

---

To view the source code for the legacy interface (all versions up to and including **v0.26-beta**), go **[here](https://github.com/CatmanFan/FriishProduce-Legacy)**.
=======