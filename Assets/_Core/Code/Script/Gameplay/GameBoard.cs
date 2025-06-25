using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public static GameBoard Instance { get; private set; }

        [SerializeField] private Transform contentTransform;
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private SOGameBoardUnitList allUnitSOList;

        private List<GameBoardUnit> selectedUnitList;
        private List<GameBoardUnit> allUnitList;

        private Dictionary<GameBoardUnitType, SOGameBoardUnit> unitDict;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(Instance);
            }
            Instance = this;
            unitDict = allUnitSOList.ToDict();
            selectedUnitList = new List<GameBoardUnit>();
            allUnitList = new List<GameBoardUnit>();
        }

        private void Start()
        {
            StartCoroutine(InitializeBoardRoutine());
        }

        private IEnumerator InitializeBoardRoutine()
        {
            SpawnEmptyGrid();
            yield return new WaitForEndOfFrame();
            SetInitialRandomUnits();
        }

        private void SpawnEmptyGrid()
        {
            for (int i = 0; i < NUMBER_OF_COLUMNS * 15; i++)
            {
                var unit = SpawnUnit();
                allUnitList.Add(unit);
            }
        }

        private void SetInitialRandomUnits()
        {
            for (int i = 0; i < NUMBER_OF_COLUMNS * 5; i++)
            {
                var unitSO = allUnitSOList.GetRandom();
                allUnitList[i].Initialize(this, unitSO);
            }
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

            bool valueMatched = a.UnitSO.type == b.UnitSO.type || (a.UnitSO.value + b.UnitSO.value == 10);
            if (!valueMatched)
            {
                //Debug.Log("❌ Không match về giá trị hoặc type");
                return MatchType.None;
            }

            int indexA = allUnitList.IndexOf(a);
            int indexB = allUnitList.IndexOf(b);

            int rowA = indexA / NUMBER_OF_COLUMNS;
            int colA = indexA % NUMBER_OF_COLUMNS;

            int rowB = indexB / NUMBER_OF_COLUMNS;
            int colB = indexB % NUMBER_OF_COLUMNS;

            //Case on the path from left to right, there is no blocked tile
            bool blocked = false;
            int startIndex = Mathf.Min(indexA, indexB) + 1;
            int endIndex = Mathf.Max(indexA, indexB) - 1;
            for (int betweenUnitIndex = startIndex; betweenUnitIndex <= endIndex; betweenUnitIndex++)
            {
                if (allUnitList[betweenUnitIndex].IsOccupied())
                {
                    blocked = true;
                    break;
                }
            }

            if (!blocked)
            {
                Debug.Log("✅ Match: Cùng hàng và không bị chặn");
                return MatchType.HasClearPathBetween;
            }

            //Case on the same column
            if (colA == colB)
            {
                int startCol = Mathf.Min(rowA, rowB) + 1;
                int endCol = Mathf.Max(rowA, rowB) - 1;
                blocked = false;

                for (int betweenRowIndex = startCol; betweenRowIndex <= endCol; betweenRowIndex++)
                {
                    int betweenUnitIndex = betweenRowIndex * NUMBER_OF_COLUMNS + colA;
                    if (allUnitList[betweenUnitIndex].IsOccupied())
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    Debug.Log("✅ Match: Cùng cột và không bị chặn");
                    return MatchType.Vertical;
                }
            }

            // ✅ Chéo chính (Main Diagonal) → rowA - colA == rowB - colB
            if ((rowA - colA) == (rowB - colB))
            {
                int startRow = Mathf.Min(rowA, rowB) + 1;
                int endRow = Mathf.Max(rowA, rowB) - 1;
                blocked = false;

                for (int betweenRowIndex = startRow; betweenRowIndex <= endRow; betweenRowIndex++)
                {
                    int colOffset = betweenRowIndex - (rowA - colA); // vì row - col là const trên đường chéo chính và trừ đi sẽ ra offset cần để tới column
                    int betweenUnitIndex = betweenRowIndex * NUMBER_OF_COLUMNS + colOffset;

                    if (allUnitList[betweenUnitIndex].IsOccupied())
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    Debug.Log("✅ Match: Main diagonal và không bị chặn");
                    return MatchType.MainDiagonal;
                }
            }

            // ✅ Chéo phụ (Secondary Diagonal) → rowA + colA == rowB + colB
            if ((rowA + colA) == (rowB + colB))
            {
                int startRow = Mathf.Min(rowA, rowB) + 1;
                int endRow = Mathf.Max(rowA, rowB) - 1;
                blocked = false;

                for (int betweenRowIndex = startRow; betweenRowIndex <= endRow; betweenRowIndex++)
                {
                    int ColOffset = (rowA + colA) - betweenRowIndex; // vì row + col là const trên đường chéo phụ và trừ đi sẽ ra offset cần để tới column
                    int betweenUnitIndex = betweenRowIndex * NUMBER_OF_COLUMNS + ColOffset;

                    if (allUnitList[betweenUnitIndex].IsOccupied())
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    Debug.Log("✅ Match: Secondary diagonal và không bị chặn");
                    return MatchType.SecondaryDiagonal;
                }
            }

            return MatchType.None;
        }

        private void HandleValidMatch()
        {
            foreach (var matchedUnit in selectedUnitList)
            {
                matchedUnit.CurrentState = GameBoardUnitState.Matched;
            }
            selectedUnitList.Clear();
        }
    }
}