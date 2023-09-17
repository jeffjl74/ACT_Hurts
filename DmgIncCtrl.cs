using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ACT_Hurts
{
    public partial class DmgIncCtrl : UserControl
    {
        BindingSource DmgBindingSource;
        DmgInfoList DmgInfos { get; set; } = new DmgInfoList();
        public string Player { get; set; }
        public string Spell { get; set; }
        public long MaxGreen { get; set; }
        public long MaxYellow { get; set; }


        public DmgIncCtrl()
        {
            InitializeComponent();
            ConstructorStuff();
        }

        public DmgIncCtrl(PlayerSpell ps)
        {
            InitializeComponent();

            labelPlayer.Text = Player = ps.Player;
            labelSpell.Text = Spell = ps.Spell;
            MaxYellow = ps.MaxYellow;
            MaxGreen = ps.MaxGreen;
            ConstructorStuff();
        }

        public void ConstructorStuff()
        {
            DmgBindingSource = new BindingSource(DmgInfos.DmgList, "");
            dataGridView1.DataSource = DmgBindingSource;

            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.None;
            dataGridView1.RowTemplate.Height = 16;

            dataGridView1.Columns["DmgTime"].DefaultCellStyle.Format = "T";
            dataGridView1.Columns["DmgTime"].Width = 80;
            dataGridView1.Columns["Swing"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns["Swing"].DefaultCellStyle.Format = "N0";
            dataGridView1.Columns["Landed"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns["Landed"].DefaultCellStyle.Format = "N0";
            dataGridView1.Columns["Warded"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns["Warded"].DefaultCellStyle.Format = "N0";
            dataGridView1.Columns["BT"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns["BT"].DefaultCellStyle.Format = "N0";
        }

        public void AddDmg(DmgInfo dmgInfo)
        {
            DmgBindingSource.Add(dmgInfo);
            dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1;
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Debug.WriteLine("damage data error");
        }

        private void dataGridView1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value != null)
            {
                DataGridView dgv = sender as DataGridView;
                if (dgv.Rows[e.RowIndex].Cells[e.ColumnIndex] != null)
                {
                    if (e.ColumnIndex > 0)
                    {
                        long val = (long)e.Value;
                        if (val <= MaxGreen)
                            e.CellStyle.ForeColor = Color.Lime;
                        else if (val <= MaxYellow)
                            e.CellStyle.ForeColor = Color.Yellow;
                        else
                            e.CellStyle.ForeColor = Color.Red;
                    }
                }
            }
        }

        public bool Matches(object obj)
        {
            DmgIncCtrl other = obj as DmgIncCtrl;
            if(other == null)
                return false;
            return other.Player == this.Player && other.Spell == this.Spell;
        }

        public void ClearDamage()
        {
            DmgBindingSource.Clear();
        }
    }
}
