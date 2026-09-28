# DeezNotas' Network Tools Fork

Welcome to my fork of Luca's Network Tools! I absolutely love this mod, and I've been hoping that the "SmoothCurve" feature would graduate out of the "coming soon" phase and into "check this shit out" phase. I am a professional software developer, although I've mostly been an old-school embedded audio guy, writing SW for professional and consumer electronics devices that have mics and/or speakers. I've designed and written audio DSP algorithms, audio processing and pipelining frameworks, designed system architectures, conducted subjective listening tests, written factory test sequences and factory data analysis scripts, and everything in between. HOWEVER, I haven't really done that MUCH in gaming ... YET (I'll be joining Roblox as a Staff SW Developer in November).  But in October, I've got some free time!  I've been interested in dipping my toe into CS2 modding, and super stoked about this mod, and I figure Luca is probably a busy person: life, family, day job, etc. SO I've decided to try my hand at playing around with the legendary NetworkTools mod and see what pops out! Wish me luck! 

# First: Some Docs! Always!!

Current development checkpoint: an experimental **Debug-only** Smooth Curve search
preserves connections at one rail junction endpoint by adjusting the selected path.
One saved failing case now passes preview and Apply. Broader junction support remains
work in progress; see [validation evidence and limits](NetworkTools.docs/offline-validation-confidence.md).

The first thing I've done is update this readme, and generate a couple of other docs to make others' lives easier if they ever wanted to contribute and to make my own life easier because I am forgetful as all hell: 

## Windows Build Setup: 

- [BOOTSTRAP.md](BOOTSTRAP.md)

The bootstrap also supports an optional full local game decompile with
`-Decompile -DecompilePath <directory>` for source-grounded modding investigations.

## Useful Project Guides:

- [Build system](NetworkTools.docs/build-system.md): tools, MSBuild targets, code generation, deployment, and diagnosed build issues.
- [System architecture](NetworkTools.docs/system-architecture.md): mod lifecycle, ECS tools, UI bindings, geometry processing, and open design questions.
- [Smooth Curve plan](NetworkTools.docs/smooth-curve-plan.md): prototype scope, geometry questions, and later explorations.
- [AGENTS.md](AGENTS.md): Instructions for our robot collaborators, you know you love it

# Current Goals (Brain Vomit Edition)

I want to try and take some incremental steps towards a more complete Network Tools embodiment.  The first step is the first step.  The remaining steps are not necessarily in chronological order. 

- Step 1: get "SmoothCurve" working for simple cases
- Step 2: get the curve and slope tools working together concurrently
- Step 3: get the curve and slope tools to start being context aware (understand the terrain, avoid obstacles, tunnel through mountains, bridge over or under an obstructing network segment if you set some kind of "no at grade crossings" option, etc etc)
- Step 4: Instrument the hell out of the debug builds, and use info from that instrumentation to see if we can devise some kind of out-of-game test harness to speed up iteration times.  
- Step 5: Add agentic/natural language sophistication: "find the optimal route between these buildings and around that mountain and over that river to get this freight rail segment connected to that terminal over there across the map, no at grade crossings, keep grade lower than 3%, etc etc" or "why the fuck is that node opening a hole into the Upside Down right now?!?!" - this is a bit lofty, and will probably never actually happen, and I know that this community tends to be pretty anti-AI, but I still think it could be a lot of fun.  
- Step 6: ???
- Step 7: profit

## Junction investigation setup

The development diagnostics use our locally extended **Cities II Agent Bridge**;
ordinary Network Tools use and builds do not require that mod. Follow
[local bridge setup and checks](BOOTSTRAP.md#junction-development-dependencies).
That guide also covers the `cs2-modding@csmodding` coding-agent plugin. Our bridge extension is maintained in [our fork](https://github.com/danwayneharris/cities2-agent-bridge-ndc), on `dan/junction-snapshot` pending review; upstream releases do not include these diagnostic commands yet.
