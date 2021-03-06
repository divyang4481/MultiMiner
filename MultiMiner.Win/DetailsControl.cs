﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MultiMiner.Xgminer;
using MultiMiner.Coin.Api;
using MultiMiner.Engine.Configuration;
using MultiMiner.Engine;
using MultiMiner.Xgminer.Api.Responses;
using MultiMiner.Win.Extensions;

namespace MultiMiner.Win
{
    public partial class DetailsControl : MessageBoxFontUserControl
    {
        //events
        //delegate declarations
        public delegate void CloseClickedHandler(object sender);

        //event declarations        
        public event CloseClickedHandler CloseClicked;

        private List<DeviceInformationResponse> deviceInformation;
        private List<DeviceDetailsResponse> deviceDetails;

        public DetailsControl()
        {
            InitializeComponent();
        }

        private void closeApiButton_Click(object sender, EventArgs e)
        {
            if (CloseClicked != null)
                CloseClicked(this);
        }

        private void DetailsControl_Load(object sender, EventArgs e)
        {
            PositionControls();
        }

        private void PositionControls()
        {
            closeDetailsButton.Size = new Size(22, 22);
            const int offset = 2;
            closeDetailsButton.Location = new Point(this.Width - closeDetailsButton.Width - offset, 0 + offset);
            closeDetailsButton.BringToFront();

            workersGridView.Width = this.Width - (workersGridView.Left * 2);
            workersGridView.Height = this.Height - workersGridView.Top - 6;
        }

        public void ClearDetails(int deviceCount)
        {
            deviceCountLabel.Text = deviceCount + " item";
            if (deviceCount > 1)
                deviceCountLabel.Text = deviceCountLabel.Text + "s";
            noDetailsPanel.Dock = DockStyle.Fill;
            noDetailsPanel.BringToFront();
            noDetailsPanel.Visible = true;
        }

        public void InspectDetails(Device device, CoinConfiguration coinConfiguration, CoinInformation coinInformation,
            List<DeviceInformationResponse> deviceInformation, PoolInformationResponse poolInformation,
            List<DeviceDetailsResponse> deviceDetails)
        {
            this.deviceDetails = deviceDetails;
            this.deviceInformation = deviceInformation;


            noDetailsPanel.Visible = false;
            double hashrate = 0;
            foreach (DeviceInformationResponse individualDevice in deviceInformation)
            {
                hashrate += individualDevice.AverageHashrate;
            }
            hashrateLabel.Text = hashrate.ToHashrateString();

            workersGridView.Visible = (device.Kind == DeviceKind.PXY) &&
                (deviceInformation.Count > 0);
            workersTitleLabel.Visible = workersGridView.Visible;

            //Internet or Coin API could be down
            if (coinInformation != null)
            {
            }

            //device may not be configured
            if (coinConfiguration != null)
                cryptoCoinBindingSource.DataSource = coinConfiguration.Coin;
            else
                cryptoCoinBindingSource.DataSource = new CryptoCoin();

            deviceInformationResponseBindingSource.DataSource = deviceInformation;
            
            //may not be hashing yet
            if (deviceDetails != null)
                deviceDetailsResponseBindingSource.DataSource = deviceDetails;
            else
                deviceDetailsResponseBindingSource.DataSource = new DeviceDetailsResponse();

            deviceBindingSource.DataSource = device;

            switch (device.Kind)
            {
                case DeviceKind.CPU:
                    pictureBox1.Image = imageList1.Images[3];
                    break;
                case DeviceKind.GPU:
                    pictureBox1.Image = imageList1.Images[0];
                    break;
                case DeviceKind.USB:
                    pictureBox1.Image = imageList1.Images[1];
                    break;
                case DeviceKind.PXY:
                    pictureBox1.Image = imageList1.Images[2];
                    break;
            }

            nameLabel.Width = this.Width - nameLabel.Left - closeDetailsButton.Width;

            acceptedLabel.Text = deviceInformation.Sum(d => d.AcceptedShares).ToString();
            rejectedLabel.Text = deviceInformation.Sum(d => d.RejectedShares).ToString();
            errorsLabel.Text = deviceInformation.Sum(d => d.HardwareErrors).ToString();
            utilityLabel.Text = deviceInformation.Sum(d => d.Utility).ToString();

            DeviceInformationResponse deviceInfo = (DeviceInformationResponse)deviceInformationResponseBindingSource.Current;
            if (deviceInfo != null)
            {
                if (deviceInfo.Temperature > 0)
                    tempLabel.Text = deviceInfo.Temperature + "°";
                else
                    tempLabel.Text = String.Empty;

                if (deviceInfo.FanPercent > 0)
                    fanLabel.Text = deviceInfo.FanPercent + "%";
                else
                    fanLabel.Text = String.Empty;
            }
            else
            {
                tempLabel.Text = String.Empty;
                fanLabel.Text = String.Empty;
            }

            UpdateColumnVisibility();

            //may not be hashing yet
            if (poolInformation != null)
                poolInformationResponseBindingSource.DataSource = poolInformation;
            else
                poolInformationResponseBindingSource.DataSource = new PoolInformationResponse();
        }

        private void UpdateColumnVisibility()
        {
            bool visible = false;
            foreach (DataGridViewColumn column in workersGridView.Columns)
            {
                visible = false;
                foreach (DataGridViewRow row in workersGridView.Rows)
                {
                    if (!String.IsNullOrEmpty((string)row.Cells[column.Index].FormattedValue))
                    {
                        visible = true;
                        break;
                    }
                }
                column.Visible = visible;
            }
        }

        private void workersGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == averageHashrateDataGridViewTextBoxColumn.Index ||
                e.ColumnIndex == currentHashrateDataGridViewTextBoxColumn.Index)
            {
                e.Value = ((double)e.Value).ToHashrateString();
            }
            else if (e.ColumnIndex == hardwareErrorsPercentDataGridViewTextBoxColumn.Index)
            {
                //check for >= 0.05 so we don't show 0% (due to the format string)
                e.Value = (double)e.Value >= 0.05 ? ((double)e.Value).ToString("0.#") + "%" : String.Empty;
            }
            else if (e.ColumnIndex == acceptedSharesDataGridViewTextBoxColumn.Index)
            {
                e.Value = (int)e.Value > 0 ? ((int)e.Value).ToString() : String.Empty;
            }
        }

        private void workersGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            for (int i = e.RowIndex; i < (e.RowIndex + e.RowCount); i++)
            {
                DeviceInformationResponse deviceInformation = this.deviceInformation[i];
                DeviceDetailsResponse deviceDetails = this.deviceDetails.SingleOrDefault(d => d.Name.Equals(deviceInformation.Name) && (d.Index == deviceInformation.Index));
                if (deviceDetails != null)
                    workersGridView.Rows[i].Cells[workerNameColumn.Index].Value = deviceDetails.DevicePath;
            }
        }
    }
}
