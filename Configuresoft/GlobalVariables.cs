using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Configuresoft
{
    class GlobalVariables
    {
    }
    public enum DeviceType
    {
        SystemController = 0x01,
        ArrayController = 0x02,
        RelayController = 0x03,
        StringController = 0x04,
        BatteryPackController = 0x05,
    }
    public enum MessageGroupId
    {
        Alarm = 0x00,
        Warning = 0x01,
        Error = 0x02,
        Command = 0x03,
        Data = 0x04,
        Configuration = 0x05,
        FirmwareUpdate = 0x06,
    }
    public enum ConfigurationID : byte
    {
        CAN_MSG_OVER_VOLTAGE_SETTING = 0xE0,
        CAN_MSG_UNDER_VOLTAGE_SETTING = 0xE1,
        CAN_MSG_OVER_TEMP_SETTING = 0xE2,
        CAN_MSG_UNDER_TEMP_SETTING = 0xE3,
        CAN_MSG_HCDT_SETTING = 0xE4, //High Cell Delta Temp(Diff Temp)
        CAN_MSG_HCTR_SETTING = 0xE5, //High Cell Temp Rise
        CAN_MSG_HCR_TEMP_SETTING = 0xE6, //High Charge Rate
        CAN_MSG_HDR_TEMP_SETTING = 0xE7, //High Discharge Rate
        BpCellEEPROMPage_SETTING = 0xE8
    }

    public enum DataStatus
    {
        Idle,Send,WaitAck,AckReceived,Receiving
    }


    public class Deviceinfo
    {
        public static int BurnePage;
        public static int TotalOnline;
        public static int canSendErrorTimeout
        {
            set; get;
        }
        public static bool devicecanstatus
        {
            set; get;
        } = true;
        public static bool FuncCanReceiveThreadFlag
        {
            set; get;
        } = true;
        public static bool EnableListenMode
        {
            set; get;
        } = true;
        public static bool BalancingEnable
        {
            set; get;
        } = true;
        public static int TargetValue
        {
            set; get;
        }
        public static int CellNumberValue
        {
            set; get;
        } = new Global().cellnumber;
        public static int BPNumberValue
        {
            set; get;
        } = new Global().bpnumber;
        public static int StringNumberValue
        {
            set; get;
        } = new Global().stringnumber;

        public static string CanPort
        {
            set; get;
        } = new Global().CanPort;

        public static uint MaxMsgCount
        {
            set; get;
        } = Convert.ToUInt32(new Global().CanMaxMsgCount);

        public static string CanBaudrate
        {
            set; get;
        } = new Global().CanBaudrate;

        public static string CanWriteTimeOut
        {
            set; get;
        } = new Global().CanWriteTimeOut;

        public static string CanReadTimeOut
        {
            set; get;
        } = new Global().CanReadTimeOut;

        int DeviceID { set; get; }
        int FirmwareType { set; get; }
        int FirmwareVersion { set; get; }

        public struct MyStruct
        {
            internal uint RawID;

            const int sz0 = 8, loc0 = 0, mask0 = ((1 << sz0) - 1) << loc0;
            const int sz1 = 6, loc1 = loc0 + sz0, mask1 = ((1 << sz1) - 1) << loc1;
            const int sz2 = 4, loc2 = loc1 + sz1, mask2 = ((1 << sz2) - 1) << loc2;
            const int sz3 = 4, loc3 = loc2 + sz2, mask3 = ((1 << sz3) - 1) << loc3;
            const int sz4 = 4, loc4 = loc3 + sz3, mask4 = ((1 << sz4) - 1) << loc4;

            public uint MessageID
            {
                get { return (uint)(RawID & mask0) >> loc0; }
                set { RawID = (uint)(RawID & ~mask0 | (value << loc0) & mask0); }
            }

            public uint BpID
            {
                get { return (uint)(RawID & mask1) >> loc1; }
                set { RawID = (uint)(RawID & ~mask1 | (value << loc1) & mask1); }
            }

            public uint StringID
            {
                get { return (uint)(RawID & mask2) >> loc2; }
                set { RawID = (uint)(RawID & ~mask2 | (value << loc2) & mask2); }
            }

            public uint DeviceTypeID
            {
                get { return (uint)((RawID & mask3) >> loc3); }
                set { RawID = (uint)(RawID & ~mask3 | (value << loc3) & mask3); }
            }

            public uint MessagegroupID
            {
                get { return (uint)((RawID & mask4) >> loc4); }
                set { RawID = (uint)(RawID & ~mask4 | (value << loc4) & mask4); }
            }
        }

        public MyStruct DeviceStruct;
    }
}
