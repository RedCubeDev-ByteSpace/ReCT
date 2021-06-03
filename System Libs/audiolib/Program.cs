using System;
using System.Collections.Generic;
using System.Threading;
using NetCoreAudio;

namespace audio
{
    class Program
    {
        static void Main(string[] args)
        {
            //nothing to see here
        }
    }

    //This is the Official ReCT AUDIO Package -- ©2020 RedCube
    public static class audio
    {
        static List<Player> players = new List<Player>();

        public class AudioPlayer
        {
            private int playerNum;
            
            public AudioPlayer()
            {
                players.Add(new Player());
                playerNum = players.Count - 1;
            }

            public void Play(string path)
            {
                players[playerNum].Play(path);
            }
            
            public void Pause()
            {
                players[playerNum].Pause();
            }
            
            public void Resume()
            {
                players[playerNum].Resume();
            }
            
            public void Stop()
            {
                players[playerNum].Stop();
            }
        }
    }
}