﻿using System;
using System.Collections.Generic;
using System.Linq;
using Module8;

namespace CS3110_Module8_Green
{ 
    struct CompassPositions
    {
        public List<Position> CompassList {get; private set;}
        public CompassPositions(int nothing)
        {
            CompassList = new List<Position>();
        }
        public CompassPositions(Position center)
        {
            CompassList = new List<Position>();
            //North
            int x = center.X;
            int y = center.Y + 1;
            CompassList.Add(new Position(x, y));

            //East
            x = center.X + 1;
            y = center.Y;
            CompassList.Add(new Position(x, y));

            //South
            x = center.X;
            y = center.Y - 1;
            CompassList.Add(new Position(x, y));

            //West
            x = center.X - 1;
            y = center.Y;
            CompassList.Add(new Position(x, y));
        }
    }
    
    struct DictionableResult
    {
        public Position Position { get; private set; }
        public int Result { get; private set; }

        //Constructor to interpret received results to Dictionary format
        public DictionableResult(AttackResult eResult)
        {
            Position = eResult.Position;
            //give int a value due to struct reqs, use our unknown value
            Result = -1;
            //replace the value with result type
            Result = AttackResultToInt(eResult.ResultType);
        }

        //Constructor to manually check for a Dictionary entry but not add to
        public DictionableResult(Position position, int manualType)
        {
            Position = position;
            Result = manualType;
        }
            
        private int AttackResultToInt(AttackResultType eResult)
        {
            switch (eResult)
            {
                case AttackResultType.Hit:
                    return 1;
                case AttackResultType.Miss:
                    return 0;
                case AttackResultType.Sank:
                    return 2;
                default:
                    return -1;
            }
        }
    }

    internal class GreenPlayer : IPlayer
    {
        private static readonly List<Position> Guesses = new List<Position>();
        private static List<DictionableResult> RecentAttacks = new List<DictionableResult>();
        private int _index;
        private static readonly Random rand = new Random();
        private int _gridSize;
        private static Dictionary<Position, int> PriorGuesses;
        private static CompassPositions comPos;

        public GreenPlayer(string name)
        {
            Name = name;
        }

        public void StartNewGame(int playerIndex, int gridSize, Ships ships)
        {
            _gridSize = gridSize;
            _index = playerIndex;
            PriorGuesses = new Dictionary<Position, int>();
            comPos = new CompassPositions(0);

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
                var x = availableColumns[rand.Next(availableColumns.Count)];
                availableColumns.Remove(x); //Make sure we can't pick it again

                //Choose a Y based on the ship length and grid size so it always fits
                var y = rand.Next(gridSize - ship.Length);
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

//Proposed change to structure
        //
        //

        //Get results from last round, store the position and hit/miss to rule
        //  out potential guesses and/or create a list of hits to compass from if 
        //  our current compass is empty

        //Will need to incorporate our list of remaining guesses
        //  by removing a miss but continuing to check until end or a hit
        void IPlayer.SetAttackResults(List<AttackResult> results)
        {
            DictionableResult hit;

            var checkedResult = results.Where(result => result.ResultType == AttackResultType.Hit);

            if (checkedResult.Any())
            {
                hit = new DictionableResult(checkedResult.First());
            }
            else
            {
                hit = new DictionableResult(results[0].Position, -1);
            }

            //This should only add the hit to recent attacks the first time
            //  Not -1 result type as these are only reported attacks
            if (CheckPotentialGuesses(hit.Position, hit.Result))
            {
                //First time, store on hits so we can check compass if empty
                RecentAttacks.Add(hit);
            }
        }

        //This can (should?) be looped and is the primary decision making
        //  of the AI, with everything else contributing information to the decision

        //Sequence: List > 1, List == 1, List == 0, Hit has possible compass, guess
        public Position GetAttackPosition()
        {
            while (true)
            {
                //if compass is empty, check for hits, and make a compass on them
                //  if any valid compass direction, use that as guess
                if (comPos.CompassList.Count == 0)
                {

                    //This is the last case scenario, guess random
        //TODO: implement guessing, this is not correct logic =DONE=
                    if (RecentAttacks.Count == 0)
                    {
                        var randPos = Guesses[rand.Next(0, Guesses.Count)];
                        Guesses.Remove(randPos);
                        return randPos; //assume these are always valid
                    }
                    //

                    //Go through the hits and find out if there are any viable compass directions
                    //  around them, return that position if true, and then the compass list is
                    //  repopulated
                    foreach (var hit in RecentAttacks)
                    {
                        //populate compass list
                        comPos = new CompassPositions(hit.Position);
                        //remove this hit from the list
                        RecentAttacks.Remove(hit);

                        //check for viable position
                        foreach (var compassPos in comPos.CompassList.Where(compassPos => CheckPotentialGuesses(compassPos, -1)))
                        {
                            Guesses.Remove(compassPos);
                            if (ValidatePosition(compassPos)) return compassPos; //only return if valid position on grid
                        }
                    }
                }

                //1 thing on compass, make sure it hasn't been used, return if true, will go to next 
                //after clearing the list if false
                if (comPos.CompassList.Count == 1)
                {
                    //use null for possible pre-attack check
                    if (CheckPotentialGuesses(comPos.CompassList[0], -1))
                    {
                        var compassPos = comPos.CompassList[0];
                        Guesses.Remove(compassPos);
                        if (ValidatePosition(compassPos)) return compassPos; //again, only if valid
                    }

                    //clear list because only entry on it is invalid, won't reach if valid
                    comPos.CompassList.Clear();
                }

                //otherwise, go through compassList

                while (true)
                {
                    //get random index
                    var randIndex = rand.Next(0, comPos.CompassList.Count);

                    //if it's valid, select it
                    //  use null for potential pre-attack check
                    try
                    {
                        if (CheckPotentialGuesses(comPos.CompassList[randIndex], -1))
                        {
                            var compassPos = comPos.CompassList[randIndex];
                            Guesses.Remove(compassPos);
                            if (ValidatePosition(compassPos)) return compassPos; //only if valid
                        }
                    }
                    catch(System.ArgumentOutOfRangeException outOfRange)
                    {
                        //don't bother checking or continuing, the compass list is empty
                        break;
                    }

                    //if not, remove it and try again
                    comPos.CompassList.Remove(comPos.CompassList[randIndex]);

                    //if we get here, all on the list were bad, break and we'll
                    //go for recent hits or random guess
                    if (comPos.CompassList.Count == 0)
                    {
                        break;
                    }
                }
            }
        }

        //true == is potential guess == not in dictionary
        //false == not potential == is in dictionary

        bool CheckPotentialGuesses(Position position, int result)
        {
            //store all attack results in dictionary
            if (PriorGuesses.ContainsKey(position))
            {
                return false;
            }

            PriorGuesses.Add(position, result);
            return true;
        }

//
//
        /// <summary>
        /// Validates whether a position is guessable on the current grid
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>true if valid, false otherwise</returns>
        bool ValidatePosition(Position position)
        {
            if (position.X >= _gridSize || position.Y >= _gridSize) return false;
            else if (position.X < 0 || position.Y < 0) return false;
            else return true;
        }
    }
}