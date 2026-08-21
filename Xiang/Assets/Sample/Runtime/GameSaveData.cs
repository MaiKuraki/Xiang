using System;
using System.Collections.Generic;

namespace Xiang.Sample
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int DayNumber;
        public int TotalScore;
        public string PlayerName;
        public List<PlacedTreeState> Trees = new List<PlacedTreeState>();
    }

    [Serializable]
    public sealed class PlacedTreeState
    {
        public int GridX;
        public int GridY;
        public int Size;
        public int GrowthStage;
    }
}
