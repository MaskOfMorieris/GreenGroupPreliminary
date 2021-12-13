//----------------------------------------***-----------------------------------------|
//             Christopher Toth, Duncan Myers, Carly Bryant, Justin Babin             |
//                            Module 8, Preliminary Group                             |
//                                      12.10.21                                      |
//------------------------------------------------------------------------------------|
//             Group submission for AI player for multiplayer battleship              |
//------------------------------------------------------------------------------------|
//This player currently implements ships randomly, and bases its attack around a list |
//   of cardinal directions generated on a recorded hit.  It will continue to work    |
//  the list until all valid entries are attempted, then return to recorded hits and  |
//                     create another list of cardinal directions                     |
//----------------------------------------***-----------------------------------------|
//  Known logic issue: player under some conditions will fail to select a new attack  |
//    location and repeat, and under some conditions will not successfully leave a    |
//                  cardinal direction list, also repeating entries                   |
//----------------------------------------***-----------------------------------------|

using System;
using System.Collections.Generic;
using System.Linq;
using Module8;
using System.IO;

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
            var x = center.X;
            var y = center.Y;

            CompassList = new List<Position>();
            if (x < 0 || y < 0) return;
            //North
            x = center.X;
            y = center.Y + 1;
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

        //debug
        private List<string> logChoices = new List<string>();
        private int attackLogCount = 0;
        
        public GreenPlayer(string name)
        {
            Name = name;
        }

        public static void OutputLog(GreenPlayer greenPlayer)
        {
            File.WriteAllLines("debugLog.txt", greenPlayer.logChoices);
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

        //Get results from last round, store the position and hit/miss to rule
        //  out potential guesses and/or create a list of hits to compass from if 
        //  our current compass is empty
        void IPlayer.SetAttackResults(List<AttackResult> results)
        {
            //struct to hold possible hit
            DictionableResult hit;

            //LINQ query the results to see if any hit
            var checkedResult = results.Where(result => result.ResultType == AttackResultType.Hit);

            //if query result found anything, take the first one it found
            if (checkedResult.Any())
            {
                hit = new DictionableResult(checkedResult.First());
            }
            else  //otherwise, create non-stored struct
            {
                hit = new DictionableResult(results[0].Position, -1);
            }

            //This should only add the hit to recent attacks the first time,
            //  based on whether the DictionableStruct contains a valid hit
            //  int conversion or -1
            //  Not -1 result type as these are only reported attacks
            if (CheckPotentialGuesses(hit.Position, hit.Result))
            {
                //First time, store on hits so we can check compass if empty
                RecentAttacks.Add(hit);
            }
        }
        

        //Sequence: List > 1, List == 1, List == 0, Hit has possible compass, guess
        //
        //Loops through the above sequence until an acceptable return occurs
        //  while there is a list of cardinal direction, check until all valid
        //  cardinal directions have been hit, will adjust for changes from attacks
        //  after the list is made
        //If no list, check the stored attack hits using the compass check, and if any
        //  stored hit has a valid cardinal direction guess, store all valid cardinal direction
        //  to the compass list and stick to it until it's no longer valid
        //If the list is empty and there are no stored hits, use the base random guessing logic
        public Position GetAttackPosition()
        {
            //*** whenever we get to this function, iterate count to add that to log so we can
            //  see how many choices it made in a single loop
            attackLogCount++;
            
            while (true)
            {
                //if compass is empty, check for hits, and make a compass on them
                //  if any valid compass direction, use that as guess
                if (comPos.CompassList.Count == 0)
                {
                    
                    //state entered empty compass list
                    logChoices.Add(attackLogCount + ". Decided Compass List was empty.");
                    
                    //This is the last case scenario, guess random
                    if (RecentAttacks.Count == 0)
                    {
                        var randPos = Guesses[rand.Next(0, Guesses.Count)];
                        Guesses.Remove(randPos);
                        
                        //state returned random guess position
                        logChoices.Add(attackLogCount + ". Chose->Guess (" + randPos.X  + ", " + randPos.Y + ")");
                        return randPos;
                        return randPos; //assume these are always valid
                    }

                    //Go through the hits and find out if there are any viable compass directions
                    //  around them, return that position if true, and then the compass list is
                    //  repopulated
                    foreach (var hit in RecentAttacks)
                    {
                        //state checking hits
                        logChoices.Add(attackLogCount + ". Decided there were hits to check.");
                        
                        
                        //populate compass list
                        comPos = new CompassPositions(hit.Position);
                        //remove this hit from the list whether valid is found or not
                        RecentAttacks.Remove(hit);

                        //check for viable position, return the first one, as later call of this
                        //  function for next attack will check the remaining list positions
                        foreach (var compassPos in comPos.CompassList.Where(compassPos => CheckPotentialGuesses(compassPos, -1)))
                        {
                            Guesses.Remove(compassPos);
                            
                            //state returned hitCompass
                            logChoices.Add(attackLogCount + ". Chose->Hit.CompassList (" + compassPos.X  + ", " + compassPos.Y + ")");
                            
                            return compassPos;
                            if (ValidatePosition(compassPos)) return compassPos; //only return if valid position on grid
                        }
                    }
                }

                //1 thing on compass, make sure it hasn't been used, return if true, will go to next 
                //after clearing the list if false
                if (comPos.CompassList.Count == 1)
                {
                    //state 1 on compass list
                    logChoices.Add(attackLogCount + ". Decided Compass List had 1 item on it.");
                    
                    
                    //use -1 struct as a random guess shouldn't have a result yet
                    //  but we still want to rule it out before firing if it does
                    if (CheckPotentialGuesses(comPos.CompassList[0], -1))
                    {
                        var compassPos = comPos.CompassList[0];
                        Guesses.Remove(compassPos);
                        if (ValidatePosition(compassPos)) return compassPos; //again, only if valid
                    }

                    //state cleared compass list
                    logChoices.Add(attackLogCount + ". Decided to clear compass list.");
                    
                    //clear list because only entry on it is invalid, won't reach if valid exists
                    comPos.CompassList.Clear();
                }

                //otherwise, go through compassList, check the cardinal directions
                //  for validity and if valid, select as attack.  Remove all
                //  entries whether valid or not, as cleared list is condition to 
                //  checking prior hits/random guess
                while (true)
                {
                    //state entered compass list loop
                    logChoices.Add(attackLogCount + ". Decided to check items on compass list.");
                    
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

                    //state removed compass list element
                    logChoices.Add(attackLogCount + ". Decided to remove this item from the compass list.");
                    
                    //if not, remove it and try again
                    comPos.CompassList.Remove(comPos.CompassList[randIndex]);

                    //If no valid cardinal directions on list, break out of loop to
                    //  run through base loop again and get to hits/random conditions
                    if (comPos.CompassList.Count == 0)
                    {
                        //state the compass list emptied from loop removal
                        logChoices.Add(attackLogCount + ". Decided the compass list was empty after looped removal and restarted outer loop.");
                        
                        break;
                    }
                }
            }
        }

        //True: position is valid for guess selection because it's not in the
        //  dictionary
        //False: position is not valid for guess selection because it is in
        //  the dictionary
        //      USAGE->Result: if int 0 or greater, this is a known attack which can be 
        //          recorded in the dictionary, if a -int, this is a check only, 
        //          and the position will not be recorded in the dictionary
        
        //When not in dictionary, uses 'int result' to determine whether
        //  a Dictionary entry should be recorded, which create a flow
        //  control in attack results being integers >= 0, and calls 
        //  for unknown result check are passed with -int
        bool CheckPotentialGuesses(Position position, int result)
        {
            //Present in dictionary, invalid for attack
            if (PriorGuesses.ContainsKey(position))
            {
                return false;
            }

            //not in dictionary, valid for attack, record or not
            //  based on int arg
            if (result >= 0)
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