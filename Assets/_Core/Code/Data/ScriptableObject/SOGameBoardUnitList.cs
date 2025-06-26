using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NumMatch
{
    [CreateAssetMenu(menuName = "ScriptableObject/SOGameBoardUnitList", fileName = "SOGameBoardUnitList")]
    public class SOGameBoardUnitList : ScriptableObject
    {
        public List<SOGameBoardUnit> unitList;

        public SOGameBoardUnit GetByType(GameBoardUnitType type)
        {
            return unitList.Find(u => u.type == type);
        }

        public Dictionary<GameBoardUnitType, SOGameBoardUnit> ToDict()
        {
            return unitList.ToDictionary(u => u.type);
        }

        public SOGameBoardUnit GetRandom()
        {
            //return unitList[0];
            return unitList[Random.Range(0, unitList.Count)];
        }
    }
}