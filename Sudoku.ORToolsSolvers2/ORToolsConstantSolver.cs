using Google.OrTools.Sat;
using Sudoku.Shared;
using System.Collections.Generic;
using System.Linq;

namespace Sudoku.ORToolsSolvers
{
    public class ORToolsConstantSolver : ISudokuSolver
    {
        public SudokuGrid Solve(SudokuGrid s)
        {
            // 1. Modèle CP-SAT
            CpModel model = new CpModel();
            
            // 2. Création des variables avec constantes pour les valeurs fixes
            IntVar[,] cells = CreateVariablesWithConstants(model, s);

            // 3. Ajout des contraintes AllDifferent optimisées
            AddAllDifferentConstraints(model, cells);

            // 4. Résolution
            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);

            // 5. Extraction et validation
            if (status == CpSolverStatus.Feasible || status == CpSolverStatus.Optimal)
            {
                ExtractSolution(s, cells, solver);
                return s;
            }

            throw new InvalidOperationException("Aucune solution trouvée");
        }

        // Crée les variables en utilisant des constantes pour les cellules pré-remplies
        private IntVar[,] CreateVariablesWithConstants(CpModel model, SudokuGrid grid)
        {
            IntVar[,] cells = new IntVar[9, 9];
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    cells[i, j] = grid.Cells[i, j] == 0 
                        ? model.NewIntVar(1, 9, $"cell_{i}_{j}") 
                        : model.NewConstant(grid.Cells[i, j]);
                }
            }
            return cells;
        }

        // Ajoute toutes les contraintes AllDifferent en une seule passe
        private void AddAllDifferentConstraints(CpModel model, IntVar[,] cells)
        {
            // Contraintes pour les lignes et colonnes
            for (int i = 0; i < 9; i++)
            {
                model.AddAllDifferent(GetRow(cells, i));
                model.AddAllDifferent(GetColumn(cells, i));
            }

            // Contraintes pour les blocs 3x3 (méthode optimisée)
            for (int blockRow = 0; blockRow < 3; blockRow++)
            {
                for (int blockCol = 0; blockCol < 3; blockCol++)
                {
                    model.AddAllDifferent(GetBlock(cells, blockRow * 3, blockCol * 3));
                }
            }
        }

        // Extrait une ligne
        private IEnumerable<IntVar> GetRow(IntVar[,] cells, int row) 
            => Enumerable.Range(0, 9).Select(col => cells[row, col]);

        // Extrait une colonne
        private IEnumerable<IntVar> GetColumn(IntVar[,] cells, int col) 
            => Enumerable.Range(0, 9).Select(row => cells[row, col]);

        // Extrait un bloc 3x3
        private IEnumerable<IntVar> GetBlock(IntVar[,] cells, int startRow, int startCol)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    yield return cells[startRow + i, startCol + j];
                }
            }
        }

        // Extrait la solution finale
        private void ExtractSolution(SudokuGrid grid, IntVar[,] cells, CpSolver solver)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    grid.Cells[i, j] = (int)solver.Value(cells[i, j]);
                }
            }
        }
    }
}