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
//using System.Windows.Forms;
//using POS.Data;
using System.Threading;

namespace ConsoleApplication1
{
    public class PrintHelper
    {
        public static readonly string LogTxt = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\PrinterLog.txt";
        // @"C:\PrinterLog.txt";
        const int MAX_COPYS = 2;

       // private mainForm mForm;
        public PosPrinter mPrinter { get; set; }

        public bool mInitialized { get; set; }
        public bool mPrinterReady { get; set; }

        //public ManualResetEvent mManualResetEvent = new ManualResetEvent(true);
        public bool mPrinting { get; set; }

        //Contract.Order mPrintingOrder;
        //Queue<Contract.Order> mOrders;

        private bool mPrinterCoverOK = true;
        private bool mReceiptEmpty = false;
        private bool mPowerOff = false;
        private bool mOffline = false;

        private System.Object mInitializeLock = new System.Object();
        private System.Object mPrintLock = new System.Object();

        int mCurrentCounts = 0;

        //public PrintHelper(System.Windows.Forms.Form frame)
        //{
        //    mForm = frame as mainForm;
        //    mPrinter = null;
        //    mInitialized = false;
        //    mPrinting = false;
        //    mOrders = new Queue<Contract.Order>();

        //    //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //    Initialize();
        //}

        public PrintHelper()
        {
            //mForm = new mainForm();
            mPrinter = null;
            mInitialized = false;
            Initialize();
        }

        public void Print(string orderid = "")  //打印
        {
            //System.IO.File.AppendAllText(LogTxt,
            //    "PrintHelper.Initialize():" + DateTime.Now.ToString() + ": PrintHelper:Print(): 1" + "\r\n");

            mPrinting = true;

            lock (mPrintLock)
            {
                //System.IO.File.AppendAllText(LogTxt,
                //    "PrintHelper.Initialize():" + DateTime.Now.ToString() + ": PrintHelper:Print(): 2" + "\r\n");
                try
                {
                    #region IsInitialized
                    if (!mInitialized)
                    {
                        string strMessage = "打印机未初始化，不能打印,可能的原因是: \n" +
                                            "打印机线缆连接故障.\n" +
                                            "其他程序是否占用打印机, 退出该程序: \n" +
                                            "是否重新尝试初始化打印机？(这可能要花几秒钟)";

                        // When error occurs, display a message to ask the user whether retry or not.
                       Console.WriteLine(strMessage, "打印机异常");

                        //if (dialogResult == DialogResult.Yes)
                        //{
                        //    Initialize();
                        //}
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

                    //#region BatchPrint
                    //if (orderid.Length == 0 || orderid == null)
                    //{
                    //    //System.IO.File.AppendAllText(LogTxt,
                    //    //     "PrintHelper.Initialize():" + DateTime.Now.ToString() + ": PrintHelper:Print(): 1" + "\r\n");
                    //    if (mOrders != null) mOrders.Clear();
                    //    mForm.mSQLiteHelper.GetPrintedDatas(ref mOrders);

                    //    if (mOrders.Count > 0)
                    //    {
                    //        mPrintingOrder = mOrders.Dequeue();
                    //        AsyncPrint(ref mPrintingOrder);
                    //    }
                    //    else
                    //    {
                    //        OutputOverHandle();
                    //    }
                    //}
                    //#endregion
                    //#region SinglePrint
                    //else
                    //{
                    //    if (mOrders != null)
                    //        mOrders.Clear();
                    //    else
                    //        mOrders = new Queue<Contract.Order>();
                    //    mPrintingOrder = mForm.mSQLiteHelper.GetPrintedData(orderid);
                    //    AsyncPrint(ref mPrintingOrder);
                    //}
                    //#endregion

                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(LogTxt,
                        "PrintHelper.Print(): GetPrintedDatas() error! " + DateTime.Now.ToString() + ": " + ex.ToString() + "\r\n");
                    OutputOverHandle();
                }
            }
        }

        /// <summary>
        /// The processing code required in order to enable to use of service is written here.
        /// </summary>
        public void Initialize()  //初始化
        {
            //prevent concurrence run initialize() in multi-threads

            lock (mInitializeLock)
            {
                string strLogicalName = "PosPrinter";

                if (mInitialized) return;

                //Create PosExplorer
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
                    /// There must be only one device of that type currently in the system, or if there is more than one, 
                    /// one must have been configured as the default device. If there is more than one device of the specified 
                    /// type and no device has been configured as the default device, an exception will be thrown.
                    //MessageBox.Show("当前系统中不止一台PosPrinter设备，请设置默认的PosPrinter类型打印机.", "打印机异常");
                    //System.IO.File.AppendAllText(LogTxt,
                    //    "PrintHelper.Initialize():" + DateTime.Now.ToString() + getDeviceEx.ToString() + "\r\n");
                    //OutputOverHandle();
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
                    //MessageBox.Show("创建PosPrinter设备实例失败.", "打印机异常");
                    //System.IO.File.AppendAllText(LogTxt,
                    //    "PrintHelper.Initialize():" + DateTime.Now.ToString() + createInstanceEx.ToString() + "\r\n");
                    //OutputOverHandle();

                    InitialError(createInstanceEx,
                        "请运行: 开始 -> 程序 -> EPSON OPOS ADK for .NET -> SetupPOS for OPOS.NET\n" +
                        "检查PosPrinter打印机配置,然后重启打印机");
                    return;
                }

                //Register Event
                AddOutputCompleteEvent(mPrinter);
                AddErrorEvent(mPrinter);
                AddStatusUpdateEvent(mPrinter);

                try
                {
                    mPrinter.Open();
                }
                catch (PosControlException openEx)
                {
                    //System.IO.File.AppendAllText(LogTxt,
                    //    "PrintHelper.Initialize():" + DateTime.Now.ToString() + openEx.ToString() + "\r\n");
                    //if (openEx.ErrorCode == ErrorCode.Illegal)
                    //    MessageBox.Show("打印机已经被打开!", "打印机故障");
                    //else
                    //    MessageBox.Show("打开打印机失败!", "打印机故障");
                    //OutputOverHandle();
                    InitialError(openEx);
                    return;
                }
                try
                {
                    //Get the exclusive control right for the opened device.
                    //Then the device is disable from other application.
                    mPrinter.Claim(1000);
                }
                catch (PosControlException claimEx)
                {
                    //System.Diagnostics.Debug.WriteLine(claimEx.ToString());
                    //System.IO.File.AppendAllText(LogTxt,
                    //    "PrintHelper.Initialize():" + DateTime.Now.ToString() + claimEx.ToString() + "\r\n");
                    //MessageBox.Show("请检查打印机电源、连接线及退出其他正在占用打印机的程序,并稍后重试",
                    //    "打印机故障");
                    //OutputOverHandle();
                    InitialError(claimEx);
                    return;
                }
                try
                {
                    mPrinter.DeviceEnabled = true;
                }
                catch (PosControlException enabledEx)
                {
                    //System.Diagnostics.Debug.WriteLine(enabledEx.ToString());
                    //System.IO.File.AppendAllText(LogTxt,
                    //    "PrintHelper.Initialize():" + DateTime.Now.ToString() + enabledEx.ToString() + "\r\n");
                    //MessageBox.Show("试图使能打印机失败", "打印机故障");
                    //OutputOverHandle();
                    InitialError(enabledEx);
                    return;
                }

                try
                {
                    //Output by the high quality mode
                    mPrinter.RecLetterQuality = true;
                    // Even if using any printers, 0.01mm unit makes it possible to print neatly.
                    mPrinter.MapMode = MapMode.Metric;
                }
                catch (PosControlException setModeEx)
                {
                    //System.Diagnostics.Debug.WriteLine(setModeEx.ToString());
                    //System.IO.File.AppendAllText(LogTxt,
                    //    "PrintHelper.Initialize():" + DateTime.Now.ToString() + setModeEx.ToString() + "\r\n");
                    //OutputOverHandle();
                    InitialError(setModeEx);
                    return;
                }

                //Capability TEST !!!
                //(mPrinter.CapCoverSensor)
                //(mPrinter.CapRecEmptySensor)
                //(mPrinter.CapRecNearEndSensor)
                //(mPrinter.RecNearEnd)
                //(mPrinter.RecEmpty)
                mInitialized = true;
            }
        }

        private void InitialError(Exception ex, string showMsg = "")  //基本错误
        {
            string msg = (showMsg.Length > 0) ?
                showMsg : "请检查线缆连接、打印机纸张及打印机上盖 并重启打印机.\n" +
                          "等待十几秒后,重试.\n";

            //MessageBox.Show(msg, "打印机错误(I)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            System.IO.File.AppendAllText(LogTxt,
                "PrintHelper.Initialize():" + DateTime.Now.ToString() + " " + ex.ToString() + "\r\n");
        }

        /// <summary>
        ///  A method "Print" calls some another method.
        ///  They are method for printing.
        /// </summary>
        //protected void SyncPrint(ref Contract.Order order)  //同步打印
        //{
        //    DialogResult dialogResult;

        //    Cursor.Current = Cursors.WaitCursor;

        //    try
        //    {
        //        if (mPrinter.CapRecPresent == true)
        //        {
        //            #region While
        //            while (true)
        //            {
        //                #region TransactionPrint
        //                try
        //                {
        //                    //System.IO.File.AppendAllText(LogTxt,
        //                    //    "PrintHelper.Initialize():" + DateTime.Now.ToString() + ": PrintHelper:SyncPrint(): 1" + "\r\n");

        //                    //Batch processing mode
        //                    mPrinter.TransactionPrint(PrinterStation.Receipt,
        //                        PrinterTransactionControl.Transaction);
        //                }
        //                catch (PosControlException TransactionPrintEx)
        //                {
        //                    PrintError(TransactionPrintEx);
        //                    return;
        //                }
        //                #endregion TransactionPrint
        //                try
        //                {
        //                    OutputTicketContent(ref order);
        //                    break;
        //                }
        //                #region exception
        //                catch (PosControlException PrintNormalEx)
        //                {

        //                    System.IO.File.AppendAllText(LogTxt,
        //                        "PrintHelper.SyncPrint():" + DateTime.Now.ToString() + PrintNormalEx.ToString() + "\r\n");

        //                    // When error occurs, display a message to ask the user whether retry or not.
        //                    //dialogResult = MessageBox.Show("请检查打印机、纸卷、线缆连接.\n\n" +
        //                    //     "是否重试? [如果重试仍然失败,请重启打印机]"
        //                    //    , "打印错误(S)", MessageBoxButtons.AbortRetryIgnore);
        //                    if (dialogResult == DialogResult.Abort)
        //                    {
        //                        try
        //                        {
        //                            // Clear the buffered data since the buffer retains print data when an error occurs during printing.
        //                            mPrinter.ClearOutput();
        //                        }
        //                        catch (PosControlException ClearOutputEx)
        //                        {
        //                            System.IO.File.AppendAllText(LogTxt,
        //                                "PrintHelper.SyncPrint():" + DateTime.Now.ToString() + ClearOutputEx.ToString() + "\r\n");
        //                        }
        //                        finally
        //                        {
        //                            OutputOverHandle();
        //                        }
        //                        return;
        //                    }
        //                    else if (dialogResult == DialogResult.Ignore)
        //                    {
        //                        break; //break while then goto region WaitPrinterIdle
        //                    }

        //                    //DialogResult.Retry
        //                    try
        //                    {
        //                        // Clear the buffered data since the buffer retains print data when an error 
        //                        // occurs during printing.
        //                        mPrinter.ClearOutput();
        //                    }
        //                    catch (PosControlException ClearOutputEx)
        //                    {
        //                        System.IO.File.AppendAllText(LogTxt,
        //                            "PrintHelper.SyncPrint():" + DateTime.Now.ToString() + ClearOutputEx.ToString() + "\r\n");
        //                        OutputOverHandle();
        //                    }
        //                    continue;
        //                }
        //                #endregion exception
        //            }
        //            #endregion While

        //            #region WaitPrinterIdle
        //            //print all the buffer data. and exit the batch processing mode.
        //            while (mPrinter.State != ControlState.Idle)
        //            {
        //                try
        //                {
        //                    switch (mPrinter.State)
        //                    {
        //                        case ControlState.Busy:
        //                            System.Threading.Thread.Sleep(100);
        //                            break;
        //                        case ControlState.Closed:
        //                            mPrinter.ClearOutput();
        //                            PrintError(new Exception("My Message: mPrinter.State == ControlState.Closed"));
        //                            break;
        //                        case ControlState.Error:
        //                            mPrinter.ClearOutput();
        //                            PrintError(new Exception("My Message: mPrinter.State == ControlState.Error"));
        //                            break;
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    PrintError(ex);
        //                }
        //            }
        //            #endregion WaitPrinterIdle
        //            //real print !!!
        //            //if sync mode, will wait here! until print completed! if async mode, will immediately return.
        //            mPrinter.TransactionPrint(PrinterStation.Receipt, PrinterTransactionControl.Normal);
        //        }
        //        else
        //        {
        //            PrintError(new Exception("My Message: Cannot use a Receipt Station"));
        //            return;
        //        }
        //    }
        //    catch (PosControlException ex)
        //    {
        //        PrintError(ex);
        //    }

        //    // When a cursor is back to its default shape, it means the process ends
        //    Cursor.Current = Cursors.Default;
        //}

        private void PrintError(Exception ex, string showMsg = "")  //打印错误
        {
            string msg = (showMsg.Length > 0) ?
                showMsg : "请检查线缆连接、打印机纸张及打印机上盖 并重启打印机.\n" +
                          "等待十几秒后,重试.\n";

            //MessageBox.Show(msg, "打印错误(S)", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ///UNICODE chinese :
                ///    primary 4E00－9FA5 
                ///    others  2E80－A4CF  ||   F900-FAFF　||　FE30-FE4F
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
                //UNICODE chinese :
                //    primary 4E00－9FA5 
                //    others  2E80－A4CF  ||   F900-FAFF　||　FE30-FE4F
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


        /// The information related to the error from the parameter
        /// "ex" is received as a type of "int".
        /// Information by the sentence corresponding to the received
        /// information is returned as "strErrorCodeEx".
        /// </summary>
        /// <param name="ex"></param>
        /// <returns>
        /// "int" type information is changed into the information
        /// by the sentence, and is returned as a "String" type.
        /// "strErrorCodeEx" holds the information on this "int" type.
        /// </returns>
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

        /// <summary>
        /// A method "Asynchronous Printing" calls some another method.
        /// This includes methods for starting and ending "AsyncMode", and for printing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //protected void AsyncPrint(ref Contract.Order order) //同步打印
        //{
        //    try
        //    {
        //        ///About ASYNC: NO error status rather than raise EVENT
        //        ///The service object buffers the request, sets the OutputID property to an identifier for this request, 
        //        ///and returns as soon as possible. When the device successfully completes the request, the service object 
        //        ///raises an OutputCompleteEvent event. A parameter of this event contains the Output ID of the completed 
        //        ///request.
        //        ///Asynchronous printer methods will not return an error status because of a printing problem, such as out 
        //        ///of paper or printer fault. These errors will be reported only by an ErrorEvent event. An error status is 
        //        ///returned only if the printer is not claimed and enabled, a parameter is invalid, or the request cannot 
        //        ///be queued. The first two error cases are because of an application error. The last is a serious system 
        //        ///resource exception.
        //        ///
        //        ///About Transaction: BUFFER printer operations
        //        ///A transaction is a sequence of print operations that is printed to a station as a unit. 
        //        ///During a transaction, the print operations are first validated. If valid, they are added to the transaction 
        //        ///but not yet printed. After the application has added as many operations as required, it calls the 
        //        ///TransactionPrint method.
        //        ///If the transaction is printed synchronously, the returned status indicates either that the transaction 
        //        ///printed successfully or that an error occurred during the print. If the transaction is printed asynchronously,
        //        ///if an error occurs and the ErrorEvent handler causes a retry, the transaction is retried.

        //        mPrinter.AsyncMode = true;
        //        SyncPrint(ref order);
        //        mPrinter.AsyncMode = false;
        //    }
        //    catch (PosControlException ex)
        //    {
        //        AsyncPrintError(ex);
        //    }
        //}

        private void AsyncPrintError(Exception ex, string showMsg = "")  //同步打印错误
        {
            string msg = (showMsg.Length > 0) ?
                showMsg : "请检查线缆连接、打印机纸张及打印机上盖 并重启打印机.\n" +
                          "等待十几秒后,重试.\n";

            //MessageBox.Show(msg, "打印机错误(A)", MessageBoxButtons.OK, MessageBoxIcon.Error);
            System.IO.File.AppendAllText(LogTxt,
                "PrintHelper.AsyncPrint():" + DateTime.Now.ToString() + " " + ex.ToString() + "\r\n");
            OutputOverHandle();
        }
        protected void AddOutputCompleteEvent(object eventSource)   //添加要输出的完整事件
        {
            ///
            /// PosPrinter.OutputCompleteEvent  
            /// Queued by the service object to notify the application when asynchronous processing 
            /// that corresponds to an OutputID has successfully completed. 
            /// 
            /// Object.GetType() : Gets the Type of the current instance.
            /// Type.GetEvent(String) : Returns the EventInfo object representing the specified public event.
            /// EventInfo..AddEventHandler(Object, Delegate) : Adds an event handler to an event source.
            EventInfo outputCompleteEvent = eventSource.GetType().GetEvent("OutputCompleteEvent");
            if (outputCompleteEvent != null)
            {
                outputCompleteEvent.AddEventHandler(eventSource,
                    new OutputCompleteEventHandler(OnOutputCompleteEvent));
            }
        }
        protected void AddErrorEvent(object eventSource)   //添加错误事件
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
        protected void AddStatusUpdateEvent(object eventSource)  //添加状态更新事件
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
        /// <summary>
        /// Typically, the application will call a method, such as PrintNormal, to start asynchronous output processing. 
        /// The service object holds the request in program memory to be delivered to the printer as soon as the printer 
        /// can receive and process it. In the interim, the service object updates the PosPrinter.OutputID property that 
        /// uses the identifier for the output request, then returns control from the method to the application. When the 
        /// printer completes processing of the output request, the service object queues an OutputCompleteEvent event to 
        /// the application. This includes the OutputID value in the OutputCompleteEventArgs.OutputID property.
        /// 
        /// If the application exits the output request before it completes — for example, by calling the ClearOutput 
        /// method or responding to an ErrorEvent event with a Clear instruction, then the service object will not queue 
        /// an OutputCompleteEvent event. !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        protected void OnOutputCompleteEvent(object source, OutputCompleteEventArgs e)  //输出完整事件
        {
            OutputCompletedHandle();
        }

        protected void OutputCompletedHandle()  //输出完整事件处理
        {
            if (++mCurrentCounts >= MAX_COPYS)
            {
                //one order printed over!!!( 2 papers)
                mCurrentCounts = 0;

                //POS.Data.OrderStatus status = null;
                bool success = false;


                //System.IO.File.AppendAllText(LogTxt,
                //    "PrintHelper.OutputCompletedHandle():" + DateTime.Now.ToString() + " 1 " + "\r\n");

                while (!success)
                {
                    try
                    {
                        //System.IO.File.AppendAllText(LogTxt,
                        //    "PrintHelper.OutputCompletedHandle():" + DateTime.Now.ToString() + " 2 " + "\r\n");

                        //status = mForm.mSQLiteHelper.QueryOrderStatus(mPrintingOrder.orderID);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        //if (ex.ToString().Contains("locked"))
                        //{
                        //}
                        System.Threading.Thread.Sleep(10);
                        System.IO.File.AppendAllText(LogTxt,
                            "PrintHelper.OutputCompletedHandle():" + DateTime.Now.ToString() + " " + ex.ToString() + "\r\n");
                        continue;
                    }
                }

                //    if (status.orderstatus < POS.Data.Contract.ORDER_STATUS_PRODUCING)
                //    {
                //        success = false;
                //        while (!success)
                //        {
                //            try
                //            {
                //                //System.IO.File.AppendAllText(LogTxt,
                //                //    "PrintHelper.OutputCompletedHandle():" + DateTime.Now.ToString() + " 3 " + "\r\n");

                //                //mForm.mSQLiteHelper.UpdateOrder(mPrintingOrder.orderID,
                //                    Data.Contract.ORDER_STATUS_PRODUCING);

                //                success = true;
                //            }
                //            catch (Exception ex)
                //            {
                //                //if (ex.ToString().Contains("locked"))
                //                //{
                //                //}
                //                System.Threading.Thread.Sleep(10);
                //                System.IO.File.AppendAllText(LogTxt,
                //                    "PrintHelper.OutputCompletedHandle():" + DateTime.Now.ToString() + " " + ex.ToString() + "\r\n");
                //                continue;
                //            }
                //        }
                //        //mForm.RefreshDisplay();
                //    }

                //    //continue print other orders in mOrders [print queue]
                //    if ((mOrders != null) && (mOrders.Count > 0))
                //    {
                //        mPrintingOrder = mOrders.Dequeue();
                //        AsyncPrint(ref mPrintingOrder);
                //    }
                //    else
                //    {
                //        //single Order or batch Orders print over !!!
                //        OutputOverHandle();
                //    }
                //}
                //else
                //{
                //    AsyncPrint(ref mPrintingOrder);
                //}
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
                //Loop error!
                //PrintError(new Exception("mPrinter.ClearOutput"));
            }
            //if (mForm.AutoPrintMode())
            //{
            //    /// self-recursion prevent from concurrence run AutoPrintHandle on thread pool thread 
            //    /// Threading.Timer same as Timers.Timer: perhapse simultaneously run!!!
            //    /// The callback can be executed simultaneously on two thread pool threads 
            //    /// if the timer interval is less than the time required to execute the callback, 
            //    /// or if all thread pool threads are in use and the callback is queued multiple times.
            //    /// See PrintHeleper.cs : OnCompletedEvent()
            //    long period = Convert.ToInt32(POS.Properties.Settings.Default.AutoPrintPeriod);
            //    mForm.mAutoPrintTimer.Change(period, System.Threading.Timeout.Infinite);
            //}
            mPrinting = false;
           // EnableBtnPrintMode(true);
        }

        private void readyPrint()   //准备打印
        {
            mCurrentCounts = 0;
           // EnableBtnPrintMode(false);
        }

        delegate void EnableBtnPrintModeDelegate(bool enable);
        //private void EnableBtnPrintMode(bool enable)  // 使用按钮打印模式 ?
        //{
        //    if (mForm.InvokeRequired)
        //    {
        //        //Ensure calls to Windows Form Controls are from this application's thread
        //        mForm.Invoke(new EnableBtnPrintModeDelegate(EnableBtnPrintMode), enable);
        //        return;
        //    }
        //    mForm.EnableBtnPrintMode(enable);
        //}

        /// <summary> 
        /// Important !!! I use async print, all error in print progress, will delvery here!!!
        /// 
        /// The service object buffers the request, sets the OutputID property to an identifier for this request, 
        /// and returns as soon as possible. When the device successfully completes the request, the service object 
        /// raises an OutputCompleteEvent event. A parameter of this event contains the Output ID of the completed 
        /// request. Asynchronous printer methods will not return an error status because of a printing problem, 
        /// such as out of paper or printer fault. These errors will be reported only by an ErrorEvent event. An 
        /// error status is returned only if the printer is not claimed and enabled, a parameter is invalid, or the 
        /// request cannot be queued. The first two error cases are because of an application error. The last is a 
        /// serious system resource exception.
        /// If an error occurs when it performs an asynchronous request, the service object queues an ErrorEvent event 
        /// and delivers it. The ErrorStation property is set to the station or stations that were printing when the 
        /// error occurred. The ErrorLevel and ErrorString properties are also set. The event handler can call synchronous
        /// print methods (but not asynchronous methods), can then either retry the outstanding output or clear it.
        /// The service object guarantees that asynchronous output is performed on a first-in first-out basis. All 
        /// output buffered by the application can be deleted by calling the ClearOutput method. OutputCompleteEvent 
        /// events will not be raised for cleared output. This method also stops any output that may be in progress 
        /// (when it is possible).
        /// Queued by the service object when an error is detected and the device's State transitions into the error state. 
        /// If DeviceErrorEventArgs.ErrorCode is Extended, DeviceErrorEventArgs.ErrorCodeExtended 
        /// is set to one of the following values:
        /// ExtendedErrorCoverOpen            The printer cover is open.
        /// 
        /// ExtendedErrorJrnEmpty             The journal station is out of paper.
        /// ExtendedErrorRecEmpty             The receipt station is out of paper.
        /// ExtendedErrorSlpEmpty             A form is not inserted in the slip station.
        /// 
        /// ExtendedErrorCartridgeRemoved     The journal station cartridge is not present.
        /// ExtendedErrorRecCartridgeRemoved  The receipt station cartridge is not present.
        /// ExtendedErrorSlpCartridgeRemoved  The slip station cartridge is not present.
        /// 
        /// ExtendedErrorJrnCartridgeEmpty    The journal cartridge is empty.
        /// ExtendedErrorRecCartridgeEmpty    The receipt cartridge is empty.
        /// ExtendedErrorSlpCartridgeEmpty    The slip cartridge is empty.
        /// 
        /// ExtendedErrorJrnHeadCleaning      The journal station head is being cleaned.
        /// ExtendedErrorRecHeadCleaning      The receipt station head is being cleaned.
        /// ExtendedErrorSlpHeadCleaning      The slip station head is being cleaned.
        /// 
        /// The DeviceErrorEventArgs.ErrorResponse property is preset to ErrorResponse.Retry. 
        /// The application may set the value to one of the following:
        /// ErrorResponse.Retry    Retry the asynchronous output. The error state is exited.
        /// ErrorResponse.Clear    Clear the asynchronous output. The error state is exited.
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        protected void OnErrorEvent(object source, DeviceErrorEventArgs e)  //错误事件
        {
            //if (mForm.InvokeRequired)
            //{
            //    //Ensure calls to Windows Form Controls are from this application's thread
            //    mForm.Invoke(new DeviceErrorEventHandler(OnErrorEvent), new object[2] { source, e });
            //    return;
            //}

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
            //DialogResult dialogResult;
            //dialogResult = MessageBox.Show(strMessage, "打印错误(E)",
            //    MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

            //if (dialogResult == DialogResult.Cancel)
            //{
            //    e.ErrorResponse = ErrorResponse.Clear;
            //    OutputOverHandle();
            //}
            //else if (dialogResult == DialogResult.Retry)
            //{
            //    e.ErrorResponse = ErrorResponse.Retry;
            //}
            OutputOverHandle();
            //System.IO.File.AppendAllText(LogTxt,
            //    "PrintHelper.OnErrorEvent():" + DateTime.Now.ToString() + e.ToString() + "\r\n");
        }

        /// <summary>
        /// Examples are a change in the cash drawer position (open versus closed), 
        /// a change in a POS printer sensor (form present vs. absent), 
        /// or a change in the power state of the device.
        /// When a device is enabled, the Service Object may raise initial StatusUpdateEvents 
        /// to inform the application of the device state. However, this behavior is not required.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        protected void OnStatusUpdateEvent(object source, StatusUpdateEventArgs e)  //事件状态更新
        {
            //if (mForm.InvokeRequired)
            //{
            //    //Ensure calls to Windows Form Controls are from this application's thread
            //    mForm.Invoke(new StatusUpdateEventHandler(OnStatusUpdateEvent), new object[2] { source, e });
            //    return;
            //}

            //When there is a change of the status on the printer, the event is fired.
            switch (e.Status)
            {
                case PosPrinter.StatusPowerOff:
                    mPowerOff = true;
                    //MessageBox.Show("请打开打印机电源!");
                    break;
                case PosPrinter.StatusPowerOffline:
                    mOffline = true;
                    //MessageBox.Show("请确保打开打印机电源及检查通讯连线!");
                    break;
                case PosPrinter.StatusPowerOffOffline:
                    mPowerOff = true;
                    mOffline = true;
                   // MessageBox.Show("请确保打开打印机电源及检查通讯连线!");
                    break;
                case PosPrinter.StatusPowerOnline:
                    mPowerOff = mOffline = false;
                    break;

                case PosPrinter.StatusCoverOpen:
                    mPrinterCoverOK = false;
                    //MessageBox.Show("请盖好打印机盖子,否则无法正常打印!");
                    break;
                case PosPrinter.StatusCoverOK:
                    mPrinterCoverOK = true;
                    break;

                case PosPrinter.StatusReceiptEmpty:
                    mReceiptEmpty = true;
                    //MessageBox.Show("纸张耗尽,请更换新的纸卷!");
                    break;
                case PosPrinter.StatusReceiptPaperOK:
                case PosPrinter.StatusReceiptNearEmpty:
                    mReceiptEmpty = false;
                    break;
            }
            //mPrinterStatusReady = (!mPowerOff && !mOffline && !mReceiptEmpty &&
            //                      ((mPrinterCoverOK) || !mPrinter.CapCoverSensor)) ? true : false;
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
                //String header = Properties.Settings.Default.TicketHeader;
               // String adv = Properties.Settings.Default.TicketAdv;
               // String storeName = Properties.Settings.Default.TicketStoreName;

                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + "\u001b|bC" + "\u001b|2C" + header + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + "\u001b|bC" + order.orderID + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "顾客：" + order.userName + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "地址：" + order.userAddress + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "电话：" + order.userTelephone + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "备注：" + order.ext + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "发票：" + order.invoiceTitle + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "订单提交时间：" + order.addTime + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "预计送达时间：" + order.bookTime + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|300uF");

                mPrinter.PrintNormal(PrinterStation.Receipt, "************************************************\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                mPrinter.PrintNormal(PrinterStation.Receipt, @"名称                      数量       原价(元) " + "\n");
                if (order.productList != null)
                    foreach (Contract.Product p in order.productList)
                    {
                        String printData = MakePrintStringMultiLines(mPrinter.RecLineChars, p.productName, p.productNum, p.totalPrice);
                        mPrinter.PrintNormal(PrinterStation.Receipt, printData + "\n");
                    }

                mPrinter.PrintNormal(PrinterStation.Receipt, "************************************************\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                mPrinter.PrintNormal(PrinterStation.Receipt, @"优惠名称                  数量       优惠(元)" + "\n");
                if (order.discountProductList != null)
                    foreach (Contract.DiscountProduct d in order.discountProductList)
                    {
                        string printData = MakePrintStringMultiLines(mPrinter.RecLineChars, d.discountName, d.discountNum, d.discountAmount);
                        mPrinter.PrintNormal(PrinterStation.Receipt, printData + "\n");
                    }

                mPrinter.PrintNormal(PrinterStation.Receipt, "************************************************\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");

                mPrinter.PrintNormal(PrinterStation.Receipt, "外 送 费：" + order.freight + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "订单合计：" + order.totalAmount + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "优惠金额：" + order.discountAmount + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "应付金额：" + order.payAmount + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "支付方式：" + POS.Data.Contract.getPayTypeStr(order.payType) + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|4C" + "\u001b|bC" + "应收现金：" + order.cash + "\n");

                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");

                if (mPrinter.CapRecBarCode == true)
                {
                    //Barcode printing
                    //Barcode CODE128 MAX LENGTH IS <=23
                    mPrinter.PrintBarCode(PrinterStation.Receipt,
                        order.orderID,
                        BarCodeSymbology.Code128, 1000,
                    mPrinter.RecLineWidth, PosPrinter.PrinterBarCodeCenter,
                        BarCodeTextPosition.None);
                }
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + "\u001b|4C" + adv + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + storeName + order.storeID + "\n");
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|300uF");
                mPrinter.PrintNormal(PrinterStation.Receipt,
                    "\u001b|cA" + "查询电话:" + order.storeTelephone + "\n");

                //Need UI checkbox set whether cut paper configuration
                mPrinter.PrintNormal(PrinterStation.Receipt, "\u001b|fP");

            }
            catch (PosControlException ex)
            {
                //string msg = "打印机故障, 请重启打印机及计算机(C)";
                //MessageBox.Show(msg); 
                System.IO.File.AppendAllText(LogTxt,
                    "PrintHelper.CreateTicketContent():" + DateTime.Now.ToString() + ex.ToString() + "\r\n");
                throw ex;
            }
        }
    }
}
