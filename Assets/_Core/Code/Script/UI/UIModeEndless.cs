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




        [SerializeField] private GameObject gameOverUI;
        [SerializeField] private Button retryButton;
        [SerializeField] private TextMeshProUGUI finalizedScoreText;

        [SerializeField] private List<AudioClip> clickButtonSoundList;

        private void Start()
        {
            currentStageText.text = $"Stage: {GameBoard.Instance.CurrentStageNumber}";
            currentScoreText.text = $"{GameBoard.Instance.CurrentScore}";
            attemptsLeftDisplayNumber.text = $"{GameBoard.Instance.AddNumberAttemptsLeft}";
            GameBoard.Instance.OnAddNumberAttemptsLeftChanged += GameBoard_OnAddNumberAttemptsLeftChanged;
            GameBoard.Instance.OnCurrentScoreChanged += GameBoard_OnCurrentScoreChanged;
            GameBoard.Instance.OnCurrentStageNumberChanged += GameBoard_OnCurrentStageNumberChanged;
            GameBoard.Instance.OnCurrentGameStateChanged += GameBoard_OnCurrentGameStateChanged;
            homeButton.onClick.AddListener(() =>
            {
                Debug.Log("Load home screen!");
                SoundManager.Instance.PlaySoundEffect(clickButtonSoundList);
            });

            settingsButton.onClick.AddListener(() =>
            {
                Debug.Log("Popup settings UI!");
                SoundManager.Instance.PlaySoundEffect(clickButtonSoundList);

            });

            addMoreNumbersButton.onClick.AddListener(() =>
            {
                GameBoard.Instance.HandleAddMoreNumberRequest();
                SoundManager.Instance.PlaySoundEffect(clickButtonSoundList);

            });

            ToggleGameOverUI(false);
            SetUIGameOver();
        }

        private void ToggleGameOverUI(bool doesShow)
        {
            gameOverUI.SetActive(doesShow);
        }

        private void SetUIGameOver()
        {
            finalizedScoreText.text = $"{GameBoard.Instance.CurrentScore}";
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => {
                GameBoard.Instance.RestartGame();
                SoundManager.Instance.PlaySoundEffect(clickButtonSoundList);
            });
        }

        private void GameBoard_OnCurrentGameStateChanged(object sender, System.EventArgs e)
        {
            switch (GameBoard.Instance.CurrentGameState)
            {
                case GameState.UnInitialized:
                    ToggleGameOverUI(false);
                    break;
                case GameState.Idle:
                    ToggleGameOverUI(false);


                    break;
                case GameState.MatchingUnits:
                    ToggleGameOverUI(false);

                    break;
                case GameState.GameOver:
                    ToggleGameOverUI(true);
                    SetUIGameOver();
                    break;
            }
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