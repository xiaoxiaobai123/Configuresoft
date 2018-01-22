using ECAN;
using ECanTest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Configuresoft
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ComProc mCan = new ComProc();
        static object locker = new object();
        DateTime lastSendTime;
        DataStatus CurrentState = DataStatus.Idle;
        bool MainthreadFlag = false;
        uint CurrentMsgSent = 0;
        String TxtMsg = "";
        public MainWindow()
        {
            InitializeComponent();
            CheckDriverLoop();
            mCan.EnableProc = true;

            for (uint i = 0; i < 9; i++)
            {
                QuerySettingValue((uint)ConfigurationID.CAN_MSG_OVER_VOLTAGE_SETTING + i);
            }


            Thread Td = new Thread(CheckRunStatus);
            Td.Start();
            MainthreadFlag = true;

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0,0,0, 20);
            dispatcherTimer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            HinttextBox.Text = TxtMsg;
        }
        public void HintTextShow(string Msg)
        {
            TxtMsg += Msg + Environment.NewLine;
        }
        public string MessageType()
        {
            string MsgType = "";
            switch (CurrentMsgSent)
            {
                case (uint)ConfigurationID.CAN_MSG_OVER_VOLTAGE_SETTING:
                    MsgType = "过压设置";
                    break;
                case (uint)ConfigurationID.CAN_MSG_UNDER_VOLTAGE_SETTING:
                    MsgType = "低压设置";
                    break;
                case (uint)ConfigurationID.CAN_MSG_OVER_TEMP_SETTING:
                    MsgType = "过温设置";
                    break;
                case (uint)ConfigurationID.CAN_MSG_UNDER_TEMP_SETTING:
                    MsgType = "低温设置";
                    break;
                case (uint)ConfigurationID.CAN_MSG_HCR_TEMP_SETTING:
                    MsgType = "充电过流设置";
                    break;
                case (uint)ConfigurationID.CAN_MSG_HDR_TEMP_SETTING:
                    MsgType = "放电过流设置";
                    break;
                case (uint)ConfigurationID.BpCellEEPROMPage_SETTING:
                    MsgType = "BP数量和Cell数量设置";
                    break;
            }
            return MsgType;
        }
        void CheckRunStatus()
        {
            CAN_OBJ frameinfo = new CAN_OBJ();
            Deviceinfo Dc = new Deviceinfo();
            string MsgType = "";
            while (MainthreadFlag)
            {
                if (CurrentState == DataStatus.Idle)
                    ;
                else if(CurrentState == DataStatus.WaitAck)
                {
                    TimeSpan GworkTime = DateTime.Now - lastSendTime;
                    double Wholeseconds = GworkTime.TotalSeconds;
                    if (Wholeseconds >= 1)
                    {
                        MsgType = MessageType();
                        
                        HintTextShow(MsgType+ "未收到Ack，设置失败！");                       
                        CurrentState = DataStatus.Idle;
                    }
                }

                while (mCan.gRecMsgBufHead != mCan.gRecMsgBufTail)
                {
                    frameinfo = mCan.gRecMsgBuf[mCan.gRecMsgBufTail];
                    Dc.DeviceStruct.RawID = frameinfo.ID;


                    if (CurrentState == DataStatus.WaitAck)
                    {
                        TimeSpan GworkTime = DateTime.Now - lastSendTime;
                        double Wholeseconds = GworkTime.TotalSeconds;
                        if (Wholeseconds >= 1)
                        {
                            MsgType = MessageType();

                            HintTextShow(MsgType + "未收到Ack，设置失败！");
                            CurrentState = DataStatus.Idle;
                        }

                        if(Dc.DeviceStruct.MessageID == CurrentMsgSent)
                        {
                            CurrentState = DataStatus.AckReceived;
                            MsgType = MessageType();
                            HintTextShow(MsgType + "设置成功");
                        }
                    }
          
                }


                
            }

        }
        private void CheckDriverLoop()
        {
            //      Console                    .WriteLine("Searching for driver...");
            //      while (true)
            {

                System.Management.SelectQuery query = new System.Management.SelectQuery("Win32_PnPEntity");
                query.Condition = "Name = 'GC-Tech USBCAN Device'";
                System.Management.ManagementObjectSearcher searcher = new System.Management.ManagementObjectSearcher(query);
                var drivers = searcher.Get();

                if (drivers.Count > 0)
                {
                    if (ECANDLL.StartCAN(1, 0, 0) != ECAN.ECANStatus.STATUS_OK)
                    {
                        HintTextShow("Driver found.");
                        CanStartProcess();
                    }
                }
                else
                {                   
                    HintTextShow("Driver could not be found.");
                    ECANDLL.CloseDevice(1, 0);
                }
            }
        }

        public void CanStartProcess()
        {
            INIT_CONFIG init_config = new INIT_CONFIG();
            init_config.AccCode = 0x00000000; /*滤波寄存器*/
            init_config.AccMask = 0xFFFFFFFF;  /*屏蔽寄存器*/
            init_config.Filter = 0;   /*单滤波*/
            init_config.Timing0 = 0x03;
            init_config.Timing1 = 0x1c;
            init_config.Mode = 0;
            if (ECANDLL.OpenDevice(1, 0, 0) != ECAN.ECANStatus.STATUS_OK)
            {
                string Hint = "Seems already occouyed by another Software,Plz Check";
                HintTextShow(Hint);

            }
            if (ECANDLL.InitCAN(1, 0, 0, ref init_config) != ECAN.ECANStatus.STATUS_OK)
            {     
                ECANDLL.CloseDevice(1, 0);
                return;
            }
            if (ECANDLL.StartCAN(1, 0, 0) == ECAN.ECANStatus.STATUS_OK)
            {
                string Hint = "Start Successfully";
                HintTextShow(Hint);
                CAN_OBJ frameinfo = new CAN_OBJ();
                frameinfo.SendType = 0;
                frameinfo.data = new byte[8];
                frameinfo.ID = 0x800DD;
                frameinfo.DataLen = 8;
                frameinfo.ExternFlag = 1;
                for (int i = 0; i < frameinfo.DataLen; i++)
                    frameinfo.data[i] = (byte)i;
                FillSendBuffer(frameinfo);
                Thread.Sleep(50);

            }
        }
        void FillSendBuffer(CAN_OBJ frame)
        {
            lock (locker)
            {
                if (mCan.gSendMsgBufHead > ComProc.SEND_MSG_BUF_MAX - 1)
                    mCan.gSendMsgBufHead = 0;
                mCan.gSendMsgBuf[mCan.gSendMsgBufHead].ID = frame.ID;
                mCan.gSendMsgBuf[mCan.gSendMsgBufHead].DataLen = frame.DataLen;
                //   Array.Copy(frame.data, mCan.gSendMsgBuf[mCan.gSendMsgBufHead].data, frame.DataLen);
                mCan.gSendMsgBuf[mCan.gSendMsgBufHead].data = frame.data;
                mCan.gSendMsgBuf[mCan.gSendMsgBufHead].ExternFlag = frame.ExternFlag;
                mCan.gSendMsgBuf[mCan.gSendMsgBufHead].RemoteFlag = frame.RemoteFlag;
                mCan.gSendMsgBufHead += 1;
                if (mCan.gSendMsgBufHead > ComProc.SEND_MSG_BUF_MAX - 1)
                    mCan.gSendMsgBufHead = 0;
 
                //Thread.Sleep(50);


            }
        }

        public void QuerySettingValue(uint SettingMessageID)
        {
            CAN_OBJ burndatainfo1 = new CAN_OBJ();
            uint bpnumber = (uint)BpNumber.SelectedIndex + 1;
            uint stringnumber = (uint)SCNumber.SelectedIndex + 1;

            Deviceinfo sendDeviceInfo = new Deviceinfo();
            sendDeviceInfo.DeviceStruct.DeviceTypeID = (uint)DeviceType.ArrayController;
            sendDeviceInfo.DeviceStruct.StringID = (uint)stringnumber;                               /*暂时 用 广播替代*/
            sendDeviceInfo.DeviceStruct.BpID = bpnumber;
            sendDeviceInfo.DeviceStruct.MessagegroupID = (uint)MessageGroupId.Data;
            sendDeviceInfo.DeviceStruct.MessageID = SettingMessageID;
            burndatainfo1.ID = sendDeviceInfo.DeviceStruct.RawID;
            burndatainfo1.DataLen = 0;
            burndatainfo1.ExternFlag = 1;
            FillSendBuffer(burndatainfo1);
        }


        private void ReadNumber_Click(object sender, RoutedEventArgs e)
        {
            QuerySettingValue((uint)ConfigurationID.BpCellEEPROMPage_SETTING);
        }

        private void WriteNumber_Click(object sender, RoutedEventArgs e)
        {
            byte[] NumbersSetting = new byte[8];

            lastSendTime = DateTime.Now;
            if (SettingBpNumber.Text == "" || SettingCellNumber.Text == "" )
            {
                MessageBox.Show("输入不正确！");
                return;
            }
            int HTemp = Convert.ToInt16(SettingBpNumber.Text);
            NumbersSetting[0] = (byte)HTemp;

            NumbersSetting[1] = (byte)Convert.ToInt16(SettingCellNumber.Text);

            NumbersSetting[2] = 1;   /*设置成功标志位*/


            SendMsg((uint)ConfigurationID.BpCellEEPROMPage_SETTING, NumbersSetting);

            CurrentState = DataStatus.WaitAck;
            CurrentMsgSent = (uint)ConfigurationID.BpCellEEPROMPage_SETTING;
          
        }

        private void WriteHtempThreshold_Click(object sender, RoutedEventArgs e)
        {
            byte[] HTempInfo = new byte[8];

            if (EnterHTempAlarm_SettingValue.Text == "" || ExitHTempAlarm_SettingValue.Text == "" || EnterHTempWarning_SettingValue.Text == "" || ExitHTempWarning_SettingValue.Text == "")
            {
                MessageBox.Show("输入不正确！");
                return;
            }
            int HTemp = (int)(decimal.Parse(EnterHTempAlarm_SettingValue.Text) * 10);
            HTempInfo[0] = (byte)(HTemp >> 8);
            HTempInfo[1] = (byte)(HTemp);

            HTemp = (int)(decimal.Parse(ExitHTempAlarm_SettingValue.Text) * 10);
            HTempInfo[2] = (byte)(HTemp >> 8);
            HTempInfo[3] = (byte)(HTemp);

            HTemp = (int)(decimal.Parse(EnterHTempWarning_SettingValue.Text) * 10);
            HTempInfo[4] = (byte)(HTemp >> 8);
            HTempInfo[5] = (byte)(HTemp);

            HTemp = (int)(decimal.Parse(ExitHTempWarning_SettingValue.Text) * 10);
            HTempInfo[6] = (byte)(HTemp >> 8);
            HTempInfo[7] = (byte)(HTemp);


            SendMsg((uint)ConfigurationID.CAN_MSG_OVER_TEMP_SETTING, HTempInfo);

            CurrentState = DataStatus.WaitAck;
            CurrentMsgSent = (uint)ConfigurationID.CAN_MSG_OVER_TEMP_SETTING;
        }

        private void ReadLVolThreshold_Click(object sender, RoutedEventArgs e)
        {
            QuerySettingValue((uint)ConfigurationID.CAN_MSG_UNDER_VOLTAGE_SETTING);
        }

        private void ReadLTempThreshold_Click(object sender, RoutedEventArgs e)
        {
            QuerySettingValue((uint)ConfigurationID.CAN_MSG_UNDER_TEMP_SETTING);
        }

        private void WriteLTempThreshold_Click(object sender, RoutedEventArgs e)
        {
            byte[] LTempInfo = new byte[8];

            if (EnterLTempAlarm_SettingValue.Text == "" || ExitLTempAlarm_SettingValue.Text == "" || EnterLTempWarning_SettingValue.Text == "" || ExitLTempWarning_SettingValue.Text == "")
            {
                MessageBox.Show("输入不正确！");
                return;
            }
            int LTemp = (int)(decimal.Parse(EnterLTempAlarm_SettingValue.Text) * 10);
            LTempInfo[0] = (byte)(LTemp >> 8);
            LTempInfo[1] = (byte)(LTemp);

            LTemp = (int)(decimal.Parse(ExitLTempAlarm_SettingValue.Text) * 10);
            LTempInfo[2] = (byte)(LTemp >> 8);
            LTempInfo[3] = (byte)(LTemp);

            LTemp = (int)(decimal.Parse(EnterLTempWarning_SettingValue.Text) * 10);
            LTempInfo[4] = (byte)(LTemp >> 8);
            LTempInfo[5] = (byte)(LTemp);

            LTemp = (int)(decimal.Parse(ExitLTempWarning_SettingValue.Text) * 10);
            LTempInfo[6] = (byte)(LTemp >> 8);
            LTempInfo[7] = (byte)(LTemp);


            SendMsg((uint)ConfigurationID.CAN_MSG_UNDER_TEMP_SETTING, LTempInfo);

            CurrentState = DataStatus.WaitAck;
            CurrentMsgSent = (uint)ConfigurationID.CAN_MSG_UNDER_TEMP_SETTING;
        }

        private void ReadHChargeThreshold_Click(object sender, RoutedEventArgs e)
        {
            
            QuerySettingValue((uint)ConfigurationID.CAN_MSG_HCR_TEMP_SETTING);
        }

        private void WriteHChargeThreshold_Click(object sender, RoutedEventArgs e)
        {
            byte[] HChargeInfo = new byte[8];

            if (EnterHChargeAlarm_SettingValue.Text == "" || ExitHChargeAlarm_SettingValue.Text == "" || EnterHChargeWarning_SettingValue.Text == "" || ExitHChargeWarning_SettingValue.Text == "")
            {
                MessageBox.Show("输入不正确！");
                return;
            }
            int HCharge = (int)(decimal.Parse(EnterHChargeAlarm_SettingValue.Text) * 100);
            HChargeInfo[0] = (byte)(HCharge >> 8);
            HChargeInfo[1] = (byte)(HCharge);

            HCharge = (int)(decimal.Parse(ExitHChargeAlarm_SettingValue.Text) * 100);
            HChargeInfo[2] = (byte)(HCharge >> 8);
            HChargeInfo[3] = (byte)(HCharge);

            HCharge = (int)(decimal.Parse(EnterHChargeWarning_SettingValue.Text) * 100);
            HChargeInfo[4] = (byte)(HCharge >> 8);
            HChargeInfo[5] = (byte)(HCharge);

            HCharge = (int)(decimal.Parse(ExitHChargeWarning_SettingValue.Text) * 100);
            HChargeInfo[6] = (byte)(HCharge >> 8);
            HChargeInfo[7] = (byte)(HCharge);


            SendMsg((uint)ConfigurationID.CAN_MSG_HCR_TEMP_SETTING, HChargeInfo);

            CurrentState = DataStatus.WaitAck;
            CurrentMsgSent = (uint)ConfigurationID.CAN_MSG_HCR_TEMP_SETTING;
        }

        private void ReadDisChargeThreshold_Click(object sender, RoutedEventArgs e)
        {            
            QuerySettingValue((uint)ConfigurationID.CAN_MSG_HDR_TEMP_SETTING);
        }

        private void WriteDisChargeThreshold_Click(object sender, RoutedEventArgs e)
        {
            byte[] HDischargeInfo = new byte[8];

            if (EnterHDisChargeAlarm_SettingValue.Text == "" || ExitHDisChargeAlarm_SettingValue.Text == "" || EnterHDisChargeWarning_SettingValue.Text == "" || ExitHDisChargeWarning_SettingValue.Text == "")
            {
                MessageBox.Show("输入不正确！");
                return;
            }
            short HDisharge = (short)(decimal.Parse(EnterHDisChargeAlarm_SettingValue.Text) * 100);
            HDischargeInfo[0] = (byte)(HDisharge >> 8);
            HDischargeInfo[1] = (byte)(HDisharge);

            HDisharge = (short)(decimal.Parse(ExitHDisChargeAlarm_SettingValue.Text) * 100);
            HDischargeInfo[2] = (byte)(HDisharge >> 8);
            HDischargeInfo[3] = (byte)(HDisharge);

            HDisharge = (short)(decimal.Parse(EnterHDisChargeWarning_SettingValue.Text) * 100);
            HDischargeInfo[4] = (byte)(HDisharge >> 8);
            HDischargeInfo[5] = (byte)(HDisharge);

            HDisharge = (short)(decimal.Parse(ExitHDisChargeWarning_SettingValue.Text) * 100);
            HDischargeInfo[6] = (byte)(HDisharge >> 8);
            HDischargeInfo[7] = (byte)(HDisharge);
            SendMsg((uint)ConfigurationID.CAN_MSG_HDR_TEMP_SETTING, HDischargeInfo);

            CurrentState = DataStatus.WaitAck;
            CurrentMsgSent = (uint)ConfigurationID.CAN_MSG_HDR_TEMP_SETTING;
        }

        private void BpNumberselect_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> LS = new List<string>();
            for (int i = 0; i < new Global().bpnumber; i++)
            {
                LS.Add("bp" + (i + 1).ToString());
            }
            BpNumber.ItemsSource = LS;
            BpNumber.SelectedIndex = 0;
        }

        private void SCNumberselect_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> LS = new List<string>();
            for (int i = 0; i < new Global().stringnumber; i++)
            {
                LS.Add("string" + (i + 1).ToString());
            }

            SCNumber.ItemsSource = LS;
            SCNumber.SelectedIndex = 0;
        }

        private void SCNumberselect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BpNumberselect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ReadOVThreshold_Click(object sender, RoutedEventArgs e)
        {
            QuerySettingValue((uint)ConfigurationID.CAN_MSG_OVER_VOLTAGE_SETTING);
        }

        private void ReadHtempThreshold_Click(object sender, RoutedEventArgs e)
        {
            QuerySettingValue((uint)ConfigurationID.CAN_MSG_OVER_TEMP_SETTING);
        }

        private void OverVolAlarmWarning_SettingValue_LostFocus(object sender, RoutedEventArgs e)
        {
            float VolAlarmWarning = 0;
            TextBox Texttemp = (TextBox)sender;

            if (Texttemp.Text.Length < 3)
                return;
            
            try
            {
                VolAlarmWarning = float.Parse(Texttemp.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Texttemp.Text = null;
                Texttemp.Focus();
                return;
            }
            if (VolAlarmWarning >= 2.5 && VolAlarmWarning <= 3.7)
            {

            }
            else
            {
                Texttemp.Text = null;
                MessageBox.Show("输入的电压报警数值不正确！");
            }
        }

        private void OverTempAlarmWarning_SettingValue_LostFocus(object sender, RoutedEventArgs e)
        {
            float TemperatureAlarmWarning = 0;
            TextBox Texttemp = (TextBox)sender;
            if (Texttemp.Text.Length < 3)
                return;


            try
            {
                TemperatureAlarmWarning = float.Parse(Texttemp.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Texttemp.Text = null;
                return;
            }
            if (TemperatureAlarmWarning >= -10 && TemperatureAlarmWarning <= 60)
            {

            }
            else
            {
                Texttemp.Text = null;
                MessageBox.Show("输入的温度报警数值不正确！");
            }
        }

        private void UnderVolAlarmWarning_SettingValue_LostFocus(object sender, RoutedEventArgs e)
        {
            float VolAlarmWarning = 0;
            TextBox Texttemp = (TextBox)sender;

            if (Texttemp.Text.Length < 3)
                return;

            try
            {
                VolAlarmWarning = float.Parse(Texttemp.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Texttemp.Text = null;
                // EnterHVolAlarm_SettingValue.Focus();
                return;
            }
            if (VolAlarmWarning >= 2.4 && VolAlarmWarning <= 3.0)
            {

            }
            else
            {
                Texttemp.Text = null;
                MessageBox.Show("输入的电压报警数值不正确！");
            }
        }

        private void UnderTempAlarmWarning_SettingValue_LostFocus(object sender, RoutedEventArgs e)
        {
            float TemperatureAlarmWarning = 0;
            TextBox Texttemp = (TextBox)sender;

            if (Texttemp.Text.Length < 3)
                return;

            try
            {
                TemperatureAlarmWarning = float.Parse(Texttemp.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Texttemp.Text = null;
                return;
            }
            if (TemperatureAlarmWarning >= -10 && TemperatureAlarmWarning <= 60)
            {

            }
            else
            {
                Texttemp.Text = null;
                MessageBox.Show("输入的温度报警数值不正确！");
            }
        }

        private void HChargeAlarmWarning_SettingValue_LostFocus(object sender, RoutedEventArgs e)
        {
            float ChargeAlarmWarning = 0;
            TextBox Texttemp = (TextBox)sender;

            if (Texttemp.Text.Length < 3)
                return;

            try
            {
                ChargeAlarmWarning = float.Parse(Texttemp.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Texttemp.Text = null;
                return;
            }
            if (ChargeAlarmWarning >= 30 && ChargeAlarmWarning <= 200)
            {

            }
            else
            {
                Texttemp.Text = null;
                MessageBox.Show("输入的充电电流报警数值不正确！");
            }
        }

        private void HDisChargeAlarmWarning_SettingValue_LostFocus(object sender, RoutedEventArgs e)
        {
            float DisChargeAlarmWarning = 0;
            TextBox Texttemp = (TextBox)sender;
            if (Texttemp.Text.Length < 3)
                return;

            try
            {
                DisChargeAlarmWarning = float.Parse(Texttemp.Text, CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                Texttemp.Text = null;
                return;
            }
            if (DisChargeAlarmWarning >= -200 && DisChargeAlarmWarning <= -30)
            {

            }
            else
            {
                Texttemp.Text = null;
                MessageBox.Show("输入的充电电流报警数值不正确！");
            }
        }

        private void WriteOVThreshold_Click(object sender, RoutedEventArgs e)
        {
            byte[] HVolInfo = new byte[8];

            if (EnterHVolAlarm_SettingValue.Text == "" || ExitHVolAlarm_SettingValue.Text == "" || EnterHVolWarning_SettingValue.Text == "" || ExitHVolWarning_SettingValue.Text == "")
            {
                MessageBox.Show("输入不正确！");
                return;
            }
            int Hvol = (int)(decimal.Parse(EnterHVolAlarm_SettingValue.Text) * 1000);
            HVolInfo[0] = (byte)(Hvol >> 8);
            HVolInfo[1] = (byte)(Hvol);
          
            Hvol = (int)(decimal.Parse(ExitHVolAlarm_SettingValue.Text) * 1000);
            HVolInfo[2] = (byte)(Hvol >> 8);
            HVolInfo[3] = (byte)(Hvol);
          
            Hvol = (int)(decimal.Parse(EnterHVolWarning_SettingValue.Text) * 1000);
            HVolInfo[4] = (byte)(Hvol >> 8);
            HVolInfo[5] = (byte)(Hvol);
          
            Hvol = (int)(decimal.Parse(ExitHVolWarning_SettingValue.Text) * 1000);
            HVolInfo[6] = (byte)(Hvol >> 8);
            HVolInfo[7] = (byte)(Hvol);


            SendMsg((uint)ConfigurationID.CAN_MSG_OVER_VOLTAGE_SETTING, HVolInfo);

            CurrentState = DataStatus.WaitAck;
            CurrentMsgSent = (uint)ConfigurationID.CAN_MSG_OVER_VOLTAGE_SETTING;
        }

        private void WriteLVolThreshold_Click(object sender, RoutedEventArgs e)
        {
            byte[] LVolInfo = new byte[8];

            if(EnterLVolAlarm_SettingValue.Text == "" || ExitLVolAlarm_SettingValue.Text == "" || EnterLVolWarning_SettingValue.Text == "" || ExitLVolWarning_SettingValue.Text == "")
            {
                MessageBox.Show("输入不正确！");
                return;
            }
            int Lvol = (int)(decimal.Parse(EnterLVolAlarm_SettingValue.Text) * 1000);                                     
            LVolInfo[0] = (byte)(Lvol >> 8);
            LVolInfo[1] = (byte)(Lvol);

            Lvol = (int)(decimal.Parse(ExitLVolAlarm_SettingValue.Text) * 1000);
            LVolInfo[2] = (byte)(Lvol >> 8);
            LVolInfo[3] = (byte)(Lvol);

            Lvol = (int)(decimal.Parse(EnterLVolWarning_SettingValue.Text) * 1000);
            LVolInfo[4] = (byte)(Lvol >> 8);
            LVolInfo[5] = (byte)(Lvol);

            Lvol = (int)(decimal.Parse(ExitLVolWarning_SettingValue.Text) * 1000);
            LVolInfo[6] = (byte)(Lvol >> 8);
            LVolInfo[7] = (byte)(Lvol);

    
            SendMsg((uint)ConfigurationID.CAN_MSG_UNDER_VOLTAGE_SETTING, LVolInfo);

            CurrentState = DataStatus.WaitAck;
            CurrentMsgSent = (uint)ConfigurationID.CAN_MSG_UNDER_VOLTAGE_SETTING;
        }

        void SendMsg(uint msgID,byte[] data)
        {
            CAN_OBJ burndatainfo1 = new CAN_OBJ();
            uint bpnumber = (uint)BpNumber.SelectedIndex + 1;
            uint stringnumber = (uint)SCNumber.SelectedIndex + 1;

            Deviceinfo sendDeviceInfo = new Deviceinfo();
            sendDeviceInfo.DeviceStruct.DeviceTypeID = (uint)DeviceType.ArrayController;
            sendDeviceInfo.DeviceStruct.StringID = (uint)stringnumber;                               /*暂时 用 广播替代*/
            sendDeviceInfo.DeviceStruct.BpID = bpnumber;
            sendDeviceInfo.DeviceStruct.MessagegroupID = (uint)MessageGroupId.Data;
            sendDeviceInfo.DeviceStruct.MessageID = msgID;
            burndatainfo1.ID = sendDeviceInfo.DeviceStruct.RawID;
            burndatainfo1.DataLen = 8;
            burndatainfo1.ExternFlag = 1;
            burndatainfo1.data = data;
            FillSendBuffer(burndatainfo1);
        }
    }
}
