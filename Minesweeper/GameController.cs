using Microsoft.SqlServer.Server;
using Minesweeper.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Minesweeper
{
    class GameController
    {
        public bool IsActive { get; set; }
        public Mine[,] Mines { get; set; }
        public int MinesCount { get; set; }
        int cellsLeft;
        
        public delegate void End(bool isSuccess, int time);
        public event End Ending;
        Label minesLeft, timeLabel;
        int time;
        int Time { get => time; set => SetTime(value); }
        int flagsCount;
        int FlagsCount { get => flagsCount;  set => SetFlags(value); }
        Timer timer;

        public GameController(int y, int x, int minesCount, int buttonSize, Control control, Label minesLeft, Label timeLabel, Timer timer)
        {
            MinesCount = minesCount;
            cellsLeft = x * y - minesCount;
            this.minesLeft = minesLeft;
            this.timeLabel = timeLabel;
            FlagsCount = 0;
            Mines = new Mine[y, x];
            this.timer = timer;
            timer.Tick += (sender, e) => Time++;
            for(int i = 0; i < y; i++)
            {
                for(int j = 0; j < x; j++)
                {
                    Mine mine = new Mine(i, j, buttonSize, control);
                    Mines[i, j] = mine;
                    mine.MineDisarmed += MineOpened;              
                    mine.Exploded += Explosion;
                    Ending += mine.EndGame;
                    mine.CellOpened += CellOpened;
                    mine.FlagSet += (modifier) => FlagsCount += modifier;
                }
            }
        }

        void Explosion(int y, int x)
        {
            IsActive = false;
            timer.Enabled = false;
            Ending?.Invoke(false, Time);
        }

        void MineOpened(int posY, int posX)
        {

            if(!IsActive)
            {
                IsActive = true;
                NewGame(posY, posX);
                timer.Enabled = true;
            }
            cellsLeft--;
            if(Mines[posY, posX].MinesAround == 0)
            {
                for(int y = Math.Max(0, posY - 1); y < Math.Min(Mines.GetLength(0), posY + 2); y++)
                {
                    for(int x = Math.Max(0, posX - 1); x < Math.Min(Mines.GetLength(1), posX + 2); x++)
                    {
                        if(y == posY && x == posX)
                        {
                            continue;
                        }
                        if(!Mines[y, x].IsDisarmed && !Mines[y, x].HasFlag)
                        {
                            Mines[y, x].Disarm();
                        }

                    }
                }
            }
            if(cellsLeft == 0 && IsActive)
            {
                timer.Enabled = false;
                IsActive = false;
                FlagsCount = MinesCount;
                Ending?.Invoke(true, Time);
            }
        }

        void CellOpened(int posY, int posX)
        {
            int flags = CountFlags(posY, posX);
            if(flags == Mines[posY, posX].MinesAround)
            {
                for(int y = Math.Max(0, posY - 1); y < Math.Min(Mines.GetLength(0), posY + 2); y++)
                {
                    for(int x = Math.Max(0, posX - 1); x < Math.Min(Mines.GetLength(1), posX + 2); x++)
                    {
                        if(y == posY && x == posX)
                        {
                            continue;
                        }
                        if(!Mines[y, x].HasFlag && !Mines[y, x].IsDisarmed)
                        {
                            Mines[y, x].Disarm();
                        }
                    }
                }
            }
        }
        int CountFlags(int posY, int posX)
        {
            int count = 0;
            for(int y = Math.Max(0, posY - 1); y < Math.Min(Mines.GetLength(0), posY + 2); y++)
            {
                for(int x = Math.Max(0, posX - 1); x < Math.Min(Mines.GetLength(1), posX + 2); x++)
                {
                    if(y == posY && x == posX)
                    {
                        continue;
                    }
                    if(Mines[y, x].HasFlag)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        void NewGame(int posY, int posX)
        {
            List<(int, int)> cells = new List<(int, int)>();
            for(int i = 0; i < Mines.GetLength(0); i++)
            {
                for(int j = 0; j < Mines.GetLength(1); j++)
                {
                    if(Math.Abs(posY - i) < 2 && Math.Abs(posX - j) < 2)
                    {
                        continue;
                    }
                    cells.Add((i, j));
                }
            }

            Random r = new Random();
            for(int i = 0; i < cells.Count * 10; i++)
            {
                int a = r.Next(0, cells.Count), b = r.Next(0, cells.Count);
                (cells[a], cells[b]) = (cells[b], cells[a]);
            }
            for(int i = 0; i < MinesCount; i++)
            {
                Mines[cells[i].Item1, cells[i].Item2].IsMine = true;
            }
            for(int i = 0; i < Mines.GetLength(0); i++)
            {
                for(int j = 0; j < Mines.GetLength(1); j++)
                {
                    int count = 0;
                    for(int y = Math.Max(0, i - 1); y < Math.Min(Mines.GetLength(0), i + 2); y++)
                    {
                        for(int x = Math.Max(0, j - 1); x < Math.Min(Mines.GetLength(1), j + 2); x++)
                        {
                            if(i == y && j == x)
                            {
                                continue;
                            }
                            if(Mines[y, x].IsMine)
                            {
                                count++;
                            }
                        }
                    }
                    Mines[i, j].MinesAround = count;
                    
                }
            }
        }
        public void Reset()
        {
            foreach(Mine mine in Mines)
            {
                mine.Reset();
            }
            timer.Enabled = false;
            cellsLeft = Mines.GetLength(0) * Mines.GetLength(1) - MinesCount;
            FlagsCount = 0;
            Time = 0;
        }
        public void SetFlags(int value)
        {
            minesLeft.Text = $"{MinesCount - value}/{MinesCount}";
            flagsCount = value;
        }

        void SetTime(int value)
        {
            timeLabel.Text = string.Format("{0:D2}:{1:D2}", value / 60, value % 60);
            time = value;
        }
    } 
}
