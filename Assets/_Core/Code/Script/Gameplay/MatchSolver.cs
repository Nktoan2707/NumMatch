using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Utils;
using Debug = UnityEngine.Debug;

namespace NumMatch.Algorithm
{
    public class MatchSolver
    {
        public class MatchStep
        {
            public int RowA, ColA, RowB, ColB;

            public MatchStep(int rowA, int colA, int rowB, int colB)
            {
                RowA = rowA; ColA = colA; RowB = rowB; ColB = colB;
            }

            public override string ToString()
            {
                // +1 để chuyển từ base-0 sang base-1
                return $"{RowA + 1},{ColA + 1},{RowB + 1},{ColB + 1}";
            }
        }

        public class BoardState
        {
            public List<MatchStep> Steps;

            public BoardState(List<MatchStep> steps)
            {
                Steps = steps;
            }
        }

        public static GameBoardUnitType?[] ApplyMatchSteps(GameBoardUnitType?[] initialBoard, int numCols, List<MatchStep> steps)
        {
            var board = (GameBoardUnitType?[])initialBoard.Clone();

            foreach (var step in steps)
            {
                int indexA = step.RowA * numCols + step.ColA;
                int indexB = step.RowB * numCols + step.ColB;
                board[indexA] = null;
                board[indexB] = null;
            }

            return board;
        }

        public static int GenerateBoardKey(GameBoardUnitType?[] board)
        {
            unchecked
            {
                int hash = 17;
                foreach (var cell in board)
                {
                    int v = cell.HasValue ? (int)cell.Value : -1;
                    hash = hash * 31 + v;
                }
                return hash;
            }
        }

        public static List<List<MatchStep>> FindOptimalMatchSteps(List<GameBoardUnitType> flatBoard, int numCols, GameBoardUnitType targetType, int takeUpToTop)
        {
            var initialBoard = flatBoard.Cast<GameBoardUnitType?>().ToArray();
            var initialSteps = new List<MatchStep>();
            int initialTargetCount = CountTarget(initialBoard, targetType);

            PriorityQueue<BoardState, int> priorityQueue = new();
            List<List<MatchStep>> results = new();
            HashSet<int> seenBoardKeys = new();  // 🔥 quan trọng

            int initialHeuristic = initialTargetCount / 2;
            priorityQueue.Enqueue(new BoardState(initialSteps), initialHeuristic);

            while (priorityQueue.Count > 0 && results.Count < takeUpToTop)
            {
                var state = priorityQueue.Dequeue();
                //Console.WriteLine("Dequeued state with " + state.Steps.Count + " steps");
                var stateBoard = ApplyMatchSteps(initialBoard, numCols, state.Steps);
                var matchablePairs = GetMatchablePairs(stateBoard, numCols);

                foreach (var pair in matchablePairs)
                {
                    //Console.WriteLine($"MatchablePairs = {matchablePairs.Count}");
                    int idxA = pair.Item1;
                    int idxB = pair.Item2;

                    var newSteps = new List<MatchStep>(state.Steps);
                    newSteps.Add(ToStep(idxA, idxB, numCols));
                    var newBoard = stateBoard; //  reuse board
                    newBoard[idxA] = null;
                    newBoard[idxB] = null;

                    // ✅ Use board state key to prevent revisiting
                    int boardKey = GenerateBoardKey(newBoard); // hoặc GenerateBoardHash(newBoard).ToString()
                    if (seenBoardKeys.Contains(boardKey)) continue;
                    seenBoardKeys.Add(boardKey);

                    int remainingTarget = CountTarget(newBoard, targetType);
                    int g = newSteps.Count;
                    //int h = remainingTarget / 2;
                    int h = 0;
                    int priority = g + h;

                    if (remainingTarget <= 1)
                    {
                        results.Add(newSteps);
                        //Console.WriteLine($"🎯 ✅ Unique solution #{results.Count} found in {newSteps.Count} steps");
                    }
                    else
                    {
                        priorityQueue.Enqueue(new BoardState(newSteps), priority);
                    }
                }
                //Console.WriteLine($"Queue={priorityQueue.Count} | Seen={seenBoardKeys.Count} |  pairs matchable={matchablePairs.Count} Results={results.Count}");
            }

            return results;
        }

        private static MatchStep ToStep(int idxA, int idxB, int numCols)
        {
            int rowA = idxA / numCols, colA = idxA % numCols;
            int rowB = idxB / numCols, colB = idxB % numCols;
            return new MatchStep(rowA, colA, rowB, colB);
        }

        private static int CountTarget(GameBoardUnitType?[] board, GameBoardUnitType targetType)
        {
            int countTarget = board.Count(x => x == targetType);
            //Console.WriteLine($"📊 Board contains {countTarget} target type: {targetType}");

            return board.Count(x => x == targetType);
        }

        private static List<(int, int)> GetMatchablePairs(GameBoardUnitType?[] board, int numCols)
        {
            List<(int, int)> result = new();
            int len = board.Length;
            for (int i = 0; i < len; i++)
            {
                if (board[i] == null) continue;
                int rowA = i / numCols, colA = i % numCols;
                for (int j = i + 1; j < len; j++)
                {
                    if (board[j] == null) continue;
                    int rowB = j / numCols, colB = j % numCols;

                    if (IsMatchable(board[i].Value, board[j].Value) && IsClearPath(board, numCols, rowA, colA, rowB, colB))
                    {
                        result.Add((i, j));
                    }
                }
            }
            return result;
        }

        private static bool IsMatchable(GameBoardUnitType a, GameBoardUnitType b)
        {
            return a == b || ((int)a + 1) + ((int)b + 1) == 10;
        }

        private static bool IsClearPath(GameBoardUnitType?[] board, int cols, int rowA, int colA, int rowB, int colB)
        {
            var matchType = GetMatchType(board.ToList<GameBoardUnitType?>(), cols, rowA, colA, rowB, colB);
            return matchType != MatchType.None;
        }

        private static MatchType GetMatchType(List<GameBoardUnitType?> board, int cols, int rowA, int colA, int rowB, int colB)
        {
            int indexA = rowA * cols + colA;
            int indexB = rowB * cols + colB;

            // 👉 1. Match cùng hàng (row)
            int start = Math.Min(indexA, indexB) + 1;
            int end = Math.Max(indexA, indexB) - 1;
            bool isBlocked = false;

            for (int i = start; i <= end; i++)
            {
                if (board[i] != null)
                {
                    isBlocked = true;
                    break;
                }
            }

            if (!isBlocked)
            {
                return MatchType.HasClearPathBetween;
            }

            // 👉 2. Match cùng cột (column)
            if (colA == colB)
            {
                int startRow = Math.Min(rowA, rowB) + 1;
                int endRow = Math.Max(rowA, rowB) - 1;
                isBlocked = false;

                for (int r = startRow; r <= endRow; r++)
                {
                    int betweenIndex = r * cols + colA;
                    if (board[betweenIndex] != null)
                    {
                        isBlocked = true;
                        break;
                    }
                }

                if (!isBlocked)
                {
                    return MatchType.Vertical;
                }
            }

            // 👉 3. Match chéo chính (Main Diagonal): row - col là hằng số
            if ((rowA - colA) == (rowB - colB))
            {
                int startRow = Math.Min(rowA, rowB) + 1;
                int endRow = Math.Max(rowA, rowB) - 1;
                isBlocked = false;

                for (int r = startRow; r <= endRow; r++)
                {
                    int c = r - (rowA - colA);
                    int betweenIndex = r * cols + c;
                    if (board[betweenIndex] != null)
                    {
                        isBlocked = true;
                        break;
                    }
                }

                if (!isBlocked)
                {
                    return MatchType.MainDiagonal;
                }
            }

            // 👉 4. Match chéo phụ (Secondary Diagonal): row + col là hằng số
            if ((rowA + colA) == (rowB + colB))
            {
                int startRow = Math.Min(rowA, rowB) + 1;
                int endRow = Math.Max(rowA, rowB) - 1;
                isBlocked = false;

                for (int r = startRow; r <= endRow; r++)
                {
                    int c = (rowA + colA) - r;
                    int betweenIndex = r * cols + c;
                    if (board[betweenIndex] != null)
                    {
                        isBlocked = true;
                        break;
                    }
                }

                if (!isBlocked)
                {
                    return MatchType.SecondaryDiagonal;
                }
            }

            return MatchType.None;
        }

        public static void WriteOutput(string path, List<List<MatchStep>> solutions)
        {
            using StreamWriter writer = new(path);
            foreach (var sol in solutions)
            {
                string line = string.Join("|", sol.Select(s => s.ToString()));
                writer.WriteLine(line);
            }
            Debug.Log($"📝 Wrote {solutions.Count} solution(s) to output");
        }

        public static List<GameBoardUnitType> ParseInput(string filePath, int numCols, Dictionary<GameBoardUnitType, SOGameBoardUnit> dict)
        {
            string content = File.ReadAllText(filePath).Trim();
            var list = new List<GameBoardUnitType>();

            foreach (char c in content)
            {
                int targetValue = int.Parse(c.ToString());
                var match = dict.FirstOrDefault(kv => kv.Value.value == targetValue);
                if (match.Equals(default(KeyValuePair<GameBoardUnitType, SOGameBoardUnit>)))
                    throw new Exception($"Không tìm thấy GameBoardUnitType cho giá trị: {targetValue}");
                list.Add(match.Key);
            }
            return list;
        }

        public static void Run(string inputPath, string outputPath, int numCols, Dictionary<GameBoardUnitType, SOGameBoardUnit> dict, GameBoardUnitType targetType)
        {
            Debug.Log($"📥 Reading input from: {inputPath}");
            var board = ParseInput(inputPath, numCols, dict);
            Debug.Log($"📐 Parsed {board.Count} units from input. Starting solve...");

            Console.WriteLine("Start running algorithm with input: " + inputPath);
            int takeUpToTop = 10;
            var stopwatch = Stopwatch.StartNew();
            var solutions = FindOptimalMatchSteps(board, numCols, GameBoardUnitType.Five, takeUpToTop);
            stopwatch.Stop();
            Debug.Log($"✅ Found {solutions.Count} solution(s)");
            Debug.Log($"⏱️ Total time: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.Elapsed.TotalSeconds:F2} seconds)");

            WriteOutput(outputPath, solutions);
            Debug.Log($"📤 Output written to: {outputPath}");
        }
    }
}