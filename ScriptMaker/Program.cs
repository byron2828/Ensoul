
using EnsoulSharp;
using EnsoulSharp.SDK;
using Script_Maker.Champions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Script_Maker
{
    
     class Program
    {
        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += On_LoadGame;
        }
        private static void On_LoadGame()
        {
           
                ScriptMaker.OnLoad();
                Chat.Print("Welcome to Script Maker :)");
            
        }
    }
}
