using UnityEngine;
using UnityEngine.UI;

namespace NumMatch
{
    public class GameBoardUnitVisual : MonoBehaviour
    {
        [SerializeField] private GameBoardUnit logic;
        [SerializeField] private GameObject background;
        [SerializeField] private GameObject selected;
        [SerializeField] private GameObject icon;



        private void Awake()
        {
            ToggleIcon(false);
            SetSelected(false);
        }

        private void Start()
        {
            logic.OnCurrentStateChanged += Logic_OnCurrentStateChanged;
        }

        private void Logic_OnCurrentStateChanged(object sender, GameBoardUnit.OnCurrentStateChangedEventArgs e)
        {
            var newState = e.newState;
            switch (newState)
            {
                case GameBoardUnitState.UnInitialized:
                    ToggleIcon(false);
                    ToggleIconMatchedColor(false);

                    SetSelected(false);
                    break;

                case GameBoardUnitState.Initialized:
                    SetIcon(logic.UnitSO.icon);
                    ToggleIcon(true);
                    ToggleIconMatchedColor(false);

                    SetSelected(false);
                    break;

                case GameBoardUnitState.Selected:
                    ToggleIcon(true);
                    ToggleIconMatchedColor(false);
                    SetSelected(true);
                    break;

                case GameBoardUnitState.MatchedInProgress:
                    ToggleIconMatchedColor(false);
                    SetSelected(false);
                    break;

                case GameBoardUnitState.Matched:
                    ToggleIcon(true);
                    ToggleIconMatchedColor(true);
                    SetSelected(false);
                    break;

                case GameBoardUnitState.ClearedFromBoard:
                    ToggleIcon(false);
                    ToggleIconMatchedColor(false);
                    SetSelected(true);

                    break;
            }
        }

        private void SetIcon(Sprite newIcon)
        {
            icon.GetComponent<Image>().sprite = newIcon;
        }

        private void ToggleIcon(bool doesShowIcon)
        {
            icon.SetActive(doesShowIcon);
        }

        private void ToggleIconMatchedColor(bool isMatched)
        {
            icon.GetComponent<Image>().color = isMatched ? Color.gray : Color.black;
        }

        private void SetSelected(bool isSelected)
        {
            selected.SetActive(isSelected);
        }
    }
}