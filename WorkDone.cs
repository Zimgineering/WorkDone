using System.Windows.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Helpers;
using ff14bot.Managers;

namespace WorkDone
{
	public class WorkDone : BotPlugin
	{
		public override string Name => "WorkDone";
		public override string Author => "Zimble";
		public override Version Version => new Version(1,0,0,0);
		public override bool WantButton => true;
		public override string ButtonText => "Settings";
		public static WorkDoneSettings settings = WorkDoneSettings.Instance;
		private static Color LogColor = Colors.Wheat;
		private static System.Timers.Timer quitwatch = new System.Timers.Timer();
		public static Dictionary<string, Action> actionDict = new Dictionary<string, Action>();

		public override void OnButtonPress()
		{
			CreateSettingsForm();
		}
		public override void OnEnabled()
		{
			actionDict["Close FFXIV"] = CloseFFXIV;
			actionDict["Logoff PC"] = LogoffPC;
			actionDict["Restart PC"] = RestartPC;
			actionDict["Shutdown PC"] = ShutdownPC;

			Logging.Write(LogColor, $"[{Name}] is enabled and set to {settings.Action} {settings.Delay}m after {settings.BotBase} has stopped");
			quitwatch.AutoReset = false;
			TreeRoot.OnStop += new ff14bot.BotEvent(OnBotStop);
			quitwatch.Elapsed += new System.Timers.ElapsedEventHandler(OnTimedEvent);
		}
		void OnBotStart(BotBase bot)
		{
			Logging.Write(LogColor, $"[{Name}] timer failed because we started again");
			quitwatch.Stop();
		}			
		public override void OnDisabled()
		{
			TreeRoot.OnStart -= OnBotStart;
			TreeRoot.OnStop -= OnBotStop;
		}
		private void OnBotStop(BotBase bot)
		{			
			quitwatch.Interval = settings.Delay * 60000;

			Logging.Write(LogColor, $"[{Name}] {bot.EnglishName} finished or stopped");

			if (bot.Name == settings.BotBase)
			{				
				quitwatch.Start();				
				TreeRoot.OnStart += new ff14bot.BotEvent(OnBotStart);
			}
		}
		public override void OnInitialize()
		{
		}
		void OnTimedEvent (object o, System.Timers.ElapsedEventArgs e)
		{
			Logging.Write(LogColor, $"[{Name}] timer success ({TreeRoot.Current.Name} finished or stopped) going to {settings.Action}");
			ExecuteOrder();
		}
		public void ExecuteOrder()
		{
			if (settings.Alert = true)
			{
				System.Media.SystemSounds.Asterisk.Play();
			}
			actionDict[settings.Action]();

		}
		public void CreateSettingsForm()
		{
			Form1 settingsForm = new Form1();
			settingsForm.ShowDialog();
		}
		public void CloseFFXIV()
		{
			Logging.Write(LogColor, $"[{Name}] Closing FFXIV");
			Process ffxiv = Core.Memory.Process;
			ffxiv.Kill();
		}
		public void LogoffPC()
		{
			Logging.Write(LogColor, $"[{Name}] PC Logoff");
			System.Diagnostics.Process.Start("ShutDown", "/l");
		}
		public void RestartPC()
		{
			Logging.Write(LogColor, $"[{Name}] PC Restart");
			System.Diagnostics.Process.Start("ShutDown", "/r");
		}
		public void ShutdownPC()
		{			
			Logging.Write(LogColor, $"[{Name}] PC Shutdown");
			System.Diagnostics.Process.Start("ShutDown", "/s");
		}
	}
	#region GUI stuff
	public class Form1 : Form
	{
		private FlowLayoutPanel controlPanel = new FlowLayoutPanel();
		private ComboBox actionBox = new ComboBox();
		private ComboBox botbaseBox = new ComboBox();
		private CheckBox alertBox = new CheckBox();
		private NumericUpDown delayBox = new NumericUpDown();
		private Label afterLabel = new Label();
		private Label isdoneLabel = new Label();
		private Dictionary<string, Action> actionDict = WorkDone.actionDict;

		private void SetupLayout()
		{
			controlPanel.Controls.Add(actionBox);
			controlPanel.Controls.Add(delayBox);
			controlPanel.Controls.Add(afterLabel);
			controlPanel.Controls.Add(botbaseBox);
			controlPanel.Controls.Add(isdoneLabel);
			controlPanel.Controls.Add(alertBox);
			controlPanel.AutoSize = true;
			controlPanel.Dock = DockStyle.Top;
			
			Controls.Add(controlPanel);

			Text = "WorkDone";
			Size = new System.Drawing.Size(100, 180);
			MaximizeBox = false;
			MinimizeBox = false;
			FormBorderStyle = FormBorderStyle.FixedDialog;

			actionBox.DataSource = new BindingSource(actionDict, null);
			actionBox.ValueMember = "Key";
			actionBox.DropDownStyle = ComboBoxStyle.DropDownList;
			actionBox.SelectedValue = WorkDone.settings.Action;
			actionBox.SelectionChangeCommitted += new EventHandler(actionBox_SelectionChangeCommitted);

			botbaseBox.DataSource = BotManager.Bots;
			botbaseBox.ValueMember = "Name";
			botbaseBox.DropDownStyle = ComboBoxStyle.DropDownList;
			botbaseBox.SelectedValue = WorkDone.settings.BotBase;
			botbaseBox.SelectionChangeCommitted += new EventHandler(botbaseBox_SelectionChangeCommitted);
			
			alertBox.Text = "Audio alert";
			alertBox.Checked = WorkDone.settings.Alert;
			alertBox.CheckedChanged += new EventHandler(alertBox_CheckedChanged);
			
			delayBox.Value = WorkDone.settings.Delay;
			delayBox.ValueChanged += new EventHandler(delayBox_ValueChanged);
			
			afterLabel.Text = "minutes after";
			afterLabel.AutoSize = true;
			afterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

			isdoneLabel.Text = "is finished.";
			isdoneLabel.AutoSize = true;
			isdoneLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;			
		}
		public Form1()
		{
			Load += new EventHandler(Form1_Load);
			FormClosing += new FormClosingEventHandler(Form1_Unload);
		}
		private void Form1_Load(object sender, EventArgs e)
		{
			SetupLayout();
		}
		private void Form1_Unload(object sender, FormClosingEventArgs e)
		{
		}
		private void actionBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			WorkDone.settings.Action = actionBox.Text;
		}
		private void botbaseBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			WorkDone.settings.BotBase = botbaseBox.Text;
		}
		private void alertBox_CheckedChanged(object sender, EventArgs e)
        {
            WorkDone.settings.Alert = alertBox.Checked;
        }
		private void delayBox_ValueChanged(object sender, EventArgs e)
		{
			WorkDone.settings.Delay = Convert.ToInt32(delayBox.Value); //numericupdown.value returns decimal type
		}
		
	}
	#endregion
	#region Settings
	public class WorkDoneSettings : JsonSettings
	{
		private static WorkDoneSettings _instance;
		private string _action;
		private int _delay;
		private string _botBase;
		private bool _alert;

		public static WorkDoneSettings Instance
		{
			get { return _instance ?? (_instance = new WorkDoneSettings("WorkDoneSettings")); }
		} 

		public WorkDoneSettings(string filename) : base(Path.Combine(CharacterSettingsDirectory, filename + ".json"))
		{
		}

		[Setting]
		[DefaultValue("Close FFXIV")]
		public string Action
		{
			get => _action;
			set { 
					_action = value;
					Save();
				}
		}
		[Setting]
		[DefaultValue(5)]
		public int Delay
		{
			get => _delay;
			set { 
					_delay = value;
					Save();
				}
		}

		[Setting]
		[DefaultValue("Order Bot")]
		public string BotBase
		{
			get => _botBase;
			set { 
					_botBase = value;
					Save();
				}
		}

		[Setting]
		[DefaultValue(false)]
		public bool Alert
		{
			get => _alert;
			set { 
					_alert = value;
					Save();
				}
		}
	}
	#endregion
}
