using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NumMatch
{
    public class UIModeEndless : MonoBehaviour
    {
        [SerializeField] private Button homeButton;

        [SerializeField] private Button settingsButton;

        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI currentStageText;


        [SerializeField] private Button addMoreNumbersButton;
        [SerializeField] private TextMeshProUGUI attemptsLeftDisplayNumber;

        private void Start()
        {
            currentStageText.text = $"Stage: {GameBoard.Instance.CurrentStageNumber}";
            currentScoreText.text = $"{GameBoard.Instance.CurrentScore}";
            attemptsLeftDisplayNumber.text = $"{GameBoard.Instance.AddNumberAttemptsLeft}";
            GameBoard.Instance.OnAddNumberAttemptsLeftChanged += GameBoard_OnAddNumberAttemptsLeftChanged;
            GameBoard.Instance.OnCurrentScoreChanged += GameBoard_OnCurrentScoreChanged;
            GameBoard.Instance.OnCurrentStageNumberChanged += GameBoard_OnCurrentStageNumberChanged;
            homeButton.onClick.AddListener(() =>
            {
                Debug.Log("Load home screen!");
            });

            settingsButton.onClick.AddListener(() =>
            {
                Debug.Log("Popup settings UI!");
            });

            addMoreNumbersButton.onClick.AddListener(() =>
            {
                GameBoard.Instance.OnPlayerClickedAddMoreNumber();
            });
        }

        private void GameBoard_OnCurrentStageNumberChanged(object sender, System.EventArgs e)
        {
            currentStageText.text = $"Stage: {GameBoard.Instance.CurrentStageNumber}";

        }

        private void GameBoard_OnCurrentScoreChanged(object sender, System.EventArgs e)
        {
            currentScoreText.text = $"{GameBoard.Instance.CurrentScore}";

        }

        private void GameBoard_OnAddNumberAttemptsLeftChanged(object sender, System.EventArgs e)
        {
            attemptsLeftDisplayNumber.text = $"{GameBoard.Instance.AddNumberAttemptsLeft}";
        }
    }
}
