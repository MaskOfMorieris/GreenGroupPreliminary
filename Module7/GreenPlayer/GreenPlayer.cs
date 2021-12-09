using System;
using System.Collections.Generic;

namespace Module8
{
    struct CompassPositions
    {
        private List<Position> compassList = new List<Position>();
        public CompassPositions(Position center)
        {
            //North
            int x = center.X;
            int y = center.Y + 1;
            compassList.Add(new Position(x, y));

            //East
            x = center.X + 1;
            y = center.Y;
            compassList.Add(new Position(x, y));

            //South
            x = center.X;
            y = center.Y - 1;
            compassList.Add(new Position(x, y));

            //West
            x = center.X - 1;
            y = center.Y;
            compassList.Add(new Position(x, y));
        }

        public Position North()
        {
            return compassList[0];
        }
    }
    internal class GreenPlayer : IPlayer
    {
        private static readonly List<Position> Guesses = new List<Position>();
        private static List<List<AttackResult>> RecentAttacks = new List<List<AttackResult>>();
        private int _index;
        private static readonly Random Random = new Random();
        private int _gridSize;
        private int turnCount;
        private bool compass;
        private Dictionary<int, Position> PriorGuesses;

        public GreenPlayer(string name)
        {
            Name = name;
        }

        public void StartNewGame(int playerIndex, int gridSize, Ships ships)
        {
            _gridSize = gridSize;
            _index = playerIndex;
            PriorGuesses = new Dictionary<int, Position>();
            turnCount = -1;

            GenerateGuesses();

            //Random player just puts the ships in the grid in Random columns
            //Note it cannot deal with the case where there's not enough columns
            //for 1 per ship
            var availableColumns = new List<int>();
            for (int i = 0; i < gridSize; i++)
            {
                availableColumns.Add(i);
            }

            foreach (var ship in ships._ships)
            {
                //Choose an X from the set of remaining columns
                var x = availableColumns[Random.Next(availableColumns.Count)];
                availableColumns.Remove(x); //Make sure we can't pick it again

                //Choose a Y based on nthe ship length and grid size so it always fits
                var y = Random.Next(gridSize - ship.Length);
                ship.Place(new Position(x, y), Direction.Vertical);
            }
        }

        private void GenerateGuesses()
        {
            //We want all instances of GreenPlayer to share the same pool of random guesses
            //So they don't repeat each other.

            //We need to populate the guesses list, but not for every instance
            //- so we only do it if the set is missing some guesses
            if (Guesses.Count < _gridSize * _gridSize)
            {
                Guesses.Clear();
                for (int x = 0; x < _gridSize; x++)
                {
                    for (int y = 0; y < _gridSize; y++)
                    {
                        Guesses.Add(new Position(x, y));
                    }
                }
            }
        }

        public string Name { get; }
        public int Index => _index;

        public Position GetAttackPosition()
        {
            Position guess = new Position(0,0);
            if (turnCount >= 0)
            {
                AttackResult lastAttack = RecentAttacks[turnCount][_index];
                if (turnCount >= 1)
                {
                    AttackResult attackBeforeLast = RecentAttacks[turnCount - 1][_index];
                    if (lastAttack.ResultType == AttackResultType.Hit && 
                    attackBeforeLast.ResultType == AttackResultType.Hit)
                    {
                        //get Direction to Attack in and make guess
                    }
                    else
                    {
                        compass = true;
                        guess = CompassAttack();
                        Guesses.Remove(guess);
                    }
                }
            }
            else if (compass)
            {
                guess = CompassAttack();
                Guesses.Remove(guess);
            }
            else
            {
                guess = Guesses[Random.Next(Guesses.Count)];
                Guesses.Remove(guess); //Don't use this one again
            }
            turnCount++; //Count that we made a guess
            return guess;
        }

        public void SetAttackResults(List<AttackResult> results)
        {
            Position priorAttack = results[0].Position;
            PriorGuesses.Add(turnCount, priorAttack);
            RecentAttacks.Add(results);
        }

        private Position CompassAttack()
        {
            Position lastAttack_pos = RecentAttacks[turnCount][_index].Position;
            //Get all directional positions for next attacks
        }
    }
}
