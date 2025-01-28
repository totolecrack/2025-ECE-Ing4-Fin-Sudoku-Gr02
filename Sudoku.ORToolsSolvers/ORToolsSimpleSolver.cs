using Google.OrTools.Sat;
using Sudoku.Shared;

namespace Sudoku.ORToolsSolvers;

public class ORToolsSimpleSolver : ISudokuSolver
{
    public SudokuGrid Solve(SudokuGrid s)
    {
        // Création du modèle de contraintes
        CpModel model = new CpModel();

        // Définir les variables pour chaque cellule (9x9)
        // Les variables peuvent prendre des valeurs entre 1 et 9 (valeurs possibles pour chaque cellule)
        IntVar[,] cells = new IntVar[9, 9];
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                cells[i, j] = model.NewIntVar(1, 9, $"cell_{i}_{j}");
            }
        }

        // Ajouter des contraintes pour les lignes, colonnes et régions 3x3
        for (int i = 0; i < 9; i++)
        {
            // Contrainte pour chaque ligne : chaque cellule doit être unique
            model.AddAllDifferent(Enumerable.Range(0, 9).Select(j => cells[i, j]).ToArray());
            // Contrainte pour chaque colonne : chaque cellule doit être unique
            model.AddAllDifferent(Enumerable.Range(0, 9).Select(j => cells[j, i]).ToArray());
        }

        // Ajouter des contraintes pour les régions 3x3
        for (int box = 0; box < 9; box++)
        {
            int rowStart = (box / 3) * 3;
            int colStart = (box % 3) * 3;
            List<IntVar> boxCells = new List<IntVar>();

            for (int r = rowStart; r < rowStart + 3; r++)
            {
                for (int c = colStart; c < colStart + 3; c++)
                {
                    boxCells.Add(cells[r, c]);
                }
            }
            model.AddAllDifferent(boxCells.ToArray());
        }

        // Ajouter des contraintes pour les cases déjà remplies dans le Sudoku
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (s.Cells[i, j] != 0)
                {
                    var iclosure = i;
                    var jclosure = j;
                    // Si la cellule est déjà remplie, fixer sa valeur
                    model.Add( cells[iclosure, jclosure] == s.Cells[iclosure, jclosure]);
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
                    s.Cells[i, j] = (int)solver.Value(cells[i, j]);
                }
            }
        }
        else
        {
            // Si aucune solution n'a été trouvée, retourner une grille vide ou générer une exception
            throw new InvalidOperationException("No solution found for the Sudoku puzzle.");
        }

        return s;
    }
}
