using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PointOfService;
using System.Globalization;
//using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;

using System.Drawing;
using System.Collections;

using System.Data;

//using POS.Data;
using System.Threading;
namespace ConsoleApplication1
{
    public class PrinterThread
    {
       // private mainForm mForm;
        //private POS.Data.SQLiteHelper mSQLiteHelper;
        private Thread pThread;
        private Printer printer;
        private PosPrinter m_Printer;
        private bool mStop;
        public PrinterThread()  //
        {
            //mForm = form;
            mStop = false;
        }
        public void begin()  //开始打印
        {
           // mSQLiteHelper = POS.Data.SQLiteHelper.sInstance;
            //printer = new Printer(mSQLiteHelper);
            pThread = new Thread(new ThreadStart(run));
            mStop = false;
            pThread.Start();
            /*
            System.IO.File.AppendAllText(@"C:\POS_Log.txt", 
                DateTime.Now.ToString() + "Printer.cs:begin():" + "\r\n");
             */
        }
        public void run()   //运行打印
        {
            // SQLiteHelper helper = new SQLiteHelper();
            // Printer printer = new Printer();
            //printer = new Printer();
            bool initialized = false;

            while (!mStop)
            {
                if (!initialized)
                {
                    initialized = printer.init(true);
                }
                else
                {
                    m_Printer = printer.getPrinter();

                    //List<POS.Printer.Contract.Order> infoList = mSQLiteHelper.GetPrintedDatas();
                    //printer.print(infoList, 2);
                }
                System.Threading.Thread.Sleep(1000 * 60);
            }
        }
        public void Stop()   //停止打印
        {
            mStop = true;
            if (m_Printer != null)
            {
                m_Printer.Close();
            }
        }
    }
    public class Printer
    {
        //private POS.Data.SQLiteHelper mSQLiteHelper;
        //private mainForm mForm;
        const int MAX_LINE_WIDTHS = 2;
        PosPrinter m_Printer = null;
        bool async = false;
        //public Printer(POS.Data.SQLiteHelper Helper)  //打印
        //{
        //    //mSQLiteHelper = Helper;
        //    //mForm = form;
        //}
        public bool init(bool asasynchronous)   //初始化
        {

            async = asasynchronous;
            //Use a Logical Device Name which has been set on the SetupPOS.
            string strLogicalName = "PosPrinter";

            try
            {
                //Create PosExplorer
                PosExplorer posExplorer = new PosExplorer();

                DeviceInfo deviceInfo = null;

                try
                {
                    deviceInfo = posExplorer.GetDevice(DeviceType.PosPrinter, strLogicalName);
                    m_Printer = (PosPrinter)posExplorer.CreateInstance(deviceInfo);
                }
                catch (Exception)
                {
                    //ChangeButtonStatus(false);
                    /*
                    System.IO.File.AppendAllText(@"C:\POS_Log.txt", "Printer.cs:init():" + 
                        DateTime.Now.ToString() + exception.ToString() + "\r\n");
                     */
                    return false;
                }


                //<<<step10>>>--Start	
                //Register OutputCompleteEvent
                //AddErrorEvent(m_Printer);

                //Register OutputCompleteEvent
                // AddStatusUpdateEvent(m_Printer);


                //Open the device
                m_Printer.Open();

                //Get the exclusive control right for the opened device.
                //Then the device is disable from other application.
                m_Printer.Claim(1000);
                //Enable the device.
                m_Printer.DeviceEnabled = true;

                //Output by the high quality mode
                m_Printer.RecLetterQuality = true;


                // Even if using any printers, 0.01mm unit makes it possible to print neatly.
                m_Printer.MapMode = MapMode.Metric;

            }
            catch (PosControlException ex)
            {
                //ChangeButtonStatus(false);
                /*
                System.IO.File.AppendAllText(@"C:\POS_Log.txt", DateTime.Now.ToString() + 
                    "Printer.cs:init():" + ex.ToString() + "\r\n");
                 */
                System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + ex.ToString() + "\r\n");
                return false;
            }
            //<<<step1>>>--End
            return true;
        }

        public PosPrinter getPrinter()  //获取打印机名字
        {
            return m_Printer;
        }

        /*
        private void ChangeButtonStatus()
        {
           btnPrint.Enabled = false;
        }
        */
        private int getChineseNum(string str) //获取中字数量
        {
            int len = 0;
            char[] c = str.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] >= 0x4e00 && c[i] <= 0x9fbb)
                {
                    len += 1;
                }
            }
            return len;
        }
        private String MakePrintString(int iLineChars, String name, String num, String price)
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
                for (int i = 0; i < iSpaces1; i++)
                {
                    tab1 += " ";
                }
                iSpaces2 = iLineChars - (coord1 + num.Length + price.Length + ext);
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

        public bool print(int printNum)  //打印内容
        {
            bool status = false;
            int mPrintNum = 1;
            //foreach (Contract.Order inf in infoList)
            //{

                mPrintNum = printNum;
                //Initialization
                string iHeader = @"***功夫团膳***";
                string iOrderid = inf.orderID;
                //iOrderid = "TS02000201506060020";
                string iConsumer = "顾客：" + inf.userName;
                string iAddress = "地址：" + inf.userAddress;
                string iTelephone = "电话：" + inf.userTelephone;
                string iRemark = "备注：" + inf.ext;
                string iSalesSlip = "发票: " + inf.invoiceTitle;
                string iSubmitTime = "订单提交时间：" + inf.addTime;
                string iDeliveredTime = "预计送达时间：" + inf.bookTime;

                string iDivisionLine = "************************************************";
                /**************************************************/
                string iItemTitle = @"名称                      数量       原价(元) ";
                /****************************************************/
                string iItemDisTitle = @"优惠名称                  数量       优惠(元)";
                /*****************************************************/

                string iFreight = "外 送 费：" + inf.freight;
                string iTotalAmount = "订单合计：" + inf.totalAmount;
                string iDiscountAmount = "优惠金额：" + inf.discountAmount;
                string iPayAmount = "应付金额：" + inf.payAmount;


                string payTypeStr = "";
                if (inf.payType.Equals(POS.Data.Contract.PAY_TYPE_WX))
                {
                    payTypeStr = POS.Properties.Resources.PAY_TYPE_WX_str;
                }
                else if (inf.payType.Equals(POS.Data.Contract.PAY_TYPE_ALIPAY))
                {
                    payTypeStr = POS.Properties.Resources.PAY_TYPE_ALIPAY_str;
                }
                else if (inf.payType.Equals(POS.Data.Contract.PAY_TYPE_BANK))
                {
                    payTypeStr = POS.Properties.Resources.PAY_TYPE_BANK_str;
                }
                else if (inf.payType.Equals(POS.Data.Contract.PAY_TYPE_BALANCE))
                {
                    payTypeStr = POS.Properties.Resources.PAY_TYPE_BALANCE_str;
                }
                else if (inf.payType.Equals(POS.Data.Contract.PAY_TYPE_CASH))
                {
                    payTypeStr = POS.Properties.Resources.PAY_TYPE_CASH_str;
                }
                else if (inf.payType.Equals(POS.Data.Contract.PAY_TYPE_DEBT))
                {
                    payTypeStr = POS.Properties.Resources.PAY_TYPE_DEBT_str;
                }

                string iPayType = "支付方式：" + payTypeStr;

                string iCash = "应收现金：" + inf.payAmount;

                /***************   订单编码  ************/
                string foot1 = "感谢惠顾功夫团膳";
                string foot2 = "功夫团膳餐厅" + inf.storeID;
                string foot3 = "查询电话:" + inf.storeTelephone;
                int[] RecLineChars = new int[MAX_LINE_WIDTHS] { 0, 0 };
                //When outputting to a printer,a mouse cursor becomes like a hourglass.
                //Cursor.Current = Cursors.WaitCursor;

                for (int i = 0; i < mPrintNum; i++)
                {
                    if (m_Printer.CapRecPresent)
                    {
                        if (async == true)
                        {
                            m_Printer.AsyncMode = true;
                        }

                        try
                        {
                            //for (int i = 0; i < printNum; i++ )
                            //{
                            //<<<step6>>>--Start
                            //Batch processing mode
                            m_Printer.TransactionPrint(PrinterStation.Receipt
                                , PrinterTransactionControl.Transaction);

                            //<<<step3>>>--Start
                            //m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|1B");
                            //<<<step3>>>--End

                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|cA" + "\u001b|bC" + "\u001b|2C"
                                + iHeader + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|cA" + "\u001b|bC"
                                + iOrderid + "\n");

                            m_Printer.PrintNormal(PrinterStation.Receipt, iConsumer + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iAddress + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iTelephone + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iRemark + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iSalesSlip + "\n");


                            //m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|80uF");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iSubmitTime + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iDeliveredTime + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|300uF");

                            m_Printer.PrintNormal(PrinterStation.Receipt, iDivisionLine + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iItemTitle + "\n");
                            //foreach (Contract.Product p in inf.productList)
                            //{
                            //    string printData = MakePrintString(m_Printer.RecLineChars, p.productName, p.productNum, p.totalPrice);
                            //    m_Printer.PrintNormal(PrinterStation.Receipt, printData + "\n");
                            //}
                            //m_Printer.PrintNormal(PrinterStation.Receipt, iDivisionLine + "\n");
                            //m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                            //m_Printer.PrintNormal(PrinterStation.Receipt, iItemDisTitle + "\n");
                            //foreach (Contract.DiscountProduct p in inf.discountProductList)
                            //{
                            //    string printData = MakePrintString(m_Printer.RecLineChars, p.discountName, p.discountNum, p.discountAmount);
                            //    m_Printer.PrintNormal(PrinterStation.Receipt, printData + "\n");
                            //}
                            m_Printer.PrintNormal(PrinterStation.Receipt, iDivisionLine + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");

                            m_Printer.PrintNormal(PrinterStation.Receipt, iFreight + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iTotalAmount + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iDiscountAmount + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iPayAmount + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, iPayType + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|4C" + "\u001b|bC"
                                + iCash + "\n");

                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");

                            if (m_Printer.CapRecBarCode == true)
                            {
                                //Barcode printing
                                //Barcode CODE128 MAX LENGTH IS <=23
                                m_Printer.PrintBarCode(PrinterStation.Receipt,
                                   iOrderid,
                                   BarCodeSymbology.Code128, 1000,
                                m_Printer.RecLineWidth, PosPrinter.PrinterBarCodeCenter,
                                   BarCodeTextPosition.None);
                            }
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|200uF");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|cA" + "\u001b|4C"
                                + foot1 + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|cA"
                                + foot2 + "\n");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|300uF");
                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|cA"
                                + foot3 + "\n");

                            m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|fP");

                            //print all the buffer data. and exit the batch processing mode.
                            m_Printer.TransactionPrint(PrinterStation.Receipt
                                , PrinterTransactionControl.Normal);
                            status = true;

                            //<<<step6>>>--End
                        }
                        // }
                        catch (PosControlException e)
                        {
                            status = false;
                            Console.WriteLine("**********************************");
                            Console.WriteLine(e.ErrorCode);
                            Console.WriteLine("+++++++++++++++++++++++++++++++++++");
                            System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
                        }
                        if (status)
                        {
                            //string suffix = DateTime.Today.ToString(SQLiteHelper.DateSuffixFormat);
                            //mSQLiteHelper.UpdateOrder(iOrderid, POS.Data.Contract.ORDER_STATUS_PRODUCING);
                            //mForm.RefreshDisplay();
                        }
                    }
                    m_Printer.AsyncMode = false;
                }

                //<<<step6>>>--Start
                // When a cursor is back to its default shape, it means the process ends
                //Cursor.Current = Cursors.Default;
                //<<<step6>>>--End
            }
           // return status;
        }
    }

