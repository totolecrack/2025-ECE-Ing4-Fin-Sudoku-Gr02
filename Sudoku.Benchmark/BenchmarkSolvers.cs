﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.CoreRun;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using BenchmarkDotNet.Toolchains.Roslyn;
using Sudoku.Shared;



namespace Sudoku.Benchmark
{

    public class QuickBenchmarkSolversHard : QuickBenchmarkSolversEasy
    {
        public QuickBenchmarkSolversHard() : base()
		{
            NbPuzzles = 10;
            MaxSolverDuration = TimeSpan.FromSeconds(30);
			Difficulty = SudokuDifficulty.Hard;
        }
    }


    public class QuickBenchmarkSolversMedium : QuickBenchmarkSolversEasy
    {
        public QuickBenchmarkSolversMedium() : base()
		{
            NbPuzzles = 10;
            MaxSolverDuration = TimeSpan.FromSeconds(20);
			Difficulty = SudokuDifficulty.Medium;
        }
    }


    [Config(typeof(Config))]
    public class QuickBenchmarkSolversEasy : BenchmarkSolversBase
    {
        public QuickBenchmarkSolversEasy() : base()
		{
            MaxSolverDuration = TimeSpan.FromSeconds(10);
            NbPuzzles = 2;
        }
		private class Config : SudokuBenchmarkConfigBase
        {
			public Config(): base()
            {
				var baseJob = GetBaseJob()
                    .WithIterationCount(1)
					.WithInvocationCount(1);
				this.AddJob(baseJob);
            }
        }

        public override SudokuDifficulty Difficulty { get; set; } = SudokuDifficulty.Easy;

    }


    [Config(typeof(Config))]
    public class CompleteBenchmarkSolvers : BenchmarkSolversBase
    {

        public CompleteBenchmarkSolvers(): base()
        {
            MaxSolverDuration = TimeSpan.FromMinutes(1);
        }

		private class Config : SudokuBenchmarkConfigBase
		{
			public Config() : base()
			{
				var baseJob = GetBaseJob();
				this.AddJob(baseJob);
			}
		}

		[ParamsAllValues]
		public override SudokuDifficulty Difficulty { get; set; }


	}


	public abstract class SudokuBenchmarkConfigBase : ManualConfig
	{

		public SudokuBenchmarkConfigBase()
        {
			if (Program.IsDebug)
            {
                Options |= ConfigOptions.DisableOptimizationsValidator;
			}
				this.AddColumnProvider(DefaultColumnProviders.Instance);
				this.AddColumn(new RankColumn(NumeralSystem.Arabic));
			
			this.AddLogger(ConsoleLogger.Default);
			//this.AddExporter(new CsvExporter(CsvSeparator.Comma, SummaryStyle.Default));
			this.UnionRule = ConfigUnionRule.AlwaysUseLocal;

		}

		public static Job GetBaseJob()
        {
            var baseJob = Job.Dry
                    .WithId("Solving Sudokus")
                    .WithPlatform(Platform.X64)
                    .WithJit(Jit.RyuJit)
                    .WithRuntime(CoreRuntime.Core80)
                    .WithLaunchCount(1) // Réduit le nombre de lancements
                    .WithWarmupCount(0) // Supprime le warmup
                    .WithIterationCount(1) // Réduit le nombre d'itérations
                    .WithInvocationCount(1) // Exécute chaque benchmark une seule fois
                    .WithUnrollFactor(1)
                    .WithToolchain(InProcessEmitToolchain.Instance);
            return baseJob;
        }


    }





    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MinColumn, MaxColumn, MeanColumn, MedianColumn]
    public abstract class BenchmarkSolversBase
    {

        static BenchmarkSolversBase()
        {
			
            _Solvers = new[] { new EmptySolver() }.Concat(Shared.SudokuGrid.GetSolvers().Select(s =>
            {
	            try
	            {
		            return s.Value.Value;
				}
	            catch (Exception e)
	            {
		            Console.WriteLine(e);
		            return new EmptySolver();
	            }
	            
            }).Where(s => s.GetType() != typeof(EmptySolver))).Select(s => new SolverPresenter() { Solver = s }).ToList();
			//_Solvers = SudokuGrid.GetSolvers().Where(s => ! s.Value.Value.GetType().Name.ToLowerInvariant().StartsWith("pso")).Select(s => new SolverPresenter() { Solver = s.Value.Value }).ToList();
			
        }


        [GlobalSetup]
        public virtual void GlobalSetup()
        {
            AllPuzzles = new Dictionary<SudokuDifficulty, IList<Shared.SudokuGrid>>();
            foreach (var difficulty in Enum.GetValues(typeof(SudokuDifficulty)).Cast<SudokuDifficulty>())
            {
                AllPuzzles[difficulty] = SudokuHelper.GetSudokus(Difficulty);
            }

        }

		private static SudokuGrid _WarmupSudoku = SudokuGrid.ReadSudoku("483921657967345001001806400008102900700000008006708200002609500800203009005010300");

        [IterationSetup]
        public void IterationSetup()
        {
            IterationPuzzles = new List<Shared.SudokuGrid>(NbPuzzles);
            for (int i = 0; i < NbPuzzles; i++)
            {
                IterationPuzzles.Add(AllPuzzles[Difficulty][i].CloneSudoku());
            }

			try
			{
				SolverPresenter.SolveWithTimeLimit(_WarmupSudoku, TimeSpan.FromSeconds(10));
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

        }

        private static readonly Stopwatch Clock = Stopwatch.StartNew();

        public TimeSpan MaxSolverDuration;

        public int NbPuzzles { get; set; } = 10;

        public virtual SudokuDifficulty Difficulty { get; set; }

        public IDictionary<SudokuDifficulty, IList<Shared.SudokuGrid>> AllPuzzles { get; set; }
        public IList<Shared.SudokuGrid> IterationPuzzles { get; set; }

        [ParamsSource(nameof(GetSolvers))]
        public SolverPresenter SolverPresenter { get; set; }


		private static IList<SolverPresenter> _Solvers;



        public IEnumerable<SolverPresenter> GetSolvers()
        {
            return _Solvers.Where(s => 
                s.Solver.GetType().Name.Contains("ORToolsSimpleSolvers") ||
                s.Solver.GetType().Name.Contains("BacktrackingDotNetSolver")
            );

        }


        [Benchmark(Description = "Benchmarking GrilleSudoku Solvers")]
        public void Benchmark()
        {
            foreach (var puzzle in IterationPuzzles)
            {
                try
                {
                    Console.WriteLine($"▶️ Starting benchmark for solver: {SolverPresenter}");
                    var startTime = Clock.Elapsed;
                    var solution = SolverPresenter.SolveWithTimeLimit(puzzle, MaxSolverDuration);
                    if (!solution.IsValid(puzzle))
                    {
                        throw new ApplicationException($"sudoku has {solution.NbErrors(puzzle)} errors");
                    }
                    var duration = Clock.Elapsed - startTime;
                    Console.WriteLine($"✅ Finished benchmark for solver: {SolverPresenter} in {duration.TotalMilliseconds} ms");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"❌ Error in solver {SolverPresenter}: {e.Message}");
                    throw;
                }
            }
        }



    }
}