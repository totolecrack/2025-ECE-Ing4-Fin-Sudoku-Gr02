using Sudoku.Shared;

namespace Sudoku.Norvig
{
    public class NorvigSolver : ISudokuSolver
    {
        public SudokuGrid Solve(SudokuGrid s)
        {
            var grid = ConvertToGrid(s);
            if (SolveGrid(ref grid))
            {
                return ConvertToSudokuGrid(grid);
            }
            return s; // Retourne la grille originale si non résoluble
        }

        private Dictionary<string, string> ConvertToGrid(SudokuGrid s)
        {
            var grid = new Dictionary<string, string>();
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    string cell = $"{(char)('A' + row)}{col + 1}";
                    int value = s.Cells[row, col];
                    grid[cell] = value == 0 ? "123456789" : value.ToString();
                }
            }
            return grid;
        }

        private SudokuGrid ConvertToSudokuGrid(Dictionary<string, string> grid)
        {
            var solvedGrid = new SudokuGrid();
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    string cell = $"{(char)('A' + row)}{col + 1}";
                    solvedGrid.Cells[row, col] = int.Parse(grid[cell]);
                }
            }
            return solvedGrid;
        }

        private bool SolveGrid(ref Dictionary<string, string> grid)
        {
            grid = PropagateConstraints(grid);
            if (grid.Values.Any(v => v == "")) return false; // Échec si une case est vide
            if (grid.Values.All(v => v.Length == 1)) return true; // Succès si toutes les cases sont remplies

            var cell = grid.Where(kv => kv.Value.Length > 1).OrderBy(kv => kv.Value.Length).First().Key;
            foreach (var val in grid[cell])
            {
                var newGrid = new Dictionary<string, string>(grid);
                newGrid[cell] = val.ToString();
                if (SolveGrid(ref newGrid))
                {
                    grid = newGrid;
                    return true;
                }
            }
            return false;
        }

        private Dictionary<string, string> PropagateConstraints(Dictionary<string, string> grid)
        {
            bool changed;
            do
            {
                changed = false;
                foreach (var cell in grid.Keys.ToList())
                {
                    if (grid[cell].Length == 1)
                    {
                        foreach (var peer in GetPeers(cell))
                        {
                            if (grid[peer].Contains(grid[cell]))
                            {
                                grid[peer] = grid[peer].Replace(grid[cell], "");
                                changed = true;
                            }
                        }
                    }
                }
            } while (changed);
            return grid;
        }

        private List<string> GetPeers(string cell)
        {
            var peers = new HashSet<string>();
            char row = cell[0];
            int col = int.Parse(cell[1].ToString());

            for (int i = 1; i <= 9; i++)
            {
                peers.Add($"{row}{i}");
                peers.Add($"{(char)('A' + (row - 'A' + i) % 9)}{col}");
            }

            int startRow = (row - 'A') / 3 * 3;
            int startCol = (col - 1) / 3 * 3;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    peers.Add($"{(char)('A' + startRow + i)}{startCol + j + 1}");
                }
            }

            peers.Remove(cell);
            return peers.ToList();
        }
    }
}
