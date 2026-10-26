# Mastermind

[![CI](https://github.com/lgamorim/mastermind/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/lgamorim/mastermind/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

_My implementation in .NET/C# of the Mastermind board game._

**[Play the web version in your browser](https://lgamorim.github.io/mastermind/)**, no install needed.

In both the web and console versions, the **computer is the Code Maker** and **you are the Code Breaker**. The computer picks a secret code and your goal is to break it within the allowed number of guesses.

## Rules

- The secret code is made up of **4 colored pegs**, arranged in a specific order.
- There are **6 colors** to choose from: **Red**, **Blue**, **Yellow**, **Green**, **White**, and **Black**.
- Colors **may repeat** within the code, and any color can appear in any position.
- You have **10 guesses** to crack the code.

After each guess, the Code Maker gives you feedback using small key pegs:

- A **Black** key peg — one of your pegs is the **right color in the right position**.
- A **White** key peg — one of your pegs is the **right color but in the wrong position**.
- **No** key peg — that color is not in the secret code (or not in the remaining unmatched positions).

The feedback tells you *how many* pegs are correct, but **not which ones**. Each peg in the secret code earns at most one key peg, so colors are never counted twice.

You **win** the moment a guess earns **4 black key pegs** (the exact code in the exact order). If you use up all 10 guesses without doing so, the **Code Maker wins** and the secret code is revealed.

## How to play

There are two versions of the game: a **web app** and a **console app**. Both follow the same rules.

### Web app

Play it [online](https://lgamorim.github.io/mastermind/), or build and run the web app locally:

```
dotnet run --project src/Mastermind.WebApp
```

Then open the printed localhost URL in your browser.

1. Click **New Game** to start. The Code Maker silently locks in a secret code behind the shield at the top of the board.
2. **Drag** a colored peg from the palette at the bottom and **drop** it onto one of the four peg holes in the active row. Click a filled hole to clear it.
3. Once all four holes are filled, click **Submit** to lock in your guess.
4. The Code Maker responds with key pegs (black and white dots) on the right side of the row. Use that feedback to refine your next guess.
5. Keep guessing until you crack the code or exhaust all 10 attempts. If you run out, the secret code is revealed.

Your game is saved in the browser after every guess, so reloading the page or coming back later picks it up where you left off. Pegs placed but not yet submitted are not saved. **New Game** replaces the saved game.

Below the board, **Statistics** tracks your games in the same browser: games played, win rate, your current and best winning streaks, and a chart of how many guesses each win took. Starting a new game while one with guesses is still in progress counts as a loss. Games played in debug mode are not counted. Use **Hide statistics** under the board to collapse the panel, and **Show statistics** to bring it back.

#### Coach (web app)

Tick **Coach** above the board for feedback on your deduction. A line under the board says how many of the 1,296 possible codes still fit the feedback so far. If a guess could not have been the code, given what earlier feedback proved, the coach says which earlier guess it contradicts and marks the row's number in amber. The coach never reveals which codes remain, and coached games count toward your statistics.

#### Debug mode (web app)

To play with the secret code visible behind the shield, add `?debug=true` to the URL:

```
http://localhost:5012/?debug=true
```

Debug mode only works when you run the app locally with `dotnet run`; the [online version](https://lgamorim.github.io/mastermind/) ignores it.

### Console app

Build and run the console app:

```
dotnet run --project src/Mastermind.ConsoleApp
```

When the game starts:

1. The available colors are listed on screen, and the Code Maker locks in a hidden secret code (`[X] [X] [X] [X]`).
2. At each prompt, type your guess as **4 colors separated by a single space**, for example:

   ```
   Red Blue Green Black
   ```

   Color names are case-insensitive. If you enter the wrong number of colors or an unknown color, you'll be asked to try again (it doesn't cost you a guess).
3. The Code Maker responds with key pegs: `●` for each peg of the right color in the right position, `○` for each right color in the wrong position, or `none`. Use that feedback to refine your next guess.
4. After each guess, the board so far is shown as a table, with each code peg drawn as a colored `●` next to its color name.
5. Keep guessing until you crack the code or run out of your 10 attempts.

Colors appear in terminals that support them. When output is redirected, or the `NO_COLOR` environment variable is set, the game prints plain text, and the color names still say which peg is which.

Type `/coach` at any prompt to turn the coach on or off for the rest of the session. After each guess it prints how many codes still fit the feedback, and warns when a guess could not have been the code given earlier feedback. Typing `/coach` doesn't use up a guess.

#### Debug mode (console app)

To play with the secret code revealed on screen (handy for testing), pass the `DEBUG` argument:

```
dotnet run --project src/Mastermind.ConsoleApp -- DEBUG
```
