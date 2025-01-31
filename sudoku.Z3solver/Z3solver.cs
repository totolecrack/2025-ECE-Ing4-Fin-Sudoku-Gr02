using Sudoku.Shared;
using Microsoft.Z3;
using System;
using System.Collections.Generic;

namespace sudoku.Z3Solver
{
    public class Z3Solver : ISudokuSolver
    {
public SudokuGrid Solve(SudokuGrid s)
        {
            using (Context ctx = new Context())
            {
                var solverParams = ctx.MkParams();
                solverParams.Add("auto_config", true);
                solverParams.Add("smt.arith.solver", 2);

                Solver solver = ctx.MkSolver();
                solver.Parameters = solverParams;

                BitVecExpr[,] cells = DeclareVariables(ctx, solver);
                AddSudokuConstraints(ctx, solver, cells);
                FixInitialValues(ctx, solver, s, cells);

                return SolveSudoku(solver, cells, s);
            }
        }

        private BitVecExpr[,] DeclareVariables(Context ctx, Solver solver)
        {
            BitVecExpr[,] cells = new BitVecExpr[9, 9];
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    cells[row, col] = ctx.MkBVConst($"cell_{row}_{col}", 4);
                    // Correction : Utiliser MkBVULE (Unsigned Less Than or Equal)
                    solver.Add(ctx.MkAnd(
                        ctx.MkBVULE(ctx.MkBV(1, 4), cells[row, col]), // 1 <= cell (non-signé)
                        ctx.MkBVULE(cells[row, col], ctx.MkBV(9, 4))  // cell <= 9 (non-signé)
                    ));
                }
            }
            return cells;
        }

        private void AddSudokuConstraints(Context ctx, Solver solver, BitVecExpr[,] cells)
        {
            for (int i = 0; i < 9; i++)
            {
                solver.Add(ctx.MkDistinct(GetRow(cells, i)));
                solver.Add(ctx.MkDistinct(GetColumn(cells, i)));
            }

            for (int boxRow = 0; boxRow < 3; boxRow++)
            {
                for (int boxCol = 0; boxCol < 3; boxCol++)
                {
                    solver.Add(ctx.MkDistinct(GetBox(cells, boxRow, boxCol)));
                }
            }
        }

        private void FixInitialValues(Context ctx, Solver solver, SudokuGrid s, BitVecExpr[,] cells)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (s.Cells[row, col] != 0)
                    {
                        // Correction : Utiliser MkBV avec une interprétation non-signée
                        solver.Add(ctx.MkEq(cells[row, col], ctx.MkBV(s.Cells[row, col], 4)));
                    }
                }
            }
        }

        private SudokuGrid SolveSudoku(Solver solver, BitVecExpr[,] cells, SudokuGrid s)
        {
            if (solver.Check() == Status.SATISFIABLE)
            {
                SudokuGrid solvedGrid = s.CloneSudoku();
                Model model = solver.Model;

                for (int row = 0; row < 9; row++)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        // Extraction sécurisée de la valeur
                        solvedGrid.Cells[row, col] = (int)((BitVecNum)model.Eval(cells[row, col])).UInt64;
                    }
                }
                return solvedGrid;
            }
            else
            {
                throw new Exception("Aucune solution trouvée pour cette grille.");
            }
        }

        // Méthodes auxiliaires pour récupérer les lignes, colonnes et blocs
        private Expr[] GetRow(BitVecExpr[,] grid, int row)
        {
            Expr[] rowValues = new Expr[9];
            for (int col = 0; col < 9; col++)
            {
                rowValues[col] = grid[row, col];
            }
            return rowValues;
        }

        private Expr[] GetColumn(BitVecExpr[,] grid, int col)
        {
            Expr[] colValues = new Expr[9];
            for (int row = 0; row < 9; row++)
            {
                colValues[row] = grid[row, col];
            }
            return colValues;
        }

        private Expr[] GetBox(BitVecExpr[,] grid, int boxRow, int boxCol)
        {
            List<Expr> boxValues = new List<Expr>();
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    boxValues.Add(grid[3 * boxRow + row, 3 * boxCol + col]);
                }
            }
            return boxValues.ToArray();
        }
    }
}