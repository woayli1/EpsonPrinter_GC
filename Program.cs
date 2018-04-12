using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.PointOfService;


namespace ConsoleApplication1
{
    public class Program
    {
        //static PosPrinter mPrinter = null;
        //static void Main(string[] args)
        //{
        //    Initialize();
        //    Console.WriteLine("按下任意键退出。");
        //    System.Console.ReadKey();
        //}

        public static string Initialize(PosPrinter mPrinter)
        {
            PosExplorer posExplorer = null;
            DeviceInfo deviceInfo = null;
            string strLogicalName = "Printer";
            try
            {
                //Create PosExplorer
                posExplorer = new PosExplorer();

                try
                {
                    deviceInfo = posExplorer.GetDevice(DeviceType.PosPrinter, strLogicalName);
                }
                catch (Exception e)
                {
                    string s1="当前系统中不止一台Printer设备，请设置默认的Printer类型打印机. 打印机异常";
                    s1+="前往 C:\\Program Files\\epson\\OPOS for .NET\\SetupPOS,运行 Epson.opos.tm.setpos.exe ,更改默认实例名为'Printer'";
                    Console.WriteLine(s1);
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    return s1;
                }
                try
                {
                    mPrinter = (PosPrinter)posExplorer.CreateInstance(deviceInfo);
                }
                catch (Exception e)
                {
                   string s2= "创建PosPrinter设备实例失败.打印机异常";
                    s2+="前往 C:\\Program Files\\epson\\OPOS for .NET\\SetupPOS,运行 Epson.opos.tm.setpos.exe ,检查默认实例名是否为'Printer'";
                    Console.WriteLine(s2);
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    return s2;
                }

                AddErrorEvent(mPrinter);
                AddStatusUpdateEvent(mPrinter);

                //Open the device
                try
                {
                    mPrinter.Open();
                }
                catch (PosControlException e)
                {
                    string s3=null;
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    if (e.ErrorCode == ErrorCode.Illegal)
                        s3="打印机已经被打开! 打印机故障";
                    else
                        s3="打开打印机失败! 打印机故障";
                    Console.WriteLine(s3);
                    return s3;
                }

                //Get the exclusive control right for the opened device.
                //Then the device is disable from other application.
                try
                {
                    mPrinter.Claim(1000);
                }
                catch (Exception e)
                {
                    string s4="请检查打印机电源、连接线及退出其他正在占用打印机的程序,并稍后重试 打印机故障";
                    Console.WriteLine(s4);
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    return s4
;
                }

                //Enable the device.
                try
                {
                    mPrinter.DeviceEnabled = true;
                }
                catch (Exception e)
                {
                    string s5="试图使能打印机失败 打印机故障";
                    Console.WriteLine(s5);
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    return s5;
                }

                try
                {
                    //Output by the high quality mode
                    mPrinter.RecLetterQuality = true;


                    // Even if using any printers, 0.01mm unit makes it possible to print neatly.
                    mPrinter.MapMode = MapMode.Metric;
                }
                catch (Exception e)
                {
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    return e.ToString();
                }

            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + ex.ToString() + "\r\n");
                return ex.ToString();
            }
            
            try
            {
                mPrinter.DeviceEnabled = false;
                mPrinter.Release();
            }
            catch (PosControlException)
            {
            }
            finally
            {
                mPrinter.Close();
            }
            return "打印机正常!";
        }

        public static string printer(PosPrinter mPrinter,string e)
        {
            //string iDivisionLine = "************************************************";
            try
            {
                //mPrinter.PrintNormal(PrinterStation.Receipt, iDivisionLine + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, e + "\n");
                //mPrinter.PrintNormal(PrinterStation.Receipt, iDivisionLine + "\n");

                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|fP");
            }
            catch (Exception ex)
            {
                string msg = "打印机故障, 请重启打印机及计算机(C)";
                //MessageBox.Show(msg); 
                System.IO.File.AppendAllText(@"C:\hot.txt",
                    "PrintHelper.CreateTicketContent():" + DateTime.Now.ToString() + ex.ToString() + "\r\n");
                return msg;
            }
            return "打印完成！";
        }


        protected static void AddErrorEvent(object eventSource)
        {
            ///PosPrinter.ErrorEvent  
            ///Queued by the service object when an error is detected and the device’s State transitions into 
            ///the error state.  
            EventInfo errorEvent = eventSource.GetType().GetEvent("ErrorEvent");
            if (errorEvent != null)
            {
                errorEvent.AddEventHandler(eventSource,
                    new DeviceErrorEventHandler(OnErrorEvent));
            }
        }
        protected static void AddStatusUpdateEvent(object eventSource)
        {
            ///PosPrinter.StatusUpdateEvent  
            ///Raised by the Service Object to alert the application of a device status change
            EventInfo statusUpdateEvent = eventSource.GetType().GetEvent("StatusUpdateEvent");
            if (statusUpdateEvent != null)
            {
                statusUpdateEvent.AddEventHandler(eventSource,
                    new StatusUpdateEventHandler(OnStatusUpdateEvent));
            }
        }

        protected static void OnErrorEvent(object source, DeviceErrorEventArgs e)
        {
            string strMessage = "打印错误原因: \n";

            switch (e.ErrorCodeExtended)
            {
                case PosPrinter.ExtendedErrorBadFormat:
                    strMessage += "不支持的打印格式或打印格式中有错,请查看订单详情";
                    break;
                case PosPrinter.ExtendedErrorCoverOpen:
                    strMessage += "打印机上盖未盖好,请盖好打印机上盖";
                    break;

                case PosPrinter.ExtendedErrorFirmwareBadFile:
                    strMessage += "打印机固件文件损坏";
                    strMessage += " ";
                    break;

                case PosPrinter.ExtendedErrorJournalCartridgeEmpty:
                case PosPrinter.ExtendedErrorReceiptCartridgeEmpty:
                case PosPrinter.ExtendedErrorSlipCartridgeEmpty:
                    strMessage += "无打印头";
                    break;
                case PosPrinter.ExtendedErrorJournalCartridgeRemoved:
                case PosPrinter.ExtendedErrorReceiptCartridgeRemoved:
                case PosPrinter.ExtendedErrorSlipCartridgeRemoved:
                    strMessage += "打印头已被拆卸";
                    break;
                case PosPrinter.ExtendedErrorJournalHeadCleaning:
                case PosPrinter.ExtendedErrorReceiptHeadCleaning:
                case PosPrinter.ExtendedErrorSlipHeadCleaning:
                    strMessage += "打印头正在清洗";
                    break;
                case PosPrinter.ExtendedErrorJournalEmpty:
                case PosPrinter.ExtendedErrorReceiptEmpty:
                case PosPrinter.ExtendedErrorSlipEmpty:
                    strMessage += "打印纸耗尽,请更换纸卷并点击<重试>,重新打印该订单";
                    break;
                case PosPrinter.ExtendedErrorSlipForm:
                    strMessage += "a form is present while the printer is being taken out of from removal mode";
                    break;
                case PosPrinter.ExtendedErrorStatistics:
                    strMessage += "打印机的打印统计数据不能复位或更新";
                    break;
                case PosPrinter.ExtendedErrorStatisticsDependency:
                    strMessage += "打印机的打印统计数据的依赖发生错误";
                    break;
                case PosPrinter.ExtendedErrorTooBig:
                    strMessage += "用于打印的位图太大";
                    break;


                default:
                    strMessage += "请检查线缆连接、打印机纸张及打印机上盖 并重启打印机.\n" +
                                  "等待十几秒后,重试.\n";
                    break;
            }
            Console.WriteLine(strMessage);
        }
        protected static void OnStatusUpdateEvent(object source, StatusUpdateEventArgs e)
        {
            //When there is a change of the status on the printer, the event is fired.
            switch (e.Status)
            {
                case PosPrinter.StatusPowerOff:
                    Console.WriteLine("请打开打印机电源!");
                    break;
                case PosPrinter.StatusPowerOffline:
                    Console.WriteLine("请确保打开打印机电源及检查通讯连线!");
                    break;
                case PosPrinter.StatusPowerOffOffline:
                    Console.WriteLine("请确保打开打印机电源及检查通讯连线!");
                    break;
                case PosPrinter.StatusPowerOnline:
                    break;

                case PosPrinter.StatusCoverOpen:
                    Console.WriteLine("请盖好打印机盖子,否则无法正常打印!");
                    break;
                case PosPrinter.StatusCoverOK:
                    break;

                case PosPrinter.StatusReceiptEmpty:
                    Console.WriteLine("纸张耗尽,请更换新的纸卷!");
                    break;
                case PosPrinter.StatusReceiptPaperOK:
                case PosPrinter.StatusReceiptNearEmpty:
                    break;
            }
        }
    }
}

