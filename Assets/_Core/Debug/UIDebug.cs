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

        private void Awake()
        {
            Instance = this;
        }

        // Start is called before the first frame update
        private void Start()
        {
            initBoard.onClick.AddListener(() =>
            {
                GameBoard.Instance.CleanUp();

                StartCoroutine(GameBoard.Instance.InitializeBoardRoutine());
            });

            decreaseStage.onClick.AddListener(() =>
            {
                GameBoard.Instance.CurrentStageNumber--;
            });

            increaseStage.onClick.AddListener(() =>
            {
                GameBoard.Instance.CurrentStageNumber++;
            });
        }

        // Update is called once per frame
        private void Update()
        {
        }
    }
}