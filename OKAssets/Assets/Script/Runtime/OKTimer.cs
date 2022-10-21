using System.Collections.Generic;
using UnityEngine;

namespace OKAssets
{
    public class OKTimer
    {
        private static OKTimer _instance = null;

        public static OKTimer Inatance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new OKTimer();

                }

                return _instance;
            }
        }

        private List<ITicker> tickerList = new List<ITicker>();



        internal void Update()
        {
            for (int i = 0; i < tickerList.Count; i++)
            {
                tickerList[i].OnUpdate();
            }
        }

        public void Add(ITicker _ticker)
        {
            tickerList.Add(_ticker);
        }

        public void Remove(ITicker _ticker)
        {
            tickerList.Remove(_ticker);
        }
    }

    public interface ITicker
    {
        abstract void OnUpdate();
    }
}