using Sudoku.Shared;
using Google.OrTools.Sat;

namespace Sudoku.ORToolsSolvers
{
    public class ORToolsSimpleSolvers : ISudokuSolver
    {
        public SudokuGrid Solve(SudokuGrid s)
        {
            // Création du modèle OR-Tools
            CpModel model = new CpModel();

            // Définir les variables : chaque cellule est une variable avec des valeurs entre 1 et 9
            IntVar[,] cells = new IntVar[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    cells[row, col] = model.NewIntVar(1, 9, $"cell_{row}_{col}");
                }
            }

            // Contraintes pour les lignes : chaque chiffre de 1 à 9 doit apparaître une fois
            for (int row = 0; row < 9; row++)
            {
                model.AddAllDifferent(GetRow(cells, row));
            }

            // Contraintes pour les colonnes : chaque chiffre de 1 à 9 doit apparaître une fois
            for (int col = 0; col < 9; col++)
            {
                model.AddAllDifferent(GetColumn(cells, col));
            }

            // Contraintes pour chaque boîte 3x3 : chaque chiffre de 1 à 9 doit apparaître une fois
            for (int boxRow = 0; boxRow < 3; boxRow++)
            {
                for (int boxCol = 0; boxCol < 3; boxCol++)
                {
                    List<IntVar> box = new List<IntVar>();
                    for (int row = 0; row < 3; row++)
                    {
                        for (int col = 0; col < 3; col++)
                        {
                            box.Add(cells[3 * boxRow + row, 3 * boxCol + col]);
                        }
                    }
                    model.AddAllDifferent(box);
                }
            }

            // Contraintes pour les cellules données initialement
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (s.Cells[row, col] != 0)
                    {
                        model.Add(cells[row, col] == s.Cells[row, col]);
                    }
                }
            }

            // Résolution
            CpSolver solver = new CpSolver();
            CpSolverStatus status = solver.Solve(model);

            // Vérification du statut de la solution
            if (status == CpSolverStatus.Feasible || status == CpSolverStatus.Optimal)
            {
                SudokuGrid solutionGrid = s.CloneSudoku();

                for (int row = 0; row < 9; row++)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        solutionGrid.Cells[row, col] = (int)solver.Value(cells[row, col]);
                    }
                }

                return solutionGrid;
            }
            else
            {
                throw new Exception("Aucune solution trouvée pour cette grille de Sudoku.");
            }
        }

        private IEnumerable<IntVar> GetRow(IntVar[,] grid, int row)
        {
            for (int col = 0; col < 9; col++)
            {
                yield return grid[row, col];
            }
        }

        private IEnumerable<IntVar> GetColumn(IntVar[,] grid, int col)
        {
            for (int row = 0; row < 9; row++)
            {
                yield return grid[row, col];
            }
        }
    }
}