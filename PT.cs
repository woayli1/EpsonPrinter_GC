using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.PointOfService;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.ComponentModel;
using System.Drawing;
using System.Collections;
using System.Data;
using System.Threading;

namespace ConsoleApplication1
{
    public class PT
    {
        private Thread pThread;
        private Printer printer;
        private PosPrinter m_Printer;
        private bool mStop;
        public void PrinterThread()  //
        {
            mStop = false;
        }
        public void begin()  //开始打印
        {
            pThread = new Thread(new ThreadStart(run));
            mStop = false;
            pThread.Start();

        }
        public void run()   //运行打印
        {
            Printer printer = new Printer();
            printer = new Printer();
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
                }
                //System.Threading.Thread.Sleep(1000 * 60);
                printer.print(1);
                Stop();
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

        const int MAX_LINE_WIDTHS = 2;
        PosPrinter m_Printer = null;
        bool async = false;

        public bool init(bool asasynchronous)   //初始化
        {

            async = asasynchronous;
            string strLogicalName = "PosPrinter";

            try
            {
                 PosExplorer posExplorer = new PosExplorer();

                DeviceInfo deviceInfo = null;

                try
               {
                    deviceInfo = posExplorer.GetDevice(DeviceType.PosPrinter, strLogicalName);
                    m_Printer = (PosPrinter)posExplorer.CreateInstance(deviceInfo);
                }
                catch (Exception)
               {
                    //return false;
                }

                m_Printer.Open();

                m_Printer.Claim(1000);

                m_Printer.DeviceEnabled = true;

                m_Printer.RecLetterQuality = true;

                m_Printer.MapMode = MapMode.Metric;

            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + ex.ToString() + "\r\n");
                return false;
            }

            return true;
        }

        public PosPrinter getPrinter()  //获取打印机名字
        {
            return m_Printer;
        }

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

        public void print(int printNum)  //打印内容
        {
            // bool status = false;
            int mPrintNum = 1;

            mPrintNum = printNum;
            //Initialization
            string iHeader = @"***功夫团膳***";
            string iOrderid = " CC1001 ";
            //iOrderid = "TS02000201506060020";
            string iConsumer = "顾客：" + " 郭灿 ";
            string iAddress = "地址：" + " 三林镇如日商务园 ";
            string iTelephone = "电话：" + " 13312345678 ";
            string iRemark = "备注：" + " 无 ";
            string iSalesSlip = "发票: " + " 无 ";
            string iSubmitTime = "订单提交时间：" + " 2017-07-11 12:01:02";
            string iDeliveredTime = "预计送达时间：" + " 2017-07-11 13：00：00 ";

            string iDivisionLine = "************************************************";
            /**************************************************/
            string iItemTitle = @"名称                      数量       原价(元) ";
            /****************************************************/
            string iItemDisTitle = @"优惠名称                  数量       优惠(元)";
            /*****************************************************/

            string iFreight = "外 送 费：" + " 3元 ";
            string iTotalAmount = "订单合计：" + " 29元 ";
            string iDiscountAmount = "优惠金额：" + " 2元 ";
            string iPayAmount = "应付金额：" + " 27元 ";


            string payTypeStr = " 微信支付 ";

            string iPayType = "支付方式：" + payTypeStr;

            string iCash = "应收现金：" + " 0元 ";

            /***************   订单编码  ************/
            string foot1 = "感谢惠顾功夫团膳";
            string foot2 = "功夫团膳餐厅" + "XX0001";
            string foot3 = "查询电话:" + "15112345678";
            int[] RecLineChars = new int[MAX_LINE_WIDTHS] { 0, 0 };
            //When outputting to a printer,a mouse cursor becomes like a hourglass.
            //Cursor.Current = Cursors.WaitCursor;

            //for (int i = 0; i < mPrintNum; i++)
            //{
            //    //if (m_Printer.CapRecPresent)
            //    //{
            //    //    if (async == true)
            //    //    {
            //    //        m_Printer.AsyncMode = true;
            //    //    }

            //        try
            //        {

            //            //m_Printer.TransactionPrint(PrinterStation.Receipt
            //            //    , PrinterTransactionControl.Transaction);

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


                        m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|80uF");
                        m_Printer.PrintNormal(PrinterStation.Receipt, iSubmitTime + "\n");
                        m_Printer.PrintNormal(PrinterStation.Receipt, iDeliveredTime + "\n");
                        m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|300uF");

                        m_Printer.PrintNormal(PrinterStation.Receipt, iDivisionLine + "\n");
                        m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                        m_Printer.PrintNormal(PrinterStation.Receipt, iItemTitle + "\n");


                        m_Printer.PrintNormal(PrinterStation.Receipt, iDivisionLine + "\n");
                        m_Printer.PrintNormal(PrinterStation.Receipt, "\u001b|100uF");
                        m_Printer.PrintNormal(PrinterStation.Receipt, iItemDisTitle + "\n");

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

            //            if (m_Printer.CapRecBarCode == true)
            //            {
            //                m_Printer.PrintBarCode(PrinterStation.Receipt,
            //                   iOrderid,
            //                   BarCodeSymbology.Code128, 1000,
            //                m_Printer.RecLineWidth, PosPrinter.PrinterBarCodeCenter,
            //                   BarCodeTextPosition.None);
            //            }
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


            //            m_Printer.TransactionPrint(PrinterStation.Receipt
            //                , PrinterTransactionControl.Normal);

            //        }

            //        catch (PosControlException e)
            //        {
            //            Console.WriteLine("**********************************");
            //            Console.WriteLine(e.ErrorCode);
            //            Console.WriteLine("+++++++++++++++++++++++++++++++++++");
            //            System.IO.File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString() + ": " + e.ToString() + "\r\n");
            //        }

            //    }
            // m_Printer.AsyncMode = false;
        }
    }
}
//}

