using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Sudoku.Shared;

namespace Sudoku.CNN2
{
    public class CNN2Solver : ISudokuSolver
    {
        private readonly InferenceSession _session;

        public CNN2Solver()
        {
            // Définir le chemin absolu du modèle ONNX
            string modelPath = @"C:\Users\alice\.spyder-py3\sudoku_cnn.onnx";

            // Vérifier si le fichier ONNX existe
            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException($"❌ Modèle ONNX introuvable : {modelPath}");
            }

            Console.WriteLine($"✅ Chargement du modèle ONNX depuis : {modelPath}");
            _session = new InferenceSession(modelPath);
        }

        public SudokuGrid Solve(SudokuGrid s)
{
    // Convertir la grille Sudoku en entrée pour le modèle
    var inputTensor = PreprocessGrid(s);

    // Utiliser le bon nom d'entrée ("args_0")
    var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor("args_0", inputTensor)
    };

    // Exécuter le modèle ONNX
    using var results = _session.Run(inputs);

    // Récupérer la sortie avec le bon nom ("sequential_7")
    var outputTensor = results.First(r => r.Name == "sequential_7").AsTensor<float>();

    return PostprocessPredictions(outputTensor, s);
}


        private Tensor<float> PreprocessGrid(SudokuGrid grid)
        {
            float[] inputData = new float[9 * 9];

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    inputData[i * 9 + j] = grid.Cells[i, j] / 9.0f; // Normalisation (0-1)
                }
            }

            return new DenseTensor<float>(inputData, new[] { 1, 9, 9, 1 });
        }

        private SudokuGrid PostprocessPredictions(Tensor<float> predictions, SudokuGrid originalGrid)
        {
            SudokuGrid solvedGrid = originalGrid.CloneSudoku();

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (originalGrid.Cells[i, j] == 0) // Modifier seulement les cases vides
                    {
                        int predictedValue = (int)(predictions[0, i, j, 0] * 9.0) + 1;
                        solvedGrid.Cells[i, j] = predictedValue;
                    }
                }
            }

            Console.WriteLine("✅ Sudoku résolu avec ONNX !");
            return solvedGrid;
        }
    }
}
