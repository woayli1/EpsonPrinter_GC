using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using Microsoft.PointOfService;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Reflection;

namespace ConsoleApplication1
{
    public class EpsonPrinterServices : WebSocketBehavior
    {
        static PosPrinter mPrinter = null;
        private void mSend(string d)
        {
            try
            {
                if (State == WebSocketState.Open)
                {
                    Send(d);
                    
                }
                else if (State == WebSocketState.Closed || State == WebSocketState.Closing)
                {
                    Sessions.CloseSession(ID);
                    Sessions.Sweep();
                }
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + ex.ToString() + "\r\n");
            }

        }
        protected override void OnOpen()
        {
            base.OnOpen();
            //mSend("connected!");
            mSend("连接成功!");
            Initialize(mPrinter);
        }
        protected override void OnClose(CloseEventArgs e)
        {
            try
            {
                //mSend("disConnected!");
                mSend("连接已断开!");
                Sessions.CloseSession(ID);
                Sessions.Sweep();
                base.OnClose(e);
            }
            catch(Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + ex.ToString() + "\r\n");
            }
           
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                if (e.Data == "")
                {
                    Console.WriteLine("heartbeat!");
                    return;
                }
                //myLogger.consoleLog(e.Data);
               printer(mPrinter, e.Data);
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + ex.ToString() + "\r\n");
            }
        }

        public void Initialize(PosPrinter mPrinter)
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
                    mSend("当前系统中不止一台Printer设备，请设置默认的Printer类型打印机. 打印机异常");
                    mSend("前往 C:\\Program Files\\epson\\OPOS for .NET\\SetupPOS,运行 Epson.opos.tm.setpos.exe ,更改默认实例名为'Printer'");
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    return ;
                }
                try
                {
                    mPrinter = (PosPrinter)posExplorer.CreateInstance(deviceInfo);
                }
                catch (Exception e)
                {
                    mSend("创建PosPrinter设备实例失败.打印机异常");
                    mSend("前往 C:\\Program Files\\epson\\OPOS for .NET\\SetupPOS,运行 Epson.opos.tm.setpos.exe ,检查默认实例名是否为'Printer'");
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    return ;
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
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    if (e.ErrorCode == ErrorCode.Illegal)
                        mSend("打印机已经被打开! 打印机故障");
                    else
                        mSend("打开打印机失败! 打印机故障");
                    return ;
                }

                //Get the exclusive control right for the opened device.
                //Then the device is disable from other application.
                try
                {
                    mPrinter.Claim(1000);
                }
                catch (Exception e)
                {
                    mSend("请检查打印机电源、连接线及退出其他正在占用打印机的程序,并稍后重试 打印机故障");
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    return ;
                }

                //Enable the device.
                try
                {
                    mPrinter.DeviceEnabled = true;
                }
                catch (Exception e)
                {
                    mSend("试图使能打印机失败 打印机故障");
                    System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                    return ;
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
                    return ;
                }

            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + ex.ToString() + "\r\n");
                return ;
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
            mSend("打印机正常!");
        }

        public void printer(PosPrinter mPrinter, string e)
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
                mSend("打印机故障, 请重启打印机及计算机(C)");
                //MessageBox.Show(msg); 
                System.IO.File.AppendAllText(@"C:\hot.txt",
                    "PrintHelper.CreateTicketContent():" + DateTime.Now.ToString() + ex.ToString() + "\r\n");
                return;
            }
            mSend( "打印完成！");
        }


        protected void AddErrorEvent(object eventSource)
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
        protected void AddStatusUpdateEvent(object eventSource)
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

        protected void OnErrorEvent(object source, DeviceErrorEventArgs e)
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
            mSend(strMessage);
        }
        protected void OnStatusUpdateEvent(object source, StatusUpdateEventArgs e)
        {
            //When there is a change of the status on the printer, the event is fired.
            switch (e.Status)
            {
                case PosPrinter.StatusPowerOff:
                    mSend("请打开打印机电源!");
                    break;
                case PosPrinter.StatusPowerOffline:
                    mSend("请确保打开打印机电源及检查通讯连线!");
                    break;
                case PosPrinter.StatusPowerOffOffline:
                    mSend("请确保打开打印机电源及检查通讯连线!");
                    break;
                case PosPrinter.StatusPowerOnline:
                    break;

                case PosPrinter.StatusCoverOpen:
                    mSend("请盖好打印机盖子,否则无法正常打印!");
                    break;
                case PosPrinter.StatusCoverOK:
                    break;

                case PosPrinter.StatusReceiptEmpty:
                    mSend("纸张耗尽,请更换新的纸卷!");
                    break;
                case PosPrinter.StatusReceiptPaperOK:
                case PosPrinter.StatusReceiptNearEmpty:
                    break;
            }
        }

    }
}
