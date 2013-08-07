using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using QvAffinityConfigurator.QMSAPI;
using Exception = System.Exception;

namespace QvAffinityConfigurator
{
    public partial class Form1 : Form
    {
        readonly IQMS _apiClient = new QMSClient();

        public QDSSettings DistributionServiceSettings;

        public List<ServiceInfo> DistributionServices;

        public Form1()
        {
            InitializeComponent();

            try
            {
                ServiceKeyClientMessageInspector.ServiceKey = _apiClient.GetTimeLimitedServiceKey();

                DistributionServices = _apiClient.GetServices(ServiceTypes.QlikViewDistributionService);

                if (DistributionServices == null) return;

                bindingSource1.DataSource = DistributionServices;
                cboQvdServices.DataSource = bindingSource1.DataSource;
                cboQvdServices.DisplayMember = "Name";
                cboQvdServices.ValueMember = "Name";

                GetQDSSettings();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        public void GetQDSSettings(int qds = 0)
        {
            DistributionServiceSettings = _apiClient.GetQDSSettings(DistributionServices[qds].ID, QDSSettingsScope.Advanced);

            var cpuAffinity = DistributionServiceSettings.Advanced.CPUAffinity.Count();

            foreach (var checkBox in groupAffinity.Controls.OfType<CheckBox>().OrderBy(box => Convert.ToInt32(box.Name.Remove(0, 8))).Reverse().Take(64 - cpuAffinity).Reverse())
                checkBox.Enabled = false;

            var checkBoxList = groupAffinity.Controls.OfType<CheckBox>().OrderBy(box => Convert.ToInt32(box.Name.Remove(0, 8))).Take(cpuAffinity).ToList();

            for (int i = 0; i < cpuAffinity; i++)
                checkBoxList[i].Checked = DistributionServiceSettings.Advanced.CPUAffinity[i];

            switch (DistributionServiceSettings.Advanced.CPUPriority)
            {
                case CPUPriority.High:
                    cpuPriorityHigh.Select();
                    break;
                case CPUPriority.Normal:
                    cpuPriorityNormal.Select();
                    break;
                case CPUPriority.Low:
                    cpuPriorityLow.Select();
                    break;
            }

            txtMaxQvbAdmin.Text = DistributionServiceSettings.Advanced.MaxQvbAdmin.ToString();
            txtMaxQvbDist.Text = DistributionServiceSettings.Advanced.MaxQvbDist.ToString();

            btnUpdate.Enabled = false;
        }

        public void SetQDSSettings(int qds = 0)
        {
            ServiceKeyClientMessageInspector.ServiceKey = _apiClient.GetTimeLimitedServiceKey();

            var cpuAffinity = DistributionServiceSettings.Advanced.CPUAffinity.Count();

            var checkBoxList = groupAffinity.Controls.OfType<CheckBox>().OrderBy(box => Convert.ToInt32(box.Name.Remove(0, 8))).Take(cpuAffinity).ToList();

            for (int i = 0; i < cpuAffinity; i++)
                DistributionServiceSettings.Advanced.CPUAffinity[i] = checkBoxList[i].Checked;

            var checkedButton = groupPriority.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);

            switch (checkedButton.Name)
            {
                case "cpuPriorityHigh":
                    DistributionServiceSettings.Advanced.CPUPriority = CPUPriority.High;
                    break;
                case "cpuPriorityNormal":
                    DistributionServiceSettings.Advanced.CPUPriority = CPUPriority.Normal;
                    break;
                case "cpuPriorityLow":
                    DistributionServiceSettings.Advanced.CPUPriority = CPUPriority.Low;
                    break;
            }

            DistributionServiceSettings.Advanced.MaxQvbAdmin = Convert.ToInt32(txtMaxQvbAdmin.Text);
            DistributionServiceSettings.Advanced.MaxQvbDist = Convert.ToInt32(txtMaxQvbDist.Text);

            var result = _apiClient.SaveQDSSettings(DistributionServiceSettings);

            if (String.IsNullOrEmpty(result))
            {
                MessageBox.Show(result);
            }
            else
            {
                btnUpdate.Text = "Saved";
                btnUpdate.Enabled = false;
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            SetQDSSettings(cboQvdServices.SelectedIndex);
        }

        private void SettingsChanged(object sender, EventArgs e)
        {
            if (!btnUpdate.Enabled)
                btnUpdate.Enabled = true;

            if (btnUpdate.Text != "Update")
                btnUpdate.Text = "Update";
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            GetQDSSettings(cboQvdServices.SelectedIndex);
        }

        private void cboQvdServices_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetQDSSettings(cboQvdServices.SelectedIndex);
        }
    }
}
