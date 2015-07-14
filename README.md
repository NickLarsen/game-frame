# two-player-games
An implementation of a bot capable of playing a wide assortment of 2 player games.

###Technologies

- negamax
- iterative deepening
- time per move cutoff
- alpha/beta pruning
- transposition table
- history heuristic

###Games implemented

- [tic tac toe](https://en.wikipedia.org/wiki/Tic-tac-toe)
  - status: **solved**
  - state heuristic: always tie
  - history heuristic: last move, score: 2^depth 
- [nine men's morris](https://en.wikipedia.org/wiki/Nine_Men%27s_Morris)
  - status: **untested**
  - state heuristic: pieces advantage / 10 + adjacent count / 10000
  - history heuristic: from/to, score: ceiling(1.5^depth)

