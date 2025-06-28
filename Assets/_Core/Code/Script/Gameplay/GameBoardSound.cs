using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NumMatch
{
    public class GameBoardSound : MonoBehaviour
    {
        [SerializeField] private List<AudioClip> selectANumberSoundList;
        [SerializeField] private List<AudioClip> pairMatchedSoundList;
        [SerializeField] private List<AudioClip> rowClearedSoundList;

        private void Start()
        {
            GameBoard.Instance.OnPlayerClickedAUnit += GameBoard_OnPlayerClickedAUnit; ;
            GameBoard.Instance.OnAPairMatched += GameBoard_OnAPairMatched; ;
            GameBoard.Instance.OnARowCleared += GameBoard_OnARowCleared; ;
        }

        private void GameBoard_OnARowCleared(object sender, System.EventArgs e)
        {
            SoundManager.Instance.PlaySoundEffect(rowClearedSoundList);
        }

        private void GameBoard_OnAPairMatched(object sender, System.EventArgs e)
        {
            SoundManager.Instance.PlaySoundEffect(pairMatchedSoundList);
        }

        private void GameBoard_OnPlayerClickedAUnit(object sender, System.EventArgs e)
        {
            SoundManager.Instance.PlaySoundEffect(selectANumberSoundList);
        }
    }
}