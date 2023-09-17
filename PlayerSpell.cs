using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ACT_Hurts
{
    public class PlayerSpell : IEquatable<PlayerSpell>
    {
        [XmlIgnore]
        public bool Active { get; set; } = false;

        [XmlAttribute]
        public string Player { get; set; } = string.Empty;
        [XmlAttribute]
        public string Spell { get; set; } = string.Empty;
        [XmlAttribute]
        public string Mob { get; set; } = string.Empty;
        [XmlAttribute]
        public long MaxGreen { get; set; } = 100000000;
        [XmlAttribute]
        public long MaxYellow { get; set; } = 1000000000;

        public bool Equals(PlayerSpell other)
        {
            bool result = other.Player == Player && other.Spell == Spell && other.Active == Active;
            return result;
        }

        public override string ToString()
        {
            return $"{Player}:{Spell}";
        }
    }
}
