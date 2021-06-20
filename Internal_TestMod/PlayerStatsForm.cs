using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NinOnline;

namespace NinMods
{
    public partial class PlayerStatsForm : Form
    {
        enum ETrackedStats : int
        {
            Level,
            Experience,
            Ryo,
            Health,
            Mana,
            Strength,
            Fortitude,
            Intelligence,
            Agility,
            Chakra,
            MapID,
            Location,
            FacingDirection,
            LocationOffset,
            MovingBitmask,
            IsRunning,
            StepIndex,
            CanMove,
            DeathTimer,
            WeaponTimer,
            AttackTimer,
            CastTimer,
            MapTimer,
            EventTimer,
            ChargeTimer,
            ProjectileTimer,
            KickbackDirection,
            KickDistance,
            MAX
        }

        SFML.System.Vector2i lastGameWndPosition = SFML.System.Vector2i.Zero;

        delegate void dListView_SetItemText(int itemIndex, int subItemIndex, string text);
        dListView_SetItemText oListView_SetItemText;

        bool IsUpdating = false;

        Dictionary<ETrackedStats, string> curStatValues = new Dictionary<ETrackedStats, string>((int)ETrackedStats.MAX);
        Dictionary<ETrackedStats, string> prevStatValues = new Dictionary<ETrackedStats, string>((int)ETrackedStats.MAX);

        public PlayerStatsForm()
        {
            InitializeComponent();

            EnableDoubleBuffering();

            System.Reflection.MethodInfo methodInfo = typeof(System.Windows.Forms.ListView).GetMethod("SetItemText", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, new Type[] { typeof(int), typeof(int), typeof(string) }, null);
            if (methodInfo == null)
            {
                Logger.Log.WriteError("PlayerStatsForm", "PlayerStatsForm::ctor", "Could not get SetItemText methodinfo");
                return;
            }
            oListView_SetItemText = (dListView_SetItemText)methodInfo.CreateDelegate(typeof(dListView_SetItemText), listviewPlayerStats);
        }

        public void EnableDoubleBuffering()
        {
            // Set the value of the double-buffering style bits to true.
            this.SetStyle(ControlStyles.DoubleBuffer |
               ControlStyles.UserPaint |
               ControlStyles.AllPaintingInWmPaint,
               true);
            this.UpdateStyles();
        }

        public void Reposition_OnDesktop(int x, int y)
        {
            Logger.Log.Write("PlayerStatsForm", "Reposition_OnDesktop", $"Repositioning form to {x}, {y}");
            this.SetDesktopLocation(x, y);
        }

        void PopulateStatValues(client.modTypes.PlayerRec playerRecord, ref Dictionary<ETrackedStats, string> statDict)
        {
            statDict[ETrackedStats.Level] = playerRecord.Level.ToString();
            statDict[ETrackedStats.Experience] = playerRecord.Exp.ToString();
            statDict[ETrackedStats.Ryo] = playerRecord.Ryo.ToString();
            statDict[ETrackedStats.Health] = playerRecord.Vital[(int)client.modEnumerations.Vitals.HP].ToString() + "/" + playerRecord.MaxVital[(int)client.modEnumerations.Vitals.HP].ToString();
            statDict[ETrackedStats.Mana] = playerRecord.Vital[(int)client.modEnumerations.Vitals.MP].ToString() + "/" + playerRecord.MaxVital[(int)client.modEnumerations.Vitals.MP].ToString();
            statDict[ETrackedStats.Strength] = playerRecord.Stat[(int)client.modEnumerations.Stats.Strength].ToString();
            statDict[ETrackedStats.Fortitude] = playerRecord.Stat[(int)client.modEnumerations.Stats.Fortitude].ToString();
            statDict[ETrackedStats.Intelligence] = playerRecord.Stat[(int)client.modEnumerations.Stats.Intellect].ToString();
            statDict[ETrackedStats.Agility] = playerRecord.Stat[(int)client.modEnumerations.Stats.Agility].ToString();
            statDict[ETrackedStats.Chakra] = playerRecord.Stat[(int)client.modEnumerations.Stats.Chakra].ToString();
            statDict[ETrackedStats.MapID] = playerRecord.Map.ToString();
            statDict[ETrackedStats.Location] = playerRecord.X.ToString() + ", " + playerRecord.Y.ToString();
            statDict[ETrackedStats.FacingDirection] = playerRecord.Dir.ToString();
            statDict[ETrackedStats.LocationOffset] = playerRecord.xOffset.ToString() + ", " + playerRecord.yOffset.ToString();
            statDict[ETrackedStats.MovingBitmask] = playerRecord.Moving.ToString();
            statDict[ETrackedStats.IsRunning] = playerRecord.Running.ToString();
            statDict[ETrackedStats.StepIndex] = playerRecord.Step.ToString();
            statDict[ETrackedStats.CanMove] = playerRecord.CanIMove.ToString();
            statDict[ETrackedStats.DeathTimer] = playerRecord.DeathTimer.ToString();
            statDict[ETrackedStats.WeaponTimer] = playerRecord.WeaponAttackTimer.ToString();
            statDict[ETrackedStats.AttackTimer] = playerRecord.AttackTimer.ToString();
            statDict[ETrackedStats.CastTimer] = playerRecord.CastTimer.ToString();
            statDict[ETrackedStats.MapTimer] = playerRecord.MapGetTimer.ToString();
            statDict[ETrackedStats.EventTimer] = playerRecord.EventTimer.ToString();
            statDict[ETrackedStats.ChargeTimer] = playerRecord.ChargeTimer.ToString();
            statDict[ETrackedStats.ProjectileTimer] = playerRecord.ProjectileTimer.ToString();
            statDict[ETrackedStats.KickbackDirection] = playerRecord.KickbackDir.ToString();
            statDict[ETrackedStats.KickDistance] = playerRecord.KickDistance.ToString();
        }

        void UpdateItemLabelsIfNew()
        {
            for (int index = 0; index < (int)ETrackedStats.MAX; index++)
            {
                if (index == (int)ETrackedStats.MAX - 1)
                    IsUpdating = false;
                if (prevStatValues[(ETrackedStats)index] != curStatValues[(ETrackedStats)index])
                    oListView_SetItemText(index, 1, curStatValues[(ETrackedStats)index]);
            }
        }

        public void UpdatePlayerStats(client.modTypes.PlayerRec playerRecord)
        {
            if ((client.modGraphics.GameWindowForm == null) || (oListView_SetItemText == null)) return;
            // doesn't work: client.modGraphics.GameWindowForm.Root.GlobalPosition;
            // does work:
            SFML.System.Vector2i gameWndPosition = client.modGraphics.GameWindowForm.Window.Position;
            //Logger.Log.Write("PlayerStatsForm", "UpdatePlayerStats", $"Saw gamewindow positioned at {gameWndPosition.X}, {gameWndPosition.Y}");
            if ((gameWndPosition.X != lastGameWndPosition.X) || (gameWndPosition.Y != lastGameWndPosition.Y))
                Reposition_OnDesktop(gameWndPosition.X - this.Width, gameWndPosition.Y);
            lastGameWndPosition = gameWndPosition;

            //Logger.Log.Write("PlayerStatsForm", "UpdatePlayerStats", "Updating player stats");
            lblPlayerName.Text = $"{playerRecord.Name} ({playerRecord.UUID})";
            // i hate this
            //listviewPlayerStats.Visible = false;
            PopulateStatValues(playerRecord, ref curStatValues);

            IsUpdating = true;
            listviewPlayerStats.BeginUpdate();
            UpdateItemLabelsIfNew();
            listviewPlayerStats.EndUpdate();
            //listviewPlayerStats.Visible = true;

            PopulateStatValues(playerRecord, ref prevStatValues);
        }

        private void listviewPlayerStats_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (IsUpdating) return;

            TextFormatFlags flags = TextFormatFlags.Left;
            switch (e.Header.TextAlign)
            {
                case HorizontalAlignment.Center:
                    flags = TextFormatFlags.HorizontalCenter;
                    break;
                case HorizontalAlignment.Right:
                    flags = TextFormatFlags.Right;
                    break;
            }
            e.DrawText(flags);
        }

        private void listviewPlayerStats_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (IsUpdating) return;

            TextFormatFlags flags = TextFormatFlags.Left;
            e.DrawText(flags);
        }
    }
}
