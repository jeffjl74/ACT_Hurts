using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Linq;

[assembly: AssemblyTitle("Incoming Damage View Plugin")]
[assembly: AssemblyDescription("Tracks incoming damage of a particular spell name")]
[assembly: AssemblyCompany("Mineeme")]
[assembly: AssemblyVersion("1.0.1.0")]

namespace ACT_Hurts
{
    public partial class Hurts : UserControl, IActPluginV1
	{

        Label lblStatus;    // The status label that appears in ACT's Plugin tab

        // player data
        BindingSource PlayersBindingSource;
        PlayerList playerList = new PlayerList();
        readonly PlayerList activeList = new PlayerList();
        string sortedColumnName = "Player";         // initial sort on startup
        SortOrder sortOrder = SortOrder.Ascending;

        // damage collection
        int firstAbsorption = -1;
        int lastAbsorption = -1;
        long totalAbsorption = 0;
        long totalBleedthrough = 0;
        string lastWarded = string.Empty;
        readonly Regex reBleedthrough = new Regex(@"^\((?<bleed>\d+) BT\)", RegexOptions.Compiled);

        // damage display
        DamageForm damageForm;

        // data persistence
        readonly string settingsFile = Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "Config\\Hurts.config.xml");
		XmlSerializer xmlSerializer;

        // UI thread support
        readonly WindowsFormsSynchronizationContext mUiContext = new WindowsFormsSynchronizationContext();

        readonly string helpUrl = "https://github.com/jeffjl74/ACT_Hurts#Spell-Damage-Tracker-Plugin-for-Advanced-Combat-Tracker";

        public Hurts()
		{
			InitializeComponent();
		}


		#region IActPluginV1 Interface
		
		public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
		{
			lblStatus = pluginStatusText;	        // save the status label's reference to our local var
			pluginScreenSpace.Controls.Add(this);	// Add this UserControl to the tab ACT provides
			this.Dock = DockStyle.Fill;             // Expand the UserControl to fill the tab's client space

            PlayersBindingSource = new BindingSource(playerList.Players, "");
            dataGridView1.DataSource = PlayersBindingSource;

            xmlSerializer = new XmlSerializer(typeof(PlayerList));
			LoadSettings();
            InitGridView();

            damageForm = new DamageForm
            {
                Visible = false,
                Size = new Size(playerList.Width, playerList.Height),
                Location = new Point(playerList.X, playerList.Y)
            };

            // Create some sort of parsing event handler.  After the "+=" hit TAB twice and the code will be generated for you.
            ActGlobals.oFormActMain.AfterCombatAction += OFormActMain_AfterCombatAction;

            if (ActGlobals.oFormActMain.GetAutomaticUpdatesAllowed())
            {
                // If ACT is set to automatically check for updates, check for updates to the plugin
                // If we don't put this on a separate thread, web latency will delay the plugin init phase
                new Thread(new ThreadStart(oFormActMain_UpdateCheckClicked)).Start();
            }

            lblStatus.Text = "Plugin Started";
		}

        public void DeInitPlugin()
		{
			// Unsubscribe from any events you listen to when exiting!
            ActGlobals.oFormActMain.AfterCombatAction -= OFormActMain_AfterCombatAction;

            SaveSettings();
			lblStatus.Text = "Plugin Exited";
		}

        #endregion

        private void OFormActMain_AfterCombatAction(bool isImport, CombatActionEventArgs actionInfo)
        {
            if (activeList.Players.Count > 0)
            {
                if (actionInfo.swingType == (int)SwingTypeEnum.Melee
                || actionInfo.swingType == (int)SwingTypeEnum.NonMelee)
                {
                    PlayerSpell swingSpell = new PlayerSpell { Player = actionInfo.victim, Spell = actionInfo.theAttackType, Active = true };
                    PlayerSpell ps = activeList.Players.FirstOrDefault(x => x.Equals(swingSpell));
                    if (ps != null)
                    {
                        if (lastAbsorption == -1
                        || (actionInfo.timeSorter == lastAbsorption + 1 && actionInfo.victim == lastWarded))
                        {
                            //Debug.WriteLine($"{actionInfo.time.ToString("T")} swing:{actionInfo.damage.Number} landed:{actionInfo.damage.Number - totalAbsorption} warded:{totalAbsorption} bleed:{totalBleedthrough}");
                            DmgInfo di = new DmgInfo(actionInfo.time, actionInfo.damage.Number, actionInfo.damage.Number - totalAbsorption, totalAbsorption, totalBleedthrough);
                            HitData hd = new HitData { playerSpell = ps, dmgInfo = di };
                            mUiContext.Post(UiAddDmg, hd);
                        }
                    }
                    firstAbsorption = lastAbsorption = -1;
                    totalAbsorption = totalBleedthrough = 0;
                    lastWarded = string.Empty;
                }
                else if (actionInfo.swingType == (int)SwingTypeEnum.Healing)
                {
                    if (actionInfo.theDamageType == "Absorption" && activeList.ContainsPlayer(actionInfo.victim))
                    {
                        lastWarded = actionInfo.victim;
                        if (firstAbsorption == -1)
                        {
                            firstAbsorption = lastAbsorption = actionInfo.timeSorter;
                            totalAbsorption = actionInfo.damage;
                            Match match = reBleedthrough.Match(actionInfo.special);
                            if (match.Success)
                                totalBleedthrough = Int64.Parse(match.Groups["bleed"].Value);
                        }
                        else if (actionInfo.timeSorter == lastAbsorption + 1)
                        {
                            lastAbsorption = actionInfo.timeSorter;
                            totalAbsorption += actionInfo.damage;
                            Match match = reBleedthrough.Match(actionInfo.special);
                            if (match.Success)
                                totalBleedthrough += Int64.Parse(match.Groups["bleed"].Value);
                        }
                        //Debug.WriteLine($"warded:{actionInfo.combatAction.Damage} {actionInfo.combatAction.Special}");
                    }
                    else
                    {
                        firstAbsorption = lastAbsorption = -1;
                        totalAbsorption = totalBleedthrough = 0;
                        lastWarded = string.Empty;
                    }
                }
                else
                {
                    firstAbsorption = lastAbsorption = -1;
                    totalAbsorption = totalBleedthrough = 0;
                }
            }

        }

        void oFormActMain_UpdateCheckClicked()
        {
            try
            {
                Version localVersion = this.GetType().Assembly.GetName().Version;
                Task<Version> vtask = Task.Run(() => { return GetRemoteVersionAsync(); });
                vtask.Wait();
                if (vtask.Result > localVersion)
                {
                    Rectangle screen = Screen.GetWorkingArea(ActGlobals.oFormActMain);
                    DialogResult result = SimpleMessageBox.Show(new Point(screen.Width / 2 - 100, screen.Height / 2 - 100),
                          @"There is an update for Hurts."
                        + @"\line Update it now?"
                        + @"\line (If there is an update to ACT"
                        + @"\line you should click No and update ACT first.)"
                        + @"\line\line Release notes at project website:"
                        + @"{\line\ql " + helpUrl +"}"
                        , "Hurts Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Task<FileInfo> ftask = Task.Run(() => { return GetRemoteFileAsync(); });
                        ftask.Wait();
                        if (ftask.Result != null)
                        {
                            ActPluginData pluginData = ActGlobals.oFormActMain.PluginGetSelfData(this);
                            pluginData.pluginFile.Delete();
                            File.Move(ftask.Result.FullName, pluginData.pluginFile.FullName);
                            Application.DoEvents();
                            ThreadInvokes.CheckboxSetChecked(ActGlobals.oFormActMain, pluginData.cbEnabled, false);
                            Application.DoEvents();
                            ThreadInvokes.CheckboxSetChecked(ActGlobals.oFormActMain, pluginData.cbEnabled, true);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteExceptionLog(ex, "RoR Parcels Plugin Update Download:" + ex.Message);
            }
        }

        private async Task<Version> GetRemoteVersionAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    ProductInfoHeaderValue hdr = new ProductInfoHeaderValue("ACT_Hurts", "1");
                    client.DefaultRequestHeaders.UserAgent.Add(hdr);
                    HttpResponseMessage response = await client.GetAsync(@"https://api.github.com/repos/jeffjl74/ACT_Hurts/releases/latest");
                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Regex reVer = new Regex(@".tag_name.:.v([^""]+)""");
                        Match match = reVer.Match(responseBody);
                        if (match.Success)
                            return new Version(match.Groups[1].Value);
                    }
                    return new Version("0.0.0");
                }
            }
            catch { return new Version("0.0.0"); }
        }

        private async Task<FileInfo> GetRemoteFileAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    ProductInfoHeaderValue hdr = new ProductInfoHeaderValue("ACT_Hurts", "1");
                    client.DefaultRequestHeaders.UserAgent.Add(hdr);
                    HttpResponseMessage response = await client.GetAsync(@"https://github.com/jeffjl74/ACT_Hurts/releases/latest/download/Hurts.dll");
                    if (response.IsSuccessStatusCode)
                    {
                        string tmp = Path.GetTempFileName();
                        var stream = await response.Content.ReadAsStreamAsync();
                        var fileStream = new FileStream(tmp, FileMode.Create);
                        await stream.CopyToAsync(fileStream);
                        fileStream.Close();
                        Application.DoEvents();
                        FileInfo fi = new FileInfo(tmp);
                        return fi;
                    }
                }
                return null;
            }
            catch { return null; }
        }

        void LoadSettings()
		{

            if (File.Exists(settingsFile))
			{
				try
				{
					using (FileStream fs = new FileStream(settingsFile, FileMode.Open))
					{
						var v = (PlayerList)xmlSerializer.Deserialize(fs);
                        if(v != null)
                        {
                            playerList = v;
                            sortOrder = SortOrder.Ascending;
                            PlayerSorter playerSorter = new PlayerSorter(sortedColumnName, sortOrder);
                            playerList.Players.Sort(playerSorter);
                            dataGridView1.DataSource = null;
                            PlayersBindingSource = new BindingSource(playerList.Players, "");
                            dataGridView1.DataSource = PlayersBindingSource;
                        }
                    }
                }
				catch (Exception ex)
				{
					lblStatus.Text = "Error loading settings: " + ex.Message;
                }
            }
        }

        void SaveSettings()
		{
			using (TextWriter writer = new StreamWriter(settingsFile))
			{
                if(damageForm != null)
                {
                    playerList.X = damageForm.FormLoc.X;
                    playerList.Y = damageForm.FormLoc.Y;
                    playerList.Height = damageForm.FormSize.Height;
                    playerList.Width = damageForm.FormSize.Width;
                }

                //remove blanks
                //(not sure blanks still happen after implimenting prevention methods)
                Stack<int> removes = new Stack<int>();
                for(int i=0; i<playerList.Players.Count; i++)
                {

                    if (string.IsNullOrEmpty(playerList.Players[i].Player) || string.IsNullOrEmpty(playerList.Players[i].Spell))
                        removes.Push(i);
                }
                while (removes.Count > 0)
                    playerList.Players.RemoveAt(removes.Pop());

                xmlSerializer.Serialize(writer, playerList);
				writer.Close();
			}
		}

        private void InitGridView()
        {
            dataGridView1.Columns["Active"].AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridView1.Columns["Player"].SortMode = DataGridViewColumnSortMode.Programmatic;
            dataGridView1.Columns[sortedColumnName].HeaderCell.SortGlyphDirection = SortOrder.Ascending;
            dataGridView1.Columns["Spell"].SortMode = DataGridViewColumnSortMode.Programmatic;
            dataGridView1.Columns["Mob"].SortMode = DataGridViewColumnSortMode.Programmatic;
            dataGridView1.Columns["MaxGreen"].DefaultCellStyle.Format = "N0";
            dataGridView1.Columns["MaxGreen"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dataGridView1.Columns["MaxYellow"].DefaultCellStyle.Format = "N0";
            dataGridView1.Columns["MaxYellow"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void UiAddDmg(object o)
        {
            if (o is HitData hd && damageForm != null)
            {
                damageForm.AddHit(hd);
            }
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            Debug.WriteLine($"{playerList.Players.Count}");
            SaveSettings();
        }

        private void buttonHelp_Click(object sender, EventArgs e)
        {
            Process.Start(helpUrl);
        }

        private void dataGridView1_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell is DataGridViewCheckBoxCell)
            {
                // this makes the _CellContentClick see the current state rather than the previous state
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
            else
            {
                if(playerList.Players[dataGridView1.CurrentCell.RowIndex].Active)
                {
                    // if modifying an active player, need to remove the control until the new
                    // parameters are done and the user re-checks the Active checkbox
                    playerList.Players[dataGridView1.CurrentCell.RowIndex].Active = false;
                    damageForm.RemoveControl(playerList.Players[dataGridView1.CurrentCell.RowIndex]);
                    this.Focus(); // we lost focus when the damage form rearranged
                    activeList.Players.Remove(playerList.Players[dataGridView1.CurrentCell.RowIndex]);
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns["Active"].Index && e.RowIndex >= 0)
            {
                DataGridViewCheckBoxCell cell = (DataGridViewCheckBoxCell)dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];

                if (cell.Value != null)
                {
                    bool isChecked = (bool)cell.Value;
                    if(isChecked)
                    {
                        if(!damageForm.Visible)
                        {
                            damageForm.FormLoc = new Point(playerList.X, playerList.Y);
                            damageForm.FormSize = new Size(playerList.Width, playerList.Height);
                            damageForm.Show(ActGlobals.oFormActMain);
                        }
                        if (!string.IsNullOrEmpty(playerList.Players[e.RowIndex].Player) && !string.IsNullOrEmpty(playerList.Players[e.RowIndex].Spell))
                        {
                            damageForm.AddControl(playerList.Players[e.RowIndex]);
                            activeList.Players.Add(playerList.Players[e.RowIndex]);
                        }
                    }
                    else
                    {
                        damageForm.RemoveControl(playerList.Players[e.RowIndex]);
                        activeList.Players.Remove(playerList.Players[e.RowIndex]);
                    }
                }
            }
        }

        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            SortOrder newOrder = SortOrder.Ascending;
            if(sortedColumnName == dataGridView1.Columns[e.ColumnIndex].HeaderText)
                newOrder = sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            PlayerSorter playerSorter = new PlayerSorter(dataGridView1.Columns[e.ColumnIndex].HeaderText, newOrder);
            playerList.Players.Sort(playerSorter);
            sortedColumnName = dataGridView1.Columns[e.ColumnIndex].HeaderText;

            PlayersBindingSource.ResetBindings(false);
            dataGridView1.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = newOrder;
            sortOrder = newOrder;
        }

        private void dataGridView1_RowValidating(object sender, DataGridViewCellCancelEventArgs e)
        {
            bool cancel = false;
            DataGridViewRow row = dataGridView1.Rows[e.RowIndex];
            if(!row.IsNewRow)
            {
                DataGridViewCell playerCell = row.Cells[dataGridView1.Columns["Player"].Index];
                playerCell.ErrorText = string.Empty;
                if (playerCell.Value == null || string.IsNullOrEmpty(playerCell.Value.ToString()))
                {
                    playerCell.ErrorText = "Player cannot be empty";
                    cancel = true;
                }
                else if (playerCell.Value.ToString().Trim() != playerCell.Value.ToString())
                {
                    playerCell.ErrorText = "Remove all leading and trailing spaces, then exit the row again";
                    cancel = true;
                }

                DataGridViewCell spellCell = row.Cells[dataGridView1.Columns["Spell"].Index];
                spellCell.ErrorText = string.Empty;
                if (spellCell.Value == null || string.IsNullOrEmpty(spellCell.Value.ToString()))
                {
                    spellCell.ErrorText = "Spell cannot be empty";
                    cancel = true;
                }
                else if (spellCell.Value.ToString().Trim() != spellCell.Value.ToString())
                {
                    spellCell.ErrorText = "Remove all leading and trailing spaces, then exit the row again";
                    cancel = true;
                }
            }
            e.Cancel = cancel;
        }

        private void dataGridView1_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (playerList.Players[e.Row.Index].Active)
            {
                // if removing an active player, need to remove the control
                playerList.Players[e.Row.Index].Active = false;
                damageForm.RemoveControl(playerList.Players[e.Row.Index]);
                activeList.Players.Remove(playerList.Players[e.Row.Index]);
            }
        }
    }


}
