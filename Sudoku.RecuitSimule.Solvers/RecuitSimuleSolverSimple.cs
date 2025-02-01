using System;
using System.Collections.Generic;
using System.Linq;
using Sudoku.Shared;

namespace Sudoku.RecuitSimule.Solvers
{
    public class RecuitSimuleSolverSimple : ISudokuSolver
    {
        private static Random random = new Random();

        public SudokuGrid Solve(SudokuGrid s)
        {
            int[,] grid = (int[,])s.Cells.Clone();
            SolveWithSimulatedAnnealing(grid);
            return new SudokuGrid { Cells = grid };
        }

        private void SolveWithSimulatedAnnealing(int[,] grid)
        {
            double temperature = 1.0;
            double coolingRate = 0.995;
            int iterations = 100000;

            int[,] currentSolution = InitializeSolution(grid);
            int[,] bestSolution = (int[,])currentSolution.Clone();
            int currentEnergy = ComputeEnergy(currentSolution);
            int bestEnergy = currentEnergy;

            for (int i = 0; i < iterations; i++)
            {
                int[,] newSolution = MakeSmallChange(currentSolution);
                int newEnergy = ComputeEnergy(newSolution);

                if (newEnergy < bestEnergy || AcceptWorseSolution(currentEnergy, newEnergy, temperature))
                {
                    currentSolution = newSolution;
                    currentEnergy = newEnergy;
                    if (currentEnergy < bestEnergy)
                    {
                        bestSolution = (int[,])currentSolution.Clone();
                        bestEnergy = currentEnergy;
                    }
                }

                temperature *= coolingRate;
                if (bestEnergy == 0) break;
            }

            Array.Copy(bestSolution, grid, grid.Length);
        }

        private int[,] InitializeSolution(int[,] grid)
        {
            int[,] solution = (int[,])grid.Clone();
            for (int i = 0; i < 9; i++)
            {
                HashSet<int> missingNumbers = new HashSet<int>(Enumerable.Range(1, 9));
                for (int j = 0; j < 9; j++)
                {
                    if (solution[i, j] != 0)
                        missingNumbers.Remove(solution[i, j]);
                }
                foreach (int j in Enumerable.Range(0, 9).Where(j => solution[i, j] == 0))
                {
                    if (missingNumbers.Count > 0)
                    {
                        int number = missingNumbers.First();
                        solution[i, j] = number;
                        missingNumbers.Remove(number);
                    }
                }
            }
            return solution;
        }

        private int ComputeEnergy(int[,] grid)
        {
            int conflicts = 0;
            for (int i = 0; i < 9; i++)
            {
                conflicts += CountConflicts(grid, i, true);
                conflicts += CountConflicts(grid, i, false);
            }
            return conflicts;
        }

        private int CountConflicts(int[,] grid, int index, bool isRow)
        {
            HashSet<int> seen = new HashSet<int>();
            int conflicts = 0;
            for (int j = 0; j < 9; j++)
            {
                int num = isRow ? grid[index, j] : grid[j, index];
                if (seen.Contains(num)) conflicts++;
                seen.Add(num);
            }
            return conflicts;
        }

        private int[,] MakeSmallChange(int[,] grid)
        {
            int[,] newGrid = (int[,])grid.Clone();
            int row = random.Next(0, 9);
            int col1 = random.Next(0, 9);
            int col2 = random.Next(0, 9);
            (newGrid[row, col1], newGrid[row, col2]) = (newGrid[row, col2], newGrid[row, col1]);
            return newGrid;
        }

        private bool AcceptWorseSolution(int currentEnergy, int newEnergy, double temperature)
        {
            if (newEnergy < currentEnergy) return true;
            double probability = Math.Exp((currentEnergy - newEnergy) / temperature);
            return random.NextDouble() < probability;
        }
    }
}


