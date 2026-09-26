# DeezNotas' Network Tools Fork

Welcome to my fork of Luca's Network Tools! I absolutely love this mod, and I've been hoping that the "SmoothCurve" feature would graduate out of the "coming soon" phase and into "check this shit out" phase. I am a professional software developer, although I'm mostly and old embedded audio guy, writing SW for professional and consumer electronics devices that have mics and/or speakers. I've designed and written audio DSP algorithms, audio processing and pipelining frameworks, designed system architectures, conducted subjective listening tests, written factory test sequences and factory data analysis scripts, and everything in between. HOWEVER, I haven't really done that MUCH in gaming ... YET (I am starting a staff-level SW dev role at Roblox in November).  I've also been interested in dipping my toe into the CS2 modding world, so I decided to try my hand at playing around with the Luca's legendary NetworkTools mod!  

# First, some docs!

The first thing I've done is update this readme, and generate a couple of other docs to make others' lives easier if they ever wanted to contribute: 

## Windows build setup: 

- [BOOTSTRAP.md](BOOTSTRAP.md)

## Project guides:

- [Build system](NetworkTools.docs/build-system.md): tools, MSBuild targets, code generation, deployment, and diagnosed build issues.
- [System architecture](NetworkTools.docs/system-architecture.md): mod lifecycle, ECS tools, UI bindings, geometry processing, and open design questions.

# Current Goals

I want to try and take some incremental steps towards a more complete Network Tools embodiment.  The first step is the first step.  The remaining steps are not necessarily in chronological order. 

- Step 1: get "SmoothCurve" working for simple cases
- Step 2: get the curve and slope tools working together concurrently
- Step 3: get the curve and slope tools to start being context aware (avoid obstacles, tunnel through mountains, bridge over or under an obstructing network segment if you set some kind of "no at grade crossings" type of option, etc etc)
- Step 4: Add agentic sophistication: "find the optimal route between these buildings and around that mountain and over that river to get this freight rail connected to that terminal over there" - this is a bit lofty, and I know that this community tends to be pretty anti-AI, but I still think it could be a lot of fun.  
- Step 5: ???
- Step 6: profit
