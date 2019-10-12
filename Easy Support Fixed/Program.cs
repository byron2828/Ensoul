using Easy_Sup.scripts;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy_Sup
{
    class Program
    {
        //⢸⣿⣿⣿⣿⠃⠄⢀⣴⡾⠃⠄⠄⠄⠄⠄⠈⠺⠟⠛⠛⠛⠛⠻⢿⣿⣿⣿⣿⣶⣤⡀⠄
        //⢸⣿⣿⣿⡟⢀⣴⣿⡿⠁⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⠄⣸⣿⣿⣿⣿⣿⣿⣿⣷
        //⢸⣿⣿⠟⣴⣿⡿⡟⡼⢹⣷⢲⡶⣖⣾⣶⢄⠄⠄⠄⠄⠄⢀⣼⣿⢿⣿⣿⣿⣿⣿⣿⣿
        //⢸⣿⢫⣾⣿⡟⣾⡸⢠⡿⢳⡿⠍⣼⣿⢏⣿⣷⢄⡀⠄⢠⣾⢻⣿⣸⣿⣿⣿⣿⣿⣿⣿
        //⡿⣡⣿⣿⡟⡼⡁⠁⣰⠂⡾⠉⢨⣿⠃⣿⡿⠍⣾⣟⢤⣿⢇⣿⢇⣿⣿⢿⣿⣿⣿⣿⣿
        //⣱⣿⣿⡟⡐⣰⣧⡷⣿⣴⣧⣤⣼⣯⢸⡿⠁⣰⠟⢀⣼⠏⣲⠏⢸⣿⡟⣿⣿⣿⣿⣿⣿
        //⣿⣿⡟⠁⠄⠟⣁⠄⢡⣿⣿⣿⣿⣿⣿⣦⣼⢟⢀⡼⠃⡹⠃⡀⢸⡿⢸⣿⣿⣿⣿⣿⡟
        //⣿⣿⠃⠄⢀⣾⠋⠓⢰⣿⣿⣿⣿⣿⣿⠿⣿⣿⣾⣅⢔⣕⡇⡇⡼⢁⣿⣿⣿⣿⣿⣿⢣
        //⣿⡟⠄⠄⣾⣇⠷⣢⣿⣿⣿⣿⣿⣿⣿⣭⣀⡈⠙⢿⣿⣿⡇⡧⢁⣾⣿⣿⣿⣿⣿⢏⣾
        //⣿⡇⠄⣼⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠟⢻⠇⠄⠄⢿⣿⡇⢡⣾⣿⣿⣿⣿⣿⣏⣼⣿
        //⣿⣷⢰⣿⣿⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⢰⣧⣀⡄⢀⠘⡿⣰⣿⣿⣿⣿⣿⣿⠟⣼⣿⣿
        //⢹⣿⢸⣿⣿⠟⠻⢿⣿⣿⣿⣿⣿⣿⣿⣶⣭⣉⣤⣿⢈⣼⣿⣿⣿⣿⣿⣿⠏⣾⣹⣿⣿
        //⢸⠇⡜⣿⡟⠄⠄⠄⠈⠙⣿⣿⣿⣿⣿⣿⣿⣿⠟⣱⣻⣿⣿⣿⣿⣿⠟⠁⢳⠃⣿⣿⣿
        //⠄⣰⡗⠹⣿⣄⠄⠄⠄⢀⣿⣿⣿⣿⣿⣿⠟⣅⣥⣿⣿⣿⣿⠿⠋⠄⠄⣾⡌⢠⣿⡿⠃
        //⠜⠋⢠⣷⢻⣿⣿⣶⣾⣿⣿⣿⣿⠿⣛⣥⣾⣿⠿⠟⠛⠉⠄⠄










        private static Render.Sprite logo;

        public static void Main(string[] args)
        {
 
                    GameEvent.OnGameLoad += OnGameLoad;
            
        }

       private static void OnGameLoad()
{
 	Chat.Print("Supported Champions: Alistar, Blitz, Lux, Morgana, Pyke, Soraka, Thresh");
            Chat.Print("SPrediction Port By Mask");
            if (ObjectManager.Player.CharacterName == "Soraka")
            {
                Soraka.Load();
                Chat.Print("Soraka Script Load");
            }
            else if (ObjectManager.Player.CharacterName == "Blitzcrank")
            {
                Blitz.BlitzOnLoad();
                Chat.Print("Blitzcrank Script Load");
                Chat.Print("This script is a Port of KurisuBlitz (Code of Kurisu)");
            }
            else if (ObjectManager.Player.CharacterName == "Lux")
            {
                Lux.Load();
                Chat.Print("Partial Port of ChewyMoon Lux Load");
            }
            else if (ObjectManager.Player.CharacterName == "Pyke")
            {
                Pyke.On_Load();
                Chat.Print("011110001.Pyke Load");
            }
            else if (ObjectManager.Player.CharacterName == "Thresh")
            {
                Thresh.OnLoad();
                Chat.Print("011110001.Thresh Load");
            }
            else if (ObjectManager.Player.CharacterName == "Alistar")
            {
                Alistar.OnLoad();
            }
            else if(ObjectManager.Player.CharacterName == "Morgana")
            {
                Morgana.OnLoad();
                Chat.Print("Morgana Script Load");
                Chat.Print("This script is Fixed By Memory");
                Notifications.Add(new Notification("Easy Support Fixed By Memory", "Credit  Memory"));
            }
        }
}
}
      
