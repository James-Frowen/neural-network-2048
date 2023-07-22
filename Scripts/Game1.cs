using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D WhitePixel;
        private SpriteFont font1;
        private KeyboardState oldstate;
        private Random r = new Random();

        public Puzzle ActivePuzzle;
        private int drawsize = 50;
        private int drawsperation = 2;
        private List<Brain> Brains;
        private int BrainsPerGen = 1000;
        private int PuzzlesPerBrain = 1;

        private int brainNumber = 0;
        private int puzzleNumber = 0;
        private int generation = 0;
        private bool nextgen = false;
        private double MaxFit;
        private double AvgFit;
        private double MinFit;
        private double BrainTickSpeed = 1;
        private double tickCounter = 0;
        private bool Paused = false;
        private bool WholeGeneration = true;
        private bool Do1Gen = false;
        private StreamWriter output;

        private Stopwatch timer = new Stopwatch();
        private int ticksCount;
        private long totalMs;
        private long totalMainThreadMs;
        private BrainThread[] threads;
        private int winnerCount;

        private void DoBrainTick()
        {
            timer.Restart();

            var B = Brains[brainNumber];

            B.CalculateMove(ActivePuzzle);
            B.MakeMove(ActivePuzzle);

            if (ActivePuzzle.hasWon)
            {
                // have perfect brain
                B.Fitness += ActivePuzzle.Score * 2;
                winnerCount++;
            }
            else if (ActivePuzzle.hasLost)
            {
                B.Fitness += ActivePuzzle.Score;
                ActivePuzzle = new Puzzle();

                puzzleNumber++;
                if (puzzleNumber >= PuzzlesPerBrain)
                {
                    puzzleNumber = 0;

                    brainNumber++;
                    if (brainNumber >= Brains.Count)
                    {
                        brainNumber = 0;

                        CalculateGenFitness();
                        GetNextGenCrossover();
                        generation++;
                        nextgen = true;
                    }
                }
            }

            timer.Stop();
            ticksCount++;
            totalMs += timer.ElapsedMilliseconds;
        }


        private void DoGenerationInParallel()
        {
            const int threadCount = 4;
            if (threads == null)
            {
                threads = new BrainThread[threadCount];
                for (var i = 0; i < threadCount; i++)
                    threads[i] = new BrainThread();
            }

            var stopwatch = Stopwatch.StartNew();
            for (var i = 0; i < threadCount; i++)
            {
                threads[i].ticksCount = 0;
                threads[i].totalMs = 0;
            }

            var countPerThread = Brains.Count / threadCount;
            for (var i = 0; i < threadCount; i++)
                threads[i].RunTask(Brains.Skip(i * countPerThread).Take(countPerThread), PuzzlesPerBrain);


            for (var i = 0; i < threadCount; i++)
            {
                while (threads[i].Running)
                {
                    Thread.Sleep(0);
                }
            }
            stopwatch.Stop();

            for (var i = 0; i < threadCount; i++)
            {
                ticksCount += threads[i].ticksCount;
                totalMs += threads[i].totalMs;

                winnerCount += threads[i].Winners.Count;
            }

            CalculateGenFitness();
            GetNextGenCrossover();
            generation++;
            nextgen = true;

            totalMainThreadMs = stopwatch.ElapsedMilliseconds;
        }

        private void CalculateGenFitness()
        {
            var byfitness = Brains.OrderByDescending(x => x.Fitness).ToList();

            MaxFit = byfitness[0].Fitness;
            MinFit = byfitness[byfitness.Count - 1].Fitness;
            AvgFit = 0;
            //average fitness
            for (var n = 0; n < byfitness.Count; n++)
            {
                AvgFit += byfitness[n].Fitness;
            }
            AvgFit /= byfitness.Count;
            MaxFit /= PuzzlesPerBrain;
            AvgFit /= PuzzlesPerBrain;
            MinFit /= PuzzlesPerBrain;
            output.WriteLine("{0},{1},{2},{3}", generation, MaxFit, AvgFit, MinFit);
        }

        private void GetNextGen()
        {
            var byfitness = Brains.OrderByDescending(x => x.Fitness).ToList();

            Brains = new List<Brain>();
            var skiped = 0;
            for (var n = 0; n < byfitness.Count / 2; n++)
            {
                // chance to skip
                // n/count max chance of skiping (0% for first brain 50% for last brain
                // if chance is > random then skip
                var skip = (n / byfitness.Count) > r.NextDouble();

                if (skip)
                {
                    skiped++;
                }
                else
                {
                    Brains.Add(byfitness[n + skiped].Evolve());
                    Brains.Add(byfitness[n + skiped].Evolve());
                }
            }
        }

        private void GetNextGenCrossover()
        {
            var byfitness = Brains.OrderByDescending(x => x.Fitness).ToArray();

            var newCount = Math.Max(byfitness.Length * 0.90f, 100);
            //if (newCount <= 100)
            //    PuzzlesPerBrain = 4;
            //else if (newCount <= 250)
            //    PuzzlesPerBrain = 3;
            //else if (newCount <= 1000)
            //    PuzzlesPerBrain = 2;
            //else if (newCount <= 2000)
            //    PuzzlesPerBrain = 1;
            //else
            //    PuzzlesPerBrain = 1;


            Brains.Clear();
            //10% elite continue to next generation
            for (var n = 0; n < newCount * .1f; n++)
            {
                Brains.Add(byfitness[n]);
            }

            // next 30% will mutate
            for (var n = (int)(newCount * .1f); n < newCount * .4f; n++)
            {
                Brains.Add(byfitness[n].Evolve());
            }


            // rest use crossover
            var start = (int)(newCount * .4f);
            for (var n = start; n < newCount; n += 2) // +2 beccause we create 2 children
            {
                var first = (n - start) / 4;
                var second = ((n - start) / 2) + 1;

                var P = byfitness[first];
                var Q = byfitness[second];
                Brain C1, C2;
                Brain.Mate(P, Q, out C1, out C2);
                Brains.Add(C1);
                Brains.Add(C2);
            }

            // makes sure all fitness are reset
            for (var n = 0; n < Brains.Count; n++)
            {
                Brains[n].Fitness = 0;
            }
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            IsFixedTimeStep = false;
            //TargetElapsedTime = TimeSpan.FromSeconds(1.0/50.0);
            MaxElapsedTime = TimeSpan.FromSeconds(10);

            IsMouseVisible = true;

        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Brain.r = new Random();

            Brains = new List<Brain>();
            //create first gen
            //100 brains
            for (var n = 0; n < BrainsPerGen; n++)
            {
                Brains.Add(new Brain());
                Brains[n].Initnew();
            }
            ActivePuzzle = new Puzzle();



            oldstate = Keyboard.GetState();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            WhitePixel = new Texture2D(GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            WhitePixel.SetData(new Color[] { Color.White });
            font1 = Content.Load<SpriteFont>("Arial16");

            output = new StreamWriter("output.csv");
            output.WriteLine("{0},{1},{2},{3}", "Generation", "MaxFit", "AvgFit", "MinFit");

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
            output.Close();

            if (threads != null)
            {
                foreach (var thread in threads)
                    thread.Dispose();
                threads = null;
            }
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            var state = Keyboard.GetState();

            #region human player 
            //if (state.IsKeyDown(Keys.W) && oldstate.IsKeyUp(Keys.W))
            //{
            //    ActivePuzzle.Move(Puzzle.MoveDirection.up);
            //}
            //else if (state.IsKeyDown(Keys.S) && oldstate.IsKeyUp(Keys.S))
            //{
            //    ActivePuzzle.Move(Puzzle.MoveDirection.down);
            //}
            //else if (state.IsKeyDown(Keys.A) && oldstate.IsKeyUp(Keys.A))
            //{
            //    ActivePuzzle.Move(Puzzle.MoveDirection.left);
            //}
            //else if (state.IsKeyDown(Keys.D) && oldstate.IsKeyUp(Keys.D))
            //{
            //    ActivePuzzle.Move(Puzzle.MoveDirection.right);
            //}
            #endregion

            if (state.IsKeyDown(Keys.Q) && oldstate.IsKeyUp(Keys.Q))
            {
                BrainTickSpeed *= 1.2;
            }
            if (state.IsKeyDown(Keys.E) && oldstate.IsKeyUp(Keys.E))
            {
                BrainTickSpeed /= 1.2;
            }
            if (state.IsKeyDown(Keys.Space) && oldstate.IsKeyUp(Keys.Space))
            {
                Paused = !Paused;
            }
            if (state.IsKeyDown(Keys.Enter) && oldstate.IsKeyUp(Keys.Enter))
            {
                WholeGeneration = !WholeGeneration;
            }
            if (state.IsKeyDown(Keys.LeftShift) && oldstate.IsKeyUp(Keys.LeftShift))
            {
                Do1Gen = true;
                WholeGeneration = true;
                Paused = false;
            }




            if (!Paused)
            {
                ticksCount = 0;
                totalMs = 0;
                if (WholeGeneration)
                {
                    nextgen = false;

                    DoGenerationInParallel();
                    //while (!nextgen)
                    //{
                    //    DoBrainTick();
                    //}

                    if (Do1Gen)
                    {
                        WholeGeneration = false;
                        Do1Gen = false;
                        Paused = true;
                    }
                }
                else
                {
                    tickCounter += BrainTickSpeed;
                    while (tickCounter > 1)
                    {
                        tickCounter--;
                        DoBrainTick();
                    }
                }

            }


            if (generation % 1000 == 0)
            {
                Paused = true;
            }

            oldstate = state;
            base.Update(gameTime);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();

            Puzzle P;
            if ((P = ActivePuzzle) != null)
            {
                for (var x = 0; x < P.Width; x++)
                {
                    for (var y = 0; y < P.Height; y++)
                    {
                        var v = P.Grid[x, y] == 0 ? 0 : Math.Log(P.Grid[x, y], 2);
                        var m = Math.Log(P.HighestOnGrid(), 2);//Math.Log(P.Max, 2);
                        var a = Convert.ToInt32(255 * v / m);
                        if (a != 0)
                        {
                            spriteBatch.Draw(WhitePixel, new Rectangle(x * (drawsize + drawsperation), y * (drawsize + drawsperation), drawsize, drawsize), new Color(a, a, a));
                            var b = a < 120 ? 255 : 0;
                            spriteBatch.DrawString(font1, P.Grid[x, y].ToString(), new Vector2(x * (drawsize + drawsperation), y * (drawsize + drawsperation)), new Color(b, b, b));
                        }
                    }
                }

                var infoPos = new Vector2((P.Width + 1) * (drawsize + drawsperation), 0);
                DrawTextGroup(ref infoPos, "Score", P.Score.ToString());
                DrawTextGroup(ref infoPos, "Brain number", (brainNumber + 1).ToString());
                DrawTextGroup(ref infoPos, "Generation", generation.ToString());
                DrawTextGroup(ref infoPos, "Max Fitness", MaxFit.ToString("0"));
                DrawTextGroup(ref infoPos, "Avg Fitness", AvgFit.ToString("0"));
                DrawTextGroup(ref infoPos, "Min Fitness", MinFit.ToString("0"));
                DrawTextGroup(ref infoPos, "Brain Count", Brains.Count.ToString());
                DrawTextGroup(ref infoPos, "Winners", winnerCount.ToString());

                //spriteBatch.DrawString(font1, "Last move: " + P.LastMove.ToString(), new Vector2((P.Width + 1) * (drawsize + drawsperation), 200), Color.Black);

                //spriteBatch.DrawString(font1, Brains[brainNumber].Mutability.ToString("0.00"), new Vector2((P.Width * 2 +3) * (drawsize + drawsperation), 250), Color.Black);


                var settingsPos = new Vector2(4, (P.Height + 1) * (drawsize + drawsperation));
                DrawTextGroup(ref settingsPos, "Speed", $"{BrainTickSpeed:0.00}");
                DrawTextGroup(ref settingsPos, "Paused", Paused);
                DrawTextGroup(ref settingsPos, "Do1Gen", Do1Gen);
                DrawTextGroup(ref settingsPos, "WholeGeneration", WholeGeneration);


                var timerPos = settingsPos + new Vector2(0, 20);
                DrawTextGroup(ref timerPos, "Tick average", $"{(double)totalMs / ticksCount * 1000:0.00}us");
                DrawTextGroup(ref timerPos, "Tick count", ticksCount);
                DrawTextGroup(ref timerPos, "Tick total", $"{totalMs}ms");
                DrawTextGroup(ref timerPos, "Main total", $"{totalMainThreadMs}ms");

                //spriteBatch.Draw(WhitePixel, new Rectangle(500, 50, 50, 50), gameTime.IsRunningSlowly ? Color.Red : Color.Green);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawTextGroup(ref Vector2 pos, string label, object value)
        {
            const float LabelSize = 200;

            spriteBatch.DrawString(font1, label, pos, Color.Black);
            spriteBatch.DrawString(font1, value.ToString(), pos + new Vector2(LabelSize, 0), Color.Black);
            pos.Y += 24;
        }
    }
}
