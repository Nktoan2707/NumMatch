using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NumMatch
{
    [CreateAssetMenu(menuName = "ScriptableObject/SOGameBoardUnit", fileName = "SOGameBoardUnit")]
    public class SOGameBoardUnit : ScriptableObject
    {
        public GameBoardUnitType type;
        public Sprite icon;
        public int value;
    }
}
