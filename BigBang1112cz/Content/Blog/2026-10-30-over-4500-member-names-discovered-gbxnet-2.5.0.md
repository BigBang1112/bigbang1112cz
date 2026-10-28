---
title: "Over 4500 member names discovered, including public IP addresses in TM2 replays – GBX.NET 2.5.0"
published: 2026-10-28 15:00
og-image: /img/blog1.jpg
---

GBX.NET 2 was released 2.5 years ago already, so it was about time to look at some of its technical debt in version 2.5.

With the releases of Sol 6 or tools like REA, I realized what kind of future we are heading to with software... so I had to do a hard and unpopular decision within the community and send the full AI workflows into the library to keep up, as it is easier than ever for someone to just clone the library into JavaScript or Python and make it "perfect" themselves, making the 5 years of manual decompile effort dead in no time.

However, that also opened me to some gold within Gbx files and the engine as a whole. More on that below.

## Generator performance

The main performance smell of the library was definitely the class generator code. It was very slow to build the library at some point and it was basically impossible to figure out the cause, as performance benchmarks of the generator code were always reporting negligible time spans. So I first cleared up the [ChunkL specification](https://github.com/BigBang1112/chunkl/blob/main/SPECIFICATION.md), updated the library, and then AI-generated a new generator library focused towards syntax over semantics (as it is meant to be used only within the GBX.NET library build, not by consumers). This decreased the initial full build time from about 25 seconds to 18 seconds, and the code is also a bit more manageable (especially by agents) which is handy for some complicated generation paths. The build has slown down drastically anyway to 30+ seconds due to many new class additions in this version...

## ChunkL From Decompilation

Once the generator was ready, I connected the project's code with the game decompilation using [a Ghidra MCP server](https://github.com/themixednuts/GhidraMCP), but a bit more specifically. I took multiple Trackmania games, decompiled them, and ordered them by symbol data quality:

1. Private ManiaPlanet 4.1 symboled debug build
2. Public TmNationsForever symboled build
3. Trackmania (2020) build from February 2026
4. Latest ManiaPlanet logs build (includes extra strings)
5. Older Trackmania games (excluding TmUnitedForever)
6. Virtual Skipper 5

I specified these as concrete program names as I had in Ghidra in my [ChunkL From Decompilation](https://github.com/BigBang1112/gbx-net/blob/b0d9fc8ad0fe2b94b74b606097b46e07eb00538d/.agents/skills/chunkl-from-decompilation/SKILL.md) skill in this order, which also included instructions on how to handle the ChunkL definitions in various edge cases. Before I started using the skill, I also processed class info and `GetChunkInfo`/`Chunk` functions to make it easier for the agent to look around.

Then I just ran the skill and only typed the class I wanted to investigate. This alone found nearly everything that's possible to find about the class' serialization across all Trackmania games. Now I only needed to run this for basically every single class, and spend quite a bit of tokens. Scarily enough, it only cost me 1 and a half of the ChatGPT Plus subscription bills, roughly just 30€ in total...

## What it found

This process took roughly 2 to 3 weeks to complete and the results are:

- [4400+ unknown member names have been discovered](https://github.com/BigBang1112/gbx-net/blob/895958dddad3589854707f324ccbbd5473894e71/Docs/NewMemberNames2.5.0.md)
- 250+ new classes have been added to the library
- Features from the scrapped ManiaPlanet 5 version have existing code in the private 4.1 build, marked "dead" in public builds

While not being able to run the private build, some features have been confirmed to (potentially) exist in map serialization that never made it to the game:

- World distort - a bitmap across the map that added extra terrain height across the ground blocks
- SubMaps - possible extension of the map over its boundaries via nested map objects
- Traffic paths - specifics of where scenery cars should be looping around
- Citizen paths - paths on the block info (they are encoded in the block models) where citizens would walk

The meaning is assumed from member names found. All of these now exist as a theory in the library, but when set, they will likely crash the game (it was tested that world distort in fact does).

**Next I checked replays and ghosts to find everything that was still missing**, with a bit of copium that there is a timestamp of the run somewhere (spoiler alert it isn't). Some new validation parameters have been found, like the race start time or "game rules" mask, but I also gave a deeper look into the anticheat chunk `0x023`.

The AI found a couple of scuffed parameters in there:

- CpuDescription
- CpuKind
- GpuDescription
- GpuDeviceId
- GpuVendorId
- **HardwareUid**
- **NetworkAddress**
- **LoggedUserName**

Now why would this need to be permanently stored in the ghost object? This information is already sent to the master server upon connecting. I don't have any answers to this really. Now luckily, it's not that bad. **All are empty in all replays except for CpuKind and NetworkAddress.**

CpuKind is the CPU architecture *type* used to run the game. If the game is built as 32bit, it reports 32bit architectures, and same for 64bit. It differentiates Intel, AMD, and other vendor CPUs, so not a big deal slash anything too useful and was already available through `Validate_CpuKind` a long time ago.

The real concern is NetworkAddress. This was a suspicious 32bit integer value that kept changing on Trackmania 2 replays, so I gave it a closer look. The integer was indeed an efficiently stored IP address. That was weird though, as settings across the game usually store IP addresses as strings. But as I converted that integer into an IP address, I saw the deal going on here. There were player IP addresses stored in all ghosts. At first I saw they were all in local format (`192.168/16`, `172.16/12`, `169.254/16`, ...) and thought it was fine, but then I got jumpscared by a public IP format and seeing my own public IP address in one of my replays. I thought I had some misconfiguration somewhere, but going deeper, I saw that I'm not alone, and it's not individual player's fault.

**In ManiaPlanet 4, roughly 9.7% of the ghosts have public IP address included**, using the sample size of 9644 ghosts downloaded from leaderboards. 40% of the ghosts have a private LAN address, and 50% have a link-local address.

What's weirder is that **ManiaPlanet 3 ghosts have the public IP stored in 35.1% of the cases** (sample size of 22829 ghosts), with link-local taking a single digit portion, and private LAN address taking the majority.

A fellow guy Mystixor reported me that his IP address in the replay isn't anything he recognizes, so I would not take it as the ultimate proof of someone's location by any means, but it's likely that you will spot your public IP in your own replays. By the analysis, **the IP is more about what the game decided to select as available, than what your real address was**, but the game still prefers picking the public IP when it can find one. What's worse is that when you play on a server and save a session replay, it includes all other player's ghosts, including their public IP addresses occasionally...

Now you don't need to worry too much. **NetworkAddress is set only in Trackmania 2, Trackmania Turbo, and Shootmania. Trackmania (2020) and TrackMania Forever games and below are not affected by this issue.** TM2020 sets the NetworkAddress to `0.0.0.0` since the release of the game. Interestingly enough, replays driven in alpha version before the release still stored the IP as before. Luckily, Nadeo removed this behaviour in June 2020, but they didn't bother patching ManiaPlanet, of course... Older Trackmania games don't store anticheat data chunk at all.

Extra note to know: if you don't pay extra for a static public IP, your IP likely rotates roughly every week or when you restart your router. So that means your IP becomes a bit different than in a 1 week old replay, but the approximate location is still mostly readable.

If you wish to avoid your IP to be stored and forever read by others in Trackmania 2 and Trackmania Turbo, I recommend you to install the [Mystixor's IP-less Ghosts OpenPlanet plugin](https://openplanet.dev/plugin/984). It sets the IP in your next runs to `0.0.0.0`.

Gbx Explorer also blurs public IP addresses by default from now on, so you don't need to worry about accidentally exposing someone on stream or something similar when browsing replay info.

## 99.99% parse success

The goal was also to reach basically error-less parse using the large scale apply that AI allows. I created a GbxCLI app (will be properly distributed soon) with verification features and wrote simple instruction to look for all GameData and Packs folder and report failing file names, which would then be directed to the ChunkL From Decompilation skill, until ideally 100% parse success. It's realistically 99.99%, as some Gbxs are just too quirky.

## Builders

Node builders have been very useful in GBX.NET 1, and now they are finally back in their full shape on every single node! The class generator now includes fluent builder layers and separates them by game. This makes it very easy to create new objects:

```cs
using GBX.NET.Engines.Game;

var clip = CGameCtnMediaClip.Create()
    .WithName("Intro")
    .ForTMF()
    .Build();
```

Game Version Interfaces also add an easier overview of the classes scoped to a specific game of your interest:

```cs
using GBX.NET.Engines.Game;
using GBX.NET.Interfaces.Game;

IGameCtnMediaTrackTMF track = CGameCtnMediaTrack.Create()
    .ForTMF()
    .WithName("Camera")
    .BuildInterface();

IGameCtnMediaClipTMF clip = CGameCtnMediaClip.Create()
    .ForTMF()
    .WithTracks(track)
    .BuildInterface();
```

Unfortunately, because the generated code ended up way bigger than expected (5MB+), builders have been split into the separate GBX.NET.Builders package. But when trimming is enabled, this can be reduced to a few kBs pretty much (just depends what you want to use!).

## External nodes for anything

The split of Node and NodeFile is finally gone and you can now reference external files to any node you want.

This was done by making private `NodeRef<T>` field, which privately handles resolution and references of nodes, the property still uses the expected node type (therefore no deep nesting), and you can view these node references in the `References` view property instead.

This is once again fully generated behaviour based on ChunkL files. There are still some minor hacks around the spec, but those should be resolved rather soon.

## Conclusion

[GBX.NET 2.5.0 is now available as a stable build.](https://github.com/BigBang1112/gbx-net/releases/tag/v2.5.0)

The worrying part is the dangers of AI definitely. You no longer need low level geniuses to figure out small little things, anyone can find a big problem in the smallest thing somewhere and cause damage on daily basis.

What should be understood though is that if I wouldn't come public with this, someone else with bad intentions would certainly know soon enough. Obfuscation is officially pointless. We should be prepared for the upcoming dangers of AI and fight them with the good will.

And most importantly, **store IPs only in your private databases, please.**

Once again, install [Mystixor's IP-less Ghosts OpenPlanet plugin](https://openplanet.dev/plugin/984) if you don't want your next replays to contain a public IP address in Trackmania 2 and Trackmania Turbo.


## Links

- [GBX.NET](https://github.com/BigBang1112/gbx-net) and [Gbx Explorer](https://explorer.gbx.tools/)
- [IP-less Ghosts](https://openplanet.dev/plugin/984)
- [GameBox Sandbox Discord Server](https://discord.gbx.tools)
