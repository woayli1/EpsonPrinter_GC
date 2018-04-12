using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Globalization;
using Microsoft.PointOfService;
using System.Threading;

namespace ConsoleApplication1
{
    public class PH
    {
        public static readonly string LogTxt = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\PrinterLog.txt";
        const int MAX_COPYS = 2;

        public PosPrinter mPrinter { get; set; }

        public bool mInitialized { get; set; }
        public bool mPrinterReady { get; set; }

        public bool mPrinting { get; set; }

        private bool mPrinterCoverOK = true;
        private bool mReceiptEmpty = false;
        private bool mPowerOff = false;
        private bool mOffline = false;

        private System.Object mInitializeLock = new System.Object();
        private System.Object mPrintLock = new System.Object();

        int mCurrentCounts = 0;

        public void PrintHelper()
        {
            mPrinter = null;
            mInitialized = false;
            Initialize();
        }

        public void Print(string orderid = "")  //打印
        {
            mPrinting = true;

            lock (mPrintLock)
            {
                try
                {
                    #region IsInitialized
                    if (!mInitialized)
                    {
                        string strMessage = "打印机未初始化，不能打印,可能的原因是: \n" +
                                            "打印机线缆连接故障.\n" +
                                            "其他程序是否占用打印机, 退出该程序: \n" +
                                            "是否重新尝试初始化打印机？(这可能要花几秒钟)";

                       Console.WriteLine(strMessage, "打印机异常");

                        OutputOverHandle();
                        return;
                    }
                    #endregion
                    #region IsPrinterReady
                    else if (!mPrinterReady)
                    {
                        string strMessage = "打印机未就绪，不能打印,可能的原因是: \n" +
                                            "打印机未就绪,请检查打印机上盖、纸张、线缆连接.\n" +
                                            "其他程序是否占用打印机, 退出相应的程序程序重启POS \n";
                        Console.WriteLine(strMessage, "打印错误");
                        OutputOverHandle();
                        return;
                    }
                    #endregion
                    readyPrint();
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(LogTxt,
                        "PrintHelper.Print(): GetPrintedDatas() error! " + DateTime.Now.ToString() + ": " + ex.ToString() + "\r\n");
                    OutputOverHandle();
                }
            }
        }

        public void Initialize()  //初始化
        {
            lock (mInitializeLock)
            {
                string strLogicalName = "PosPrinter";

                if (mInitialized) return;

                PosExplorer posExplorer = null;
                try
                {
                    posExplorer = new PosExplorer();
                }
                catch (Exception e)
                {
                    System.Console.WriteLine(e.InnerException.ToString());
                }

                DeviceInfo deviceInfo = null;
                try
                {
                    deviceInfo = posExplorer.GetDevice(DeviceType.PosPrinter, strLogicalName);
                }
                catch (Exception getDeviceEx)
                {

                    Console.WriteLine("当前系统中不止一台PosPrinter设备，请设置默认的PosPrinter类型打印机.", "打印机异常");
                    System.IO.File.AppendAllText(LogTxt,
                        "PrintHelper.Initialize():" + DateTime.Now.ToString() + getDeviceEx.ToString() + "\r\n");
                    OutputOverHandle();
                    InitialError(getDeviceEx, "请运行: 开始 -> 程序 -> EPSON OPOS ADK for .NET -> SetupPOS for OPOS.NET\n" +
                        "检查PosPrinter打印机配置,然后重启打印机");
                    return;
                }
                try
                {
                    mPrinter = (PosPrinter)posExplorer.CreateInstance(deviceInfo);
                }
                catch (Exception createInstanceEx)
                {
                    Console.WriteLine("创建PosPrinter设备实例失败.", "打印机异常");
                    System.IO.File.AppendAllText(LogTxt,
                        "PrintHelper.Initialize():" + DateTime.Now.ToString() + createInstanceEx.ToString() + "\r\n");
                    OutputOverHandle();

                    InitialError(createInstanceEx,
                        "请运行: 开始 -> 程序 -> EPSON OPOS ADK for .NET -> SetupPOS for OPOS.NET\n" +
                        "检查PosPrinter打印机配置,然后重启打印机");
                    return;
                }

                AddOutputCompleteEvent(mPrinter);
                AddErrorEvent(mPrinter);
                AddStatusUpdateEvent(mPrinter);

                try
                {
                    mPrinter.Open();
                }
                catch (PosControlException openEx)
                {
                    System.IO.File.AppendAllText(LogTxt,
                        "PrintHelper.Initialize():" + DateTime.Now.ToString() + openEx.ToString() + "\r\n");
                    if (openEx.ErrorCode == ErrorCode.Illegal)
                        Console.WriteLine("打印机已经被打开!", "打印机故障");
                    else
                        Console.WriteLine("打开打印机失败!", "打印机故障");
                    OutputOverHandle();
                    InitialError(openEx);
                    return;
                }
                try
                {
                    mPrinter.Claim(1000);
                }
                catch (PosControlException claimEx)
                {
                   System.Diagnostics.Debug.WriteLine(claimEx.ToString());
                    System.IO.File.AppendAllText(LogTxt,
                        "PrintHelper.Initialize():" + DateTime.Now.ToString() + claimEx.ToString() + "\r\n");
                    Console.WriteLine("请检查打印机电源、连接线及退出其他正在占用打印机的程序,并稍后重试",
                        "打印机故障");
                    OutputOverHandle();
                    InitialError(claimEx);
                    return;
                }
                try
                {
                    mPrinter.DeviceEnabled = true;
                }
                catch (PosControlException enabledEx)
                {
                    System.Diagnostics.Debug.WriteLine(enabledEx.ToString());
                    System.IO.File.AppendAllText(LogTxt,
                        "PrintHelper.Initialize():" + DateTime.Now.ToString() + enabledEx.ToString() + "\r\n");
                    Console.WriteLine("试图使能打印机失败", "打印机故障");
                    OutputOverHandle();
                    InitialError(enabledEx);
                    return;
                }

                try
                {
                    mPrinter.RecLetterQuality = true;
                    mPrinter.MapMode = MapMode.Metric;
                }
                catch (PosControlException setModeEx)
                {
                    System.Diagnostics.Debug.WriteLine(setModeEx.ToString());
                    System.IO.File.AppendAllText(LogTxt,
                        "PrintHelper.Initialize():" + DateTime.Now.ToString() + setModeEx.ToString() + "\r\n");
                    OutputOverHandle();
                    InitialError(setModeEx);
                    return;
                }
                mInitialized = true;
            }
        }

        private void InitialError(Exception ex, string showMsg = "")  //基本错误
        {
            string msg = (showMsg.Length > 0) ?
                showMsg : "请检查线缆连接、打印机纸张及打印机上盖 并重启打印机.\n" +
                          "等待十几秒后,重试.\n";

            Console.WriteLine(msg, "打印机错误(I)");
            System.IO.File.AppendAllText(LogTxt,
                "PrintHelper.Initialize():" + DateTime.Now.ToString() + " " + ex.ToString() + "\r\n");
        }

        private void PrintError(Exception ex, string showMsg = "")  //打印错误
        {
            string msg = (showMsg.Length > 0) ?
                showMsg : "请检查线缆连接、打印机纸张及打印机上盖 并重启打印机.\n" +
                          "等待十几秒后,重试.\n";

            Console.WriteLine(msg, "打印错误(S)");
            System.IO.File.AppendAllText(LogTxt,
                "PrintHelper.SyncPrint():" + DateTime.Now.ToString() + " " + ex.ToString() + "\r\n");
            OutputOverHandle();
        }

        private int getChineseNum(string str)  //获取中字序号
        {
            int len = 0;
            char[] c = str.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] >= 0x4E00 && c[i] <= 0x9FA5)
                {
                    len += 1;
                }
            }
            return len;
        }

        private string getSubStr(string str, int maxWidth)  //获取复制的字符串
        {
            int length = 0;
            int widths = 0;
            foreach (char c in str)
            {
                widths += (c >= 0x4E00 && c <= 0x9FA5) ? 2 : 1;

                length++;
                if (widths == maxWidth)
                {
                    length--;
                    return str.Substring(0, length) + "*";
                }
                else if (widths > maxWidth)
                {
                    length -= 2;
                    return str.Substring(0, length) + "*";
                }
            }
            return str.Substring(0, length);
        }


        private String MakePrintStringOneLine(int iLineChars, String name, String num, String price)  //制作打印字符串的一条线
        {
            int span1 = 0;
            int span2 = 0;
            const int columnNameWidth = 28 + 1;
            const int columnNumWidth = 6 + 1;
            int columnPriceWidth = iLineChars - columnNameWidth - columnNumWidth; //48 - 29 - 7 = 11 + 1
            String tab1 = "";
            String tab2 = "";
            string cutName = "";
            string cutNum = "";
            string cutPrice = "";
            try
            {
                cutName = getSubStr(name, columnNameWidth);
                span1 = columnNameWidth - (cutName.Length + getChineseNum(cutName)); //chinese word is double width, one space
                for (int i = 0; i < span1; i++)
                {
                    tab1 += " ";
                }
                cutNum = getSubStr(num, columnNumWidth);
                span2 = columnNumWidth - cutNum.Length;//have not chinese word
                for (int j = 0; j < span2; j++)
                {
                    tab2 += " ";
                }
                cutPrice = getSubStr(price, columnPriceWidth);//have not chinese word
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(LogTxt,
                    "PrintHelper.MakePrintString():" + DateTime.Now.ToString() + " " + ex.ToString() + "\r\n");
            }
            return cutName + tab1 + cutNum + tab2 + cutPrice;
        }

        private String MakePrintStringMultiLines(int iLineChars, String name, String num, String price)  //制作打印字符串的绘制线
        {
            int iSpaces1 = 0;
            int iSpaces2 = 0;
            int coord1 = 27;
            int ext = 4;
            String tab1 = "";
            String tab2 = "";
            try
            {
                int lenCh = getChineseNum(name);
                iSpaces1 = coord1 - (name.Length + lenCh);
                if (iSpaces1 <= 0) tab1 = " ";
                for (int i = 0; i < iSpaces1; i++)
                {
                    tab1 += " ";
                }
                iSpaces2 = iLineChars - (coord1 + num.Length + price.Length + ext);
                if (iSpaces2 <= 0) tab2 = " ";
                for (int j = 0; j < iSpaces2; j++)
                {
                    tab2 += " ";
                }
            }
            catch (Exception)
            {
            }
            int len1 = tab1.Length;
            int len2 = tab2.Length;
            return name + tab1 + num + tab2 + price;
        }

        public string GetErrorCode(PosControlException ex)  //获取错误代码
        {
            string strErrorCodeEx = "";

            switch (ex.ErrorCodeExtended)
            {
                case PosPrinter.ExtendedErrorBadFormat:
                case PosPrinter.ExtendedErrorCoverOpen:
                case PosPrinter.ExtendedErrorJournalEmpty:
                case PosPrinter.ExtendedErrorReceiptEmpty:
                case PosPrinter.ExtendedErrorSlipEmpty:
                    strErrorCodeEx = ex.Message;
                    break;
                default:
                    string strEC = ex.ErrorCode.ToString();
                    string strECE = ex.ErrorCodeExtended.ToString();
                    strErrorCodeEx = "ErrorCode =" + strEC + "\nErrorCodeExtended =" + strECE + "\n"
                        + ex.Message;
                    break;
            }
            return strErrorCodeEx;
        }

        private void AsyncPrintError(Exception ex, string showMsg = "")  //同步打印错误
        {
            string msg = (showMsg.Length > 0) ?
                showMsg : "请检查线缆连接、打印机纸张及打印机上盖 并重启打印机.\n" +
                          "等待十几秒后,重试.\n";

            Console.WriteLine(msg, "打印机错误(A)");
            System.IO.File.AppendAllText(LogTxt,
                "PrintHelper.AsyncPrint():" + DateTime.Now.ToString() + " " + ex.ToString() + "\r\n");
            OutputOverHandle();
        }
        protected void AddOutputCompleteEvent(object eventSource)   //添加要输出的完整事件
        {
            EventInfo outputCompleteEvent = eventSource.GetType().GetEvent("OutputCompleteEvent");
            if (outputCompleteEvent != null)
            {
                outputCompleteEvent.AddEventHandler(eventSource,
                    new OutputCompleteEventHandler(OnOutputCompleteEvent));
            }
        }
        protected void AddErrorEvent(object eventSource)   //添加错误事件
        {
            EventInfo errorEvent = eventSource.GetType().GetEvent("ErrorEvent");
            if (errorEvent != null)
            {
                errorEvent.AddEventHandler(eventSource,
                    new DeviceErrorEventHandler(OnErrorEvent));
            }
        }
        protected void AddStatusUpdateEvent(object eventSource)  //添加状态更新事件
        {
            EventInfo statusUpdateEvent = eventSource.GetType().GetEvent("StatusUpdateEvent");
            if (statusUpdateEvent != null)
            {
                statusUpdateEvent.AddEventHandler(eventSource,
                    new StatusUpdateEventHandler(OnStatusUpdateEvent));
            }
        }
      
        protected void OnOutputCompleteEvent(object source, OutputCompleteEventArgs e)  //输出完整事件
        {
            OutputCompletedHandle();
        }

        protected void OutputCompletedHandle()  //输出完整事件处理
        {
            if (++mCurrentCounts >= MAX_COPYS)
            {
                mCurrentCounts = 0;
                bool success = false;


                System.IO.File.AppendAllText(LogTxt,
                    "PrintHelper.OutputCompletedHandle():" + DateTime.Now.ToString() + " 1 " + "\r\n");

                while (!success)
                {
                    try
                    {
                        System.IO.File.AppendAllText(LogTxt,
                            "PrintHelper.OutputCompletedHandle():" + DateTime.Now.ToString() + " 2 " + "\r\n");
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        if (ex.ToString().Contains("locked"))
                        {
                        }
                        System.Threading.Thread.Sleep(10);
                        System.IO.File.AppendAllText(LogTxt,
                            "PrintHelper.OutputCompletedHandle():" + DateTime.Now.ToString() + " " + ex.ToString() + "\r\n");
                        continue;
                    }
                }
            }
        }

                

        protected void OutputOverHandle()  //输出结束处理
        {
            try
            {
                mPrinter.ClearOutput();
            }
            catch
            {
                PrintError(new Exception("mPrinter.ClearOutput"));
            }
        }

        private void readyPrint()   //准备打印
        {
            mCurrentCounts = 0;
        }

        delegate void EnableBtnPrintModeDelegate(bool enable);
       
        protected void OnErrorEvent(object source, DeviceErrorEventArgs e)  //错误事件
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
            OutputOverHandle();
              
        }

        protected void OnStatusUpdateEvent(object source, StatusUpdateEventArgs e)  //事件状态更新
        {
            switch (e.Status)
            {
                case PosPrinter.StatusPowerOff:
                    mPowerOff = true;
                    Console.WriteLine("请打开打印机电源!");
                    break;
                case PosPrinter.StatusPowerOffline:
                    mOffline = true;
                    Console.WriteLine("请确保打开打印机电源及检查通讯连线!");
                    break;
                case PosPrinter.StatusPowerOffOffline:
                    mPowerOff = true;
                    mOffline = true;
                    Console.WriteLine("请确保打开打印机电源及检查通讯连线!");
                    break;
                case PosPrinter.StatusPowerOnline:
                    mPowerOff = mOffline = false;
                    break;

                case PosPrinter.StatusCoverOpen:
                    mPrinterCoverOK = false;
                    Console.WriteLine("请盖好打印机盖子,否则无法正常打印!");
                    break;
                case PosPrinter.StatusCoverOK:
                    mPrinterCoverOK = true;
                    break;

                case PosPrinter.StatusReceiptEmpty:
                    mReceiptEmpty = true;
                    Console.WriteLine("纸张耗尽,请更换新的纸卷!");
                    break;
                case PosPrinter.StatusReceiptPaperOK:
                case PosPrinter.StatusReceiptNearEmpty:
                    mReceiptEmpty = false;
                    break;
            }
           
            if (!mPowerOff && !mOffline && !mReceiptEmpty &&
               (mPrinterCoverOK || !mPrinter.CapCoverSensor))
            {
                mPrinterReady = true;
            }
            OutputOverHandle();
        }

        public void Close()  //退出打印
        {
            if (mPrinter != null)
            {
                try
                {
                    RemoveOutputCompleteEvent(mPrinter);
                    RemoveErrorEvent(mPrinter);
                    RemoveStatusUpdateEvent(mPrinter);
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
            }
        }

        protected void RemoveOutputCompleteEvent(object eventSource)  //移除完整事件输出
        {
            EventInfo outputCompleteEvent = eventSource.GetType().GetEvent("OutputCompleteEvent");
            if (outputCompleteEvent != null)
            {
                outputCompleteEvent.RemoveEventHandler(eventSource,
                    new OutputCompleteEventHandler(OnOutputCompleteEvent));
            }
        }

        protected void RemoveErrorEvent(object eventSource)  //移除错误事件
        {
            EventInfo errorEvent = eventSource.GetType().GetEvent("ErrorEvent");
            if (errorEvent != null)
            {
                errorEvent.RemoveEventHandler(eventSource,
                    new DeviceErrorEventHandler(OnErrorEvent));
            }
        }

        protected void RemoveStatusUpdateEvent(object eventSource)  //移除事件状态更新
        {
            EventInfo statusUpdateEvent = eventSource.GetType().GetEvent("StatusUpdateEvent");
            if (statusUpdateEvent != null)
            {
                statusUpdateEvent.RemoveEventHandler(eventSource,
                    new StatusUpdateEventHandler(OnStatusUpdateEvent));
            }
        }

        protected void OutputTicketContent()  //输出票的内容
        {
            try
            {
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + "\u001b|bC" + "\u001b|2C" + " 凯撒餐厅 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + "\u001b|bC" + " CN10001 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "顾客：" + " 郭灿 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "地址：" + " 三林镇如日商务园 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "电话：" + " 13101234567 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "备注：" + " 无 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "发票：" + " 无 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "订单提交时间：" + " 2017-7-21 11:01:02 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "预计送达时间：" + " 2017-7-21 11:42:00 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|300uF");

                mPrinter.PrintNormal(PrinterStation.Receipt, "************************************************\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                mPrinter.PrintNormal(PrinterStation.Receipt, @"名称                      数量       原价(元) " + "\n");

                mPrinter.PrintNormal(PrinterStation.Receipt, "炸鸡 1  15元" + "\n");

                mPrinter.PrintNormal(PrinterStation.Receipt, "************************************************\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                mPrinter.PrintNormal(PrinterStation.Receipt, @"优惠名称                  数量       优惠(元)" + "\n");

                mPrinter.PrintNormal(PrinterStation.Receipt, "优惠卷 1  1元" + "\n");

                mPrinter.PrintNormal(PrinterStation.Receipt, "************************************************\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");

                mPrinter.PrintNormal(PrinterStation.Receipt, "外 送 费：" + " 5 元" + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "订单合计：" + " 15 元" + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "优惠金额：" + " 1 元 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "应付金额：" + " 19 元" + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "支付方式：" + " 支付宝 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|4C" + "\u001b|bC" + "应收现金：" + " 19元 " + "\n");

                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");

                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + "\u001b|4C" + " 广告位 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + " 凯撒餐厅 " + " SC001 " + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|300uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + "查询电话:" + " 13612345678 " + "\n");

               
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|fP");

            }
            catch (PosControlException ex)
            {
                string msg = "打印机故障, 请重启打印机及计算机(C)";
                Console.WriteLine(msg); 
                System.IO.File.AppendAllText(LogTxt,
                    "PrintHelper.CreateTicketContent():" + DateTime.Now.ToString() + ex.ToString() + "\r\n");
                throw ex;
            }
        }

    }
}
