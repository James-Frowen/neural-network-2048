using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace NeuralNetwork2048_v2
{
    public class BrainThread : IDisposable
    {
        private Stopwatch timer = new Stopwatch();
        public int ticksCount;
        public long totalMs;
        private bool stop;

        private Puzzle puzzle;
        private List<Brain> Brains = new List<Brain>();
        private int PuzzlesPerBrain = 1;
        public bool Running { get; private set; }

        public List<Brain> Winners = new List<Brain>();

        public BrainThread()
        {
            var thread = new Thread(Loop);
            thread.Start();
        }

        public void RunTask(IEnumerable<Brain> brains, int puzzlesPerBrain = 1) => RunTask(brains.ToList(), puzzlesPerBrain);
        public void RunTask(List<Brain> brains, int puzzlesPerBrain = 1)
        {
            Brains = brains;
            PuzzlesPerBrain = puzzlesPerBrain;
            Winners.Clear();
            Running = true;
        }

        public void Dispose()
        {
            stop = true;
        }
        private void Loop()
        {
            while (!stop)
            {
                if (Running)
                {
                    foreach (var brain in Brains)
                    {
                        RunBrain(brain);
                    }
                    Running = false;
                }

                // wait for next task
                Thread.Sleep(1);
            }
        }

        private void RunBrain(Brain B)
        {
            var puzzleNumber = 1;
            var playing = true;
            while (playing)
            {
                timer.Restart();

                puzzle ??= new Puzzle();

                B.CalculateMove(puzzle);
                B.MakeMove(puzzle);

                if (puzzle.hasWon)
                {
                    // have perfect brain
                    Winners.Add(B);
                    B.Fitness += puzzle.Score * 2;
                    playing = false;
                }
                if (puzzle.hasLost)
                {
                    B.Fitness += puzzle.Score;
                    puzzle = new Puzzle();

                    puzzleNumber++;
                    if (puzzleNumber >= PuzzlesPerBrain)
                    {
                        puzzleNumber = 0;
                        playing = false;
                    }
                }

                timer.Stop();
                ticksCount++;
                totalMs += timer.ElapsedMilliseconds;
            }
        }
    }
}
