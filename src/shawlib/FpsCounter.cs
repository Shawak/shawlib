using System.Collections.Generic;
using System.Linq;

namespace ShawLib
{
    public class FpsCounter
    {
        /* Use like this:
         * DateTime lastTime = DateTime.Now;
         * [...]
         * var delta = (float)(DateTime.Now - lastTime).TotalMilliseconds / 1000;
         * fpsCounter.Update(delta);
         * lastTime = DateTime.Now;
         */

        public float CurrentFPS { get; private set; }
        public float AverageFPS { get; private set; }
        public float Frametime { get; private set; }

        float lastUpdate;
        Queue<float> buffer;

        public FpsCounter()
        {
            CurrentFPS = 0;
            AverageFPS = 0;
            lastUpdate = 1;
            buffer = new Queue<float>();
        }

        public void Update(float dt)
        {
            CurrentFPS = 1.0f / dt;
            buffer.Enqueue(CurrentFPS);

            if (buffer.Count > 50)
                buffer.Dequeue();

            if (lastUpdate >= 1)
            {
                lastUpdate = 0;
                AverageFPS = buffer.Average();
                Frametime = 1.0f / AverageFPS;
            }
            else
            {
                lastUpdate += dt;
            }
        }
    }
}
