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
        public const int INITIAL_BOARD_LENGTH = NUMBER_OF_COLUMNS * 3;
        public const int INITIAL_STAGE_NUMBER = 1;
        public const int MATCH_TOTAL_VALUE = 10;
        public const int NUMBER_OF_COLUMNS = 9;
        public const int RETRY_LIMIT_GENERATE_BOARD = 50;
        public const int PADDING_ROWS = 3;
        public const int INITIAL_ADD_NUMBER_ATTEMPTS = 6;

        public event EventHandler OnAddNumberAttemptsLeftChanged;

        public event EventHandler OnCurrentScoreChanged;

        public event EventHandler OnCurrentStageNumberChanged;

        private List<GameBoardUnit> allOccupiedUnitList;
        private List<GameBoardUnit> allUnitList;
        [SerializeField] private SOGameBoardUnitList allUnitSOList;
        private Dictionary<GameBoardUnitType, SOGameBoardUnit> dict_GameBoardUnitType_SOGameBoardUnit;
        private int m_addNumberAttemptsLeft;
        private int m_currentScore;
        private int m_currentStageNumber;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Transform scrollViewContent;
        [SerializeField] private RectTransform scrollViewport;
        private List<GameBoardUnit> selectedUnitList;
        [SerializeField] private GameObject unitPrefab;
        public static GameBoard Instance { get; private set; }

        public int AddNumberAttemptsLeft
        {
            get { return m_addNumberAttemptsLeft; }
            set
            {
                m_addNumberAttemptsLeft = Math.Max(0, value);
                OnAddNumberAttemptsLeftChanged?.Invoke(this, EventArgs.Empty);
            }
        }



        public int CurrentScore
        {
            get { return m_currentScore; }
            set
            {
                m_currentScore = value;
                OnCurrentScoreChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void AddScore(int basePoint)
        {
            CurrentScore += basePoint * CurrentStageNumber;
        }

        public int CurrentStageNumber
        {
            get { return m_currentStageNumber; }
            set
            {
                m_currentStageNumber = Math.Max(INITIAL_STAGE_NUMBER, value);
                OnCurrentStageNumberChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void OnPlayerClickedAddMoreNumber()
        {
            if (AddNumberAttemptsLeft <= 0)
                return;
            StartCoroutine(AddMoreNumbersToBoard());
        }

        public IEnumerator AddMoreNumbersToBoard()
        {
            // Tạo danh sách các UnitSO copy từ allOccupiedUnitList (các units còn trên board)
            var newUnitSOs = allOccupiedUnitList
                .Where(u => !u.IsMatched())
                .Select(u => u.UnitSO)
                .ToList();

            // Tính toán số row ước tính sau khi add thêm số hiện tại
            int rowsOccupied = Mathf.CeilToInt((float)(allOccupiedUnitList.Count) / NUMBER_OF_COLUMNS);
            rowsOccupied += Mathf.CeilToInt((float)(newUnitSOs.Count) / NUMBER_OF_COLUMNS);
            int minimalRows = Mathf.CeilToInt((float)CalculateMinimalBoardGridLength() / NUMBER_OF_COLUMNS);

            // Bổ sung hàng rỗng nếu cần để giữ form
            while (rowsOccupied + PADDING_ROWS > Mathf.CeilToInt((float)allUnitList.Count / NUMBER_OF_COLUMNS))
            {
                for (int i = 0; i < NUMBER_OF_COLUMNS; i++)
                {
                    var unit = SpawnUnit();
                    allUnitList.Add(unit);
                }
            }

            // Toggle scroll
            bool needsScroll = rowsOccupied + PADDING_ROWS >= minimalRows;
            ToggleScrollDirection(needsScroll);

            //chờ các empty unit instantiate xong xuôi
            yield return null;

            // tìm đuôi của board để bắt đầu add number
            for (int i = 0, j = 0; i < allUnitList.Count && j < newUnitSOs.Count; i++)
            {
                var unit = allUnitList[i];
                if (!unit.IsUnitialized())
                {
                    continue;
                }

                unit.Initialize(this, newUnitSOs[j]);
                //unit.Initialize(this, dict_GameBoardUnitType_SOGameBoardUnit[GameBoardUnitType.One]);
                allOccupiedUnitList.Add(unit);
                j++;
            }

            AddNumberAttemptsLeft--;

            //Debug.Log("📦 AllUnitList (Full Grid View with States):");
            //PrintUnitGridWithState(allUnitList);

            //Debug.Log("📊 AllOccupiedUnitList (Logical View with States):");
            //PrintUnitGridWithState(allOccupiedUnitList);
        }

        private string Shorten(string state)
        {
            return state switch
            {
                "UnInitialized" => "UnInit",
                "Initialized" => "Init",
                "Chosen" => "Chosen",
                "ClearAnimationInProgress" => "Clear",
                _ => state
            };
        }

        private void PrintUnitGridWithState(List<GameBoardUnit> unitList)
        {
            int cols = NUMBER_OF_COLUMNS;
            int totalRows = Mathf.CeilToInt((float)unitList.Count / cols);
            string log = "";

            for (int row = 0; row < totalRows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int index = row * cols + col;
                    if (index >= unitList.Count)
                    {
                        log += "          ";
                    }
                    else
                    {
                        var unit = unitList[index];
                        string typeStr = unit.UnitSO != null ? unit.UnitSO.type.ToString() : "Empty";
                        string stateStr = unit.CurrentState.ToString();
                        log += $"{typeStr}({Shorten(stateStr)})".PadRight(12);
                    }
                }
                log += "\n";
            }

            Debug.Log(log);
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

            ToggleScrollDirection(false);
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

            //test 
            //HandleValidMatch();
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

        public GameBoardUnit SpawnUnit()
        {
            var unit = Instantiate(unitPrefab, scrollViewContent);
            return unit.GetComponent<GameBoardUnit>();
        }

        public void ToggleScrollDirection(bool doesEnableScroll)
        {
            scrollRect.vertical = doesEnableScroll;
        }

        private bool AreUnitTypeValuesMatchable(GameBoardUnitType a, GameBoardUnitType b)
        {
            if (a == b) return true;
            int va = dict_GameBoardUnitType_SOGameBoardUnit[a].value;
            int vb = dict_GameBoardUnitType_SOGameBoardUnit[b].value;
            return (va + vb) == MATCH_TOTAL_VALUE;
        }

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
            CurrentStageNumber = INITIAL_STAGE_NUMBER;
            CurrentScore = 0;
            AddNumberAttemptsLeft = INITIAL_ADD_NUMBER_ATTEMPTS;

            scrollRect.horizontal = false;
            ToggleScrollDirection(false);
        }

        private int CalculateMinimalBoardGridLength()
        {
            GameObject temp = Instantiate(unitPrefab, scrollViewContent);
            var rect = temp.GetComponent<RectTransform>().rect;
            float unitHeight = rect.height;

            Destroy(temp);

            // Get spacing từ layout group
            var layoutGroup = scrollViewContent.GetComponent<GridLayoutGroup>();
            float spacing = layoutGroup != null ? layoutGroup.spacing.x : 0f;

            float fullRowHeight = unitHeight + spacing;
            float viewportHeight = scrollViewport.rect.height;

            int numberOfRows = Mathf.CeilToInt(viewportHeight / fullRowHeight);

            //Debug.Log($"Unit height = {unitHeight}, spacing = {spacing}, viewport = {viewportHeight}, fullRowHeight = {fullRowHeight}");

            return (numberOfRows) * NUMBER_OF_COLUMNS;
        }

        private void CheckAndClearMatchedRows()
        {
            int totalUnits = allOccupiedUnitList.Count;
            int totalRows = Mathf.CeilToInt((float)totalUnits / NUMBER_OF_COLUMNS);
            List<int> listRowIndexToBeCleared = new();

            for (int row = 0; row < totalRows; row++)
            {
                int rowStart = row * NUMBER_OF_COLUMNS;
                int rowEnd = Mathf.Min(rowStart + NUMBER_OF_COLUMNS, totalUnits);

                // Nếu unit đầu tiên không matched → skip nhanh
                if (!allOccupiedUnitList[rowStart].IsMatched())
                    continue;

                bool isAllMatched = true;
                for (int i = rowStart; i < rowEnd; i++)
                {
                    if (allOccupiedUnitList[i].IsOccupied())
                    {
                        isAllMatched = false;
                        break;
                    }
                }

                if (isAllMatched)
                {
                    Debug.Log($"✅ Row {row} is matched (length: {rowEnd - rowStart}) → Add to clear list");
                    listRowIndexToBeCleared.Add(row);
                }
            }

            if (listRowIndexToBeCleared.Count > 0)
            {
                StartCoroutine(ClearMultipleRows(listRowIndexToBeCleared));
            }
        }



        private IEnumerator ClearMultipleRows(List<int> rowsToClear)
        {
            foreach (var row in rowsToClear)
            {
                for (int col = 0; col < NUMBER_OF_COLUMNS; col++)
                {
                    int index = row * NUMBER_OF_COLUMNS + col;
                    if (index >= allOccupiedUnitList.Count)
                    {
                        break;
                    }

                    var unit = allOccupiedUnitList[index];
                    unit.OnBeClearedFromBoard();
                }
            }

            yield return new WaitForSeconds(0.5f); // animation delay
            AddScore(rowsToClear.Count);

            ShiftRowsBelowUp(rowsToClear);
            CheckClearResidualRows();
            TryAdvanceStageIfCleared();

        }

        private void CheckClearResidualRows()
        {
        }

        private void ClearSelectedUnitList()
        {
            foreach (var unit in selectedUnitList)
            {
                unit.ToggleSelected(false);
            }
            selectedUnitList.Clear();
        }

        private void GenerateRandomBoard()
        {
            if (CurrentStageNumber < 1)
            {
                Debug.LogError("❌ Stage number can not be less than 1!");
                return;
            }

            int numberOfInitialMatches = CurrentStageNumber switch
            {
                1 => 3,
                2 => 2,
                _ => 1,
            };

            Debug.Log($"🚀 Generating board for Stage {CurrentStageNumber}");
            Debug.Log($"🔢 Required initial match pairs: {numberOfInitialMatches}");

            var generatedValues = GenerateValueList(numberOfInitialMatches, INITIAL_BOARD_LENGTH);

            Debug.Log("🎲 Generated values:");
            for (int i = 0; i < generatedValues.Count; i++)
            {
                Debug.Log($"• Index {i}: {generatedValues[i]}");
            }

            for (int i = 0; i < generatedValues.Count; i++)
            {
                var unitSO = dict_GameBoardUnitType_SOGameBoardUnit[generatedValues[i]];
                allUnitList[i].Initialize(this, unitSO);
                //allUnitList[i].Initialize(this, dict_GameBoardUnitType_SOGameBoardUnit[GameBoardUnitType.One]);

                allOccupiedUnitList.Add(allUnitList[i]);

                Debug.Log($"✅ Initialized unit at index {i}: {unitSO.type} ({unitSO.value})");
            }

            Debug.Log($"📦 Total units on board: {allOccupiedUnitList.Count}");

            // Kiểm tra lại số lượng cặp match hợp lệ
            AssertStageMatchCount(CurrentStageNumber);
        }


        private List<GameBoardUnitType> GenerateValueList(int numMatchPairs, int targetTotal)
        {
            var rng = new System.Random();
            int retryLimit = RETRY_LIMIT_GENERATE_BOARD;

            List<(int, int)> lastFailedFinalMatches = null; // 🔧 lưu lại kết quả cuối nếu fail

            while (retryLimit-- > 0)
            {
                List<GameBoardUnitType?> valueList = new GameBoardUnitType?[targetTotal].ToList();
                HashSet<int> usedIndices = new();
                var allPairs = GetAllMatchablePairs().OrderBy(x => rng.Next()).ToList();

                int placed = 0;
                int maxAttempts = 1000;

                while (placed < numMatchPairs && maxAttempts-- > 0)
                {
                    var pair = allPairs[placed % allPairs.Count];
                    var matchType = GetRandomMatchType();
                    var pairIndices = GetValidMatchIndexPair(matchType, usedIndices, targetTotal);

                    if (pairIndices == null) continue;

                    int indexA = pairIndices.Value.indexA;
                    int indexB = pairIndices.Value.indexB;

                    if (WouldCreateExtraMatch(valueList, indexA, pair.Item1)) continue;
                    if (WouldCreateExtraMatch(valueList, indexB, pair.Item2)) continue;

                    valueList[indexA] = pair.Item1;
                    valueList[indexB] = pair.Item2;

                    usedIndices.Add(indexA);
                    usedIndices.Add(indexB);
                    placed++;
                }

                // Phân bố đều phần còn lại
                Dictionary<GameBoardUnitType, int> count = new();
                foreach (GameBoardUnitType t in Enum.GetValues(typeof(GameBoardUnitType)))
                    count[t] = valueList.Count(x => x == t);

                for (int i = 0; i < valueList.Count; i++)
                {
                    if (valueList[i] != null) continue;

                    GameBoardUnitType chosen = GameBoardUnitType.One;
                    int min = int.MaxValue;

                    foreach (var t in Enum.GetValues(typeof(GameBoardUnitType)).Cast<GameBoardUnitType>())
                    {
                        if (WouldCreateExtraMatch(valueList, i, t)) continue;
                        if (count[t] < min)
                        {
                            min = count[t];
                            chosen = t;
                        }
                    }

                    valueList[i] = chosen;
                    count[chosen]++;
                }

                // ✅ Kiểm tra final match
                var finalMatches = GetAllMatchablePairsOnBoard(valueList);
                if (finalMatches.Count <= numMatchPairs)
                {
                    return valueList.Select(v => v.Value).ToList();
                }

                lastFailedFinalMatches = finalMatches; // ❗ lưu lại lần cuối nếu fail
            }

            if (lastFailedFinalMatches != null)
            {
                Debug.LogWarning($"❌ Retry #{RETRY_LIMIT_GENERATE_BOARD}: generated {lastFailedFinalMatches.Count} matches > expected {numMatchPairs}");
                foreach (var (a, b) in lastFailedFinalMatches)
                {
                    Debug.Log($"Pair: #{a} - #{b}");
                }
            }

            return null;
        }

        private List<(GameBoardUnitType, GameBoardUnitType)> GetAllMatchablePairs()
        {
            var pairs = new List<(GameBoardUnitType, GameBoardUnitType)>();

            // Match cùng loại
            foreach (GameBoardUnitType t in Enum.GetValues(typeof(GameBoardUnitType)))
                pairs.Add((t, t));

            // Match theo tổng = 10
            for (int i = 1; i <= 4; i++)
            {
                int j = 10 - i;
                var t1 = (GameBoardUnitType)(i - 1);
                var t2 = (GameBoardUnitType)(j - 1);
                pairs.Add((t1, t2));
            }

            return pairs;
        }

        private List<(int, int)> GetAllMatchablePairsOnBoard(List<GameBoardUnitType?> board)
        {
            List<(int, int)> validPairs = new();
            int cols = NUMBER_OF_COLUMNS;

            for (int i = 0; i < board.Count; i++)
            {
                if (board[i] == null) continue;
                int rowA = i / cols;
                int colA = i % cols;
                var typeA = board[i].Value;

                for (int j = i + 1; j < board.Count; j++)
                {
                    if (board[j] == null) continue;
                    var typeB = board[j].Value;

                    if (!AreUnitTypeValuesMatchable(typeA, typeB)) continue;

                    int rowB = j / cols;
                    int colB = j % cols;
                    var matchType = GetMatchType(board, rowA, colA, rowB, colB);
                    if (matchType != MatchType.None)
                        validPairs.Add((i, j));
                }
            }

            return validPairs;
        }

        private MatchType GetMatchType(List<GameBoardUnitType?> board, int rowA, int colA, int rowB, int colB)
        {
            int cols = NUMBER_OF_COLUMNS;
            int indexA = rowA * cols + colA;
            int indexB = rowB * cols + colB;
            if (indexA < 0 || indexB < 0 || indexA >= board.Count || indexB >= board.Count)
                return MatchType.None;

            if (rowA == rowB)
            {
                for (int c = Math.Min(colA, colB) + 1; c < Math.Max(colA, colB); c++)
                    if (board[rowA * cols + c] != null) return MatchType.None;
                return MatchType.HasClearPathBetween;
            }

            if (colA == colB)
            {
                for (int r = Math.Min(rowA, rowB) + 1; r < Math.Max(rowA, rowB); r++)
                    if (board[r * cols + colA] != null) return MatchType.None;
                return MatchType.Vertical;
            }

            if ((rowA - colA) == (rowB - colB))
            {
                for (int r = Math.Min(rowA, rowB) + 1; r < Math.Max(rowA, rowB); r++)
                {
                    int c = r - (rowA - colA);
                    int idx = r * cols + c;
                    if (idx >= 0 && idx < board.Count && board[idx] != null) return MatchType.None;
                }
                return MatchType.MainDiagonal;
            }

            if ((rowA + colA) == (rowB + colB))
            {
                for (int r = Math.Min(rowA, rowB) + 1; r < Math.Max(rowA, rowB); r++)
                {
                    int c = (rowA + colA) - r;
                    int idx = r * cols + c;
                    if (idx >= 0 && idx < board.Count && board[idx] != null) return MatchType.None;
                }
                return MatchType.SecondaryDiagonal;
            }

            return MatchType.None;
        }

        private MatchType GetMatchType(List<GameBoardUnit> listUnit, int rowA, int colA, int rowB, int colB)
        {
            int cols = NUMBER_OF_COLUMNS;

            int indexA = rowA * cols + colA;
            int indexB = rowB * cols + colB;

            // 👉 1. Match cùng hàng (row)
            int start = Mathf.Min(indexA, indexB) + 1;
            int end = Mathf.Max(indexA, indexB) - 1;
            bool isBlocked = false;

            for (int i = start; i <= end; i++)
            {
                if (listUnit[i].IsOccupied())
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
                int startRow = Mathf.Min(rowA, rowB) + 1;
                int endRow = Mathf.Max(rowA, rowB) - 1;
                isBlocked = false;

                for (int r = startRow; r <= endRow; r++)
                {
                    int betweenIndex = r * cols + colA;
                    if (listUnit[betweenIndex].IsOccupied())
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
                int startRow = Mathf.Min(rowA, rowB) + 1;
                int endRow = Mathf.Max(rowA, rowB) - 1;
                isBlocked = false;

                for (int r = startRow; r <= endRow; r++)
                {
                    int c = r - (rowA - colA);
                    int betweenIndex = r * cols + c;
                    if (listUnit[betweenIndex].IsOccupied())
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
                int startRow = Mathf.Min(rowA, rowB) + 1;
                int endRow = Mathf.Max(rowA, rowB) - 1;
                isBlocked = false;

                for (int r = startRow; r <= endRow; r++)
                {
                    int c = (rowA + colA) - r;
                    int betweenIndex = r * cols + c;
                    if (listUnit[betweenIndex].IsOccupied())
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
                        if (col < cols - 1) indexB = row * cols + (col + 1); break;
                    case MatchType.Vertical:
                        if (row < rows - 1) indexB = (row + 1) * cols + col; break;
                    case MatchType.MainDiagonal:
                        if (row < rows - 1 && col < cols - 1) indexB = (row + 1) * cols + (col + 1); break;
                    case MatchType.SecondaryDiagonal:
                        if (row < rows - 1 && col > 0) indexB = (row + 1) * cols + (col - 1); break;
                }

                if (indexB >= 0 && indexB < listUnitOnBoardLength && !usedIndices.Contains(indexA) && !usedIndices.Contains(indexB))
                    return (indexA, indexB);
            }

            return null;
        }

        private void HandleValidMatch()
        {
            foreach (var matchedUnit in selectedUnitList)
            {
                matchedUnit.CurrentState = GameBoardUnitState.Matched;
            }
            selectedUnitList.Clear();

            AddScore(1);

            CheckAndClearMatchedRows();
        }

        private void TryAdvanceStageIfCleared()
        {
            if (allOccupiedUnitList.Count > 0)
            {
                Debug.Log($"CheckClearStage: {allOccupiedUnitList.Count}");

                return;
            }

            //CurrentScore++;
            CurrentStageNumber++;
            GenerateRandomBoard();
        }

        private void ShiftRowsBelowUp(List<int> rowsToClear)
        {
            int totalRows = Mathf.CeilToInt((float)allOccupiedUnitList.Count / NUMBER_OF_COLUMNS);
            Debug.Log($"📦 Before shift: Occupied count = {allOccupiedUnitList.Count}");

            rowsToClear.Sort();
            Debug.Log("📌 Rows to clear (sorted): " + string.Join(",", rowsToClear));

            for (int i = 0; i < rowsToClear.Count; i++)
            {
                int clearedRow = rowsToClear[i];

                for (int row = clearedRow; row < totalRows - 1; row++)
                {
                    for (int col = 0; col < NUMBER_OF_COLUMNS; col++)
                    {
                        int currentIndex = row * NUMBER_OF_COLUMNS + col;
                        int belowIndex = (row + 1) * NUMBER_OF_COLUMNS + col;

                        if (currentIndex >= allUnitList.Count || belowIndex >= allUnitList.Count)
                        {
                            Debug.LogWarning($"⛔ Out of bounds: currentIndex={currentIndex}, belowIndex={belowIndex}");
                            continue;
                        }

                        var unitAbove = allUnitList[currentIndex];
                        var unitBelow = allUnitList[belowIndex];

                        allUnitList[currentIndex] = unitBelow;
                        allUnitList[belowIndex] = unitAbove;

                        if (belowIndex < allOccupiedUnitList.Count)
                        {
                            allOccupiedUnitList[currentIndex] = unitBelow;
                            allOccupiedUnitList[belowIndex] = unitAbove;
                        }
                        else
                        {
                            allOccupiedUnitList[currentIndex] = null;
                            allOccupiedUnitList.Add(unitAbove);
                        }

                        unitBelow.MoveToIndex(currentIndex);
                        unitAbove.MoveToIndex(belowIndex);

                        Debug.Log($"↕ Shift: ({row},{col}) ⇅ ({row + 1},{col}) → swap {unitAbove.name} <-> {unitBelow.name}");
                    }
                }

                // Cập nhật lại các row sau mỗi shift
                for (int j = 0; j < rowsToClear.Count; j++)
                {
                    if (rowsToClear[j] > clearedRow)
                        rowsToClear[j]--;
                }
            }

            Debug.Log($"📦 After shift: Occupied count = {allOccupiedUnitList.Count}");

            // Clear row phía cuối
            for (int i = 0; i < rowsToClear.Count; i++)
            {
                int rowIndex = totalRows - 1 - i;
                int rowStartIndex = rowIndex * NUMBER_OF_COLUMNS;

                Debug.Log($"🧹 Clearing bottom row {rowIndex}");

                for (int col = 0; col < NUMBER_OF_COLUMNS; col++)
                {
                    int index = rowStartIndex + col;
                    if (index >= allUnitList.Count) continue;

                    var unit = allUnitList[index];
                    unit.ResetState();

                    int foundIndex = allOccupiedUnitList.IndexOf(unit);
                    if (foundIndex != -1)
                    {
                        allOccupiedUnitList[foundIndex] = null;
                        //Debug.Log($"🔻 Removed unit at index {foundIndex} in occupied list: {unit.name}");
                    }
                }
            }

            Debug.Log($"🧮 After null-ing: {allOccupiedUnitList.Count}");

            allOccupiedUnitList = allOccupiedUnitList.Where(u => u != null).ToList();

            Debug.Log($"✅ Final occupied count after cleanup: {allOccupiedUnitList.Count}");
        }

        // số dòng thêm để scroll
        private void SpawnEmptyGrid()
        {
            int boardMinimalNumberOfUnits = CalculateMinimalBoardGridLength();
            Debug.Log("Min board size based on viewport: " + boardMinimalNumberOfUnits / NUMBER_OF_COLUMNS);
            for (int i = 0; i < boardMinimalNumberOfUnits; i++)
            {
                var unit = SpawnUnit();
                allUnitList.Add(unit);
            }
        }

        private void Start()
        {
            StartCoroutine(InitializeBoardRoutine());
        }

        private bool WouldCreateExtraMatch(List<GameBoardUnitType?> board, int index, GameBoardUnitType newType)
        {
            int cols = NUMBER_OF_COLUMNS;
            int row = index / cols;
            int col = index % cols;

            for (int i = 0; i < board.Count; i++)
            {
                if (i == index || board[i] == null) continue;
                var otherType = board[i].Value;

                if (!AreUnitTypeValuesMatchable(newType, otherType)) continue;

                int otherRow = i / cols;
                int otherCol = i % cols;

                if (GetMatchType(board, row, col, otherRow, otherCol) != MatchType.None)
                    return true;
            }

            return false;
        }
    }
}