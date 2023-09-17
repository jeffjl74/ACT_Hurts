using System;

namespace ACT_Hurts
{
    public class DmgInfo
    {
        public DateTime DmgTime { get; set; }
        public long Swing { get; set; } = 0;
        public long Landed { get; set; } = 0;
        public long Warded { get; set; } = 0;
        public long BT { get; set; } = 0;

        public DmgInfo(DateTime time, long swing, long landed, long warded, long bleedthrough)
        {
            DmgTime = time;
            Swing = swing;
            Landed = landed;
            Warded = warded;
            BT = bleedthrough;
        }
    }
}
