using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
            //print("state logic changed: " + newState);
            switch (newState)
            {
                case GameBoardUnitState.UnInitialized:
                    ToggleIcon(false);
                    SetSelected(false);
                    break;
                case GameBoardUnitState.Initialized:
                    SetIcon(logic.UnitSO.icon);
                    ToggleIcon(true);
                    SetSelected(false);
                    break;
                case GameBoardUnitState.Selected:
                    ToggleIcon(true);
                    SetSelected(true);
                    break;
                case GameBoardUnitState.MatchedInProgress:
                    SetSelected(false);
                    break;
                case GameBoardUnitState.Matched:
                    ToggleIcon(true);
                    icon.GetComponent<Image>().color = Color.gray;
                    SetSelected(false);
                    break;
                case GameBoardUnitState.ClearedFromBoard:
                    SetSelected(true);

                    break;
            }
        }

        private void SetIcon(Sprite newIcon)
        {
            //print(icon.GetComponent<Image>().sprite + " ---- " + newIcon);
            icon.GetComponent<Image>().sprite = newIcon;
        }

        private void ToggleIcon(bool doesShowIcon)
        {
            icon.SetActive(doesShowIcon);
        }

        private void SetSelected(bool isSelected)
        {
            selected.SetActive(isSelected);
        }


    }
}
