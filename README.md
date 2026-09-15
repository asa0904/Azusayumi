# Azusayumi
Azusayumi is a UCI chess engine written in C#.
It is currently a baseline implementation featuring only basic search algorithms.

## Features
### Board Representation
- Bitboards with Little-Endian Rank-File Mapping
- Black Magic Bitboards
### Search
- Iterative Deepening
- Principal Variation Search
- Quiescence Search
- Transposition Table
- Move Ordering
  - Hash Move
  - MVV-LVA
  - Killer Move
  - History Heuristic
- Lazy SMP
### Evaluation
- Material
- Piece-Square Tables
- Mobility
### Automated Tuning
- Logistic Regression
- Adam

## Build
With the .NET 10 SDK installed, run:

```bash
git clone https://github.com/asa0904/Azusayumi.git
cd Azusayumi
dotnet build src/Azusayumi.Cli -c Release
```

## AI Assistance
Generative AI tools (Gemini, ChatGPT, and Copilot) were used for documentation, code review, and performance analysis.
All core engine design decisions, search algorithms, evaluation logic, testing, and final code review were performed by the author.
