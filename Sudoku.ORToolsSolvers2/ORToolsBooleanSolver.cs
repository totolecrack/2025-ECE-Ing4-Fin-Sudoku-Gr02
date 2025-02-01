namespace Sudoku.ORToolsSolvers2;
using Google.OrTools.Sat;
using Sudoku.Shared;

public class ORToolsBooleanSolver : ISudokuSolver
{
    public SudokuGrid Solve(SudokuGrid grid)
    {
        // Création du modèle de contraintes
        CpModel model = new CpModel();

        // Définir les variables booléennes pour chaque valeur possible dans chaque cellule
        BoolVar[,,] cells = new BoolVar[9, 9, 9];
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                for (int k = 0; k < 9; k++)
                {
                    cells[i, j, k] = model.NewBoolVar($"cell_{i}_{j}_{k + 1}");
                }
            }
        }

        // Chaque cellule doit avoir exactement une valeur
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                model.Add(LinearExpr.Sum(Enumerable.Range(0, 9).Select(k => cells[i, j, k])) == 1);
            }
        }

        // Chaque valeur doit apparaître exactement une fois par ligne
        for (int i = 0; i < 9; i++)
        {
            for (int k = 0; k < 9; k++)
            {
                model.Add(LinearExpr.Sum(Enumerable.Range(0, 9).Select(j => cells[i, j, k])) == 1);
            }
        }

        // Chaque valeur doit apparaître exactement une fois par colonne
        for (int j = 0; j < 9; j++)
        {
            for (int k = 0; k < 9; k++)
            {
                model.Add(LinearExpr.Sum(Enumerable.Range(0, 9).Select(i => cells[i, j, k])) == 1);
            }
        }

        // Chaque valeur doit apparaître exactement une fois par région 3x3
        for (int boxRow = 0; boxRow < 3; boxRow++)
        {
            for (int boxCol = 0; boxCol < 3; boxCol++)
            {
                for (int k = 0; k < 9; k++)
                {
                    model.Add(LinearExpr.Sum(
                        from i in Enumerable.Range(0, 3)
                        from j in Enumerable.Range(0, 3)
                        select cells[boxRow * 3 + i, boxCol * 3 + j, k]) == 1);
                }
            }
        }

        // Ajouter des contraintes pour les cases déjà remplies dans le Sudoku
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (grid.Cells[i, j] != 0)
                {
                    int value = grid.Cells[i, j] - 1;
                    model.Add(cells[i, j, value] == 1);
                }
            }
        }

        // Utiliser le solveur pour résoudre le problème
        CpSolver solver = new CpSolver();
        CpSolverStatus status = solver.Solve(model);

        // Si une solution est trouvée, mettre à jour la grille Sudoku
        if (status == CpSolverStatus.Optimal || status == CpSolverStatus.Feasible)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    for (int k = 0; k < 9; k++)
                    {
                        if (solver.Value(cells[i, j, k]) == 1)
                        {
                            grid.Cells[i, j] = k + 1;
                        }
                    }
                }
            }
        }
        else
        {
            // Si aucune solution n'a été trouvée, retourner une grille vide ou générer une exception
            throw new InvalidOperationException("No solution found for the Sudoku puzzle.");
        }

        return grid;
    }
}