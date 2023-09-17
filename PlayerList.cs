using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ACT_Hurts
{
    [XmlRoot]
    public class PlayerList
    {
        [XmlAttribute]
        public int X { get; set; } = 0;
        [XmlAttribute]
        public int Y { get; set; } = 0;
        [XmlAttribute]
        public int Width { get; set; } = 560;
        [XmlAttribute]
        public int Height { get; set; } = 400;

        public List<PlayerSpell> Players { get; set; } = new List<PlayerSpell>();

        public bool ContainsPlayer(string player)
        {
            PlayerSpell ps = Players.FirstOrDefault(x => x.Player == player);
            return ps != null;
        }
    }
}
