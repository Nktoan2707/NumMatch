using NumMatch.Algorithm;
using UnityEngine;
using UnityEngine.UI;

namespace NumMatch
{
    public class UIDebug : MonoBehaviour
    {
        public static UIDebug Instance { get; private set; }

        public Button initBoard;
        public Button increaseStage;
        public Button decreaseStage;
        public Button runAlgo;

        public SOGameBoardUnitList allUnitSOList;

        private void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
        private void Start()
        {
            initBoard.onClick.AddListener(() =>
            {
                GameBoard.Instance.RestartGame();
            });

            decreaseStage.onClick.AddListener(() =>
            {
                GameBoard.Instance.CurrentStageNumber--;
            });

            increaseStage.onClick.AddListener(() =>
            {
                GameBoard.Instance.CurrentStageNumber++;
            });

            runAlgo.onClick.AddListener(() =>
            {
                MatchSolver.Run("input.txt", "output.txt", 9, allUnitSOList.ToDict(), GameBoardUnitType.Five);
            });
        }
    }
}