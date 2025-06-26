using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NumMatch
{
    public enum MatchType
    {
        None,
        MainDiagonal,
        SecondaryDiagonal,
        HasClearPathBetween,
        Vertical,
    }

    public class GameBoard : MonoBehaviour
    {
        public const int NUMBER_OF_COLUMNS = 9;
        public const int MATCH_TOTAL_VALUE = 10;
        public const int INITIAL_BOARD_LENGTH = NUMBER_OF_COLUMNS * 5;
        public const int MINIMAL_BOARD_GRID_LENGTH = NUMBER_OF_COLUMNS * 15;


        public static GameBoard Instance { get; private set; }

        [SerializeField] private Transform contentTransform;
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private SOGameBoardUnitList allUnitSOList;

        private List<GameBoardUnit> selectedUnitList;
        private List<GameBoardUnit> allUnitList;
        private List<GameBoardUnit> allOccupiedUnitList;

        private Dictionary<GameBoardUnitType, SOGameBoardUnit> dict_GameBoardUnitType_SOGameBoardUnit;
        public int m_currentStageNumber;
        public int CurrentStageNumber
        { get { return m_currentStageNumber; } set { m_currentStageNumber = Mathf.Clamp(value, 1, 3); } }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(Instance);
            }
            Instance = this;
            dict_GameBoardUnitType_SOGameBoardUnit = allUnitSOList.ToDict();
            selectedUnitList = new List<GameBoardUnit>();
            allUnitList = new List<GameBoardUnit>();
            allOccupiedUnitList = new List<GameBoardUnit>();
            CurrentStageNumber = 2;
        }

        private void Start()
        {
            StartCoroutine(InitializeBoardRoutine());
        }

        public void CleanUp()
        {
            foreach (GameBoardUnit unit in allUnitList)
            {
                Destroy(unit.gameObject);
            }
            allUnitList.Clear();
            allOccupiedUnitList.Clear();
        }

        public IEnumerator InitializeBoardRoutine()
        {
            SpawnEmptyGrid();
            yield return new WaitForEndOfFrame();
            GenerateRandomBoard();
        }

        private void SpawnEmptyGrid()
        {
            for (int i = 0; i < MINIMAL_BOARD_GRID_LENGTH; i++)
            {
                var unit = SpawnUnit();
                allUnitList.Add(unit);
            }
        }

        private List<(GameBoardUnitType, GameBoardUnitType)> GetAllMatchablePairs()
        {
            var pairs = new List<(GameBoardUnitType, GameBoardUnitType)>();

            // Match cùng loại
            foreach (GameBoardUnitType t in Enum.GetValues(typeof(GameBoardUnitType)))
            {
                pairs.Add((t, t));
            }

            // Match theo tổng = 10
            for (int i = 1; i <= 4; i++) // 1–4
            {
                int j = 10 - i;
                var t1 = (GameBoardUnitType)(i - 1); // One = 0
                var t2 = (GameBoardUnitType)(j - 1);
                pairs.Add((t1, t2));
            }

            return pairs;
        }

        private (int indexA, int indexB)? GetValidMatchIndexPair(MatchType type, HashSet<int> usedIndices, int listUnitOnBoardLength)
        {
            int cols = NUMBER_OF_COLUMNS;
            int rows = listUnitOnBoardLength / cols;
            var rng = new System.Random();

            for (int attempt = 0; attempt < 100; attempt++)
            {
                int row = rng.Next(rows);
                int col = rng.Next(cols);
                int indexA = row * cols + col;
                int indexB = -1;

                switch (type)
                {
                    case MatchType.HasClearPathBetween:
                        if (col < cols - 1)
                            indexB = row * cols + (col + 1);
                        break;

                    case MatchType.Vertical:
                        if (row < rows - 1)
                            indexB = (row + 1) * cols + col;
                        break;

                    case MatchType.MainDiagonal:
                        if (row < rows - 1 && col < cols - 1)
                            indexB = (row + 1) * cols + (col + 1);
                        break;

                    case MatchType.SecondaryDiagonal:
                        if (row < rows - 1 && col > 0)
                            indexB = (row + 1) * cols + (col - 1);
                        break;
                }

                if (indexB >= 0 && indexB < listUnitOnBoardLength && !usedIndices.Contains(indexA) && !usedIndices.Contains(indexB))
                {
                    return (indexA, indexB);
                }
            }

            return null;
        }

        private MatchType GetMatchType(List<GameBoardUnitType?> board, int rowA, int colA, int rowB, int colB)
        {
            int cols = NUMBER_OF_COLUMNS;
            int rows = Mathf.CeilToInt(board.Count / cols);

            int indexA = rowA * cols + colA;
            int indexB = rowB * cols + colB;

            if (indexA < 0 || indexB < 0 || indexA >= board.Count || indexB >= board.Count)
                return MatchType.None;

            // Cùng hàng
            if (rowA == rowB)
            {
                int start = Math.Min(colA, colB) + 1;
                int end = Math.Max(colA, colB) - 1;
                for (int c = start; c <= end; c++)
                {
                    int index = rowA * cols + c;
                    if (board[index] != null) return MatchType.None;
                }
                return MatchType.HasClearPathBetween;
            }

            // Cùng cột
            if (colA == colB)
            {
                int start = Math.Min(rowA, rowB) + 1;
                int end = Math.Max(rowA, rowB) - 1;
                for (int r = start; r <= end; r++)
                {
                    int index = r * cols + colA;
                    if (board[index] != null) return MatchType.None;
                }
                return MatchType.Vertical;
            }

            // Main Diagonal
            if ((rowA - colA) == (rowB - colB))
            {
                int start = Math.Min(rowA, rowB) + 1;
                int end = Math.Max(rowA, rowB) - 1;
                for (int r = start; r <= end; r++)
                {
                    int c = r - (rowA - colA);
                    int index = r * cols + c;
                    if (index >= 0 && index < board.Count && board[index] != null)
                        return MatchType.None;
                }
                return MatchType.MainDiagonal;
            }

            // Secondary Diagonal
            if ((rowA + colA) == (rowB + colB))
            {
                int start = Math.Min(rowA, rowB) + 1;
                int end = Math.Max(rowA, rowB) - 1;
                for (int r = start; r <= end; r++)
                {
                    int c = (rowA + colA) - r;
                    int index = r * cols + c;
                    if (index >= 0 && index < board.Count && board[index] != null)
                        return MatchType.None;
                }
                return MatchType.SecondaryDiagonal;
            }

            return MatchType.None;
        }

        private bool WouldCreateExtraMatch(List<GameBoardUnitType?> board, int toBePlacedUnitIndex, GameBoardUnitType unitType)
        {
            int cols = NUMBER_OF_COLUMNS;
            int rows = Mathf.CeilToInt(board.Count / cols);

            int row = toBePlacedUnitIndex / cols;
            int col = toBePlacedUnitIndex % cols;

            for (int otherUnitIndex = 0; otherUnitIndex < board.Count; otherUnitIndex++)
            {
                if (otherUnitIndex == toBePlacedUnitIndex || board[otherUnitIndex] == null)
                    continue;

                var otherUnitType = board[otherUnitIndex].Value;

                bool valueMatched = AreUnitTypeValuesMatchable(unitType, otherUnitType);
                if (!valueMatched) continue;

                int otherRow = otherUnitIndex / cols;
                int otherCol = otherUnitIndex % cols;
                var matchType = GetMatchType(board, row, col, otherRow, otherCol);
                if (matchType != MatchType.None)
                    return true; // ❗ Sẽ tạo ra cặp match → reject
            }

            return false;
        }

        private MatchType GetRandomMatchType()
        {
            var values = new[] {
                MatchType.HasClearPathBetween,
                MatchType.Vertical,
                MatchType.MainDiagonal,
                MatchType.SecondaryDiagonal
            };
            return values[UnityEngine.Random.Range(0, values.Length)];
        }

        private List<GameBoardUnitType> GenerateValueList(int numMatchPairs, int targetTotal)
        {
            var rng = new System.Random();
            List<GameBoardUnitType?> valueList = new GameBoardUnitType?[targetTotal].ToList();
            HashSet<int> usedIndices = new();

            // Chọn ngẫu nhiên các cặp hợp lệ
            var allPairs = GetAllMatchablePairs().OrderBy(x => rng.Next()).ToList();

            int placed = 0;
            int maxAttempts = 1000;

            while (placed < numMatchPairs && maxAttempts-- > 0)
            {
                var pair = allPairs[placed % allPairs.Count]; // để tránh tràn dù thực tế ko thể tràn vì quy định tối đa là 3 match ở stage 1
                var matchType = GetRandomMatchType();
                var pairIndices = GetValidMatchIndexPair(matchType, usedIndices, targetTotal);

                if (pairIndices == null) continue;

                var indexA = pairIndices.Value.indexA;
                var indexB = pairIndices.Value.indexB;

                if (WouldCreateExtraMatch(valueList, indexA, pair.Item1)) continue;
                if (WouldCreateExtraMatch(valueList, indexB, pair.Item2)) continue;

                // ✅ Safe to place
                valueList[indexA] = pair.Item1;
                valueList[indexB] = pair.Item2;

                usedIndices.Add(indexA);
                usedIndices.Add(indexB);

                placed++;
            }

            // Đếm số lần mỗi type xuất hiện
            Dictionary<GameBoardUnitType, int> count = new();
            foreach (GameBoardUnitType t in Enum.GetValues(typeof(GameBoardUnitType)))
                count[t] = valueList.Count(x => x == t);

            // Fill phần còn lại nhưng KHÔNG tạo thêm pair mới
            for (int index = 0; index < valueList.Count; index++)
            {
                if (valueList[index] != null) continue;
                //valueList[i] = GameBoardUnitType.One;

                GameBoardUnitType chosen = GameBoardUnitType.One;
                int min = int.MaxValue;

                foreach (var unitType in Enum.GetValues(typeof(GameBoardUnitType)).Cast<GameBoardUnitType>())
                {
                    if (WouldCreateExtraMatch(valueList, index, unitType))
                    {
                        continue; // bỏ nếu tạo match mới
                    }

                    if (count[unitType] < min)
                    {
                        min = count[unitType];
                        chosen = unitType;
                    }
                }

                valueList[index] = chosen;
                count[chosen]++;
            }

            return valueList.Select(v => v.Value).ToList();
        }

        private void GenerateRandomBoard()
        {
            if (CurrentStageNumber < 1)
            {
                Debug.LogError("stage number can not be less than 1!");
            }

            int numberOfInitialMatches = CurrentStageNumber switch
            {
                1 => 3,
                2 => 2,
                _ => 1,
            };

            var generatedValues = GenerateValueList(numberOfInitialMatches, INITIAL_BOARD_LENGTH);

            for (int i = 0; i < generatedValues.Count; i++)
            {
                var unitSO = dict_GameBoardUnitType_SOGameBoardUnit[generatedValues[i]];
                allUnitList[i].Initialize(this, unitSO);
                allOccupiedUnitList.Add(allUnitList[i]);
            }

            PrintAllValidMatchesOnBoardWithSummary();
            AssertStageMatchCount(CurrentStageNumber);
        }

        public void AssertStageMatchCount(int stageNumber)
        {
            int expectedMatches = stageNumber switch
            {
                1 => 3,
                2 => 2,
                _ => 1
            };

            int cols = NUMBER_OF_COLUMNS;
            int rows = allOccupiedUnitList.Count / cols;
            HashSet<int> matchedIndices = new();

            int totalMatch = 0;

            for (int i = 0; i < allOccupiedUnitList.Count; i++)
            {
                if (matchedIndices.Contains(i)) continue;
                var unitA = allOccupiedUnitList[i];
                if (!unitA.IsOccupied()) continue;

                int rowA = i / cols;
                int colA = i % cols;

                for (int j = i + 1; j < allOccupiedUnitList.Count; j++)
                {
                    if (matchedIndices.Contains(j)) continue;
                    var unitB = allOccupiedUnitList[j];
                    if (!unitB.IsOccupied()) continue;

                    int rowB = j / cols;
                    int colB = j % cols;

                    if (!AreUnitTypeValuesMatchable(unitA.UnitSO.type, unitB.UnitSO.type)) continue;

                    MatchType matchType = GetMatchType(allOccupiedUnitList, rowA, colA, rowB, colB);
                    if (matchType == MatchType.None) continue;

                    matchedIndices.Add(i);
                    matchedIndices.Add(j);
                    totalMatch++;
                    break;
                }
            }

            if (totalMatch == expectedMatches)
            {
                Debug.Log($"✅ Stage {stageNumber} PASSED: Exactly {totalMatch} matchable pairs found.");
            }
            else
            {
                Debug.LogWarning($"❌ Stage {stageNumber} FAILED: Found {totalMatch} matchable pairs, expected {expectedMatches}.");
            }
        }

        public void PrintAllValidMatchesOnBoardWithSummary()
        {
            int cols = NUMBER_OF_COLUMNS;
            int rows = allOccupiedUnitList.Count / cols;
            HashSet<int> matchedIndices = new(); // ✅ ngăn trùng unit
            HashSet<(int, int)> printed = new();

            int totalMatch = 0;
            Dictionary<MatchType, int> matchCountByType = new();

            foreach (MatchType type in Enum.GetValues(typeof(MatchType)))
            {
                if (type != MatchType.None)
                    matchCountByType[type] = 0;
            }

            for (int i = 0; i < allOccupiedUnitList.Count; i++)
            {
                if (matchedIndices.Contains(i)) continue; // ⛔ đã tham gia match
                var unitA = allOccupiedUnitList[i];
                if (!unitA.IsOccupied()) continue;

                int rowA = i / cols;
                int colA = i % cols;

                for (int j = i + 1; j < allOccupiedUnitList.Count; j++)
                {
                    if (matchedIndices.Contains(j)) continue; // ⛔ đã match
                    var unitB = allOccupiedUnitList[j];
                    if (!unitB.IsOccupied()) continue;

                    int rowB = j / cols;
                    int colB = j % cols;

                    if (!AreUnitTypeValuesMatchable(unitA.UnitSO.type, unitB.UnitSO.type)) continue;

                    MatchType matchType = GetMatchType(allOccupiedUnitList, rowA, colA, rowB, colB);
                    if (matchType == MatchType.None) continue;

                    var key = (i, j);
                    if (!printed.Contains(key))
                    {
                        printed.Add(key);
                        matchedIndices.Add(i); // ✅ Đánh dấu đã match
                        matchedIndices.Add(j);
                        totalMatch++;
                        matchCountByType[matchType]++;
                        string log = $"✅ {unitA.UnitSO.type} matched with {unitB.UnitSO.type} at ({rowA},{colA}) ↔ ({rowB},{colB}) [{matchType}]";
                        Debug.Log(log);
                        break; // ⛔ Dừng loop sau khi match thành công
                    }
                }
            }

            Debug.Log("📊 === MATCH SUMMARY ===");
            Debug.Log($"🔢 Total Valid Matchable Pairs: {totalMatch}");
            foreach (var kvp in matchCountByType)
            {
                Debug.Log($"• {kvp.Key}: {kvp.Value}");
            }
            Debug.Log("========================");
        }

        private MatchType GetMatchType(List<GameBoardUnit> listUnit, int rowA, int colA, int rowB, int colB)
        {
            int cols = NUMBER_OF_COLUMNS;

            // Cùng hàng
            if (rowA == rowB)
            {
                int start = Math.Min(colA, colB) + 1;
                int end = Math.Max(colA, colB) - 1;
                for (int c = start; c <= end; c++)
                {
                    int index = rowA * cols + c;
                    if (listUnit[index].IsOccupied()) return MatchType.None;
                }
                return MatchType.HasClearPathBetween;
            }

            // Cùng cột
            if (colA == colB)
            {
                int start = Math.Min(rowA, rowB) + 1;
                int end = Math.Max(rowA, rowB) - 1;
                for (int r = start; r <= end; r++)
                {
                    int index = r * cols + colA;
                    if (listUnit[index].IsOccupied()) return MatchType.None;
                }
                return MatchType.Vertical;
            }

            // Main Diagonal
            if ((rowA - colA) == (rowB - colB))
            {
                int start = Math.Min(rowA, rowB) + 1;
                int end = Math.Max(rowA, rowB) - 1;
                for (int r = start; r <= end; r++)
                {
                    int c = r - (rowA - colA);
                    int index = r * cols + c;
                    if (listUnit[index].IsOccupied()) return MatchType.None;
                }
                return MatchType.MainDiagonal;
            }

            // Secondary Diagonal
            if ((rowA + colA) == (rowB + colB))
            {
                int start = Math.Min(rowA, rowB) + 1;
                int end = Math.Max(rowA, rowB) - 1;
                for (int r = start; r <= end; r++)
                {
                    int c = (rowA + colA) - r;
                    int index = r * cols + c;
                    if (listUnit[index].IsOccupied()) return MatchType.None;
                }
                return MatchType.SecondaryDiagonal;
            }

            return MatchType.None;
        }

        private bool AreUnitTypeValuesMatchable(GameBoardUnitType a, GameBoardUnitType b)
        {
            if (a == b) return true;
            int va = dict_GameBoardUnitType_SOGameBoardUnit[a].value;
            int vb = dict_GameBoardUnitType_SOGameBoardUnit[b].value;
            return (va + vb) == MATCH_TOTAL_VALUE;
        }

        public GameBoardUnit SpawnUnit()
        {
            var unit = Instantiate(unitPrefab, contentTransform);
            return unit.GetComponent<GameBoardUnit>();
        }

        public void OnAUnitClicked(GameBoardUnit chosenUnit)
        {
            if (selectedUnitList.Count >= 2)
            {
                ClearSelectedUnitList();
                return;
            }

            if (selectedUnitList.Contains(chosenUnit))
            {
                //if click on the same tile
                ClearSelectedUnitList();
                return;
            }

            if (selectedUnitList.Count < 2)
            {
                selectedUnitList.Add(chosenUnit);
                chosenUnit.ToggleSelected(true);
            }

            if (selectedUnitList.Count < 2)
            {
                return;
            }

            if (GetMatchTypeFromSelectedUnits() == MatchType.None)
            {
                ClearSelectedUnitList();
                return;
            }
            else
            {
                HandleValidMatch();
            }
        }

        private void ClearSelectedUnitList()
        {
            foreach (var unit in selectedUnitList)
            {
                unit.ToggleSelected(false);
            }
            selectedUnitList.Clear();
        }

        private MatchType GetMatchTypeFromSelectedUnits()
        {
            if (selectedUnitList.Count < 2)
            {
                //Debug.Log("⚠️ Không đủ 2 unit được chọn");
                return MatchType.None;
            }

            var a = selectedUnitList[0];
            var b = selectedUnitList[1];

            //Debug.Log($"🔍 Kiểm tra match: {a.UnitSO.value} ({a.UnitSO.type}) và {b.UnitSO.value} ({b.UnitSO.type})");

            bool valueMatched = AreUnitTypeValuesMatchable(a.UnitSO.type, b.UnitSO.type);
            if (!valueMatched)
            {
                //Debug.Log("❌ Không match về giá trị hoặc type");
                return MatchType.None;
            }

            int indexA = allOccupiedUnitList.IndexOf(a);
            int indexB = allOccupiedUnitList.IndexOf(b);

            int rowA = indexA / NUMBER_OF_COLUMNS;
            int colA = indexA % NUMBER_OF_COLUMNS;

            int rowB = indexB / NUMBER_OF_COLUMNS;
            int colB = indexB % NUMBER_OF_COLUMNS;

            return GetMatchType(allOccupiedUnitList, rowA, colA, rowB, colB);

            ////Case on the path from left to right, there is no blocked tile
            //bool blocked = false;
            //int startIndex = Mathf.Min(indexA, indexB) + 1;
            //int endIndex = Mathf.Max(indexA, indexB) - 1;
            //for (int betweenUnitIndex = startIndex; betweenUnitIndex <= endIndex; betweenUnitIndex++)
            //{
            //    if (allUnitList[betweenUnitIndex].IsOccupied())
            //    {
            //        blocked = true;
            //        break;
            //    }
            //}

            //if (!blocked)
            //{
            //    //Debug.Log("✅ Match: Cùng hàng và không bị chặn");
            //    return MatchType.HasClearPathBetween;
            //}

            ////Case on the same column
            //if (colA == colB)
            //{
            //    int startCol = Mathf.Min(rowA, rowB) + 1;
            //    int endCol = Mathf.Max(rowA, rowB) - 1;
            //    blocked = false;

            //    for (int betweenRowIndex = startCol; betweenRowIndex <= endCol; betweenRowIndex++)
            //    {
            //        int betweenUnitIndex = betweenRowIndex * NUMBER_OF_COLUMNS + colA;
            //        if (allUnitList[betweenUnitIndex].IsOccupied())
            //        {
            //            blocked = true;
            //            break;
            //        }
            //    }

            //    if (!blocked)
            //    {
            //        //Debug.Log("✅ Match: Cùng cột và không bị chặn");
            //        return MatchType.Vertical;
            //    }
            //}

            //// ✅ Chéo chính (Main Diagonal) → rowA - colA == rowB - colB
            //if ((rowA - colA) == (rowB - colB))
            //{
            //    int startRow = Mathf.Min(rowA, rowB) + 1;
            //    int endRow = Mathf.Max(rowA, rowB) - 1;
            //    blocked = false;

            //    for (int betweenRowIndex = startRow; betweenRowIndex <= endRow; betweenRowIndex++)
            //    {
            //        int colOffset = betweenRowIndex - (rowA - colA); // vì row - col là const trên đường chéo chính và trừ đi sẽ ra offset cần để tới column
            //        int betweenUnitIndex = betweenRowIndex * NUMBER_OF_COLUMNS + colOffset;

            //        if (allUnitList[betweenUnitIndex].IsOccupied())
            //        {
            //            blocked = true;
            //            break;
            //        }
            //    }

            //    if (!blocked)
            //    {
            //        //Debug.Log("✅ Match: Main diagonal và không bị chặn");
            //        return MatchType.MainDiagonal;
            //    }
            //}

            //// ✅ Chéo phụ (Secondary Diagonal) → rowA + colA == rowB + colB
            //if ((rowA + colA) == (rowB + colB))
            //{
            //    int startRow = Mathf.Min(rowA, rowB) + 1;
            //    int endRow = Mathf.Max(rowA, rowB) - 1;
            //    blocked = false;

            //    for (int betweenRowIndex = startRow; betweenRowIndex <= endRow; betweenRowIndex++)
            //    {
            //        int ColOffset = (rowA + colA) - betweenRowIndex; // vì row + col là const trên đường chéo phụ và trừ đi sẽ ra offset cần để tới column
            //        int betweenUnitIndex = betweenRowIndex * NUMBER_OF_COLUMNS + ColOffset;

            //        if (allUnitList[betweenUnitIndex].IsOccupied())
            //        {
            //            blocked = true;
            //            break;
            //        }
            //    }

            //    if (!blocked)
            //    {
            //        //Debug.Log("✅ Match: Secondary diagonal và không bị chặn");
            //        return MatchType.SecondaryDiagonal;
            //    }
            //}

            //return MatchType.None;
        }

        private void HandleValidMatch()
        {
            foreach (var matchedUnit in selectedUnitList)
            {
                matchedUnit.CurrentState = GameBoardUnitState.Matched;
            }
            selectedUnitList.Clear();
            CheckAndClearMatchedRows();
        }

        private void CheckAndClearMatchedRows()
        {
            int totalRows = Mathf.CeilToInt(allOccupiedUnitList.Count / NUMBER_OF_COLUMNS);
            List<int> listRowIndexToBeCleared = new List<int>();

            for (int row = 0; row < totalRows; row++)
            {
                if (!allOccupiedUnitList[row * NUMBER_OF_COLUMNS].IsMatched())
                {
                    // if the first tile is not a matched one, then no need to go further, this also apply for rows used for padding (UnInitialized)
                    continue;
                }

                bool isAllUnitsInRowMatched = true;

                for (int col = 1; col < NUMBER_OF_COLUMNS; col++)
                {
                    int index = row * NUMBER_OF_COLUMNS + col;
                    if (allOccupiedUnitList[index].IsOccupied())
                    {
                        isAllUnitsInRowMatched = false;
                        break;
                    }
                }

                if (isAllUnitsInRowMatched)
                {
                    Debug.Log($"Row {row} is empty → add to clear list");
                    listRowIndexToBeCleared.Add(row);
                }
            }
            Debug.Log($"Clearing empty rows");
            StartCoroutine(ClearMultipleRows(listRowIndexToBeCleared));
        }

        private IEnumerator ClearMultipleRows(List<int> rowsToClear)
        {
            foreach (var row in rowsToClear)
            {
                for (int col = 0; col < NUMBER_OF_COLUMNS; col++)
                {
                    int index = row * NUMBER_OF_COLUMNS + col;
                    var unit = allOccupiedUnitList[index];
                    unit.OnBeClearedFromBoard();
                }
            }

            yield return new WaitForSeconds(0.5f); // animation delay

            ShiftRowsBelowUp_MultipleRows(rowsToClear);
        }

        private void ShiftRowsBelowUp_MultipleRows(List<int> rowsToClear)
        {
            int totalRows = allOccupiedUnitList.Count / NUMBER_OF_COLUMNS;

            // Sort row indices từ thấp nhất → cao nhất
            rowsToClear.Sort();
            //basically, we will shift the min index cleared row to the bottom (which will also shift other cleared rows above it by 1 unit)
            // then repeat for n times with n is the length of cleared rows
            for (int i = 0; i < rowsToClear.Count; i++)
            {
                int clearedRow = rowsToClear[i];

                for (int row = clearedRow; row < totalRows - 1; row++)
                {
                    for (int col = 0; col < NUMBER_OF_COLUMNS; col++)
                    {
                        int currentIndex = row * NUMBER_OF_COLUMNS + col;
                        int belowIndex = (row + 1) * NUMBER_OF_COLUMNS + col;

                        var unitAbove = allOccupiedUnitList[currentIndex];
                        var unitBelow = allOccupiedUnitList[belowIndex];

                        allOccupiedUnitList[currentIndex] = unitBelow;
                        unitBelow.MoveToIndex(currentIndex);

                        allOccupiedUnitList[belowIndex] = unitAbove;
                        unitAbove.MoveToIndex(belowIndex);
                    }
                }

                // Cập nhật lại chỉ số cho các row phía trên vì chúng đã bị shift lên 1 đơn vị
                for (int j = 0; j < rowsToClear.Count; j++)
                {
                    if (rowsToClear[j] > clearedRow)
                    {
                        rowsToClear[j]--;
                    }
                }
            }

            // Thêm số row rỗng tương ứng ở cuối
            for (int i = 0; i < rowsToClear.Count; i++)
            {
                int rowIndex = totalRows - 1 - i;
                int rowStartIndex = rowIndex * NUMBER_OF_COLUMNS;
                for (int col = 0; col < NUMBER_OF_COLUMNS; col++)
                {
                    int index = rowStartIndex + col;
                    var unit = allOccupiedUnitList[index];

                    unit.ResetState();
                    //unit.MoveToIndex(index);
                }
            }
        }
    }
}