using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ACT_Hurts
{
    public class PlayerSorter : IComparer<PlayerSpell>
    {
        string _field;
        SortOrder _order;

        public PlayerSorter(string field, SortOrder order)
        {
            _order = order;
            _field = field;
        }

        public int Compare(PlayerSpell x, PlayerSpell y)
        {
            if (x == null && y == null) return 0;
            if (x != null && y == null) return 1;
            if (x == null && y != null) return -1;

            if (_order == SortOrder.Descending)
            {
                PlayerSpell tmp = x;
                x = y;
                y = tmp;
            }

            switch (_field)
            {
                case "Player":
                    int first = x.Player.CompareTo(y.Player);
                    if (first == 0)
                    {
                        // secondary sort is by Spell, always ascending
                        int second;
                        if (_order == SortOrder.Ascending)
                            second = x.Spell.CompareTo(y.Spell);
                        else
                            second = y.Spell.CompareTo(x.Spell);
                        if (second == 0)
                        {
                            // tertiary mob sort is always ascending
                            if (_order == SortOrder.Ascending)
                                return x.Mob.CompareTo(y.Mob);
                            else
                                return y.Mob.CompareTo(x.Mob);
                        }
                        else
                            return second;
                    }
                    else
                        return first;

                case "Spell":
                    first = x.Spell.CompareTo(y.Spell);
                    if (first == 0)
                    {
                        // secondary sort is by player, always ascending
                        int second;
                        if (_order == SortOrder.Ascending)
                            second = x.Player.CompareTo(y.Player);
                        else
                            second = y.Player.CompareTo(x.Player);
                        if (second == 0)
                        {
                            // tertiary mob sort is always ascending
                            if (_order == SortOrder.Ascending)
                                return x.Mob.CompareTo(y.Mob);
                            else
                                return y.Mob.CompareTo(x.Mob);
                        }
                        else
                            return second;
                    }
                    else
                        return first;

                case "Mob":
                    first = x.Mob.CompareTo(y.Mob);
                    if (first == 0)
                    {
                        // secondary sort is by Player, always ascending
                        int second;
                        if (_order == SortOrder.Ascending)
                            second = x.Player.CompareTo(y.Player);
                        else
                            second = y.Player.CompareTo(x.Player);
                        if (second == 0)
                        {
                            // tertiary spell sort is always ascending
                            if (_order == SortOrder.Ascending)
                                return x.Spell.CompareTo(y.Spell);
                            else
                                return y.Spell.CompareTo(x.Spell);
                        }
                        else
                            return second;
                    }
                    else
                        return first;

                case "MaxGreen":
                    first = x.MaxGreen.CompareTo(y.MaxGreen);
                    if (first == 0)
                    {
                        // secondary sort is by Player, always ascending
                        int second;
                        if (_order == SortOrder.Ascending)
                            second = x.Player.CompareTo(y.Player);
                        else
                            second = y.Player.CompareTo(x.Player);
                        if (second == 0)
                        {
                            // tertiary spell sort is always ascending
                            if (_order == SortOrder.Ascending)
                                return x.Spell.CompareTo(y.Spell);
                            else
                                return y.Spell.CompareTo(x.Spell);
                        }
                        else
                            return second;
                    }
                    else
                        return first;

                case "MaxYellow":
                    first = x.MaxYellow.CompareTo(y.MaxYellow);
                    if (first == 0)
                    {
                        // secondary sort is by Player, always ascending
                        int second;
                        if (_order == SortOrder.Ascending)
                            second = x.Player.CompareTo(y.Player);
                        else
                            second = y.Player.CompareTo(x.Player);
                        if (second == 0)
                        {
                            // tertiary spell sort is always ascending
                            if (_order == SortOrder.Ascending)
                                return x.Spell.CompareTo(y.Spell);
                            else
                                return y.Spell.CompareTo(x.Spell);
                        }
                        else
                            return second;
                    }
                    else
                        return first;


                default:
                    return x.Player.CompareTo(y.Player);
            }
        }
    }
}
