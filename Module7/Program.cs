// Multiplayer Battleship Game with AI - Partial Solution

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module8
{
    class Program
    {
        static void Main(string[] args)
        {
            List<IPlayer> players = new List<IPlayer>();
            players.Add(new DumbPlayer("Dumb 1"));
            players.Add(new DumbPlayer("Dumb 2"));
            players.Add(new DumbPlayer("Dumb 3"));
            players.Add(new RandomPlayer("Random 1"));
            players.Add(new RandomPlayer("Random 2"));
            players.Add(new RandomPlayer("Random 3"));
            players.Add(new RandomPlayer("Random 4"));
            players.Add(new RandomPlayer("Random 5"));

            //Your code here
            players.Add(new CS3110_Module8_Green.GreenPlayer("Green 1"));
            players.Add(new CS3110_Module8_Green.GreenPlayer("Green 2"));
            players.Add(new CS3110_Module8_Green.GreenPlayer("Green 3"));
            players.Add(new CS3110_Module8_Green.GreenPlayer("Green 4"));

            MultiPlayerBattleShip game = new MultiPlayerBattleShip(players);
            game.Play(PlayMode.Pause);
        }
    }
}
