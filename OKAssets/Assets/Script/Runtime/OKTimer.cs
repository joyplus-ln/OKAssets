using System.Collections.Generic;
using System.Timers;

namespace OKAssets
{
    public class OKTimer
    {
        Timer tTimer = new Timer(100); //实例化Timer类，设置间隔时间为10000毫秒；
        private static OKTimer _instance = null;

        public static OKTimer Inatance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new OKTimer();
                    _instance.Init();
                }

                return _instance;
            }
        }

        private List<ITicker> tickerList = new List<ITicker>();

        private void Init()
        {
            tTimer.Elapsed += new System.Timers.ElapsedEventHandler(Update);
            tTimer.AutoReset = true;
            tTimer.Enabled = true;
            tTimer.Start();
        }

        private void Update(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            tTimer.Stop();
            for (int i = 0; i < tickerList.Count; i++)
            {
                tickerList[i].Update();
            }

            tTimer.Start();
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
        abstract void Update();
    }
}