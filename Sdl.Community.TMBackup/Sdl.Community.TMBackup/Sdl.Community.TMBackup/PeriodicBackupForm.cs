﻿using Sdl.Community.TMBackup.Helpers;
using System;
using System.Windows.Forms;
using Sdl.Community.TMBackup.Models;

namespace Sdl.Community.TMBackup
{
	public partial class PeriodicBackupForm : Form
	{
		public PeriodicBackupForm()
		{
			InitializeComponent();

			SetDateTimeFormat();

			InitializeFormData();
		}	
		
		private void SetDateTimeFormat()
		{
			timePicker_At.Format = DateTimePickerFormat.Custom;
			timePicker_At.CustomFormat = "HH:mm:ss tt";
			timePicker_At.ShowUpDown = true;
		}

	    private void btn_Close_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void btn_Set_Click(object sender, EventArgs e)
		{
			PeriodicBackupModel periodicBackupModel = new PeriodicBackupModel();
			periodicBackupModel.BackupInterval = int.Parse(txtBox_TimeInterval.Text);
			periodicBackupModel.TimeType = cmbBox_Interval.SelectedItem.ToString();
			periodicBackupModel.FirstBackup = dateTimePicker_FirstBackup.Value;
			periodicBackupModel.BackupAt = timePicker_At.Text;
			periodicBackupModel.IsRunOption = radioBtn_RunOption.Checked;
			periodicBackupModel.IsWaitOption = radioBtn_WaitOption.Checked;

			Persistence persistence = new Persistence();
			persistence.SavePeriodicBackupInfo(periodicBackupModel);

			this.Close();
		}

		private void btn_Now_Click(object sender, EventArgs e)
		{
			dateTimePicker_FirstBackup.Value = DateTime.Now;
			timePicker_At.Text = DateTime.Now.Hour.ToString();
		}

		private void InitializeFormData()
		{
			cmbBox_Interval.DataSource = EnumHelper.GetTimeTypeDescription();

			Persistence persistence = new Persistence();
			var result = persistence.ReadFormInformation();

			if(result != null)
			{
				cmbBox_Interval.SelectedItem = result.PeriodicBackupModel != null ? result.PeriodicBackupModel.TimeType : string.Empty;
				txtBox_TimeInterval.Text = result.PeriodicBackupModel != null ? result.PeriodicBackupModel.BackupInterval.ToString() : string.Empty;
				dateTimePicker_FirstBackup.Value = result.PeriodicBackupModel != null ? result.PeriodicBackupModel.FirstBackup : DateTime.Now;
				timePicker_At.Text = result.PeriodicBackupModel != null ? result.PeriodicBackupModel.BackupAt : string.Empty;
				radioBtn_RunOption.Checked = result.PeriodicBackupModel != null ? result.PeriodicBackupModel.IsRunOption: false;
				radioBtn_WaitOption.Checked = result.PeriodicBackupModel != null ? result.PeriodicBackupModel.IsWaitOption : false;
			}
		}
	}
}