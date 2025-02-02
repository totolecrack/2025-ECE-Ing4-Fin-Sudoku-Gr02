using Sudoku.Shared;
using Google.OrTools.Sat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.ORToolsHybridSolver
{
    public class ORToolsHybridSolver : ISudokuSolver
    {
        // Paramètres optimisés pour la recherche locale
        private const int MaxLocalSearchIterations = 100000;
        private const double InitialTemperature = 100.0;
        private const double CoolingRate = 0.9999;
        private readonly Random _random = new Random();

        public SudokuGrid Solve(SudokuGrid s)
        {
            // Étape 1: Résolution garantie avec OR-Tools
            var initialSolution = SolveWithORTools(s);
            
            // Étape 2: Optimisation avec recherche locale
            var finalSolution = LocalSearch(initialSolution);
            
            // Validation finale
            if (CalculateTotalErrors(finalSolution) != 0)
                throw new InvalidOperationException("Solution invalide après optimisation");
            
            return finalSolution;
        }

        private SudokuGrid SolveWithORTools(SudokuGrid grid)
        {
            var model = new CpModel();
            var variables = new IntVar[9, 9];
            var originalCells = grid.Cells;

            // Création des variables avec contraintes initiales
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    variables[i, j] = originalCells[i, j] == 0
                        ? model.NewIntVar(1, 9, $"cell_{i}_{j}")
                        : model.NewConstant(originalCells[i, j]);
                }
            }

            // Ajout des contraintes Sudoku
            AddSudokuConstraints(model, variables);

            // Résolution avec vérification stricte
            var solver = new CpSolver();
            var status = solver.Solve(model);
            
            if (status != CpSolverStatus.Feasible && status != CpSolverStatus.Optimal)
                throw new InvalidOperationException("Aucune solution trouvée avec OR-Tools");

            return ExtractSolution(grid.CloneSudoku(), solver, variables);
        }

        private void AddSudokuConstraints(CpModel model, IntVar[,] variables)
        {
            // Contraintes pour les lignes et colonnes
            for (int i = 0; i < 9; i++)
            {
                model.AddAllDifferent(GetRow(variables, i));
                model.AddAllDifferent(GetColumn(variables, i));
            }

            // Contraintes pour les blocs 3x3
            for (int i = 0; i < 9; i += 3)
            {
                for (int j = 0; j < 9; j += 3)
                {
                    model.AddAllDifferent(GetBlock(variables, i, j));
                }
            }
        }

        private IEnumerable<IntVar> GetRow(IntVar[,] vars, int row) =>
            Enumerable.Range(0, 9).Select(col => vars[row, col]);

        private IEnumerable<IntVar> GetColumn(IntVar[,] vars, int col) =>
            Enumerable.Range(0, 9).Select(row => vars[row, col]);

        private IEnumerable<IntVar> GetBlock(IntVar[,] vars, int startRow, int startCol)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    yield return vars[startRow + i, startCol + j];
                }
            }
        }

        private SudokuGrid ExtractSolution(SudokuGrid grid, CpSolver solver, IntVar[,] variables)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    grid.Cells[i, j] = (int)solver.Value(variables[i, j]);
                }
            }
            return grid;
        }

        private SudokuGrid LocalSearch(SudokuGrid grid)
        {
            var tempGrid = grid.CloneSudoku();
            double temperature = InitialTemperature;
            int currentErrors = CalculateTotalErrors(tempGrid);

            for (int i = 0; i < MaxLocalSearchIterations && currentErrors > 0; i++)
            {
                var newGrid = PerturbSolution(tempGrid);
                int newErrors = CalculateTotalErrors(newGrid);

                if (AcceptChange(currentErrors, newErrors, temperature))
                {
                    tempGrid = newGrid;
                    currentErrors = newErrors;
                }

                temperature *= CoolingRate;
            }

            return tempGrid;
        }

        private SudokuGrid PerturbSolution(SudokuGrid grid)
        {
            var newGrid = grid.CloneSudoku();
            
            // Échange dans un bloc aléatoire entre cellules non fixes
            var (blockRow, blockCol) = (_random.Next(3) * 3, _random.Next(3) * 3);
            
            var positions = Enumerable.Range(0, 9)
                .Select(n => (blockRow + n / 3, blockCol + n % 3))
                .Where(pos => grid.Cells[pos.Item1, pos.Item2] == 0) // Ne permuter que les cellules non fixes
                .OrderBy(_ => _random.Next())
                .Take(2)
                .ToList();

            if (positions.Count == 2)
            {
                var (r1, c1) = positions[0];
                var (r2, c2) = positions[1];
                (newGrid.Cells[r1, c1], newGrid.Cells[r2, c2]) = 
                    (newGrid.Cells[r2, c2], newGrid.Cells[r1, c1]);
            }

            return newGrid;
        }

        private int CalculateTotalErrors(SudokuGrid grid)
        {
            int errors = 0;
            var checkedValues = new HashSet<int>();

            // Vérification des lignes
            for (int i = 0; i < 9; i++)
            {
                checkedValues.Clear();
                for (int j = 0; j < 9; j++)
                {
                    if (!checkedValues.Add(grid.Cells[i, j]))
                        errors++;
                }
            }

            // Vérification des colonnes
            for (int j = 0; j < 9; j++)
            {
                checkedValues.Clear();
                for (int i = 0; i < 9; i++)
                {
                    if (!checkedValues.Add(grid.Cells[i, j]))
                        errors++;
                }
            }

            // Vérification des blocs
            for (int blockRow = 0; blockRow < 3; blockRow++)
            {
                for (int blockCol = 0; blockCol < 3; blockCol++)
                {
                    checkedValues.Clear();
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int row = blockRow * 3 + i;
                            int col = blockCol * 3 + j;
                            if (!checkedValues.Add(grid.Cells[row, col]))
                                errors++;
                        }
                    }
                }
            }

            return errors;
        }

        private bool AcceptChange(int currentErrors, int newErrors, double temperature)
        {
            if (newErrors < currentErrors) return true;
            var probability = Math.Exp((currentErrors - newErrors) / temperature);
            return _random.NextDouble() < probability;
        }
    }
}
