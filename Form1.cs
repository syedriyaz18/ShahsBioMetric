using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using DGVPrinterHelper;
namespace ShahsBioMetric
{
    public partial class Form1 : Form
    {
        string constring = (@"server=localhost;uid=root;pwd=rootL0ck3d;database=attendance;");
        MySqlConnection conn = new MySqlConnection(@"server=localhost;uid=root;pwd=rootL0ck3d;database=attendance;");
        
        private Timer timer1;

        DeviceManipulator manipulator = new DeviceManipulator();
        public ZkemClient objZkeeper;
        private bool isDeviceConnected = false;

        string lastrecord = DateTime.Today.Date.ToString("M/d/yyyy");
        string checkdate = DateTime.Today.Date.ToString("yyyy-MM-dd");

        int staffTotal = 0, staffPresent = 0, staffAbsent = 0;
        
        public Form1()
        {
            conn.Open();
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            ShowStatusBar(string.Empty, true);

   
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            int w = Width >= screen.Width ? screen.Width : (screen.Width + Width) / 2;
            int h = Height >= screen.Height ? screen.Height : (screen.Height + Height) / 2;
            this.Location = new Point((screen.Width - w) / 2, (screen.Height - h) / 2);
            this.Size = new Size(w, h);
            tabControl1.Size = new Size(w,h);

            DisplayEmpty();
            fillemp_OD();
            fillemp_CL();
          
            Fill_EmpData();
            fillph();
            fillemp_Advances();
            fillemp_HalfDays();
            //this.BindDataGridView();
            //conn.Open();
            using (var cmd = new MySqlCommand("SELECT count(distinct(employee_id)) AS empid FROM employees_nimra order by employee_code", conn))
            {
                using (var reader = cmd.ExecuteReader())
                {
                  
                    while (reader.Read())
                            {
                        TotalStaff_TB.Text = (reader["empid"].ToString());
                    }
                    //reader.Close();
                }
                staffTotal = int.Parse(TotalStaff_TB.Text);

            }
            //conn.Close();


            //DateTimePicker = new DateTimePicker();
            Month_SS_dtp.Format = DateTimePickerFormat.Custom;
            Month_SS_dtp.CustomFormat = "yyyy-MM";
            Month_SS_dtp.ShowUpDown = true; // to prevent the calendar from being displayed

            Date_OD_dtp.Format = DateTimePickerFormat.Custom;
            Date_OD_dtp.CustomFormat = "yyyy-MM";
            Date_OD_dtp.ShowUpDown = true; // to prevent the calendar from being displayed

            Date_CL_dtp.Format = DateTimePickerFormat.Custom;
            Date_CL_dtp.CustomFormat = "yyyy-MM";
            Date_CL_dtp.ShowUpDown = true; // to prevent the calendar from being displayed


            Halfday_dtp.Format = DateTimePickerFormat.Custom;
            Halfday_dtp.CustomFormat = "yyyy-MM";
            Halfday_dtp.ShowUpDown = true; // to prevent the calendar from being displayed

        }


        /// <summary>111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
        /// Code for HomePage Started Here
        /// </summary>
        public void InitTimer()
        {
            timer1 = new Timer();
            timer1.Tick += new EventHandler(timer1_Tick);

            timer1.Interval = 20000; // in miliseconds
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            //conn.Open();
            //isonline();
            ICollection<MachineInfo> lstMachineInfo = manipulator.GetLogData(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));

            if (lstMachineInfo != null && lstMachineInfo.Count > 0)
            {



                lstMachineInfo = lstMachineInfo.Where(x => x.DateTimeRecord.Contains(lastrecord)).ToList();
                BindToGridView(lstMachineInfo);
                if (dgvRecords.Columns.Count == 5)
                {
                    for (int i = 0; i < dgvRecords.Rows.Count; i++)
                    {
                        this.Cursor = Cursors.WaitCursor;

                        string query = "insert into thumpdata_nimra (machine_no,emp_id,datetime_record,date_record,time_record)" +
                            " select @machine_no, @emp_id, @datetime_record, @date_record, @time_record " +
                            " from (select 1) dummy " +
                            " where not exists ( " +
                            " select 1 " +
                            " from thumpdata_nimra " +
                            " where machine_no = @machine_no " +
                            " and emp_id  = @emp_id " +
                            " and datetime_record = @datetime_record " +
                            " and date_record = @date_record " +
                            " and time_record = @time_record " +
                            " ); ";
                        MySqlConnection conDataBase = new MySqlConnection(constring);
                        MySqlCommand cmdDataBase = new MySqlCommand(query, conDataBase);
                        MySqlDataReader myReader;

                        cmdDataBase.Parameters.AddWithValue("@machine_no", dgvRecords.Rows[i].Cells[0].Value);
                        cmdDataBase.Parameters.AddWithValue("@emp_id", dgvRecords.Rows[i].Cells[1].Value);
                        cmdDataBase.Parameters.AddWithValue("@datetime_record", dgvRecords.Rows[i].Cells[2].Value);
                        cmdDataBase.Parameters.AddWithValue("@date_record", dgvRecords.Rows[i].Cells[3].Value);
                        cmdDataBase.Parameters.AddWithValue("@time_record", dgvRecords.Rows[i].Cells[4].Value);
                        //cmdDataBase.Parameters.Clear();

                        conDataBase.Open();
                        myReader = cmdDataBase.ExecuteReader();

                        while (myReader.Read())
                        {

                        }
                        conDataBase.Close();


                    }
                    //System.Windows.Forms.MessageBox.Show("Log Data Saved, Thank You");
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Sorry, Log Data not Found");
                }
            
                MySqlCommand cmd = new MySqlCommand("select e.employee_name as EmployeeName,dayname(td.date_record) as Day, td.date_record as DatePunched, min(td.time_record) as TimeIN," +
                 " case when  e.employee_shift = 1 then if((min(td.time_record)<='08:40:00') ,1,0)" +
                 " when e.employee_shift = 2 then if((min(td.time_record)<='09:10:00'), 1,0)" +
                 " else  if((min(td.time_record)<='09:40:00'), 1,0)" +
                 " end as INCount, Max(td.time_record) as TimeOUT, " +
                 " case when  e.employee_shift = 1 then if((Max(td.time_record)>='17:00:00') ,1,0)" +
                 " when e.employee_shift = 2 then if((Max(td.time_record)>='17:00:00'), 1,0)" +
                 " else  if((Max(td.time_record)>='16:30:00'), 1,0)" +
                 " end as OUTCount " +

                 " from thumpdata_nimra td join employees_nimra e on td.emp_id=e.employee_id where td.date_record='" + checkdate + "'  group by DatePunched  ;", conn);

                DataTable dataTable = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);

                da.Fill(dataTable);

                Attendance_DGV.DataSource = dataTable;

                for (int i = 0; i < Attendance_DGV.Rows.Count; i++)
                {
                    DataGridViewRowHeaderCell cell = Attendance_DGV.Rows[i].HeaderCell;
                    cell.Value = (i + 1).ToString();
                    Attendance_DGV.Rows[i].HeaderCell = cell;
                }

                this.Cursor = Cursors.Default;

                ShowStatusBar(Attendance_DGV.RowCount + " records found !!", true);
                staffPresent = Attendance_DGV.RowCount;
                staffAbsent = staffTotal - staffPresent;
                StaffPresent_TB.Text = staffPresent.ToString();
                StaffAbsent_TB.Text = staffAbsent.ToString();
                //MessageBox.Show("Staff Present" + staffPresent);
               

            }
            //conn.Close();
        }

        public void runonce()
        {
            //conn.Open();
            ICollection<MachineInfo> lstMachineInfo = manipulator.GetLogData(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));

            if (lstMachineInfo != null && lstMachineInfo.Count > 0)
            {


                lstMachineInfo = lstMachineInfo.Where(x => x.DateTimeRecord.Contains(lastrecord)).ToList();
                BindToGridView(lstMachineInfo);
                //ShowStatusBar(lstMachineInfo.Count + " records found !!", true);

                if (dgvRecords.Columns.Count == 5)
                {
                    for (int i = 0; i < dgvRecords.Rows.Count; i++)
                    {
                        this.Cursor = Cursors.WaitCursor;

                        string query = "insert into thumpdata_nimra (machine_no,emp_id,datetime_record,date_record,time_record)" +
                            " select @machine_no, @emp_id, @datetime_record, @date_record, @time_record " +
                            " from (select 1) dummy " +
                            " where not exists ( " +
                            " select 1 " +
                            " from thumpdata_nimra " +
                            " where machine_no = @machine_no " +
                            " and emp_id  = @emp_id " +
                            " and datetime_record = @datetime_record " +
                            " and date_record = @date_record " +
                            " and time_record = @time_record " +
                            " ); ";
                        MySqlConnection conDataBase = new MySqlConnection(constring);
                        MySqlCommand cmdDataBase = new MySqlCommand(query, conDataBase);
                        MySqlDataReader myReader;

                        cmdDataBase.Parameters.AddWithValue("@machine_no", dgvRecords.Rows[i].Cells[0].Value);
                        cmdDataBase.Parameters.AddWithValue("@emp_id", dgvRecords.Rows[i].Cells[1].Value);
                        cmdDataBase.Parameters.AddWithValue("@datetime_record", dgvRecords.Rows[i].Cells[2].Value);
                        cmdDataBase.Parameters.AddWithValue("@date_record", dgvRecords.Rows[i].Cells[3].Value);
                        cmdDataBase.Parameters.AddWithValue("@time_record", dgvRecords.Rows[i].Cells[4].Value);
                        //cmdDataBase.Parameters.Clear();

                        conDataBase.Open();
                        myReader = cmdDataBase.ExecuteReader();

                        while (myReader.Read())
                        {

                        }
                        conDataBase.Close();


                    }
                    System.Windows.Forms.MessageBox.Show("Log Data Saved, Thank You");
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Sorry, Log Data not Found");
                }
             
                MySqlCommand cmd = new MySqlCommand("select e.employee_name as EmployeeName,dayname(td.date_record) as Day, td.date_record as DatePunched, min(td.time_record) as TimeIN," +
                 " case when  e.employee_shift = 1 then if((min(td.time_record)<='08:40:00') ,1,0)" +
                 " when e.employee_shift = 2 then if((min(td.time_record)<='09:10:00'), 1,0)" +
                 " else  if((min(td.time_record)<='09:40:00'), 1,0)" +
                 " end as INCount, Max(td.time_record) as TimeOUT, " +
                 " case when  e.employee_shift = 1 then if((Max(td.time_record)>='17:00:00') ,1,0)" +
                 " when e.employee_shift = 2 then if((Max(td.time_record)>='17:00:00'), 1,0)" +
                 " else  if((Max(td.time_record)>='16:30:00'), 1,0)" +
                 " end as OUTCount " +

                 " from thumpdata_nimra td join employees_nimra e on td.emp_id=e.employee_id where td.date_record='" + checkdate + "'  group by DatePunched  ;", conn);

                DataTable dataTable = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);

                da.Fill(dataTable);

                Attendance_DGV.DataSource = dataTable;

                for (int i = 0; i < Attendance_DGV.Rows.Count; i++)
                {
                    DataGridViewRowHeaderCell cell = Attendance_DGV.Rows[i].HeaderCell;
                    cell.Value = (i + 1).ToString();
                    Attendance_DGV.Rows[i].HeaderCell = cell;
                }

                this.Cursor = Cursors.Default;
                //BindToGridView(lstMachineInfo);


                ShowStatusBar(Attendance_DGV.RowCount + " records found !!", true);
                staffPresent = Attendance_DGV.RowCount;
                staffAbsent = staffTotal - staffPresent;
                StaffPresent_TB.Text = staffPresent.ToString();
                StaffAbsent_TB.Text = staffAbsent.ToString();
                //MessageBox.Show("Staff Present" + staffPresent);
               
            }
            //conn.Close();
        }

        public bool IsDeviceConnected
        {
            get { return isDeviceConnected; }
            set
            {
                isDeviceConnected = value;
                if (isDeviceConnected)
                {
                    InitTimer();
                    ShowStatusBar("The device is connected !!", true);
                    btnConnect.Text = "Disconnect";
                    //runonce();
                    //ToggleControls(true);

                }
                else
                {
                    ShowStatusBar("The device is diconnected !!", true);
                    objZkeeper.Disconnect();
                    btnConnect.Text = "Connect";
                    //ToggleControls(false);
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void BindToGridView(object list)
        {
            ClearGrid();
            dgvRecords.DataSource = list;
            dgvRecords.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            UniversalStatic.ChangeGridProperties(dgvRecords);
        }




        private void RaiseDeviceEvent(object sender, string actionType)
        {
            switch (actionType)
            {
                case UniversalStatic.acx_Disconnect:
                    {
                        ShowStatusBar("The device is switched off", true);
                        DisplayEmpty();
                        btnConnect.Text = "Connect";
                        //ToggleControls(false);
                        break;
                    }

                default:
                    break;
            }

        }



        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                ShowStatusBar(string.Empty, true);

                if (IsDeviceConnected)
                {
                    IsDeviceConnected = false;
                    this.Cursor = Cursors.Default;

                    return;
                }

                string ipAddress = tbxDeviceIP.Text.Trim();
                string port = tbxPort.Text.Trim();
                if (ipAddress == string.Empty || port == string.Empty)
                    throw new Exception("The Device IP Address and Port is mandotory !!");

                int portNumber = 4370;
                if (!int.TryParse(port, out portNumber))
                    throw new Exception("Not a valid port number");

                bool isValidIpA = UniversalStatic.ValidateIP(ipAddress);
                if (!isValidIpA)
                    throw new Exception("The Device IP is invalid !!");

                isValidIpA = UniversalStatic.PingTheDevice(ipAddress);
                if (!isValidIpA)
                    throw new Exception("The device at " + ipAddress + ":" + port + " did not respond!!");

                objZkeeper = new ZkemClient(RaiseDeviceEvent);
                IsDeviceConnected = objZkeeper.Connect_Net(ipAddress, portNumber);

                if (IsDeviceConnected)
                {
                    string deviceInfo = manipulator.FetchDeviceInfo(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));
                    lblDeviceInfo.Text = deviceInfo;
                }

            }
            catch (Exception ex)
            {
                ShowStatusBar(ex.Message, false);
            }
            this.Cursor = Cursors.Default;

        }

        private void btnPullData_Click(object sender, EventArgs e)
        {
            try
            {
                ShowStatusBar(string.Empty, true);

                ICollection<MachineInfo> lstMachineInfo = manipulator.GetLogData(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));

                if (lstMachineInfo != null && lstMachineInfo.Count > 0)
                {
                    BindToGridView(lstMachineInfo);
                    ShowStatusBar(lstMachineInfo.Count + " records found !!", true);
                    /*
                    if (dgvRecords.Columns.Count == 5)
                    {
                        for (int i = 0; i < dgvRecords.Rows.Count; i++)
                        {
                            this.Cursor = Cursors.WaitCursor;
                            
                            string query = "INSERT IGNORE INTO thumpdata_nimra (machine_no,emp_id,str_to_date(datetime_record,%M/%d/%YYYY),date_record,time_record) values (@machine_no,@emp_id,@datetime_record,@date_record,@time_record);";
                            MySqlConnection conDataBase = new MySqlConnection(constring);
                            MySqlCommand cmdDataBase = new MySqlCommand(query, conDataBase);
                            MySqlDataReader myReader;

                            cmdDataBase.Parameters.AddWithValue("@machine_no", dgvRecords.Rows[i].Cells[0].Value);
                            cmdDataBase.Parameters.AddWithValue("@emp_id", dgvRecords.Rows[i].Cells[1].Value);
                            cmdDataBase.Parameters.AddWithValue("@datetime_record", dgvRecords.Rows[i].Cells[2].Value);
                            cmdDataBase.Parameters.AddWithValue("@date_record", dgvRecords.Rows[i].Cells[3].Value);
                            cmdDataBase.Parameters.AddWithValue("@time_record", dgvRecords.Rows[i].Cells[4].Value);
                            //cmdDataBase.Parameters.Clear();

                            conDataBase.Open();
                            myReader = cmdDataBase.ExecuteReader();

                            while (myReader.Read())
                            {

                            }
                            conDataBase.Close();


                        }
                        System.Windows.Forms.MessageBox.Show("Log Data Saved, Thank You");
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("Sorry, Log Data not Found");
                    }
                    this.Cursor = Cursors.Default;
                    */
                }
                else
                    DisplayListOutput("No records found");
            }
            catch (Exception ex)
            {
                DisplayListOutput(ex.Message);
            }
        }

        public void ShowStatusBar(string message, bool type)
        {
            if (message.Trim() == string.Empty)
            {
                lblStatus.Visible = false;
                return;
            }

            lblStatus.Visible = true;
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.White;

            if (type)
                lblStatus.BackColor = Color.FromArgb(79, 208, 154);
            else
                lblStatus.BackColor = Color.FromArgb(230, 112, 134);
        }

        private void DisplayEmpty()
        {
            ClearGrid();
            dgvRecords.Controls.Add(new DataEmpty());
        }

        private void ClearGrid()
        {
            if (dgvRecords.Controls.Count > 2)
            { dgvRecords.Controls.RemoveAt(2); }


            dgvRecords.DataSource = null;
            dgvRecords.Controls.Clear();
            dgvRecords.Rows.Clear();
            dgvRecords.Columns.Clear();
        }



        private void DisplayListOutput(string message)
        {
            if (dgvRecords.Controls.Count > 2)
            { dgvRecords.Controls.RemoveAt(2); }

            ShowStatusBar(message, false);
        }




        /// <summary>
        /// Code for HomePage Ended Here
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        //1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111

        //2222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222

        /// <summary>
        /// Code for AddEmployee Startedd Here
        /// </summary>


        private void Parttime_rb_CheckedChanged(object sender, EventArgs e)
        {
            if (Parttime_rb.Checked)
            {
                Parttime_tb.Visible = true;
             
            }
            else
            {
                Parttime_tb.Visible = false;
                Parttime_tb.Text = "0";
            
            }
        }

        private void Upload_BTN1_Click(object sender, EventArgs e)
        {
            if (EmpID_TB.Text == "")
            {
                MessageBox.Show("Please Enter Employee BioMetricID");
            }
            else
            {
                using (OpenFileDialog openFileDialog1 = new OpenFileDialog())
                {
                    if (openFileDialog1.ShowDialog() == DialogResult.OK)
                    {

                        string fileName = openFileDialog1.FileName;
                        byte[] bytes = File.ReadAllBytes(fileName);
                        string contentType = "";
                        //Set the contenttype based on File Extension

                        switch (Path.GetExtension(fileName))
                        {
                            case ".jpg":
                                contentType = "image/jpeg";
                                break;
                            case ".png":
                                contentType = "image/png";
                                break;
                            case ".gif":
                                contentType = "image/gif";
                                break;
                            case ".bmp":
                                contentType = "image/bmp";
                                break;
                        }



                        string query2 = "INSERT INTO images_nimra VALUES(@employee_id, @image_type, @employee_image, @adhaar_image, @ssc_image, @inter_image, @ug_image, @pg_image, @phd_image)";
                        MySqlConnection conDataBase = new MySqlConnection(constring);
                        MySqlCommand cmd = new MySqlCommand(query2, conDataBase);

                        cmd.Parameters.AddWithValue("@employee_id", EmpID_TB.Text);
                        cmd.Parameters.AddWithValue("@image_type", contentType);
                        cmd.Parameters.AddWithValue("@employee_image", bytes);
                        cmd.Parameters.AddWithValue("@adhaar_image", null);
                        cmd.Parameters.AddWithValue("@ssc_image", null);
                        cmd.Parameters.AddWithValue("@inter_image", null);
                        cmd.Parameters.AddWithValue("@ug_image", null);
                        cmd.Parameters.AddWithValue("@pg_image", null);
                        cmd.Parameters.AddWithValue("@phd_image", null);
                        conDataBase.Open();
                        cmd.ExecuteNonQuery();



                        Upload1_TB.Text = Path.GetFileName(fileName);
                        image.ImageLocation = openFileDialog1.FileName;
                        //this.BindDataGridView();

                    }
                }
            }

        }

        private void Upload_BTN2_Click(object sender, EventArgs e)
        {
            if (EmpID_TB.Text == "")
            {
                MessageBox.Show("Please Enter Employee BioMetricID");
            }
            else
            {
                using (OpenFileDialog openFileDialog2 = new OpenFileDialog())
                {
                    if (openFileDialog2.ShowDialog() == DialogResult.OK)
                    {
                        string fileName = openFileDialog2.FileName;
                        byte[] bytes = File.ReadAllBytes(fileName);

                        string checkstr = Path.GetFileName(fileName), query2;
                        Upload2_TB.Text = checkstr.ToLower();
                        certifigates_PB.ImageLocation = openFileDialog2.FileName;

                        if (Upload2_TB.Text.Contains("adhaar"))
                        {
                            Adhaar_CB.Checked = true;
                            query2 = "Update images_nimra set adhaar_image = @adhaar_image where employee_id = @employee_id";

                            MySqlConnection conDataBase = new MySqlConnection(constring);
                            MySqlCommand cmd = new MySqlCommand(query2, conDataBase);

                            cmd.Parameters.AddWithValue("@employee_id", EmpID_TB.Text);

                            cmd.Parameters.AddWithValue("@adhaar_image", bytes);

                            conDataBase.Open();
                            cmd.ExecuteNonQuery();

                        }

                        else if (Upload2_TB.Text.Contains("ssc"))
                        {
                            query2 = "Update images_nimra set ssc_image = @ssc_image where employee_id = @employee_id";
                            SSC_CB.Checked = true;
                            MySqlConnection conDataBase = new MySqlConnection(constring);
                            MySqlCommand cmd = new MySqlCommand(query2, conDataBase);

                            cmd.Parameters.AddWithValue("@employee_id", EmpID_TB.Text);

                            cmd.Parameters.AddWithValue("@ssc_image", bytes);

                            conDataBase.Open();
                            cmd.ExecuteNonQuery();

                        }

                        else if (Upload2_TB.Text.Contains("inter"))
                        {
                            query2 = "Update images_nimra set inter_image = @inter_image where employee_id = @employee_id";
                            INTER_CB.Checked = true;
                            MySqlConnection conDataBase = new MySqlConnection(constring);
                            MySqlCommand cmd = new MySqlCommand(query2, conDataBase);

                            cmd.Parameters.AddWithValue("@employee_id", EmpID_TB.Text);

                            cmd.Parameters.AddWithValue("@inter_image", bytes);

                            conDataBase.Open();
                            cmd.ExecuteNonQuery();

                        }

                        else if (Upload2_TB.Text.Contains("ug"))
                        {
                            query2 = "Update images_nimra set ug_image = @ug_image where employee_id = @employee_id";
                            UG_CB.Checked = true;
                            MySqlConnection conDataBase = new MySqlConnection(constring);
                            MySqlCommand cmd = new MySqlCommand(query2, conDataBase);

                            cmd.Parameters.AddWithValue("@employee_id", EmpID_TB.Text);

                            cmd.Parameters.AddWithValue("@ug_image", bytes);

                            conDataBase.Open();
                            cmd.ExecuteNonQuery();

                        }

                        else if (Upload2_TB.Text.Contains("pg"))
                        {
                            query2 = "Update images_nimra set pg_image = @pg_image where employee_id = @employee_id";
                            PG_CB.Checked = true;
                            MySqlConnection conDataBase = new MySqlConnection(constring);
                            MySqlCommand cmd = new MySqlCommand(query2, conDataBase);

                            cmd.Parameters.AddWithValue("@employee_id", EmpID_TB.Text);

                            cmd.Parameters.AddWithValue("@pg_image", bytes);

                            conDataBase.Open();
                            cmd.ExecuteNonQuery();

                        }

                        else
                        {
                            query2 = "Update images_nimra set phd_image = @phd_image where employee_id = @employee_id";
                            PHD_CB.Checked = true;
                            MySqlConnection conDataBase = new MySqlConnection(constring);
                            MySqlCommand cmd = new MySqlCommand(query2, conDataBase);

                            cmd.Parameters.AddWithValue("@employee_id", EmpID_TB.Text);

                            cmd.Parameters.AddWithValue("@phd_image", bytes);

                            conDataBase.Open();
                            cmd.ExecuteNonQuery();

                        }





                        //this.BindDataGridView();

                    }
                }
            }
        }

        private void Dob_DP_ValueChanged(object sender, EventArgs e)
        {
            //Save today's date.
            var today = DateTime.Today;
            var birthdate = DateTime.Parse(Dob_DP.Text);
            // Calculate the age.
            var age = today.Year - birthdate.Year;
            // Go back to the year the person was born in case of a leap year
            if (birthdate > today.AddYears(-age)) age--;
            Age_TB.Text = age.ToString();
            //MessageBox.Show(birthdate.ToString());
        }

        private void Shift_CB_SelectedIndexChanged(object sender, EventArgs e)
        {
            //MessageBox.Show(Shift_CB.SelectedIndex.ToString());
        }

        private void EmpID_TB_TextChanged(object sender, EventArgs e)
        {
            MySqlCommand cmd12 = new MySqlCommand("select distinct(count(employee_id)) as empid from employees_nimra  where employee_id = '" + EmpID_TB.Text + "' ", conn);
            int idc = 0;
            using (MySqlDataReader reader4 = cmd12.ExecuteReader())
            {
                while (reader4.Read())
                {
                    idc = int.Parse((reader4["empid"].ToString()));

                }
            }
            if(idc>=1)
            {
                MessageBox.Show("BioMetric ID ALrready Exists, Please Refer it.");
            }
        }

        private void Save_BTN_Click(object sender, EventArgs e)
        {
            int hra = 0, da = 0, te = 0, deductions = 0, earnings = 0;
            int Salarytype, pfstate, esistate;
            //conn.Open();
            if (Basic_RB.Checked == true)
            {
                hra = (int.Parse(Salary_TB.Text) / 100) * 5;
                da = (int.Parse(Salary_TB.Text) / 100) * 67;
                te = (int.Parse(Salary_TB.Text) + hra + da);
                Salarytype = 1;
            }
            else
            {
                hra = 0;
                da = 0;
                te = int.Parse(Salary_TB.Text);
                Salarytype = 2;
            }
            if (PfYes_RB.Checked == true)
            {
                pfstate = 1;
            }
            else
            {
                pfstate = 2;
            }
            if (EsiYes_RB.Checked == true)
            {
                esistate = 1;
            }
            else
            {
                esistate = 2;
            }
            int ftpt = 0;
            if(Parttime_rb.Checked==true)
            {
                ftpt = 1;
            }
            else
            {
                ftpt = 2;
            }
            string code = "null";
            deductions = int.Parse(PT_TB.Text) + int.Parse(IT_TB.Text);
            earnings = te - deductions;
            string query1 = "insert into employees_nimra values(@employee_id,@employee_code,@employee_name,@employee_gender,@employee_dept,@employee_college,@employee_doj,@employee_shift,@employee_basic,@employee_hra,@employee_da,@employee_te,@employee_pt,@employee_it,@employee_deductions,@employee_earnings,@employees_saltype,@employees_pfstate,@employees_esistate,@employees_exclude)";

            MySqlConnection conDataBase = new MySqlConnection(constring);
            MySqlCommand cmd2 = new MySqlCommand(query1, conDataBase);
            cmd2.Parameters.AddWithValue("@employee_id", EmpID_TB.Text);
            cmd2.Parameters.AddWithValue("@employee_code", code);
            cmd2.Parameters.AddWithValue("@employee_name", EmpName_TB.Text);
            cmd2.Parameters.AddWithValue("@employee_gender", Gender_CB.SelectedItem);
            cmd2.Parameters.AddWithValue("@employee_dept", Department_CB.SelectedItem);
            cmd2.Parameters.AddWithValue("@employee_college", CollegeName_CB.SelectedItem);
            cmd2.Parameters.AddWithValue("@employee_doj", Doj_DTP.Text);
            cmd2.Parameters.AddWithValue("@employee_shift", Shift_CB.SelectedIndex.ToString());
            cmd2.Parameters.AddWithValue("@employee_basic", Salary_TB.Text);
            cmd2.Parameters.AddWithValue("@employee_hra", hra);
            cmd2.Parameters.AddWithValue("@employee_da", da);
            cmd2.Parameters.AddWithValue("@employee_te", te);
            cmd2.Parameters.AddWithValue("@employee_pt", PT_TB.Text);
            cmd2.Parameters.AddWithValue("@employee_it", IT_TB.Text);
            cmd2.Parameters.AddWithValue("@employee_deductions", deductions);
            cmd2.Parameters.AddWithValue("@employee_earnings", earnings);
            cmd2.Parameters.AddWithValue("@employees_saltype", Salarytype);
            cmd2.Parameters.AddWithValue("@employees_pfstate", pfstate);
            cmd2.Parameters.AddWithValue("@employees_esistate", esistate);
            cmd2.Parameters.AddWithValue("@employees_exclude", 0);
            conDataBase.Open();
            cmd2.ExecuteNonQuery();

            string query3 = "insert into empdetails_nimra values(@emp_id,@emp_dob,@emp_age,@emp_qualification,@emp_desig,@emp_bank,@emp_acno,@emp_adhaar,@emp_ssc,@emp_inter,@emp_ug,@emp_pg,@emp_phd,@emp_image,@emp_email,@emp_mobile,@emp_jobtype,@emp_ptdays) ";

            MySqlCommand cmd3 = new MySqlCommand(query3, conDataBase);
            cmd3.Parameters.AddWithValue("@emp_id", EmpID_TB.Text);
            cmd3.Parameters.AddWithValue("@emp_dob", Dob_DP.Text);
            cmd3.Parameters.AddWithValue("@emp_age", Age_TB.Text);
            cmd3.Parameters.AddWithValue("@emp_qualification", Qualification_TB.Text);
            cmd3.Parameters.AddWithValue("@emp_desig", Designation_CB.SelectedItem);
            cmd3.Parameters.AddWithValue("@emp_bank", BankName_TB.Text);
            cmd3.Parameters.AddWithValue("@emp_acno", AcNo_TB.Text);
            cmd3.Parameters.AddWithValue("@emp_adhaar", code);
            cmd3.Parameters.AddWithValue("@emp_ssc", code);
            cmd3.Parameters.AddWithValue("@emp_inter", code);
            cmd3.Parameters.AddWithValue("@emp_ug", code);
            cmd3.Parameters.AddWithValue("@emp_pg", code);
            cmd3.Parameters.AddWithValue("@emp_phd", code);
            cmd3.Parameters.AddWithValue("@emp_image", code);
            cmd3.Parameters.AddWithValue("@emp_email", Email_TB.Text);
            cmd3.Parameters.AddWithValue("@emp_mobile", MobileNo_TB.Text);
            cmd3.Parameters.AddWithValue("@emp_jobtype", ftpt);
            cmd3.Parameters.AddWithValue("@emp_ptdays", Parttime_tb.Text);
            cmd3.ExecuteNonQuery();

            EmpName_TB.Text = "";
            EmpID_TB.Text = "";
            Age_TB.Text = "";
            Qualification_TB.Text = "";
            Gender_CB.SelectedIndex = 0;
            CollegeName_CB.SelectedIndex = 0;
            Department_CB.SelectedIndex = 0;
            Designation_CB.SelectedIndex = 0;
            Shift_CB.SelectedIndex = 0;
            BankName_TB.Text = "";
            AcNo_TB.Text = "";
            Basic_RB.Checked = false;
            Consolidated_RB.Checked = false;
            Salary_TB.Text = "";
            PfYes_RB.Checked = false;
            PfNo_RB.Checked = false;
            EsiYes_RB.Checked = false;
            EsiNo_RB.Checked = false;
            PT_TB.Text = "";
            IT_TB.Text = "";
            Email_TB.Text = "";
            MobileNo_TB.Text = "";
            Parttime_tb.Text = "";



            MessageBox.Show(EmpName_TB.Text + "Details Registered Successfully");


        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            int MachineNumber = 1;
            string EnrollNumber = EmpID_TB.Text;
            string Name = EmpName_TB.Text;
            string Password = "123";
            int Privilege = 0;
            bool Enabled = true;

            objZkeeper.SSR_SetUserInfo(MachineNumber, EnrollNumber, Name, Password, Privilege, Enabled);

            MessageBox.Show(EmpName_TB.Text + "Details Registered Successfully");
        }

        public class ComboboxItem
        {
            public string Text { get; set; }
            public object Value { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }





        /// <summary>
        /// Code for AddEmpoyee Ended Here
        /// </summary>
        /// 

        //2222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222
        
        //3333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333

        /// <summary>
        /// Code for AddPublicHolidays Started Here
        /// </summary>
        /// 

       
        public void fillph()
        {
            //conn.Open();
            string phcheck;
            int month1 = DateTime.Today.Month;
            int year1 = DateTime.Today.Year;
            if(month1<10)
            {
                phcheck = year1 + "-0" + month1;
            }
            else
            {
                phcheck = year1 + "-" + month1;
            }

            //MessageBox.Show(phcheck);
          
            MySqlCommand cmd = new MySqlCommand("select ph_date from public_holidays where  ph_date like '" + phcheck + "-%' order by ph_date ", conn);
            DataTable dataTable = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);

            da.Fill(dataTable);
            PH_DGV.DataSource = dataTable;
            for (int i = 0; i < PH_DGV.Rows.Count; i++)
            {
                DataGridViewRowHeaderCell cell = PH_DGV.Rows[i].HeaderCell;
                cell.Value = (i + 1).ToString();
                PH_DGV.Rows[i].HeaderCell = cell;
            }
            //conn.Close();
        }

        private void Add_Ph_btn_Click(object sender, EventArgs e)
        {
            //conn.Open();
            PH_DTP.Format = DateTimePickerFormat.Custom;
            PH_DTP.CustomFormat = "yyyy-MM";
            string phcheck = PH_DTP.Text;
            //MessageBox.Show(phcheck);
            PH_DTP.Format = DateTimePickerFormat.Custom;
            PH_DTP.CustomFormat = "yyyy-MM-dd";

            MySqlCommand cmd2 = new MySqlCommand("insert into public_holidays(ph_date) values('" + PH_DTP.Text + "') ", conn);
            cmd2.ExecuteNonQuery();

            MySqlCommand cmd = new MySqlCommand("select ph_date from public_holidays where  ph_date like '" + phcheck + "-%' order by ph_date", conn);
            DataTable dataTable = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);

            da.Fill(dataTable);
            PH_DGV.DataSource = dataTable;
            for (int i = 0; i < PH_DGV.Rows.Count; i++)
            {
                DataGridViewRowHeaderCell cell = PH_DGV.Rows[i].HeaderCell;
                cell.Value = (i + 1).ToString();
                PH_DGV.Rows[i].HeaderCell = cell;
            }
            //conn.Close();
        }

        private void Delete_Ph_btn_Click(object sender, EventArgs e)
        {
            //conn.Open();
            DeletePHDate_DP.Format = DateTimePickerFormat.Custom;
            DeletePHDate_DP.CustomFormat = "yyyy-MM";
            string phcheck = DeletePHDate_DP.Text;
            //MessageBox.Show(phcheck);
            DeletePHDate_DP.Format = DateTimePickerFormat.Custom;
            DeletePHDate_DP.CustomFormat = "yyyy-MM-dd";

            MySqlCommand cmd2 = new MySqlCommand("delete from public_holidays where  ph_date='" + DeletePHDate_DP.Text + "' ", conn);
            cmd2.ExecuteNonQuery();

            MySqlCommand cmd = new MySqlCommand("select ph_date from public_holidays where  ph_date like '" + phcheck + "-%' order by ph_date ", conn);
            DataTable dataTable = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);

            da.Fill(dataTable);
            PH_DGV.DataSource = dataTable;

            for (int i = 0; i < PH_DGV.Rows.Count; i++)
            {
                DataGridViewRowHeaderCell cell = PH_DGV.Rows[i].HeaderCell;
                cell.Value = (i + 1).ToString();
                PH_DGV.Rows[i].HeaderCell = cell;
            }
            //conn.Close();
        }

      

        /// <summary>
        /// Code for AddPublicHolidays Ended Here
        /// </summary>
        /// 

        //3333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333

        //4444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444

        /// <summary>
        /// Code for AddODs Started Here
        /// </summary>
        /// 


        public void fillemp_OD()
        {
            //conn.Open();
          
            MySqlCommand cmdOD = new MySqlCommand("select employee_id,employee_name from employees_nimra where employee_status=1;", conn);

            DataTable dtOD = new DataTable();
            MySqlDataAdapter daOD = new MySqlDataAdapter(cmdOD);

            daOD.Fill(dtOD);
            //Insert the Default Item to DataTable.
            DataRow row = dtOD.NewRow();
            row[0] = 0;
            row[1] = "Please select Employee";
            dtOD.Rows.InsertAt(row, 0);

            EMP_OD_CB.DisplayMember = "employee_name";
            EMP_OD_CB.ValueMember = "employee_id";
            EMP_OD_CB.DataSource = dtOD;

            
            //conn.Close();
        }


        private void EMP_OD_CB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Fill_OD_DGV();
        }

        private void Fill_OD_DGV()
        {
            string oddate = Date_OD_dtp.Text;
            string odstart = Date_OD_dtp.Text+"-01";
            string odend = Date_OD_dtp.Text + "-31";
            if (EMP_OD_CB.SelectedIndex == 0)
            {
                OD_DGV.DataSource = null;
                OD_DGV.Rows.Clear();
                OD_DGV.Columns.Clear();
                OD_Delete_dgv.DataSource = null;
                OD_Delete_dgv.Rows.Clear();
                OD_Delete_dgv.Columns.Clear();
            }
            else
            { 
            this.Cursor = Cursors.WaitCursor;
            //conn.Open();
            //MessageBox.Show(Emp_CB.SelectedValue.ToString());
            OD_DGV.DataSource = null;
            OD_DGV.Rows.Clear();
            OD_DGV.Columns.Clear();
            OD_Delete_dgv.DataSource = null;
            OD_Delete_dgv.Rows.Clear();
            OD_Delete_dgv.Columns.Clear();
           
            string odcheck = DateTime.Today.ToString("yyyy-MM");
            //MessageBox.Show(phcheck);

            MySqlCommand cmd = new MySqlCommand(" SELECT *, dayname(missingDates) as DayName FROM " +
                " ( " +
                "  SELECT DATE_ADD('"+odstart+"', INTERVAL t4+t16+t64+t256+t1024 DAY) MissingDates  " +
                " FROM  " +
                " (SELECT 0 t4    UNION ALL SELECT 1   UNION ALL SELECT 2   UNION ALL SELECT 3  ) t4, " +
                " (SELECT 0 t16   UNION ALL SELECT 4   UNION ALL SELECT 8   UNION ALL SELECT 12 ) t16,   " +
                " (SELECT 0 t64   UNION ALL SELECT 16  UNION ALL SELECT 32  UNION ALL SELECT 48 ) t64, " +
                " (SELECT 0 t256  UNION ALL SELECT 64  UNION ALL SELECT 128 UNION ALL SELECT 192) t256, " +
                " (SELECT 0 t1024 UNION ALL SELECT 256 UNION ALL SELECT 512 UNION ALL SELECT 768) t1024 " +
                " ) b  " +
                " WHERE " +
                "     MissingDates NOT IN (SELECT DATE_FORMAT(date_record,'%Y-%m-%d') " +
                "  FROM " +
                " thumpdata_nimra where emp_id= '" + EMP_OD_CB.SelectedValue + "'  GROUP BY date_record ) " +
                "   AND " +

                "     MissingDates < '" + odend + "' AND  DAYOFWEEK(MissingDates) <> 1  " +
                " AND " +
                " MissingDates NOT IN (SELECT DATE_FORMAT(ph_date,'%Y-%m-%d')  " +
                " FROM " +
                " public_holidays where  ph_date like '" + oddate + "-%') " +
                " AND " +
                " MissingDates NOT IN (SELECT DATE_FORMAT(od_date,'%Y-%m-%d')  " +
                " FROM " +
                " ods_nimra where  emp_id='" + EMP_OD_CB.SelectedValue + "' and od_date like '" + oddate + "-%') " +
                " AND " +
                " MissingDates NOT IN (SELECT DATE_FORMAT(cl_date,'%Y-%m-%d')  " +
                " FROM " +
                " cls_nimra where  emp_id='" + EMP_OD_CB.SelectedValue + "' and cl_date like '" + oddate + "-%') " +
                " AND " +
                " MissingDates NOT IN (SELECT DATE_FORMAT(hr_date,'%Y-%m-%d')  " +
                " FROM " +
                "  hrs_nimra where  emp_id='" + EMP_OD_CB.SelectedValue + "' and hr_date like '" + oddate + "-%'); ", conn);
            DataTable dT = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);

            da.Fill(dT);
            OD_DGV.DataSource = dT;



            //Add a CheckBox Column to the DataGridView at the first position.
            DataGridViewCheckBoxColumn checkBoxColumn = new DataGridViewCheckBoxColumn();
            checkBoxColumn.HeaderText = "";
            checkBoxColumn.Width = 30;
            checkBoxColumn.Name = "checkBoxColumn";
            OD_DGV.Columns.Insert(0, checkBoxColumn);

                for (int i = 0; i < OD_DGV.Rows.Count; i++)
                {
                    DataGridViewRowHeaderCell cell = OD_DGV.Rows[i].HeaderCell;
                    cell.Value = (i + 1).ToString();
                    OD_DGV.Rows[i].HeaderCell = cell;
                }

                this.OD_DGV.Columns[1].ReadOnly = true;
            this.OD_DGV.Columns[2].ReadOnly = true;


            MySqlCommand cmd2 = new MySqlCommand(" SELECT dayname(od_date),od_date from ods_nimra where emp_id= '" + EMP_OD_CB.SelectedValue + "' and od_date like '" + oddate + "-%'", conn);
            DataTable dT2 = new DataTable();
            MySqlDataAdapter da2 = new MySqlDataAdapter(cmd2);

            da2.Fill(dT2);
            OD_Delete_dgv.DataSource = dT2;

                for (int i = 0; i < OD_Delete_dgv.Rows.Count; i++)
                {
                    DataGridViewRowHeaderCell cell = OD_Delete_dgv.Rows[i].HeaderCell;
                    cell.Value = (i + 1).ToString();
                    OD_Delete_dgv.Rows[i].HeaderCell = cell;
                }
                //conn.Close();
                //Add a CheckBox Column to the DataGridView at the first position.
                DataGridViewCheckBoxColumn checkBoxColumn2 = new DataGridViewCheckBoxColumn();
            checkBoxColumn2.HeaderText = "";
            checkBoxColumn2.Width = 30;
            checkBoxColumn2.Name = "checkBoxColumn";
            OD_Delete_dgv.Columns.Insert(0, checkBoxColumn2);
            this.Cursor = Cursors.Default;
            }
        }

        private void Add_Od_btn_Click(object sender, EventArgs e)
        {
            int inserted = 0;
            foreach (DataGridViewRow row in OD_DGV.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                if (isSelected)
                {
                   
                   
                       
                        using (MySqlCommand cmd = new MySqlCommand("INSERT INTO ods_nimra(emp_id,od_date) VALUES(@emp_id, @od_date)", conn))
                        {
                            cmd.Parameters.AddWithValue("@emp_id", EMP_OD_CB.SelectedValue);
                            cmd.Parameters.AddWithValue("@od_date", row.Cells["MissingDates"].Value);
                         
                           
                            
                            cmd.ExecuteNonQuery();
                            
                        }
                   
                    inserted++;
                }
            }
            Fill_OD_DGV();
            if (inserted > 0)
            {
                MessageBox.Show(string.Format("{0} OnDuty Added.", inserted), "Message");
            }
            
        }

        private void Delete_Od_btn_Click(object sender, EventArgs e)
        {
            int inserted = 0;
            foreach (DataGridViewRow row in OD_Delete_dgv.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                if (isSelected)
                {



                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM ods_nimra where emp_id=@emp_id and od_date=@od_date", conn))
                    {
                        cmd.Parameters.AddWithValue("@emp_id", EMP_OD_CB.SelectedValue);
                        cmd.Parameters.AddWithValue("@od_date", row.Cells["od_date"].Value);
                        


                        cmd.ExecuteNonQuery();

                    }

                    inserted++;
                }
            }
            Fill_OD_DGV();
            if (inserted > 0)
            {
                MessageBox.Show(string.Format("{0} OnDuty Removed.", inserted), "Message");
            }
        }

      

        /// <summary>
        /// Code for AddODs Ended Here
        /// </summary>
        /// 

        //4444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444

        //5555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555

        /// <summary>
        /// Code for AddCLs Started Here
        /// </summary>
        /// 

        public void fillemp_CL()
        {
            //conn.Open();
           
            MySqlCommand cmdCL = new MySqlCommand("select employee_id,employee_name from employees_nimra where employee_status=1;", conn);

            DataTable dtCL = new DataTable();
            MySqlDataAdapter daCL = new MySqlDataAdapter(cmdCL);

            daCL.Fill(dtCL);
            //Insert the Default Item to DataTable.
            DataRow row = dtCL.NewRow();
            row[0] = 0;
            row[1] = "Please select Employee";
            dtCL.Rows.InsertAt(row, 0);
            Emp_CL_CB.DisplayMember = "employee_name";
            Emp_CL_CB.ValueMember = "employee_id";
            Emp_CL_CB.DataSource = dtCL;

            //conn.Close();
        }
        private void Emp_CL_CB_SelectedIndexChanged(object sender, EventArgs e)
        {
            Fill_CL_DGV();
        }

        public void Fill_CL_DGV()
        {
            string cldate = Date_CL_dtp.Text;
            string clstart = Date_CL_dtp.Text + "-01";
            string clend = Date_CL_dtp.Text + "-31";
           
            if (Emp_CL_CB.SelectedIndex == 0)
            {
                this.CL_DGV.DataSource = null;
                this.CL_DGV.Rows.Clear();
                this.CL_DGV.Columns.Clear();
                this.CL_Delete_dgv.DataSource = null;
                this.CL_Delete_dgv.Rows.Clear();
                this.CL_Delete_dgv.Columns.Clear();
            }
            else
            {
                this.Cursor = Cursors.WaitCursor;
                //conn.Open();
                //MessageBox.Show(Emp_CB.SelectedValue.ToString());
                this.CL_DGV.DataSource = null;
                this.CL_DGV.Rows.Clear();
                this.CL_DGV.Columns.Clear();
                this.CL_Delete_dgv.DataSource = null;
                this.CL_Delete_dgv.Rows.Clear();
                this.CL_Delete_dgv.Columns.Clear();
              
                string odcheck = DateTime.Today.ToString("yyyy-MM");
                //MessageBox.Show(phcheck);

                MySqlCommand cmd = new MySqlCommand(" SELECT *, dayname(missingDates) as DayName FROM " +
                    " ( " +
                    "  SELECT DATE_ADD('"+ clstart + "', INTERVAL t4+t16+t64+t256+t1024 DAY) MissingDates  " +
                    " FROM  " +
                    " (SELECT 0 t4    UNION ALL SELECT 1   UNION ALL SELECT 2   UNION ALL SELECT 3  ) t4, " +
                    " (SELECT 0 t16   UNION ALL SELECT 4   UNION ALL SELECT 8   UNION ALL SELECT 12 ) t16,   " +
                    " (SELECT 0 t64   UNION ALL SELECT 16  UNION ALL SELECT 32  UNION ALL SELECT 48 ) t64, " +
                    " (SELECT 0 t256  UNION ALL SELECT 64  UNION ALL SELECT 128 UNION ALL SELECT 192) t256, " +
                    " (SELECT 0 t1024 UNION ALL SELECT 256 UNION ALL SELECT 512 UNION ALL SELECT 768) t1024 " +
                    " ) b  " +
                    " WHERE " +
                    "     MissingDates NOT IN (SELECT DATE_FORMAT(date_record,'%Y-%m-%d') " +
                    "  FROM " +
                    " thumpdata_nimra where emp_id= '" + Emp_CL_CB.SelectedValue + "'  GROUP BY date_record ) " +
                    "   AND " +

                    "     MissingDates < '"+ clend + "' AND  DAYOFWEEK(MissingDates) <> 1  " +
                    " AND " +
                    " MissingDates NOT IN (SELECT DATE_FORMAT(ph_date,'%Y-%m-%d')  " +
                    " FROM " +
                    " public_holidays where  ph_date like '"+ cldate + "-%') " +
                    " AND " +
                    " MissingDates NOT IN (SELECT DATE_FORMAT(od_date,'%Y-%m-%d')  " +
                    " FROM " +
                    " ods_nimra where  emp_id='" + Emp_CL_CB.SelectedValue + "' and od_date like '" + cldate + "-%') " +
                    " AND " +
                    " MissingDates NOT IN (SELECT DATE_FORMAT(cl_date,'%Y-%m-%d')  " +
                    " FROM " +
                    " cls_nimra where  emp_id='" + Emp_CL_CB.SelectedValue + "' and cl_date like '" + cldate + "-%') " +
                    " AND " +
                    " MissingDates NOT IN (SELECT DATE_FORMAT(hr_date,'%Y-%m-%d')  " +
                    " FROM " +
                    "  hrs_nimra where  emp_id='" + Emp_CL_CB.SelectedValue + "' and hr_date like '" + cldate + "-%'); ", conn);
                DataTable dT = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);

                da.Fill(dT);
                CL_DGV.DataSource = dT;

                for (int i = 0; i < CL_DGV.Rows.Count; i++)
                {
                    DataGridViewRowHeaderCell cell = CL_DGV.Rows[i].HeaderCell;
                    cell.Value = (i + 1).ToString();
                    CL_DGV.Rows[i].HeaderCell = cell;
                }


                //Add a CheckBox Column to the DataGridView at the first position.
                DataGridViewCheckBoxColumn checkBoxColumn = new DataGridViewCheckBoxColumn();
                checkBoxColumn.HeaderText = "";
                checkBoxColumn.Width = 30;
                checkBoxColumn.Name = "checkBoxColumn";
                CL_DGV.Columns.Insert(0, checkBoxColumn);

                this.CL_DGV.Columns[1].ReadOnly = true;
                this.CL_DGV.Columns[2].ReadOnly = true;


                MySqlCommand cmd2 = new MySqlCommand(" SELECT dayname(cl_date),cl_date from cls_nimra where emp_id= '" + Emp_CL_CB.SelectedValue + "' and cl_date like '" + cldate + "-%'", conn);
                DataTable dT2 = new DataTable();
                MySqlDataAdapter da2 = new MySqlDataAdapter(cmd2);

                da2.Fill(dT2);
                CL_Delete_dgv.DataSource = dT2;

                for (int i = 0; i < CL_Delete_dgv.Rows.Count; i++)
                {
                    DataGridViewRowHeaderCell cell = CL_Delete_dgv.Rows[i].HeaderCell;
                    cell.Value = (i + 1).ToString();
                    CL_Delete_dgv.Rows[i].HeaderCell = cell;
                }
                //conn.Close();
                //Add a CheckBox Column to the DataGridView at the first position.
                DataGridViewCheckBoxColumn checkBoxColumn2 = new DataGridViewCheckBoxColumn();
                checkBoxColumn2.HeaderText = "";
                checkBoxColumn2.Width = 30;
                checkBoxColumn2.Name = "checkBoxColumn";
                CL_Delete_dgv.Columns.Insert(0, checkBoxColumn2);
                this.Cursor = Cursors.Default;
            }
        }
        private void Add_Cl_btn_Click(object sender, EventArgs e)
        {
            int inserted = 0;
            foreach (DataGridViewRow row in CL_DGV.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                if (isSelected)
                {



                    using (MySqlCommand cmd = new MySqlCommand("INSERT INTO cls_nimra(emp_id,cl_date) VALUES(@emp_id, @cl_date)", conn))
                    {
                        cmd.Parameters.AddWithValue("@emp_id", Emp_CL_CB.SelectedValue);
                        cmd.Parameters.AddWithValue("@cl_date", row.Cells["MissingDates"].Value);
                       


                        cmd.ExecuteNonQuery();

                    }

                    inserted++;
                }
            }
            Fill_CL_DGV();
            if (inserted > 0)
            {
                MessageBox.Show(string.Format("{0} CL Added.", inserted), "Message");
            }
        }

        private void Delete_Cl_btn_Click(object sender, EventArgs e)
        {
            int inserted = 0;
            foreach (DataGridViewRow row in CL_Delete_dgv.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                if (isSelected)
                {



                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM cls_nimra where emp_id=@emp_id and cl_date=@cl_date", conn))
                    {
                        cmd.Parameters.AddWithValue("@emp_id", Emp_CL_CB.SelectedValue);
                        cmd.Parameters.AddWithValue("@cl_date", row.Cells["cl_date"].Value);



                        cmd.ExecuteNonQuery();

                    }

                    inserted++;
                }
            }
            Fill_CL_DGV();
            if (inserted > 0)
            {
                MessageBox.Show(string.Format("{0} CL Removed.", inserted), "Message");
            }
        }

       


        /// <summary>
        /// Code for AddCLs Ended Here
        /// </summary>
        /// 

        //5555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555

        //6666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666

        /// <summary>
        /// Code for AddOneHRs Started Here
        /// </summary>
        /// 

      


        /// <summary>
        /// Code for AddOneHRs Ended Here
        /// </summary>
        /// 

        //6666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666

        /// <summary>
        /// Code for SalaryAdvanes Ended Here
        /// </summary>
        /// 

        public void fillemp_Advances()
        {
            //conn.Open();
            
            MySqlCommand cmdAdvances = new MySqlCommand("select employee_id,employee_name from employees_nimra where employee_status=1;", conn);

            DataTable dtAdvances = new DataTable();
            MySqlDataAdapter daAdvances = new MySqlDataAdapter(cmdAdvances);

            daAdvances.Fill(dtAdvances);
            //Insert the Default Item to DataTable.
            DataRow row = dtAdvances.NewRow();
            row[0] = 0;
            row[1] = "Please select Employee";
            dtAdvances.Rows.InsertAt(row, 0);
            Advances_Emp_cb.DisplayMember = "employee_name";
            Advances_Emp_cb.ValueMember = "employee_id";

            Advances_Emp_cb.DataSource = dtAdvances;
            //conn.Close();
        }

        private void Advances_Emp_cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            Salary_Advances();
        }

        public void Salary_Advances()
        {
            this.SalaryAdvances_dgv.DataSource = null;
            this.SalaryAdvances_dgv.Rows.Clear();
            this.SalaryAdvances_dgv.Columns.Clear();
            Advances_dtp.Format = DateTimePickerFormat.Custom;
            Advances_dtp.CustomFormat = "yyyy-MM";
            string advcheck = DeletePHDate_DP.Text;
            //MessageBox.Show(phcheck);
            Advances_dtp.Format = DateTimePickerFormat.Custom;
            Advances_dtp.CustomFormat = "yyyy-MM-dd";
            MySqlCommand cmd = new MySqlCommand("Select id, advances_date,advances_amount,advances_reason from advances_nimra where emp_id='" + Advances_Emp_cb.SelectedValue + "' and advances_date like '" + advcheck + "%' ", conn);
            DataTable dataTable = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);

            da.Fill(dataTable);
         
            SalaryAdvances_dgv.DataSource = dataTable;
            //Add a CheckBox Column to the DataGridView at the first position.
            DataGridViewCheckBoxColumn checkBoxColumn2 = new DataGridViewCheckBoxColumn();
            checkBoxColumn2.HeaderText = "";
            checkBoxColumn2.Width = 30;
            checkBoxColumn2.Name = "checkBoxColumn";
            SalaryAdvances_dgv.Columns.Insert(0, checkBoxColumn2);
            
            SalaryAdvances_dgv.Columns["id"].Visible = false;

            for (int i = 0; i < SalaryAdvances_dgv.Rows.Count; i++)
            {
                DataGridViewRowHeaderCell cell = SalaryAdvances_dgv.Rows[i].HeaderCell;
                cell.Value = (i + 1).ToString();
                SalaryAdvances_dgv.Rows[i].HeaderCell = cell;
            }
        }

        private void Advances_btn_Click(object sender, EventArgs e)
        {
            using (MySqlCommand cmd = new MySqlCommand("INSERT INTO advances_nimra(emp_id,advances_date,advances_amount, advances_reason )  VALUES(@emp_id, @advances_date, @advances_amount, @advances_reason)", conn))
            {
                cmd.Parameters.AddWithValue("@emp_id", Advances_Emp_cb.SelectedValue);
                cmd.Parameters.AddWithValue("@advances_date", Advances_dtp.Text);
                cmd.Parameters.AddWithValue("@advances_amount", Advance_tb.Text);
                cmd.Parameters.AddWithValue("@advances_reason", Advances_rtb.Text);


                cmd.ExecuteNonQuery();

            }

            Salary_Advances();
            //Advances_Emp_cb.SelectedIndex = 0;
            Advances_rtb.Text = "";
            Advance_tb.Text = "";
           
            MessageBox.Show("Salary Advance Added");
        }

        private void DeleteAdvances_btn_Click_1(object sender, EventArgs e)
        {
            int inserted = 0;
            foreach (DataGridViewRow row in SalaryAdvances_dgv.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                if (isSelected)
                {



                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM advances_nimra where emp_id=@emp_id and advances_date=@cl_date and id=@id", conn))
                    {
                        cmd.Parameters.AddWithValue("@emp_id", Advances_Emp_cb.SelectedValue);
                        cmd.Parameters.AddWithValue("@cl_date", row.Cells["advances_date"].Value);
                        cmd.Parameters.AddWithValue("@id", row.Cells["id"].Value);



                        cmd.ExecuteNonQuery();

                    }

                    inserted++;
                }
            }
            Salary_Advances();
            if (inserted > 0)
            {
                MessageBox.Show(string.Format("{0} Advance Salary Removed.", inserted), "Message");
            }
        }


        /// <summary>
        /// Code for SalaryAdvanes Ended Here
        /// </summary>
        /// 

        //7777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777


        /// <summary>
        /// Code for AddHalfDays Started Here
        /// </summary>
        /// 

        public void fillemp_HalfDays()
        {
            //conn.Open();
           
            MySqlCommand cmdHalfdays = new MySqlCommand("select employee_id,employee_name from employees_nimra where employee_status=1;", conn);

            DataTable dtHalfdays = new DataTable();
            MySqlDataAdapter daHalfdays = new MySqlDataAdapter(cmdHalfdays);

            daHalfdays.Fill(dtHalfdays);
            //Insert the Default Item to DataTable.
            DataRow row = dtHalfdays.NewRow();
            row[0] = 0;
            row[1] = "Please select Employee";
            dtHalfdays.Rows.InsertAt(row, 0);
            Halfday_Emp_cb.DisplayMember = "employee_name";
            Halfday_Emp_cb.ValueMember = "employee_id";

            Halfday_Emp_cb.DataSource = dtHalfdays;
            //conn.Close();
        }

        public void Fill_Halfdays_DGV()
        {
            

            if (Halfday_Emp_cb.SelectedIndex == 0)
            {
                this.AddData_dgv.DataSource = null;
                this.AddData_dgv.Rows.Clear();
                this.AddData_dgv.Columns.Clear();
                this.ShowData_dgv.DataSource = null;
                this.ShowData_dgv.Rows.Clear();
                this.ShowData_dgv.Columns.Clear();
            }
            else
            {
                this.AddData_dgv.DataSource = null;
                this.AddData_dgv.Rows.Clear();
                this.AddData_dgv.Columns.Clear();
                this.ShowData_dgv.DataSource = null;
                this.ShowData_dgv.Rows.Clear();
                this.ShowData_dgv.Columns.Clear();
                //conn.Open();

                string dtcheck;
              
                dtcheck =Halfday_dtp.Text;
                //MessageBox.Show(dtcheck);
                this.Cursor = Cursors.WaitCursor;

                MySqlCommand cmd = new MySqlCommand("select dayname(td.date_record) as Day, td.date_record as DatePunched, min(td.time_record) as TimeIN," +
                    " case when  e.employee_shift = 1 then if((min(td.time_record)<='08:40:00') ,1,0)" +
                    " when e.employee_shift = 2 then if((min(td.time_record)<='09:10:00'), 1,0)" +
                    " else  if((min(td.time_record)<='09:40:00'), 1,0)" +
                    " end as INCount, Max(td.time_record) as TimeOUT, " +
                    " case when  e.employee_shift = 1 then if((Max(td.time_record)>='17:00:00') ,1,0)" +
                    " when e.employee_shift = 2 then if((Max(td.time_record)>='17:00:00'), 1,0)" +
                    " else  if((Max(td.time_record)>='16:30:00'), 1,0)" +
                    " end as OUTCount " +

                    " from thumpdata_nimra td join employees_nimra e on td.emp_id=e.employee_id where td.emp_id='" + Halfday_Emp_cb.SelectedValue + "' and td.date_record like '" + dtcheck + "-%' AND DAYOFWEEK(td.date_record) <> 1 group by DatePunched ;", conn);

                DataTable dataTable = new DataTable();
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);

                da.Fill(dataTable);

                DataGridViewCheckBoxColumn checkBoxColumn = new DataGridViewCheckBoxColumn();
                //Add a CheckBox Column to the DataGridView at the first position.
                checkBoxColumn.HeaderText = "";
                checkBoxColumn.Width = 30;
                checkBoxColumn.Name = "checkBoxColumn";
                AddData_dgv.Columns.Insert(0, checkBoxColumn);
              
              
                AddData_dgv.DataSource = dataTable;

                for (int i = 0; i < AddData_dgv.Rows.Count; i++)
                {
                    DataGridViewRowHeaderCell cell = AddData_dgv.Rows[i].HeaderCell;
                    cell.Value = (i + 1).ToString();
                    AddData_dgv.Rows[i].HeaderCell = cell;
                }

                AddData_dgv.Columns[3].Visible = false;
                AddData_dgv.Columns[4].Visible = false;
                AddData_dgv.Columns[5].Visible = false;
                AddData_dgv.Columns[6].Visible = false;

                DataGridViewRow row = new DataGridViewRow();

                for (int i = 0; i < AddData_dgv.Rows.Count; i++)
                {
                    int in1 = int.Parse(AddData_dgv.Rows[i].Cells[4].Value.ToString());
                    int out1 = int.Parse(AddData_dgv.Rows[i].Cells[6].Value.ToString());
                    if (in1 == 1 && out1 == 1 )
                    {
                        CurrencyManager currencyManager1 = (CurrencyManager)BindingContext[AddData_dgv.DataSource];
                        currencyManager1.SuspendBinding();
                        AddData_dgv.Rows[i].Visible = false;
                        currencyManager1.ResumeBinding();
                    }
                  
                }


             


                //conn.Close();






                MySqlCommand cmd2 = new MySqlCommand(" SELECT dayname(halfdays_date) as DayName,halfdays_date as Date from halfdays_nimra where emp_id= '" + Halfday_Emp_cb.SelectedValue + "' and halfdays_date like '" + dtcheck + "-%'", conn);
                DataTable dT2 = new DataTable();
                MySqlDataAdapter da2 = new MySqlDataAdapter(cmd2);

                da2.Fill(dT2);
                ShowData_dgv.DataSource = dT2;


                for (int i = 0; i < ShowData_dgv.Rows.Count; i++)
                {
                    DataGridViewRowHeaderCell cell = ShowData_dgv.Rows[i].HeaderCell;
                    cell.Value = (i + 1).ToString();
                    ShowData_dgv.Rows[i].HeaderCell = cell;
                }

                //conn.Close();
                //Add a CheckBox Column to the DataGridView at the first position.
                DataGridViewCheckBoxColumn checkBoxColumn2 = new DataGridViewCheckBoxColumn();
                checkBoxColumn2.HeaderText = "";
                checkBoxColumn2.Width = 30;
                checkBoxColumn2.Name = "checkBoxColumn";
                ShowData_dgv.Columns.Insert(0, checkBoxColumn2);
                this.Cursor = Cursors.Default;
            }
        }


        private void Halfday_Emp_cb_SelectedIndexChanged(object sender, EventArgs e)
        {
            Fill_Halfdays_DGV();
        }

        private void Addhalfday_btn_Click(object sender, EventArgs e)
        {
            int inserted = 0;
            foreach (DataGridViewRow row in AddData_dgv.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                if (isSelected)
                {



                    using (MySqlCommand cmd = new MySqlCommand("INSERT INTO halfdays_nimra(emp_id,halfdays_date) VALUES(@emp_id, @halfdays_date)", conn))
                    {
                        cmd.Parameters.AddWithValue("@emp_id", Halfday_Emp_cb.SelectedValue);
                        cmd.Parameters.AddWithValue("@halfdays_date", row.Cells["DatePunched"].Value);



                        cmd.ExecuteNonQuery();

                    }

                    inserted++;
                }
            }
            Fill_Halfdays_DGV();
            if (inserted > 0)
            {
                MessageBox.Show(string.Format("{0} HR or 1/2 Days Added.", inserted), "Message");
            }
        }

        private void Deletehalfday_btn_Click(object sender, EventArgs e)
        {
            int inserted = 0;
            foreach (DataGridViewRow row in ShowData_dgv.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["checkBoxColumn"].Value);
                if (isSelected)
                {



                    using (MySqlCommand cmd = new MySqlCommand("DELETE FROM halfdays_nimra where emp_id=@emp_id and halfdays_date=@halfdays_date", conn))
                    {
                        cmd.Parameters.AddWithValue("@emp_id", Halfday_Emp_cb.SelectedValue);
                        cmd.Parameters.AddWithValue("@halfdays_date", row.Cells["Date"].Value);



                        cmd.ExecuteNonQuery();

                    }

                    inserted++;
                }
            }
            Fill_Halfdays_DGV();
            if (inserted > 0)
            {
                MessageBox.Show(string.Format("{0} HR or 1/2 Days Removed.", inserted), "Message");
            }
        }


        /// <summary>
        /// Code for AddHalfDays Ended Here
        /// </summary>
        /// 

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Code for StaffWiseAttendance Started Here
        /// </summary>
        /// 

        public void Fill_EmpData()
        {
            //conn.Open();
          
            MySqlCommand cmd = new MySqlCommand("select employee_id,employee_name from employees_nimra where employee_status=1;", conn);

            DataTable dt = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);

            da.Fill(dt);
            Emp_SWA_CB.DisplayMember = "employee_name";
            Emp_SWA_CB.ValueMember = "employee_id";

            Emp_SWA_CB.DataSource = dt;
            //conn.Close();
        }

        

        private void Month_CB_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            //MessageBox.Show(Month_CB.SelectedIndex.ToString());

        }

        

        private void Month_btn_Click(object sender, EventArgs e)
        {
            //conn.Open();
            label47.Text = "InSum";
            label48.Text = "OutSum";
            label50.Text = "Late-Ins";
            label49.Visible = true;
            Earlyouts_tb.Visible = true;
            label51.Visible = true;
            sumcount.Visible = true;
            string dtcheck, add0;
            int month = Month_CB.SelectedIndex + 1;
            if (month < 10)
            {
                add0 = "0" + month;
            }
            else
            {
                add0 = month.ToString();
            }
            string year = System.DateTime.Today.Year.ToString();
            
            dtcheck = year + "-" + add0;
            //MessageBox.Show(dtcheck);
            this.Cursor = Cursors.WaitCursor;
           
            MySqlCommand cmd = new MySqlCommand("select dayname(td.date_record) as Day, td.date_record as DatePunched, min(td.time_record) as TimeIN," +
                " case when  e.employee_shift = 1 then if((min(td.time_record)<='08:40:00') ,1,0)" +
                    " when e.employee_shift = 2 then if((min(td.time_record)<='09:10:00'), 1,0)" +
                    " else  if((min(td.time_record)<='09:40:00'), 1,0)" +
                    " end as INCount, Max(td.time_record) as TimeOUT, " +
                    " case when  e.employee_shift = 1 then if((Max(td.time_record)>='17:00:00') ,1,0)" +
                    " when e.employee_shift = 2 then if((Max(td.time_record)>='17:00:00'), 1,0)" +
                    " else  if((Max(td.time_record)>='16:30:00'), 1,0)" +
                    " end as OUTCount " +

                "from thumpdata_nimra td join employees_nimra e on td.emp_id=e.employee_id where td.emp_id='" + Emp_SWA_CB.SelectedValue + "' and td.date_record like '" + dtcheck + "-%' AND DAYOFWEEK(td.date_record) <> 1 group by DatePunched union all select employee_id ,employee_code, employee_name,null,null,null  from employees_nimra where employee_id='" + Emp_SWA_CB.SelectedValue + "';", conn);

            DataTable dataTable = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);

            da.Fill(dataTable);

            int insum = 0, outsum = 0, la = 0, le = 0;
            foreach (DataRow row in dataTable.Rows)
            {
                int INCount = 0, OUTCount = 0;
                int.TryParse(row["INCount"] + "", out INCount);
                if (INCount == 0)
                {
                    la++;
                }
                int.TryParse(row["OUTCount"] + "", out OUTCount);
                if (OUTCount == 0)
                {
                    le++;
                }
                //int.TryParse(row["UserDisc_ColumnName"] + "", out UserDisc);
                //int linetotal = (UserQty * UserPrice) - (UserQty * UserDisc);
                insum += INCount;
                outsum += OUTCount;

            }
            incountsum.Text = insum.ToString();
            outcountsum.Text = outsum.ToString();
            int la1, le1;
            la1 = la - 1;
            le1 = le - 1;

            double computedcount = (double)(insum + outsum) / 2;
            sumcount.Text = computedcount.ToString();

            Lateins_tb.Text = la1.ToString();
            Earlyouts_tb.Text = le1.ToString();

         
        
            Monthly_dgv.DataSource = dataTable;

            for (int i = 0; i < Monthly_dgv.Rows.Count; i++)
            {
                DataGridViewRowHeaderCell cell = Monthly_dgv.Rows[i].HeaderCell;
                cell.Value = (i + 1).ToString();
              
                Monthly_dgv.Rows[i].HeaderCell = cell;
          
            }
            this.Cursor = Cursors.Default;
            //conn.Close();
        }

        

        private void Day_btn_Click(object sender, EventArgs e)
        {
            //conn.Open();
            label47.Text = "Total";
            label48.Text = "Present";
            label50.Text = "Abent";
           
            MySqlCommand cmd = new MySqlCommand("select  e.employee_id as EmployeeID, e.employee_code as EmployeeCode, e.employee_name as EmployeeName , min(td.time_record) as InTime," +
               " case when  e.employee_shift = 1 then if((min(td.time_record)<='08:40:00') ,1,0)" +
                    " when e.employee_shift = 2 then if((min(td.time_record)<='09:10:00'), 1,0)" +
                    " else  if((min(td.time_record)<='09:40:00'), 1,0)" +
                    " end as INCount, Max(td.time_record) as TimeOUT, " +
                    " case when  e.employee_shift = 1 then if((Max(td.time_record)>='17:00:00') ,1,0)" +
                    " when e.employee_shift = 2 then if((Max(td.time_record)>='17:00:00'), 1,0)" +
                    " else  if((Max(td.time_record)>='16:30:00'), 1,0)" +
                    " end as OUTCount " +
                    " from thumpdata_nimra td join employees_nimra e on td.emp_id = e.employee_id where td.date_record = '" + Date_dtp.Text + "' group by e.employee_id  union all  select distinct(dayname(date_record)), date_record,null,null,null,null,null from thumpdata_nimra where date_record='" + Date_dtp.Text + "'  ;", conn);

            DataTable dataTable = new DataTable();
            MySqlDataAdapter da = new MySqlDataAdapter(cmd);

            da.Fill(dataTable);
            int present=0,absent=0;
            foreach (DataRow row in dataTable.Rows)
            {
                
                    present++;
                
            }

                Monthly_dgv.DataSource = dataTable;

            for (int i = 0; i < Monthly_dgv.Rows.Count; i++)
            {
                DataGridViewRowHeaderCell cell = Monthly_dgv.Rows[i].HeaderCell;
                cell.Value = (i + 1).ToString();
                Monthly_dgv.Rows[i].HeaderCell = cell;
            }

            incountsum.Text = TotalStaff_TB.Text;
            outcountsum.Text = present.ToString();
            absent = int.Parse(TotalStaff_TB.Text) - present;
            Lateins_tb.Text = absent.ToString();
            label49.Visible = false;
            Earlyouts_tb.Visible = false;
            label51.Visible = false;
            sumcount.Visible = false;
            //conn.Close();
        }

        

        private void ExportMonthly_btn_Click(object sender, EventArgs e)
        {
            /*
            // creating Excel Application  
            Microsoft.Office.Interop.Excel._Application app = new Microsoft.Office.Interop.Excel.Application();
            // creating new WorkBook within Excel application  
            Microsoft.Office.Interop.Excel._Workbook workbook = app.Workbooks.Add(Type.Missing);
            // creating new Excelsheet in workbook  
            Microsoft.Office.Interop.Excel._Worksheet worksheet = null;
            // see the excel sheet behind the program  
            app.Visible = true;
            // get the reference of first sheet. By default its name is Sheet1.  
            // store its reference to worksheet  
            worksheet = workbook.Sheets["Sheet1"];
            worksheet = workbook.ActiveSheet;
            // changing the name of active sheet  
            worksheet.Name = "Exported from gridview";
            // storing header part in Excel  
            for (int i = 1; i < Monthly_dgv.Columns.Count + 1; i++)
            {
                worksheet.Cells[1, i] = Monthly_dgv.Columns[i - 1].HeaderText;
            }
            // storing Each row and column value to excel sheet  
            for (int i = 0; i < Monthly_dgv.Rows.Count - 1; i++)
            {
                for (int j = 0; j < Monthly_dgv.Columns.Count; j++)
                {
                    worksheet.Cells[i + 2, j + 1] = Monthly_dgv.Rows[i].Cells[j].Value.ToString();
                }
            }
            // save the application  


            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)+"\\"+Emp_SWA_CB.Text+".xls";


            //workbook.SaveAs(path, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing, true, false, XlSaveAsAccessMode.xlNoChange, XlSaveConflictResolution.xlLocalSessionChanges, Type.Missing, Type.Missing);

            workbook.SaveAs(path, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            // Exit from the application  
            //app.Quit();
            */
            DGVPrinter printer = new DGVPrinter();

            printer.Title = "Nimra Educational Society Technical Campus";

            printer.SubTitle = " '"+ Emp_SWA_CB .Text + "' BioMetric Attendance Report" ;

            printer.SubTitleFormatFlags = StringFormatFlags.LineLimit |

                                          StringFormatFlags.NoClip;

            printer.PageNumbers = true;

            printer.PageNumberInHeader = false;

            printer.PorportionalColumns = true;

            printer.HeaderCellAlignment = StringAlignment.Near;

            printer.Footer = "Shahs BioMetric";

            printer.FooterSpacing = 15;



            printer.PrintDataGridView(Monthly_dgv);
        }

        

        private void ExportDaily_btn_Click(object sender, EventArgs e)
        {
            /*
            // creating Excel Application  
            Microsoft.Office.Interop.Excel._Application app = new Microsoft.Office.Interop.Excel.Application();
            // creating new WorkBook within Excel application  
            Microsoft.Office.Interop.Excel._Workbook workbook = app.Workbooks.Add(Type.Missing);
            // creating new Excelsheet in workbook  
            Microsoft.Office.Interop.Excel._Worksheet worksheet = null;
            // see the excel sheet behind the program  
            app.Visible = true;
            // get the reference of first sheet. By default its name is Sheet1.  
            // store its reference to worksheet  
            worksheet = workbook.Sheets["Sheet1"];
            worksheet = workbook.ActiveSheet;
            // changing the name of active sheet  
            worksheet.Name = "Exported from gridview";
            // storing header part in Excel  
            for (int i = 1; i < Monthly_dgv.Columns.Count + 1; i++)
            {
                worksheet.Cells[1, i] = Monthly_dgv.Columns[i - 1].HeaderText;
            }
            // storing Each row and column value to excel sheet  
            for (int i = 0; i < Monthly_dgv.Rows.Count - 1; i++)
            {
                for (int j = 0; j < Monthly_dgv.Columns.Count; j++)
                {
                    worksheet.Cells[i + 2, j + 1] = Monthly_dgv.Rows[i].Cells[j].Value.ToString();
                }
            }
            // save the application  


            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + Date_dtp.Text + ".xls";


            //workbook.SaveAs(path, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing, true, false, XlSaveAsAccessMode.xlNoChange, XlSaveConflictResolution.xlLocalSessionChanges, Type.Missing, Type.Missing);

            workbook.SaveAs(path, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            // Exit from the application  
            //app.Quit();
            */
            DGVPrinter printer = new DGVPrinter();

            printer.Title = "Nimra Educational Society Technical Campus";

            printer.SubTitle = " '" + Date_dtp.Text + "' BioMetric Attendance Report";

            printer.SubTitleFormatFlags = StringFormatFlags.LineLimit |

                                          StringFormatFlags.NoClip;

            printer.PageNumbers = true;

            printer.PageNumberInHeader = false;

            printer.PorportionalColumns = true;

            printer.HeaderCellAlignment = StringAlignment.Near;

            printer.Footer = "Shahs BioMetric";

            printer.FooterSpacing = 15;



            printer.PrintDataGridView(Monthly_dgv);
        }

       

















        /// <summary>
        /// Code for StaffWiseAttendance Ended Here
        /// </summary>
        /// 

        //7777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777

        //8888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888

        /// <summary>
        /// Code for SalaryStatement Started Here
        /// </summary>
        /// 





        private void SalaryStatement_btn_Click(object sender, EventArgs e)
        {
            //conn.Open();
            this.Cursor = Cursors.WaitCursor;


            //conn.Open();
            string dtcheck;
            string month = Month_SS_dtp.Text;
            string monthName = Month_SS_dtp.Value.ToString("MMM", CultureInfo.InvariantCulture);
            //MessageBox.Show(monthName);
            dtcheck = Month_SS_dtp.Text;
            int year11 = Month_SS_dtp.Value.Year;
            string tablename = "salary_" + month;
            MySqlCommand cmd = new MySqlCommand("CREATE TABLE IF NOT EXISTS Salary_"+ monthName + " ( " +
                " `employee_id` int(11) NOT NULL, " +
                " `attended_days` double DEFAULT NULL," +
                "  `cl` double DEFAULT NULL, " +
                " `ph` double DEFAULT NULL, " +
                " `od` double DEFAULT NULL, " +
                " `hr` double DEFAULT NULL, " +
                " `lop` double DEFAULT NULL, " +
                " `pf` double DEFAULT NULL, " +
                " `esi` double DEFAULT NULL, " +
                " `la` double DEFAULT NULL, " +
                " `le` double DEFAULT NULL, " +
                " `lop_amount` double DEFAULT NULL, " +
                "  `totalpaid_days` double DEFAULT NULL, " +
                " `salary_advance` double DEFAULT NULL, " +
                "  `net_salary` double DEFAULT NULL, " +
                " `epf_amount` double DEFAULT NULL, " +
                "  `yearly_esi` double DEFAULT NULL, " +

                 "   UNIQUE KEY `employee_id_UNIQUE` (`employee_id`), " +

                " FOREIGN KEY  (`employee_id`) " +
                "  REFERENCES `employees_nimra` (`employee_id`) " +
                " ON DELETE NO ACTION " +
                    " ON UPDATE NO ACTION ) ENGINE=InnoDB DEFAULT CHARSET=utf8  ; ", conn);
            //cmd.Parameters.AddWithValue("@tablename", tablename);
            cmd.ExecuteNonQuery();
          
            //conn.Close();


            
            string ph = "0";
            //holidays.Enabled = true;
          
            double publicholiday;


            MySqlCommand cmd0 = new MySqlCommand("select distinct(count(ph_date)) as holiday_date from public_holidays  where ph_date like '" + dtcheck + "-%'", conn);

            DataTable dataTable0 = new DataTable();
            MySqlDataAdapter da0 = new MySqlDataAdapter(cmd0);

            da0.Fill(dataTable0);

            
            foreach (DataRow row in dataTable0.Rows)
            {
                ph = row[0].ToString();
            }

            //MessageBox.Show(ph);
            
            publicholiday = double.Parse(ph);



            MySqlCommand cmd1 = new MySqlCommand("SELECT distinct(employee_id) AS empid FROM employees_nimra order by employee_code", conn);
           

            DataTable dataTable1 = new DataTable();
            MySqlDataAdapter da1 = new MySqlDataAdapter(cmd1);

            da1.Fill(dataTable1);


            foreach (DataRow row0 in dataTable1.Rows)
            {

             
                int empid, sundaycount, totaldays, pfstate, esistate, basic = 0, ptcheck = 0, itcheck = 0, noofdays = 0;
                double sumcount, salarydays, workeddays, onduty, hourpermission, earnings, perdaysal, lopamt, netsalary, casualleaves, minus, lopdays = 0.0, incountsum, outcountsum;
                empid = int.Parse(row0[0].ToString());
                
                //MessageBox.Show(empid.ToString());
                MySqlCommand cmd21 = new MySqlCommand("INSERT IGNORE INTO Salary_" + monthName + " values  ( " + empid + ",0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0)  ", conn);

                cmd21.ExecuteNonQuery();

                MySqlCommand cmd3 = new MySqlCommand("select dayname(td.date_record) as Day, td.date_record as DatePunched, min(td.time_record) as TimeIN," +
                " case when  e.employee_shift = 1 then if((min(td.time_record)<='08:40:00') ,1,0)" +
                " when e.employee_shift = 2 then if((min(td.time_record)<='09:10:00'), 1,0)" +
                " else  if((min(td.time_record)<='09:40:00'), 1,0)" +
                " end as INCount, Max(td.time_record) as TimeOUT, " +
                " case when  e.employee_shift = 1 then if((Max(td.time_record)>='17:00:00') ,1,0)" +
                " when e.employee_shift = 2 then if((Max(td.time_record)>='17:00:00'), 1,0)" +
                " else  if((Max(td.time_record)>='16:30:00'), 1,0)" +
                " end as OUTCount " +

                "from thumpdata_nimra td join employees_nimra e on td.emp_id=e.employee_id where td.emp_id="+ empid + " and td.date_record like '" + dtcheck + "-%' AND DAYOFWEEK(td.date_record) <> 1 group by DatePunched ;", conn);

                DataTable dataTable3 = new DataTable();
                MySqlDataAdapter da3 = new MySqlDataAdapter(cmd3);

                da3.Fill(dataTable3);

                int insum = 0, outsum = 0, la = 0, le = 0;
                foreach (DataRow row in dataTable3.Rows)
                {
                    int INCount = 0, OUTCount = 0;
                    int.TryParse(row["INCount"] + "", out INCount);
                    if (INCount == 0)
                    {
                        la++;
                    }
                    int.TryParse(row["OUTCount"] + "", out OUTCount);
                    if (OUTCount == 0)
                    {
                        le++;
                    }
                    //int.TryParse(row["UserDisc_ColumnName"] + "", out UserDisc);
                    //int linetotal = (UserQty * UserPrice) - (UserQty * UserDisc);
                    insum += INCount;
                    outsum += OUTCount;

                }
                incountsum = insum;
                outcountsum = outsum;
             
                double computedcount = (double)(insum + outsum) / 2;
                sumcount = computedcount;
                //MessageBox.Show("SumCount insum + outsum "+sumcount);
                MySqlCommand cmd4 = new MySqlCommand("update Salary_" + monthName + " set attended_days='" + sumcount + "', la='" + la + "', le='" + le + "' where employee_id='" + empid + "'  ", conn);
                cmd4.ExecuteNonQuery();

             

                //First We find out last date of mont
                int year = Month_SS_dtp.Value.Year;
                int month1 = Month_SS_dtp.Value.Month;
                
                //MessageBox.Show("Date for sundays " + date1);
                DateTime endOfMonth = new DateTime(year, month1, DateTime.DaysInMonth(year, month1));
                //get only last day of month
                int day = endOfMonth.Day;

                DateTime now = DateTime.Now;
                int count;
                count = 0;
                for (int i = 0; i < day; ++i)
                {
                    noofdays++;
                    DateTime d = new DateTime(year, month1, i + 1);
                    //Compare date with sunday
                    if (d.DayOfWeek == DayOfWeek.Sunday)
                    {
                        count = count + 1;
                    }
                }
                sundaycount = count;

              
                int jobtype = 0, parttimedays = 0;
                MySqlCommand cmd18 = new MySqlCommand("select emp_jobtype, emp_ptdays from empdetails_nimra where emp_id = '" + empid + "'" , conn);
                string ptdays = "0";
                using (MySqlDataReader reader12 = cmd18.ExecuteReader())
                {
                    while (reader12.Read())
                    {
                        jobtype = int.Parse((reader12["emp_jobtype"].ToString()));
                        ptdays = (reader12["emp_ptdays"].ToString());

                    }
                }

                parttimedays = int.Parse((ptdays));

                if(jobtype==2)
                {
                    totaldays = parttimedays;
                }
                else
                {
                    totaldays = noofdays;
                }
               
                //MessageBox.Show("Total Days in Month " + totaldays);
                if (double.Parse(sumcount.ToString()) == 0)
                {
                    MySqlCommand cmd5 = new MySqlCommand("update Salary_" + monthName + " set attended_days=0, totalpaid_days=0, lop=" + totaldays + " where employee_id='" + empid + "'", conn);
                    cmd5.ExecuteNonQuery();

                    salarydays = workeddays = 0.0;

                }
                else
                {
                    int a = sundaycount;
                    double b = sumcount;//attended_days->sumcount
                    //MessageBox.Show("Sundays " + a + " SumCount insum + outsum " + b);
                    //Attended Days Update
                    MySqlCommand cmd6 = new MySqlCommand("update Salary_" + monthName + " set attended_days='" + b + "' where employee_id='" + empid + "'", conn);
                    cmd6.ExecuteNonQuery();

                    MySqlCommand cmd7 = new MySqlCommand("update Salary_" + monthName + " set ph='" + publicholiday + "' where employee_id='" + empid + "' ", conn);
                    cmd7.ExecuteNonQuery();


                    MySqlCommand cmd8 = new MySqlCommand("select distinct(count(od_date)) as odcount from ods_nimra  where emp_id = '" + empid + "' and od_date like '" + dtcheck + "-%'", conn);
                    string od = "0";
                    using (MySqlDataReader reader2 = cmd8.ExecuteReader())
                    {
                        while (reader2.Read())
                        {
                            od = (reader2["odcount"].ToString());

                        }
                    }

                    onduty = double.Parse(od);

                    MySqlCommand cmd9 = new MySqlCommand("update Salary_" + monthName + " set od='" + onduty + "' where employee_id='" + empid + "' ", conn);
                    cmd9.ExecuteNonQuery();





                    MySqlCommand cmd10 = new MySqlCommand("select distinct(count(halfdays_date)) as hrcount from halfdays_nimra  where emp_id = '" + empid + "' and halfdays_date like '" + dtcheck + "-%'", conn);
                    string hr = "0";
                    using (MySqlDataReader reader3 = cmd10.ExecuteReader())
                    {
                        while (reader3.Read())
                        {
                            hr = (reader3["hrcount"].ToString());

                        }
                    }

                    hourpermission = double.Parse(hr);

                    MySqlCommand cmd11 = new MySqlCommand("update Salary_" + monthName + " set hr='" + hourpermission + "' where employee_id='" + empid + "' ", conn);
                    cmd11.ExecuteNonQuery();

                    double summed = (double)(a + b + publicholiday + onduty + hourpermission);
                    minus = totaldays - summed;
                    workeddays = summed;
                    //MessageBox.Show("Total days added summed="+ summed+ "Days subtracted Minus="+minus);
                    //if (summed < double.Parse(totaldays))
                   
                        MySqlCommand cmd12 = new MySqlCommand("select distinct(count(cl_date)) as clcount from cls_nimra  where emp_id = '" + empid + "' and cl_date like '" + dtcheck + "-%'", conn);
                        int cl = 0;
                        using (MySqlDataReader reader4 = cmd12.ExecuteReader())
                        {
                            while (reader4.Read())
                            {
                                cl = int.Parse((reader4["clcount"].ToString()));

                            }
                        }

                        casualleaves = double.Parse(cl.ToString());
                    /*
                    if (casualleaves == 0)
                    {
                        //minus = totaldays - summed;

                        //if (summed < double.Parse(totaldays.Text))
                        if (minus >= 1.0)
                        {

                            //CL update
                            MySqlCommand cmd13 = new MySqlCommand("update Salary_" + monthName + " set cl=1 where employee_id='" + empid + "'  ", conn);
                            cmd13.ExecuteNonQuery();

                            double cladded = summed + 1.0;

                            workeddays = cladded;

                        }
                        else if (minus > 0.0 && minus < 1.0)
                        {
                            //CL update
                            MySqlCommand cmd14 = new MySqlCommand("update Salary_" + monthName + " set cl=0.5 where employee_id='" + empid + "'  ", conn);
                            cmd14.ExecuteNonQuery();

                            double cladded = summed + 0.5;

                            workeddays = cladded;
                        }
                        else
                        {
                            workeddays = summed;

                        }
                    }
                    else
                    {
                    MySqlCommand cmd14 = new MySqlCommand("update Salary_" + monthName + " set cl='" + casualleaves+"' where employee_id='" + empid + "'  ", conn);
                    cmd14.ExecuteNonQuery();

                    double cladded = summed +casualleaves;
                    workeddays = cladded;

                    }
                    */
                    MySqlCommand cmd14 = new MySqlCommand("update Salary_" + monthName + " set cl='" + casualleaves + "' where employee_id='" + empid + "'  ", conn);
                    cmd14.ExecuteNonQuery();

                    double cladded = summed + casualleaves;
                    workeddays = cladded;


                    salarydays = workeddays;//paiddays
                    //MessageBox.Show("Salary Days after casual leavves" +salarydays);
                    double saldays = salarydays;
                    double ndays = totaldays;
                    //MessageBox.Show("total days after casual leavves" + ndays);
                    double lop = (double)(ndays - saldays);
                    lopdays = lop;

                    //MessageBox.Show("lop days after casual leavves" + lopdays);

                    MySqlCommand cmd15 = new MySqlCommand("update Salary_" + monthName + " set totalpaid_days='" + salarydays + "' where employee_id='" + empid + "' ", conn);
                    cmd15.ExecuteNonQuery();

                    MySqlCommand cmd16 = new MySqlCommand("update Salary_" + monthName + " set lop='" + lopdays + "' where employee_id='" + empid + "'  ", conn);
                    cmd16.ExecuteNonQuery();
                }



                MySqlCommand cmd17 = new MySqlCommand("select employee_te,employee_pt,employee_it,employee_gross,employees_pfstate,employees_esistate from employees_nimra where employee_id='" + empid + "'", conn);
                pfstate = 0; esistate = 0;
                double deductions1, gross;
                try
                {


                    using (MySqlDataReader reader5 = cmd17.ExecuteReader())
                    {
                        while (reader5.Read())
                        {
                            basic = int.Parse(reader5["employee_te"].ToString());
                            ptcheck = int.Parse(reader5["employee_pt"].ToString());
                            itcheck = int.Parse(reader5["employee_it"].ToString());
                            earnings = double.Parse(reader5["employee_gross"].ToString());
                            pfstate = int.Parse(reader5["employees_pfstate"].ToString());
                            esistate = int.Parse(reader5["employees_esistate"].ToString());

                        }
                    }
                }
                finally
                {
                    //conn.Close();
                }

                if (sumcount == 0)
                {
                    MySqlCommand cmd181 = new MySqlCommand("update Salary_" + monthName + " set lop_amount='" + basic + "', net_salary=0, pf=0, esi=0, epf_amount=0,yearly_esi=0  where employee_id='" + empid + "'  ", conn);
                    cmd181.ExecuteNonQuery();
                }
                else
                {


                    double perdaysalary = (double)(basic / totaldays);
                    perdaysal = perdaysalary;

                    double lopamount = (double)(perdaysalary * lopdays);
                    lopamt = lopamount;

                    gross = (double)(basic - lopamount);

                    int advances = 0;
                    MySqlCommand cmdadvances = new MySqlCommand("select advances_amount from advances_nimra where emp_id = '" + empid + "'", conn);
                    string adv = "0";
                    using (MySqlDataReader readerAdv = cmdadvances.ExecuteReader())
                    {
                        while (readerAdv.Read())
                        {
                           
                            adv = (readerAdv["advances_amount"].ToString());

                        }
                    }

                    if(!String.IsNullOrEmpty(adv))
                    {
                        advances = int.Parse(adv);
                    }
                    else
                    {
                        advances = 0;
                    }

                    double pf = 0, epf = 0, esi = 0, esie = 0;
                    if (pfstate == 1)
                    {
                        //PF= (Total earnings-LOP)/100*12,  EPF= (Total earnings-LOP)/100*13.61
                        pf = ((basic - lopamount) / 100) * 12;
                        epf = ((basic - lopamount) / 100) * 13.61;

                    }
                    else
                    {
                        pf = 0;
                        epf = 0;
                    }

                    if (esistate == 1)
                    {
                        //ESI =(Total earnings-LOP)/100*1.75,    ESI/E= (Total earnings-LOP)/100*4.75
                         esi = ((basic - lopamount) / 100) * 1.75;
                         esie = ((basic - lopamount) / 100) * 4.75;
                    }
                    else
                    {
                        esi = 0;
                        esie = 0;
                    }
                    deductions1 = ptcheck + itcheck + pf + esi + advances;
                    double netsal = (double)(gross - deductions1);
                    netsalary = netsal;
                    //conn.Open();

                    MySqlCommand cmd19 = new MySqlCommand("update employees_nimra set employee_deductions='" + deductions1 + "', employee_gross='" + gross + "'  where employee_id='" + empid + "'  ", conn);
                    cmd19.ExecuteNonQuery();

                    MySqlCommand cmd20 = new MySqlCommand("update Salary_" + monthName + " set lop_amount='" + lopamt + "', net_salary='" + netsalary + "', pf='" + pf + "', esi='" + esi + "', epf_amount='" + epf + "',yearly_esi='" + esie + "'  where employee_id='" + empid + "'  ", conn);
                    cmd20.ExecuteNonQuery();
                    //conn.Close();
                }

                

            }

            MySqlCommand cmd777 = new MySqlCommand("select e.employee_id, e.employee_code, e.employee_name,e.employee_gender, e.employee_dept, employee_college, e.employee_doj, e.employee_shift, e.employee_basic, e.employee_hra, e.employee_da, e.employee_te, s.cl,s.ph, s.od,s.lop, s.la, s.le, s.lop_amount,s.attended_days, s.totalpaid_days,e.employee_gross,s.pf, s.esi, e.employee_pt, e.employee_it, e.employee_deductions,    s.salary_advance, s.net_salary, s.epf_amount, s.yearly_esi  from employees_nimra e join salary_" + monthName + " s on e.employee_id=s.employee_id  order by e.employee_code ", conn);
           
            DataTable dataTable777 = new DataTable();
            MySqlDataAdapter da777 = new MySqlDataAdapter(cmd777);

            da777.Fill(dataTable777);

            SS_dgv.DataSource = dataTable777;
            for (int i = 0; i < SS_dgv.Rows.Count; i++)
            {
                DataGridViewRowHeaderCell cell = SS_dgv.Rows[i].HeaderCell;
                cell.Value = (i + 1).ToString();
                SS_dgv.Rows[i].HeaderCell = cell;
            }
            this.Cursor = Cursors.Default;
            
        }

        private void Salary_Export_btn_Click(object sender, EventArgs e)
        {
            // creating Excel Application  
            Microsoft.Office.Interop.Excel._Application app = new Microsoft.Office.Interop.Excel.Application();
            // creating new WorkBook within Excel application  
            Microsoft.Office.Interop.Excel._Workbook workbook = app.Workbooks.Add(Type.Missing);
            // creating new Excelsheet in workbook  
            Microsoft.Office.Interop.Excel._Worksheet worksheet = null;
            // see the excel sheet behind the program  
            app.Visible = true;
            // get the reference of first sheet. By default its name is Sheet1.  
            // store its reference to worksheet  
            worksheet = workbook.Sheets["Sheet1"];
            worksheet = workbook.ActiveSheet;
            // changing the name of active sheet  
            worksheet.Name = "Exported from gridview";
            // storing header part in Excel  
            for (int i = 1; i < SS_dgv.Columns.Count + 1; i++)
            {
                worksheet.Cells[1, i] = SS_dgv.Columns[i - 1].HeaderText;
            }
            // storing Each row and column value to excel sheet  
            for (int i = 0; i < SS_dgv.Rows.Count - 1; i++)
            {
                for (int j = 0; j < SS_dgv.Columns.Count; j++)
                {
                    worksheet.Cells[i + 2, j + 1] = SS_dgv.Rows[i].Cells[j].Value.ToString();
                }
            }
            // save the application  


            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + Month_SS_dtp.Text + ".xls";


            //workbook.SaveAs(path, Microsoft.Office.Interop.Excel.XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing, true, false, XlSaveAsAccessMode.xlNoChange, XlSaveConflictResolution.xlLocalSessionChanges, Type.Missing, Type.Missing);

            workbook.SaveAs(path, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlExclusive, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            // Exit from the application  
            //app.Quit();
            MessageBox.Show("Salary Statement Exported");
        }


        /// <summary>
        /// Code for SalaryStatement Ended Here
        /// </summary>
        /// 


        //8888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888






    }
}
