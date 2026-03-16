using System;
using System.IO;
using System.Threading;

namespace ConsoleCatchGame
{
    class Program
    {
        static string logFile = "game_log.txt";
        static int w = 30, h = 15;
        static int pX = 15, pY = 14, score = 0;
        static int iX, iY;
        static char iSym;
        static Random rnd = new Random();

        static void Main()
        {
            File.WriteAllText(logFile, "--- OYUN BASLADI ---\n");
            Console.CursorVisible = false;
            
            Spawn();
            DateTime start = DateTime.Now;

            while (score < 5 && (DateTime.Now - start).TotalSeconds < 30)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    Log($"INPUT → key={key} playerX={pX} playerY={pY}");
                    int oldX = pX;
                    
                    if (key == ConsoleKey.LeftArrow && pX > 0) pX--;
                    if (key == ConsoleKey.RightArrow && pX < w - 1) pX++;
                    
                    if (oldX != pX) Log($"PLAYER_MOVE → oldX={oldX} newX={pX}");
                }

                iY++;
                Log($"OBJECT_MOVE → symbol={iSym} x={iX} y={iY}");
                Log($"COLLISION_CHECK → playerX={pX} itemX={iX} itemY={iY}");

                if (pX == iX && pY == iY)
                {
                    score++;
                    Log($"COLLISION → status=Hit score={score}");
                    Spawn();
                }
                else if (iY >= h)
                {
                    Spawn();
                }

                Console.Clear();
                Console.SetCursorPosition(iX, iY);
                Console.Write(iSym);
                Console.SetCursorPosition(pX, pY);
                Console.Write('@');
                Console.SetCursorPosition(0, 0);
                Console.Write($"Skor: {score}/5");

                Thread.Sleep(150);
            }

            Log($"GAMEOVER → final_score={score}");
            Console.Clear();
            Console.WriteLine($"Oyun Bitti! Skorunuz: {score}\nLoglar: {logFile}");
            Console.ReadLine();
        }

        static void Spawn()
        {
            iX = rnd.Next(0, w);
            iY = 0;
            iSym = rnd.Next(2) == 0 ? '*' : 'O';
            Log($"UPDATE → itemSpawned symbol={iSym} x={iX} y={iY}");
        }

        static void Log(string msg)
        {
            File.AppendAllText(logFile, msg + "\n");
        }
    }
}