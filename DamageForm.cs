using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ACT_Hurts
{
    public partial class DamageForm : Form
    {
        public int MinCtrlHeight { get; set; } = 70;
        public Point FormLoc { get; set; } = new Point();
        public Size FormSize { get; set; } = new Size(560,400);

        public DamageForm()
        {
            InitializeComponent();
        }

        // do not take the focus when the form is shown
        // but we do want topmost
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }
        private const int WS_EX_TOPMOST = 0x00000008;
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TOPMOST;
                return createParams;
            }
        }

        private void DamageForm_Shown(object sender, EventArgs e)
        {
            this.Location = FormLoc;
            this.Size = FormSize;
        }

        public void AddControl(PlayerSpell ps)
        {
            DmgIncCtrl ctrl = new DmgIncCtrl(ps);
            bool found = false;
            foreach(Control c in this.panel1.Controls)
            {
                if(ctrl.Matches(c))
                {
                    found = true;
                    break;
                }
            }
            if(!found)
            {
                this.panel1.Controls.Add(ctrl);
                SizeToFit();
            }
        }

        public void RemoveControl(PlayerSpell ps)
        {
            DmgIncCtrl ctrl = new DmgIncCtrl { Player = ps.Player, Spell = ps.Spell };
            foreach (Control c in this.panel1.Controls)
            {
                if (ctrl.Matches(c))
                {
                    this.panel1.Controls.Remove(c);
                    if (this.panel1.Controls.Count == 0)
                        this.Visible = false;
                    else
                        SizeToFit();
                    break;
                }
            }
        }

        void SizeToFit()
        {
            int controlCount = panel1.Controls.Count;
            if(controlCount > 0)
            {
                DmgIncCtrl standard = new DmgIncCtrl(); //to get default size
                int margin = 10;
                int minControlHeight = panel1.Controls[0].MinimumSize.Height;

                int availWidth = panel1.ClientRectangle.Width - SystemInformation.VerticalScrollBarWidth;
                int availHeight = panel1.ClientRectangle.Height - SystemInformation.HorizontalScrollBarHeight;
                int standardColumns = availWidth / (standard.Width + margin);
                int standardRows = availHeight / (standard.Height + margin);
                if (standardColumns == 0)
                {
                    //panel is narrower than a column
                    if(standardRows == 0)
                    {
                        //and shorter than a row
                        //set height to minimum and just do rows
                        for (int i = 0; i < controlCount; i++)
                        {
                            panel1.Controls[i].Size = new Size(standard.Width, minControlHeight);
                            panel1.Controls[i].Location = new Point(margin, i * (minControlHeight+margin));
                        }

                    }
                    else
                    {
                        //room for row(s)
                        int heightToFit = (availHeight / controlCount) - margin;
                        if (heightToFit < minControlHeight)
                            heightToFit = minControlHeight;
                        for (int i = 0; i < controlCount; i++)
                        {
                            panel1.Controls[i].Size = new Size(standard.Width, heightToFit);
                            panel1.Controls[i].Location = new Point(margin, i * (heightToFit + margin));
                        }
                    }
                }
                else
                {
                    //room for columns
                    if(standardRows == 0)
                    {
                        //shorter than a row
                        //set height to minimum and just do columns
                        for (int i = 0; i < controlCount; i++)
                        {
                            panel1.Controls[i].Size = new Size(standard.Width, minControlHeight);
                            panel1.Controls[i].Location = new Point((i * (standard.Width + margin)) + margin, 0);
                        }
                    }
                    else
                    {
                        //room for rows and columns
                        int rows = controlCount / standardColumns;
                        if (controlCount % standardColumns != 0)
                            rows++;
                        int heightToFit = availHeight / rows;
                        heightToFit -= margin;
                        if (heightToFit < minControlHeight)
                            heightToFit = minControlHeight;
                        for (int i = 0; i < controlCount; i++)
                        {
                            panel1.Controls[i].Size = new Size(standard.Width, heightToFit);
                            panel1.Controls[i].Location = new Point(((i % standardColumns) * (standard.Width + margin)) + margin, (i / standardColumns) * (heightToFit+margin));
                        }
                    }
                }

            }
        }

        private void DamageForm_ResizeEnd(object sender, EventArgs e)
        {
            FormLoc = this.Location;
            FormSize = this.Size;
            SizeToFit();
        }

        public void AddHit(HitData hitData)
        {
            foreach(DmgIncCtrl c in panel1.Controls)
            {
                if(c.Player == hitData.playerSpell.Player && c.Spell == hitData.playerSpell.Spell)
                {
                    c.AddDmg(hitData.dmgInfo);
                    break;
                }
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            foreach (DmgIncCtrl c in panel1.Controls)
            {
                c.ClearDamage();
            }
        }

        private void DamageForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // just hide if the user hits the X button
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void DamageForm_Move(object sender, EventArgs e)
        {
            FormLoc = this.Location;
            FormSize = this.Size;
        }
    }
}
