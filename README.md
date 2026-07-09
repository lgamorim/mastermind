# Mastermind

_My implementation in .NET/C# of the Mastermind board game._

In this console version, the **computer is the Code Maker** and **you are the Code Breaker**. The computer picks a secret code and your goal is to break it within the allowed number of guesses.

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

Build and run the web app:

```
dotnet run --project src/Mastermind.WebApp
```

Then open the printed localhost URL in your browser.

1. Click **New Game** to start. The Code Maker silently locks in a secret code behind the shield at the top of the board.
2. **Drag** a colored peg from the palette at the bottom and **drop** it onto one of the four peg holes in the active row. Click a filled hole to clear it.
3. Once all four holes are filled, click **Submit** to lock in your guess.
4. The Code Maker responds with key pegs (black and white dots) on the right side of the row. Use that feedback to refine your next guess.
5. Keep guessing until you crack the code or exhaust all 10 attempts. If you run out, the secret code is revealed.

#### Debug mode (web app)

To play with the secret code visible behind the shield, add `?debug=true` to the URL:

```
http://localhost:5000/?debug=true
```

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
3. The Code Maker responds with black and white key pegs. Use that feedback to refine your next guess.
4. Keep guessing until you crack the code or run out of your 10 attempts.

#### Debug mode (console app)

To play with the secret code revealed on screen (handy for testing), pass the `DEBUG` argument:

```
dotnet run --project src/Mastermind.ConsoleApp -- DEBUG
```
