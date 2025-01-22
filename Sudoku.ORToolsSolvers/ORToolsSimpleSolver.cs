using Sudoku.Shared;

namespace Sudoku.ORToolsSolvers;

public class ORToolsSimpleSolver : ISudokuSolver
{
    public SudokuGrid Solve(SudokuGrid s)
    {
        return s;
    }
}
