using System;
using UnityEngine;
using UnityEngine.UI;

namespace NumMatch
{
    public enum GameBoardUnitType
    {
        One, Two, Three, Four, Five, Six, Seven, Eight, Nine
    }

    public enum GameBoardUnitState
    {
        UnInitialized,
        Initialized,
        Selected,
        MatchedInProgress,
        Matched,
        ClearedFromBoard
    }

    public class GameBoardUnit : MonoBehaviour
    {
        private SOGameBoardUnit m_unitSO;
        public SOGameBoardUnit UnitSO
        { get { return m_unitSO; } set { m_unitSO = value; } }

        private GameBoardUnitState m_currentState;

        public GameBoardUnitState CurrentState
        {
            get
            {
                return m_currentState;
            }
            set
            {
                if (value == m_currentState)
                {
                    return;
                }
                var oldState = m_currentState;
                m_currentState = value;

                switch (m_currentState)
                {
                    case GameBoardUnitState.UnInitialized:
                        button.onClick.RemoveAllListeners();
                        button.enabled = false;
                        break;

                    case GameBoardUnitState.Initialized:
                        button.enabled = true;

                        break;

                    case GameBoardUnitState.Selected:
                        button.enabled = true;

                        break;

                    case GameBoardUnitState.MatchedInProgress:
                        button.enabled = false;

                        break;

                    case GameBoardUnitState.Matched:
                        button.enabled = false;

                        break;

                    case GameBoardUnitState.ClearedFromBoard:
                        button.enabled = false;

                        //CurrentState = GameBoardUnitState.UnInitialized;
                        //button.enabled = true;

                        break;

                    default:
                        break;
                }
                OnCurrentStateChanged?.Invoke(this, new OnCurrentStateChangedEventArgs
                {
                    oldState = oldState,
                    newState = m_currentState,
                });
            }
        }

        public class OnCurrentStateChangedEventArgs : EventArgs
        {
            public GameBoardUnitState oldState;
            public GameBoardUnitState newState;
        }

        public event EventHandler<OnCurrentStateChangedEventArgs> OnCurrentStateChanged;

        private GameBoard board;

        [SerializeField] private Button button;

        private void Awake()
        {
            m_currentState = GameBoardUnitState.UnInitialized;
        }

        public void Initialize(GameBoard board, SOGameBoardUnit unitSO)
        {
            this.board = board;
            this.m_unitSO = unitSO;
            CurrentState = GameBoardUnitState.Initialized;
            button.onClick.AddListener(() =>
            {
                board.HandlePlayerSelectingAUnit(this);
            });
        }

        public void ToggleSelected(bool isSelected)
        {
            if (isSelected)
            {
                CurrentState = GameBoardUnitState.Selected;
            }
            else
            {
                CurrentState = GameBoardUnitState.Initialized;
            }
        }

        public bool IsOccupied()
        {
            return CurrentState != GameBoardUnitState.UnInitialized &&
                CurrentState != GameBoardUnitState.Matched;
        }

        public bool IsUnitialized()
        {
            return CurrentState == GameBoardUnitState.UnInitialized;
        }

        public bool IsMatched()
        {
            return CurrentState == GameBoardUnitState.Matched;
        }

        public void OnBeClearedFromBoard()
        {
            CurrentState = GameBoardUnitState.ClearedFromBoard;
        }

        public void ResetState()
        {
            UnitSO = null;
            CurrentState = GameBoardUnitState.UnInitialized;
        }

        public void MoveToIndex(int index)
        {
            transform.SetSiblingIndex(index);
        }
    }
}